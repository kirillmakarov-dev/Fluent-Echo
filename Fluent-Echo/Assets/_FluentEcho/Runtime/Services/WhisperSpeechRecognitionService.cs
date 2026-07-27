using System;
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

        private WhisperStream stream;
        private Task prepareTask;
        private bool suppressStopEvent;
        private bool warmupCompleted;
        private float lastPrepareSeconds;
        private float lastWarmupSeconds;

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

            warmupCompleted = false;
            lastPrepareSeconds = 0f;
            lastWarmupSeconds = 0f;
            prepareTask = PrepareInternalAsync();
        }

        public async void StartListening()
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

            suppressStopEvent = false;
            stream.OnResultUpdated -= HandleTranscript;
            stream.OnResultUpdated += HandleTranscript;
            stream.OnStreamFinished -= HandleStreamFinished;
            stream.OnStreamFinished += HandleStreamFinished;

            IsListening = true;
            stream.StartStream();
            microphone.OnRecordStop -= HandleRecordStop;
            microphone.OnRecordStop += HandleRecordStop;
            microphone.StartRecord();

            if (!microphone.IsRecording)
            {
                RaiseError("The microphone could not start. Check operating-system permission.");
                stream.StopStream();
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

            prepareTask = PrepareInternalAsync();
            await prepareTask;
            return IsReady;
        }

        private async Task PrepareInternalAsync()
        {
            Stopwatch prepareStopwatch = Stopwatch.StartNew();
            ApplyConfiguration();
            string profileLabel = CurrentQualityProfile.ToString().ToUpperInvariant();
            StatusChanged?.Invoke($"Loading speech engine ({profileLabel})...");

            string modelPath = ResolveModelPath();
            string fullModelPath = ResolveFullModelPath(modelPath);
            if (!File.Exists(fullModelPath))
            {
                RaiseError($"Whisper model is missing: {fullModelPath}");
                StatusChanged?.Invoke("Whisper model is missing.");
                return;
            }

            if (!whisperManager.IsLoaded)
            {
                try
                {
                    await whisperManager.InitModel();
                }
                catch (Exception exception)
                {
                    RaiseError($"Whisper model failed to load: {exception.Message}");
                    StatusChanged?.Invoke("Whisper model failed to load.");
                    return;
                }
            }

            if (!whisperManager.IsLoaded)
            {
                RaiseError($"Whisper could not load model: {fullModelPath}");
                StatusChanged?.Invoke("Whisper could not load the selected model.");
                return;
            }

            lastPrepareSeconds = (float) prepareStopwatch.Elapsed.TotalSeconds;

            bool warmupSucceeded = true;
            if (!warmupCompleted && ResolveWarmupEnabled())
                warmupSucceeded = await WarmUpAsync(profileLabel);

            if (stream == null && !IsListening)
                stream = await whisperManager.CreateStream(microphone);

            if (stream == null)
            {
                RaiseError("Whisper stream could not be created.");
                StatusChanged?.Invoke("Whisper stream could not be created.");
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
                SetWhisperManagerField(whisperManager, "useGpu", settings.UseGpu);
                SetWhisperManagerField(whisperManager, "flashAttention", settings.FlashAttention);
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

            StatusChanged?.Invoke($"Warming up Whisper ({profileLabel})...");
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
                StatusChanged?.Invoke("Whisper warm-up failed.");
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
            if (!string.IsNullOrWhiteSpace(transcript))
                TranscriptUpdated?.Invoke(transcript);
        }

        private void HandleRecordStop(AudioChunk chunk)
        {
            if (!IsListening)
                return;

            AnalysisStarted?.Invoke();
            stream.OnStreamFinished -= HandleStreamFinished;
            stream.OnStreamFinished += HandleStreamFinished;
        }

        private void RequestFinish()
        {
            if (stream == null)
            {
                CompleteStop();
                return;
            }

            stream.OnStreamFinished -= HandleStreamFinished;
            stream.OnStreamFinished += HandleStreamFinished;

            if (microphone != null && microphone.IsRecording)
                microphone.StopRecord();
            else
                stream.StopStream();
        }

        private void HandleStreamFinished(string finalTranscript)
        {
            if (!string.IsNullOrWhiteSpace(finalTranscript))
                TranscriptUpdated?.Invoke(finalTranscript);

            CompleteStop();
        }

        private void CompleteStop()
        {
            Unsubscribe();
            IsListening = false;

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
