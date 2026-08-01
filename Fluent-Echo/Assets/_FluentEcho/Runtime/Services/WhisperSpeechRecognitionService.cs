using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float listeningTimeoutSeconds = 25f;

        private WhisperStream stream;
        private Task prepareTask;
        private Coroutine stopWatchdog;
        private Coroutine listeningWatchdog;
        private readonly Queue<Action> mainThreadActions = new();
        private bool suppressStopEvent;
        private bool finishRequested;
        private bool warmupCompleted;
        private float lastPrepareSeconds;
        private float lastWarmupSeconds;
        private int prepareGeneration;
        private int startGeneration;
        private int listeningGeneration;
        private int activeListeningGeneration;
        private string lastTranscript = string.Empty;
        private OnStreamResultUpdatedDelegate transcriptUpdatedHandler;
        private OnStreamFinishedDelegate streamFinishedHandler;
        private OnRecordStopDelegate recordStopHandler;

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

        private void Update()
        {
            while (true)
            {
                Action action;
                lock (mainThreadActions)
                {
                    if (mainThreadActions.Count == 0)
                        return;

                    action = mainThreadActions.Dequeue();
                }

                try
                {
                    action?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        public void Configure(SpeechExerciseSO exercise)
        {
            if (whisperManager != null)
            {
                whisperManager.initialPrompt = BuildRecognitionPrompt(exercise);
                RefreshWhisperManagerParams();
            }

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

        private static string BuildRecognitionPrompt(SpeechExerciseSO exercise)
        {
            if (exercise == null)
                return string.Empty;

            string prompt = exercise.Prompt?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
                return "Practice English pronunciation.";

            int colonIndex = prompt.IndexOf(':');
            if (colonIndex >= 0)
                prompt = prompt.Substring(0, colonIndex);

            prompt = prompt.Trim().TrimEnd('.', '!', '?', ':');
            if (string.IsNullOrWhiteSpace(prompt))
                return "Practice English pronunciation.";

            return $"{prompt}. Practice English pronunciation.";
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

                int generation = ++startGeneration;
                IsListening = true;
                finishRequested = false;
                suppressStopEvent = false;
                lastTranscript = string.Empty;

                if (Microphone.devices.Length == 0)
                {
                    RaiseError("No microphone device was detected.");
                    RaiseStatusChanged("No microphone device was detected.");
                    CompleteStop();
                    return;
                }

                if (!await EnsurePreparedAsync()
                    || !IsCurrentStartGeneration(generation)
                    || !IsListening
                    || finishRequested)
                {
                    CompleteStop();
                    return;
                }

                if (stream == null)
                {
                    RaiseError("Whisper stream was not ready.");
                    RaiseStatusChanged("Whisper stream was not ready.");
                    CompleteStop();
                    return;
                }

                SubscribeCurrentSessionCallbacks();
                finishRequested = false;

                StartListeningWatchdog();
                stream.StartStream();
                if (microphone != null)
                    microphone.OnRecordStop += recordStopHandler;
                if (!microphone.StartRecord())
                {
                    RaiseError("The microphone could not start. Check operating-system permission.");
                    RaiseStatusChanged("The microphone could not start. Check operating-system permission.");
                    stream.StopStream();
                    CompleteStop();
                    return;
                }

                if (!microphone.IsRecording)
                {
                    RaiseError("The microphone could not start. Check operating-system permission.");
                    RaiseStatusChanged("The microphone could not start. Check operating-system permission.");
                    stream.StopStream();
                    CompleteStop();
                }
            }
            catch (Exception exception)
            {
                RaiseError($"Listening failed: {exception.Message}");
                RaiseStatusChanged("Listening failed.");
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
            startGeneration++;

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

            RaiseStatusChanged($"Loading speech model ({profileLabel})...");

            string modelPath = ResolveModelPath();
            string fullModelPath = ResolveFullModelPath(modelPath);
            if (!File.Exists(fullModelPath))
            {
                RaiseError($"Whisper model is missing: {fullModelPath}");
                RaiseStatusChanged("Speech model is missing.");
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
                    RaiseStatusChanged("Speech model failed to load.");
                    return;
                }
            }

            if (IsPrepareStale(generation))
                return;

            if (!whisperManager.IsLoaded)
            {
                RaiseError($"Whisper could not load model: {fullModelPath}");
                RaiseStatusChanged("The selected speech model could not load.");
                return;
            }

            lastPrepareSeconds = (float) prepareStopwatch.Elapsed.TotalSeconds;

            bool warmupSucceeded = true;
            if (!warmupCompleted && ResolveWarmupEnabled())
                warmupSucceeded = await WarmUpAsync(profileLabel);

            if (IsPrepareStale(generation))
                return;

            if (stream == null)
            {
                RefreshWhisperManagerParams();
                stream = await whisperManager.CreateStream(microphone);
            }

            if (IsPrepareStale(generation))
                return;

            if (stream == null)
            {
                RaiseError("Whisper stream could not be created.");
                RaiseStatusChanged("Speech analysis could not start.");
                return;
            }

            string readyMessage = warmupSucceeded
                ? $"Whisper ready ({profileLabel}, load {lastPrepareSeconds:0.0}s"
                : $"Whisper ready ({profileLabel}, load {lastPrepareSeconds:0.0}s, warm-up failed";

            if (warmupSucceeded && lastWarmupSeconds > 0f)
                readyMessage += $", warm-up {lastWarmupSeconds:0.0}s";

            readyMessage += ").";
            RaiseStatusChanged(readyMessage);
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
                    whisperManager.noContext = true;
                    whisperManager.singleSegment = false;
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
            }

            if (whisperManager != null)
            {
                whisperManager.noContext = true;
                whisperManager.singleSegment = false;
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
                RefreshWhisperManagerParams();
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

            RaiseStatusChanged($"Warming up speech model ({profileLabel})...");
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
                RaiseStatusChanged("Speech warm-up failed.");
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

        private void RefreshWhisperManagerParams()
        {
            if (whisperManager == null || !whisperManager.IsLoaded)
                return;

            MethodInfo method = typeof(WhisperManager).GetMethod(
                "UpdateParams",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(whisperManager, null);
        }

        private void HandleTranscript(int generation, string transcript)
        {
            if (!IsCurrentListeningGeneration(generation))
                return;

            string sanitizedTranscript = SanitizeTranscript(transcript);
            if (!string.IsNullOrWhiteSpace(sanitizedTranscript))
                RaiseTranscriptUpdated(sanitizedTranscript);
        }

        private void HandleRecordStop(int generation, AudioChunk chunk)
        {
            if (!IsCurrentListeningGeneration(generation))
                return;

            if (!IsListening)
                return;

            RaiseAnalysisStarted();
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

        private void HandleStreamFinished(int generation, string finalTranscript)
        {
            if (!IsCurrentListeningGeneration(generation))
                return;

            string sanitizedTranscript = SanitizeTranscript(finalTranscript);
            if (!string.IsNullOrWhiteSpace(sanitizedTranscript))
                RaiseTranscriptUpdated(sanitizedTranscript);

            CompleteStop();
        }

        private void CompleteStop()
        {
            if (!IsListening && !finishRequested)
                return;

            StopStopWatchdog();
            StopListeningWatchdog();
            Unsubscribe();
            stream = null;
            IsListening = false;
            finishRequested = false;
            activeListeningGeneration = 0;

            if (suppressStopEvent)
            {
                suppressStopEvent = false;
                return;
            }

            if (!string.IsNullOrWhiteSpace(lastTranscript))
                RaiseTranscriptUpdated(lastTranscript);
            RaiseListeningStopped();
        }

        private void RaiseTranscriptUpdated(string transcript)
        {
            string sanitizedTranscript = SanitizeTranscript(transcript);
            if (string.IsNullOrWhiteSpace(sanitizedTranscript))
                return;

            lastTranscript = sanitizedTranscript;
            RunOnMainThread(() => TranscriptUpdated?.Invoke(sanitizedTranscript));
        }

        private void RaiseAnalysisStarted()
        {
            RunOnMainThread(() => AnalysisStarted?.Invoke());
        }

        private void RaiseListeningStopped()
        {
            RunOnMainThread(() => ListeningStopped?.Invoke());
        }

        private void RaiseStatusChanged(string message)
        {
            RunOnMainThread(() => StatusChanged?.Invoke(message));
        }

        private void RunOnMainThread(Action action)
        {
            if (action == null)
                return;

            lock (mainThreadActions)
                mainThreadActions.Enqueue(action);
        }

        private void RaiseError(string message)
        {
            Debug.LogError($"[FluentEcho.Whisper] {message}", this);
            RunOnMainThread(() => Failed?.Invoke(message));
        }

        private bool IsPrepareStale(int generation) => generation != prepareGeneration;

        private bool IsCurrentStartGeneration(int generation) =>
            generation != 0 && generation == startGeneration;

        private void EnsureStreamFinishedSubscription()
        {
            if (stream == null || streamFinishedHandler == null)
                return;

            stream.OnStreamFinished -= streamFinishedHandler;
            stream.OnStreamFinished += streamFinishedHandler;
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

        private void StartListeningWatchdog()
        {
            if (listeningWatchdog != null || !isActiveAndEnabled)
                return;

            listeningWatchdog = StartCoroutine(CompleteListenIfStalls());
        }

        private void StopListeningWatchdog()
        {
            if (listeningWatchdog == null)
                return;

            StopCoroutine(listeningWatchdog);
            listeningWatchdog = null;
        }

        private IEnumerator CompleteListenIfStalls()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(5f, listeningTimeoutSeconds));
            listeningWatchdog = null;

            if (!IsListening)
                yield break;

            RaiseError("Listening timed out.");
            RaiseStatusChanged("Listening timed out.");
            CompleteStop();
        }

        private IEnumerator CompleteStopIfStreamStalls()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(1f, stopWatchdogSeconds));
            stopWatchdog = null;

            if (!IsListening)
                yield break;

            RaiseStatusChanged("No clear speech was recognized. You can try again.");
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
                if (transcriptUpdatedHandler != null)
                    stream.OnResultUpdated -= transcriptUpdatedHandler;

                if (streamFinishedHandler != null)
                    stream.OnStreamFinished -= streamFinishedHandler;
            }

            if (microphone != null && recordStopHandler != null)
                microphone.OnRecordStop -= recordStopHandler;

            transcriptUpdatedHandler = null;
            streamFinishedHandler = null;
            recordStopHandler = null;
        }

        private void SubscribeCurrentSessionCallbacks()
        {
            if (stream == null)
                return;

            int generation = ++listeningGeneration;
            activeListeningGeneration = generation;

            transcriptUpdatedHandler = transcript => HandleTranscript(generation, transcript);
            streamFinishedHandler = finalTranscript => HandleStreamFinished(generation, finalTranscript);
            recordStopHandler = chunk => HandleRecordStop(generation, chunk);

            stream.OnResultUpdated += transcriptUpdatedHandler;
            stream.OnStreamFinished += streamFinishedHandler;
        }

        private bool IsCurrentListeningGeneration(int generation) =>
            generation != 0 && generation == activeListeningGeneration;

        private void OnDestroy()
        {
            if (IsListening)
                Cancel();
            Unsubscribe();
        }
    }
}
