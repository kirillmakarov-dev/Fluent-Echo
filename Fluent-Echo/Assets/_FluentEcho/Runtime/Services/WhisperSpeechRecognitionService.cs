using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Stopwatch = System.Diagnostics.Stopwatch;
using FluentEcho.Data;
using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace FluentEcho.Services
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(WhisperManager), typeof(MicrophoneRecord))]
    public sealed class WhisperSpeechRecognitionService : MonoBehaviour, ISpeechRecognitionService
    {
        [SerializeField] private WhisperManager whisperManager;
        [SerializeField] private MicrophoneRecord microphone;
        [SerializeField] private WhisperSettingsSO whisperSettings;
        [SerializeField] private string legacyModelPath = "Whisper/ggml-tiny.en.bin";
        [SerializeField] private float legacyStreamingStepSeconds = 1.25f;
        [SerializeField] private float legacyMicrophoneChunkSeconds = 0.25f;
        [SerializeField] private float legacyWarmupSeconds = 1.25f;
        [SerializeField] private float stopWatchdogSeconds = 6f;

        private WhisperStream stream;
        private Task prepareTask;
        private Coroutine stopWatchdog;
        private bool suppressStopEvent;
        private bool finishRequested;
        private bool warmupCompleted;
        private float lastPrepareSeconds;
        private float lastWarmupSeconds;
        private int prepareGeneration;

        public event Action<string> TranscriptUpdated;
        public event Action AnalysisStarted;
        public event Action ListeningStopped;
        public event Action<string> Failed;
        public event Action<string> StatusChanged;

        public bool IsListening { get; private set; }
        public bool IsReady => whisperManager != null && whisperManager.IsLoaded && stream != null;
        public WhisperQualityProfile CurrentQualityProfile =>
            whisperSettings != null ? whisperSettings.GetEffectiveProfile() : WhisperQualityProfile.Fast;

        private void Awake()
        {
            whisperManager ??= GetComponent<WhisperManager>();
            microphone ??= GetComponent<MicrophoneRecord>();
            ApplyConfiguration();
        }

        public void Configure(SpeechExerciseSO exercise)
        {
            if (whisperManager != null && exercise != null)
                whisperManager.initialPrompt = exercise.GetRecognitionPrompt();

            if (microphone != null && exercise != null)
            {
                microphone.vadStop = true;
                microphone.vadStopTime = exercise.SilenceTimeoutSeconds;
                microphone.echo = false;
                microphone.chunksLengthSec = ResolveMicrophoneChunkSeconds();
            }
        }

        public void SetQualityProfile(WhisperQualityProfile profile)
        {
            if (whisperSettings == null)
                return;

            whisperSettings.SetRuntimeProfile(profile);
        }

        public void Prepare()
        {
            if (whisperManager == null || microphone == null)
                return;

            if (prepareTask is { IsCompleted: false })
                return;

            int generation = ++prepareGeneration;
            warmupCompleted = false;
            lastPrepareSeconds = 0f;
            lastWarmupSeconds = 0f;
            prepareTask = PrepareInternalAsync(generation);
        }

        public async void StartListening()
        {
            try
            {
                if (IsListening)
                    return;

                if (Microphone.devices.Length == 0)
                {
                    RaiseError("No microphone device was detected.");
                    StatusChanged?.Invoke("No microphone device was detected.");
                    ListeningStopped?.Invoke();
                    return;
                }

                if (!await EnsurePreparedAsync())
                {
                    ListeningStopped?.Invoke();
                    return;
                }

                if (stream == null)
                {
                    RaiseError("Whisper stream was not ready.");
                    StatusChanged?.Invoke("Whisper stream was not ready.");
                    ListeningStopped?.Invoke();
                    return;
                }

                suppressStopEvent = false;
                finishRequested = false;
                stream.OnResultUpdated -= HandleTranscript;
                stream.OnResultUpdated += HandleTranscript;
                stream.OnStreamFinished -= HandleStreamFinished;
                stream.OnStreamFinished += HandleStreamFinished;

                IsListening = true;
                stream.StartStream();
                microphone.OnRecordStop -= HandleRecordStop;
                microphone.OnRecordStop += HandleRecordStop;
                if (!microphone.StartRecord())
                {
                    RaiseError("The microphone could not start. Check operating-system permission.");
                    StatusChanged?.Invoke("The microphone could not start. Check operating-system permission.");
                    stream.StopStream();
                    CompleteStop();
                    return;
                }

                if (!microphone.IsRecording)
                {
                    RaiseError("The microphone could not start. Check operating-system permission.");
                    StatusChanged?.Invoke("The microphone could not start. Check operating-system permission.");
                    stream.StopStream();
                    CompleteStop();
                }
            }
            catch (Exception exception)
            {
                RaiseError($"Listening failed: {exception.Message}");
                StatusChanged?.Invoke("Listening failed.");
                try
                {
                    if (stream != null)
                        stream.StopStream();
                }
                catch
                {
                    // Best effort cleanup only.
                }

                CompleteStop();
            }
        }

        public void StopListening()
        {
            if (!IsListening)
                return;

            RequestFinish();
        }

        public void Cancel()
        {
            prepareGeneration++;

            if (!IsListening)
                return;

            suppressStopEvent = true;
            RequestFinish();
        }

        private async Task<bool> EnsurePreparedAsync()
        {
            if (prepareTask is { IsCompleted: false })
                await prepareTask;

            if (IsReady)
                return true;

            int generation = ++prepareGeneration;
            prepareTask = PrepareInternalAsync(generation);
            await prepareTask;
            return IsReady;
        }

        private async Task PrepareInternalAsync(int generation)
        {
            Stopwatch prepareStopwatch = Stopwatch.StartNew();
            ApplyConfiguration();
            string profileLabel = CurrentQualityProfile.ToString().ToUpperInvariant();

            if (IsPrepareStale(generation))
                return;

            StatusChanged?.Invoke($"Loading speech model ({profileLabel})...");

            string modelPath = ResolveModelPath();
            string fullModelPath = ResolveFullModelPath(modelPath);
            if (!File.Exists(fullModelPath))
            {
                RaiseError($"Whisper model is missing: {fullModelPath}");
                StatusChanged?.Invoke("Speech model is missing.");
                return;
            }

            if (IsPrepareStale(generation))
                return;

            if (!whisperManager.IsLoaded)
            {
                try
                {
                    await whisperManager.InitModel();
                }
                catch (Exception exception)
                {
                    RaiseError($"Whisper model failed to load: {exception.Message}");
                    StatusChanged?.Invoke("Speech model failed to load.");
                    return;
                }
            }

            if (IsPrepareStale(generation))
                return;

            if (!whisperManager.IsLoaded)
            {
                RaiseError($"Whisper could not load model: {fullModelPath}");
                StatusChanged?.Invoke("The selected speech model could not load.");
                return;
            }

            lastPrepareSeconds = (float) prepareStopwatch.Elapsed.TotalSeconds;

            bool warmupSucceeded = true;
            if (!warmupCompleted && ResolveWarmupEnabled())
                warmupSucceeded = await WarmUpAsync(profileLabel);

            if (IsPrepareStale(generation))
                return;

            if (stream == null && !IsListening)
                stream = await whisperManager.CreateStream(microphone);

            if (IsPrepareStale(generation))
                return;

            if (stream == null)
            {
                RaiseError("Whisper stream could not be created.");
                StatusChanged?.Invoke("Speech analysis could not start.");
                return;
            }

            string readyMessage = warmupSucceeded
                ? $"Whisper ready ({profileLabel}, load {lastPrepareSeconds:0.0}s"
                : $"Whisper ready ({profileLabel}, load {lastPrepareSeconds:0.0}s, warm-up failed";

            if (warmupSucceeded && lastWarmupSeconds > 0f)
                readyMessage += $", warm-up {lastWarmupSeconds:0.0}s";

            readyMessage += ").";
            StatusChanged?.Invoke(readyMessage);
        }

        private void ApplyConfiguration()
        {
            WhisperSettingsSO settings = whisperSettings;
            if (settings == null)
            {
                if (whisperManager != null && !whisperManager.IsLoaded && !whisperManager.IsLoading)
                {
                    whisperManager.IsModelPathInStreamingAssets = true;
                    whisperManager.ModelPath = legacyModelPath;
                }

                if (whisperManager != null)
                    SetWhisperManagerField(whisperManager, "useGpu", true);

                if (whisperManager != null)
                    SetWhisperManagerField(whisperManager, "flashAttention", true);

                if (whisperManager != null)
                {
                    whisperManager.language = "en";
                    whisperManager.stepSec = Mathf.Max(1.25f, legacyStreamingStepSeconds);
                    whisperManager.keepSec = 0.2f;
                    whisperManager.lengthSec = 10f;
                    whisperManager.updatePrompt = true;
                    whisperManager.dropOldBuffer = false;
                    whisperManager.useVad = true;
                }

                if (microphone != null)
                {
                    microphone.frequency = 16000;
                    microphone.useVad = true;
                    microphone.vadStop = true;
                    microphone.echo = false;
                    microphone.chunksLengthSec = legacyMicrophoneChunkSeconds;
                }

                return;
            }

            if (whisperManager != null && !whisperManager.IsLoaded && !whisperManager.IsLoading)
            {
                whisperManager.IsModelPathInStreamingAssets = true;
                whisperManager.ModelPath = ResolveModelPath();
                whisperManager.language = settings.Language;
                // Whisper rejects audio shorter than one second. Keep a small safety margin
                // because microphone chunk boundaries are not sample-perfect.
                whisperManager.stepSec = Mathf.Max(1.25f, settings.StreamingStepSeconds);
                whisperManager.keepSec = settings.KeepSeconds;
                whisperManager.lengthSec = settings.StreamLengthSeconds;
                whisperManager.updatePrompt = settings.UpdatePrompt;
                whisperManager.dropOldBuffer = settings.DropOldBuffer;
                whisperManager.useVad = settings.UseVad;

                // The Vulkan GPU path in the Whisper package has been crashing the
                // Unity Editor on this prototype machine. Keep the editor on the CPU
                // backend for stability until we explicitly validate a safe GPU path.
                bool useGpu = settings.UseGpu && !Application.isEditor;
                bool flashAttention = settings.FlashAttention && useGpu;
                SetWhisperManagerField(whisperManager, "useGpu", useGpu);
                SetWhisperManagerField(whisperManager, "flashAttention", flashAttention);
            }

            if (microphone != null)
            {
                microphone.frequency = 16000;
                microphone.useVad = settings.UseVad;
                microphone.vadStop = true;
                microphone.echo = false;
                microphone.chunksLengthSec = settings.MicrophoneChunkSeconds;
            }
        }

        private async Task<bool> WarmUpAsync(string profileLabel)
        {
            float warmupSeconds = ResolveWarmupSeconds();
            if (warmupSeconds <= 0f)
            {
                warmupCompleted = true;
                return true;
            }

            if (whisperManager == null || !whisperManager.IsLoaded)
                return false;

            StatusChanged?.Invoke($"Warming up speech model ({profileLabel})...");
            Stopwatch warmupStopwatch = Stopwatch.StartNew();

            int sampleRate = microphone != null ? microphone.frequency : 16000;
            int channels = 1;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * warmupSeconds) * channels);
            float[] silence = new float[sampleCount];
            AudioClip warmupClip = AudioClip.Create("FluentEchoWarmup", sampleCount, channels, sampleRate, false);
            warmupClip.SetData(silence, 0);

            try
            {
                await whisperManager.GetTextAsync(warmupClip);
                warmupCompleted = true;
                lastWarmupSeconds = (float) warmupStopwatch.Elapsed.TotalSeconds;
                return true;
            }
            catch (Exception exception)
            {
                RaiseError($"Whisper warm-up failed: {exception.Message}");
                StatusChanged?.Invoke("Speech warm-up failed.");
                return false;
            }
            finally
            {
                if (warmupClip != null)
                    Destroy(warmupClip);
            }
        }

        private string ResolveModelPath()
        {
            if (whisperSettings != null && whisperSettings.TryResolveModelPath(out string resolvedPath))
                return resolvedPath;

            return legacyModelPath;
        }

        private float ResolveMicrophoneChunkSeconds() =>
            whisperSettings != null ? whisperSettings.MicrophoneChunkSeconds : legacyMicrophoneChunkSeconds;

        private float ResolveWarmupSeconds() =>
            whisperSettings != null ? whisperSettings.WarmupSeconds : legacyWarmupSeconds;

        private bool ResolveWarmupEnabled() =>
            whisperSettings == null || whisperSettings.WarmUpOnPrepare;

        private string ResolveFullModelPath(string modelPath) =>
            Path.Combine(Application.streamingAssetsPath, modelPath);

        private static void SetWhisperManagerField(WhisperManager manager, string fieldName, object value)
        {
            FieldInfo field = typeof(WhisperManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                return;

            field.SetValue(manager, value);
        }

        private void HandleTranscript(string transcript)
        {
            string sanitizedTranscript = SanitizeTranscript(transcript);
            if (!string.IsNullOrWhiteSpace(sanitizedTranscript))
                TranscriptUpdated?.Invoke(sanitizedTranscript);
        }

        private void HandleRecordStop(AudioChunk chunk)
        {
            if (!IsListening)
                return;

            AnalysisStarted?.Invoke();
            EnsureStreamFinishedSubscription();
            finishRequested = true;
            StartStopWatchdog();
        }

        private void RequestFinish()
        {
            finishRequested = true;
            StartStopWatchdog();

            if (stream == null)
            {
                CompleteStop();
                return;
            }

            EnsureStreamFinishedSubscription();

            if (microphone != null && microphone.IsRecording)
                microphone.StopRecord();
            else
                StopStreamSafely();
        }

        private void HandleStreamFinished(string finalTranscript)
        {
            string sanitizedTranscript = SanitizeTranscript(finalTranscript);
            if (!string.IsNullOrWhiteSpace(sanitizedTranscript))
                TranscriptUpdated?.Invoke(sanitizedTranscript);

            CompleteStop();
        }

        private void CompleteStop()
        {
            if (!IsListening && !finishRequested)
                return;

            StopStopWatchdog();
            Unsubscribe();
            IsListening = false;
            finishRequested = false;

            if (suppressStopEvent)
            {
                suppressStopEvent = false;
                return;
            }

            ListeningStopped?.Invoke();
        }

        private void RaiseError(string message)
        {
            Debug.LogError($"[FluentEcho.Whisper] {message}", this);
            Failed?.Invoke(message);
        }

        private bool IsPrepareStale(int generation) => generation != prepareGeneration;

        private void EnsureStreamFinishedSubscription()
        {
            if (stream == null)
                return;

            stream.OnStreamFinished -= HandleStreamFinished;
            stream.OnStreamFinished += HandleStreamFinished;
        }

        private void StopStreamSafely()
        {
            if (stream == null)
                return;

            try
            {
                stream.StopStream();
            }
            catch (Exception exception)
            {
                RaiseError($"Whisper stream failed to stop: {exception.Message}");
                CompleteStop();
            }
        }

        private void StartStopWatchdog()
        {
            if (stopWatchdog != null || !isActiveAndEnabled)
                return;

            stopWatchdog = StartCoroutine(CompleteStopIfStreamStalls());
        }

        private void StopStopWatchdog()
        {
            if (stopWatchdog == null)
                return;

            StopCoroutine(stopWatchdog);
            stopWatchdog = null;
        }

        private IEnumerator CompleteStopIfStreamStalls()
        {
            yield return new WaitForSeconds(Mathf.Max(1f, stopWatchdogSeconds));
            stopWatchdog = null;

            if (!IsListening)
                yield break;

            StatusChanged?.Invoke("No clear speech was recognized. You can try again.");
            CompleteStop();
        }

        private static string SanitizeTranscript(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return string.Empty;

            string sanitized = transcript
                .Replace("[BLANK_AUDIO]", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("(BLANK_AUDIO)", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            return string.Equals(sanitized, "blank audio", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : sanitized;
        }

        private void Unsubscribe()
        {
            if (stream != null)
            {
                stream.OnResultUpdated -= HandleTranscript;
                stream.OnStreamFinished -= HandleStreamFinished;
            }

            if (microphone != null)
                microphone.OnRecordStop -= HandleRecordStop;
        }

        private void OnDestroy()
        {
            if (IsListening)
                Cancel();
            Unsubscribe();
        }
    }
}
