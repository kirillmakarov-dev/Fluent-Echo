using System;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Services;
using UnityEngine;

namespace FluentEcho.Presentation
{
    public sealed class FluentEchoPresenter : IDisposable
    {
        private readonly SpeechExerciseCatalogSO exerciseCatalog;
        private readonly IFluentEchoView view;
        private readonly ISpeechRecognitionService realService;
        private readonly ISpeechRecognitionService mockService;
        private readonly IPronunciationScoringService scoringService = new HeuristicPronunciationScoringService();
        private readonly SpeechAnswerMatcher matcher = new();
        private readonly SpeechSession session = new();
        private readonly Action<AudioClip> playReference;

        private ISpeechRecognitionService activeService;
        private LessonProgressState progress;
        private SpeechExerciseSO currentExercise;
        private int currentExerciseIndex;
        private bool useMock;
        private bool success;
        private float attemptStartedAt = -1f;
        private PronunciationScoreResult lastPronunciationScore = PronunciationScoreResult.Unavailable;

        public FluentEchoPresenter(
            SpeechExerciseSO exercise,
            SpeechExerciseCatalogSO exerciseCatalog,
            IFluentEchoView view,
            ISpeechRecognitionService realService,
            ISpeechRecognitionService mockService,
            bool useMockByDefault,
            Action<AudioClip> playReference)
        {
            currentExercise = exercise;
            this.exerciseCatalog = exerciseCatalog;
            this.view = view;
            this.realService = realService;
            this.mockService = mockService;
            useMock = useMockByDefault;
            this.playReference = playReference;
            currentExerciseIndex = ResolveExerciseIndex(exercise);
        }

        public void Initialize()
        {
            EnsureExercise();
            BindView();
            LoadCurrentExercise();
            view.MicPressed += HandleMicPressed;
            view.DemoPressed += HandleDemoPressed;
            view.RetryPressed += HandleRetry;
            view.ListenPressed += HandleListen;
            view.PreviousPressed += HandlePreviousExercise;
            view.NextPressed += HandleNextExercise;
            view.MockModeChanged += HandleMockModeChanged;

            SelectService(useMock);
            ResetView();
            activeService.Prepare();
        }

        public void Dispose()
        {
            UnbindService();
            realService?.Cancel();
            mockService?.Cancel();

            view.MicPressed -= HandleMicPressed;
            view.DemoPressed -= HandleDemoPressed;
            view.RetryPressed -= HandleRetry;
            view.ListenPressed -= HandleListen;
            view.PreviousPressed -= HandlePreviousExercise;
            view.NextPressed -= HandleNextExercise;
            view.MockModeChanged -= HandleMockModeChanged;
        }

        private void HandleMicPressed()
        {
            if (success || session.Phase == SpeechSessionPhase.Cancelling)
                return;

            if (activeService.IsListening)
            {
                activeService.StopListening();
                return;
            }

            if (IsInteractionLocked())
            {
                if (session.Phase == SpeechSessionPhase.Preparing)
                    view.SetStatus("Loading speech engine...");
                return;
            }

            session.Reset();
            view.SetTranscript(string.Empty);
            view.SetWordMatches(new bool[currentExercise.GetDisplayWords().Length]);
            view.SetSuccess(false);
            view.SetStatus(useMock
                ? "Running deterministic demo..."
                : "Listening... Transcription appears in short local-processing chunks.");
            view.SetListening(true);
            session.BeginListening();
            attemptStartedAt = Time.realtimeSinceStartup;
            activeService.StartListening();
        }

        private void HandleDemoPressed()
        {
            if (IsInteractionLocked())
                return;

            if (!useMock)
            {
                useMock = true;
                SelectService(true);
                ResetView();
                activeService.Prepare();
            }

            HandleMicPressed();
        }

        private void HandleRetry()
        {
            CancelCurrentService("Stopping current attempt...");
            success = false;
            session.Reset();
            ClearAttemptState();
            ResetView();
        }

        private void HandleListen()
        {
            if (IsInteractionLocked())
                return;

            if (currentExercise.ReferenceAudio != null)
                playReference?.Invoke(currentExercise.ReferenceAudio);
        }

        private void HandleMockModeChanged(bool value)
        {
            if (IsInteractionLocked())
            {
                view.SetMode(useMock);
                return;
            }

            if (useMock == value)
                return;

            useMock = value;
            SelectService(useMock);
            ResetView();
            activeService.Prepare();
        }

        private void HandlePreviousExercise()
        {
            if (IsInteractionLocked())
                return;

            SwitchExercise(currentExerciseIndex - 1);
        }

        private void HandleNextExercise()
        {
            if (IsInteractionLocked())
                return;

            SwitchExercise(currentExerciseIndex + 1);
        }

        private void SwitchExercise(int requestedIndex)
        {
            if (exerciseCatalog == null || exerciseCatalog.Count == 0)
                return;

            int clampedIndex = Mathf.Clamp(requestedIndex, 0, exerciseCatalog.Count - 1);
            if (clampedIndex == currentExerciseIndex)
                return;

            CancelCurrentService("Switching lesson...");
            ClearAttemptState();
            currentExerciseIndex = clampedIndex;
            currentExercise = exerciseCatalog.GetExercise(currentExerciseIndex);
            EnsureExercise();
            BindView();
            LoadCurrentExercise();
            SelectService(useMock);
            ResetView();
            activeService.Prepare();
        }

        private void SelectService(bool mock)
        {
            UnbindService();
            activeService = mock ? mockService : realService;
            if (activeService == null)
                throw new InvalidOperationException("Selected speech service is missing.");

            activeService.Configure(currentExercise);
            activeService.TranscriptUpdated += HandleTranscript;
            activeService.AnalysisStarted += HandleAnalysisStarted;
            activeService.ListeningStopped += HandleListeningStopped;
            activeService.Failed += HandleFailure;
            activeService.StatusChanged += HandleServiceStatus;
            view.SetMode(mock);
        }

        private void UnbindService()
        {
            if (activeService == null)
                return;

            activeService.TranscriptUpdated -= HandleTranscript;
            activeService.AnalysisStarted -= HandleAnalysisStarted;
            activeService.ListeningStopped -= HandleListeningStopped;
            activeService.Failed -= HandleFailure;
            activeService.StatusChanged -= HandleServiceStatus;
        }

        private void EnsureExercise()
        {
            if (currentExercise != null)
                return;

            if (exerciseCatalog != null && exerciseCatalog.Count > 0)
            {
                currentExerciseIndex = Mathf.Clamp(currentExerciseIndex, 0, exerciseCatalog.Count - 1);
                currentExercise = exerciseCatalog.GetExercise(currentExerciseIndex);
            }
        }

        private void BindView()
        {
            view.Build(currentExercise.Prompt, currentExercise.GetDisplayWords());
            view.SetNavigation(
                exerciseCatalog != null && currentExerciseIndex > 0,
                exerciseCatalog != null && currentExerciseIndex < exerciseCatalog.Count - 1);
        }

        private void LoadCurrentExercise()
        {
            if (currentExercise == null)
                throw new InvalidOperationException("Fluent Echo requires an exercise to be selected.");

            progress = LessonProgressRepository.Load(currentExercise.ProgressKey, currentExercise.GetDisplayWords().Length);
            view.SetProgress(progress.GetSummaryText());
            view.SetProgressDetails(BuildProgressDetailsText());
        }

        private void HandleTranscript(string transcript)
        {
            SpeechMatchResult result = matcher.Match(
                transcript,
                currentExercise.GetAcceptedWordGroups(),
                currentExercise.GetAcceptedPhrases(),
                currentExercise.RequireWordOrder,
                currentExercise.AllowFuzzyMatch);

            session.UpdateTranscript(transcript, result);
            view.SetTranscript(transcript);
            view.SetWordMatches(result.MatchedWords);

            if (!result.IsComplete || success)
                return;

            success = true;
            session.BeginSuccess();
            view.SetSuccess(true);
            view.SetStatus("Excellent. Every target word was recognized.");
            view.SetListening(false);

            if (activeService.IsListening)
                activeService.StopListening();
        }

        private void HandleAnalysisStarted()
        {
            if (success)
                return;

            session.BeginAnalyzing();
            view.SetStatus("Analyzing speech locally...");
            view.SetListening(false);
        }

        private void HandleListeningStopped()
        {
            if (session.Phase == SpeechSessionPhase.Cancelling)
                return;

            view.SetListening(false);
            if (session.Phase == SpeechSessionPhase.Success
                || session.Phase == SpeechSessionPhase.Retry
                || session.Phase == SpeechSessionPhase.Analyzing)
            {
                UpdatePronunciationScore();
                SaveProgress();
            }

            if (success || session.Phase == SpeechSessionPhase.Error)
                return;

            session.BeginRetry();
            view.SetStatus(string.IsNullOrWhiteSpace(session.Transcript)
                ? "No speech was recognized. Check the microphone and try again."
                : "Some words are missing. Review the highlights and retry.");
        }

        private void HandleFailure(string message)
        {
            if (session.Phase == SpeechSessionPhase.Cancelling)
                return;

            UpdatePronunciationScore();
            session.BeginError();
            view.SetListening(false);
            view.SetStatus(message);
        }

        private void HandleServiceStatus(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (message.StartsWith("Loading", StringComparison.OrdinalIgnoreCase)
                || message.StartsWith("Warming", StringComparison.OrdinalIgnoreCase))
            {
                session.BeginPreparing();
            }
            else if (message.StartsWith("Whisper ready", StringComparison.OrdinalIgnoreCase))
            {
                if (session.Phase == SpeechSessionPhase.Preparing)
                    session.BeginIdle();
            }

            view.SetStatus(message);
        }

        private void ResetView()
        {
            session.Reset();
            success = false;
            ClearAttemptState();
            lastPronunciationScore = PronunciationScoreResult.Unavailable;
            view.SetTranscript(string.Empty);
            view.SetWordMatches(new bool[currentExercise.GetDisplayWords().Length]);
            view.SetProgress(progress?.GetSummaryText() ?? string.Empty);
            view.SetProgressDetails(BuildProgressDetailsText());
            view.SetPronunciation(string.Empty, string.Empty);
            view.SetListening(false);
            view.SetSuccess(false);
            view.SetStatus(useMock
                ? "Demo mode is ready. Press the microphone to simulate a correct answer."
                : "Loading speech engine...");
        }

        private void SaveProgress()
        {
            if (progress == null)
                return;

            progress.RecordAttempt(
                session.Transcript,
                session.MatchResult,
                currentExercise.GetDisplayWords().Length,
                lastPronunciationScore.OverallScore,
                lastPronunciationScore.BandLabel,
                lastPronunciationScore.SummaryText);
            LessonProgressRepository.Save(progress);
            view.SetProgress(progress.GetSummaryText());
            view.SetProgressDetails(BuildProgressDetailsText());
        }

        private bool IsInteractionLocked()
        {
            return activeService == null
                ? false
                : session.IsBusy
                  || activeService.IsListening;
        }

        private void CancelCurrentService(string statusMessage)
        {
            if (activeService == null)
                return;

            session.BeginCancelling();
            view.SetStatus(statusMessage);
            view.SetListening(false);
            ClearAttemptState();

            UnbindService();
            activeService.Cancel();
        }

        private void UpdatePronunciationScore()
        {
            lastPronunciationScore = scoringService.Score(
                currentExercise,
                session.Transcript,
                session.MatchResult,
                GetAttemptDurationSeconds());

            if (!lastPronunciationScore.IsAvailable)
            {
                view.SetPronunciation(string.Empty, string.Empty);
                return;
            }

            view.SetPronunciation(
                lastPronunciationScore.SummaryText,
                BuildPronunciationDetails(lastPronunciationScore));
        }

        private float GetAttemptDurationSeconds()
        {
            if (attemptStartedAt < 0f)
                return 0f;

            return Mathf.Max(0f, Time.realtimeSinceStartup - attemptStartedAt);
        }

        private void ClearAttemptState()
        {
            attemptStartedAt = -1f;
        }

        private string BuildProgressDetailsText()
        {
            var lines = new System.Collections.Generic.List<string>();

            if (lastPronunciationScore.IsAvailable)
            {
                lines.Add(
                    $"RESULT | {lastPronunciationScore.OverallScore}/100 | {lastPronunciationScore.BandLabel.ToUpperInvariant()}");
                lines.Add(
                    $"Coverage {lastPronunciationScore.CoverageScore}% | Precision {lastPronunciationScore.PrecisionScore}% | Tempo {lastPronunciationScore.TempoScore}%");

                if (!string.IsNullOrWhiteSpace(lastPronunciationScore.FeedbackText))
                    lines.Add(lastPronunciationScore.FeedbackText);
            }

            string history = progress?.GetHistoryText(2);
            if (!string.IsNullOrWhiteSpace(history))
            {
                if (lines.Count > 0)
                    lines.Add(string.Empty);

                lines.Add(history);
            }

            return lines.Count == 0
                ? "Recent attempts will appear here."
                : string.Join("\n", lines);
        }

        private static string BuildPronunciationDetails(PronunciationScoreResult score)
        {
            if (!score.IsAvailable)
                return string.Empty;

            string focus = score.MissingWordCount > 0
                ? $"Focus next: {score.MissingWordCount} missing word{(score.MissingWordCount == 1 ? string.Empty : "s")}."
                : "Focus next: keep the rhythm steady.";

            return string.Join(
                "\n",
                $"Coverage {score.CoverageScore}% | Precision {score.PrecisionScore}% | Tempo {score.TempoScore}%",
                focus,
                string.IsNullOrWhiteSpace(score.FeedbackText) ? string.Empty : score.FeedbackText);
        }

        private int ResolveExerciseIndex(SpeechExerciseSO selectedExercise)
        {
            if (exerciseCatalog == null || exerciseCatalog.Count == 0 || selectedExercise == null)
                return 0;

            for (int i = 0; i < exerciseCatalog.Count; i++)
            {
                if (exerciseCatalog.GetExercise(i) == selectedExercise)
                    return i;
            }

            return 0;
        }
    }
}
