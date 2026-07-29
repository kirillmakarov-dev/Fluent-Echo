using System;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Services;
using UnityEngine;

namespace FluentEcho.Presentation
{
    public sealed class FluentEchoPresenter : IDisposable
    {
        private const string OnboardingPrefsKey = "FluentEcho.OnboardingSeenV1";

        private enum NoticeAction
        {
            None,
            AdvanceOnboarding,
            CompleteOnboarding,
            OpenSettings,
            RetryAttempt
        }

        private readonly SpeechExerciseCatalogSO exerciseCatalog;
        private readonly IFluentEchoView view;
        private readonly ISpeechRecognitionService realService;
        private readonly ISpeechRecognitionService mockService;
        private readonly IPronunciationScoringService scoringService = new HeuristicPronunciationScoringService();
        private readonly IPhonemeAlignmentService phonemeAlignmentService;
        private readonly SpeechAnswerMatcher matcher = new();
        private readonly SpeechSession session = new();
        private readonly Action<AudioClip> playReference;

        private ISpeechRecognitionService activeService;
        private LessonProgressState progress;
        private SpeechExerciseSO currentExercise;
        private readonly Action<int, int> persistSelection;
        private int currentCategoryIndex;
        private int currentExerciseIndex;
        private bool useMock;
        private bool onboardingRequired;
        private float attemptStartedAt = -1f;
        private PronunciationScoreResult lastPronunciationScore = PronunciationScoreResult.Unavailable;
        private PhonemeAlignmentResult lastPhonemeAlignment = PhonemeAlignmentResult.Unavailable;
        private NoticeAction pendingNoticeAction = NoticeAction.None;

        public FluentEchoPresenter(
            SpeechExerciseSO exercise,
            SpeechExerciseCatalogSO exerciseCatalog,
            IFluentEchoView view,
            ISpeechRecognitionService realService,
            ISpeechRecognitionService mockService,
            bool useMockByDefault,
            Action<AudioClip> playReference,
            IPhonemeAlignmentService phonemeAlignmentService = null,
            int selectedCategoryIndex = 0,
            int selectedExerciseIndex = 0,
            Action<int, int> persistSelection = null)
        {
            this.exerciseCatalog = exerciseCatalog;
            this.view = view;
            this.realService = realService;
            this.mockService = mockService;
            useMock = useMockByDefault;
            this.playReference = playReference;
            this.phonemeAlignmentService = phonemeAlignmentService ?? NoOpPhonemeAlignmentService.Instance;
            this.persistSelection = persistSelection;
            currentCategoryIndex = ResolveCategoryIndex(selectedCategoryIndex);
            currentExerciseIndex = Mathf.Max(0, selectedExerciseIndex);
            currentExercise = ResolveExercise(exercise);
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
            view.CategoriesPressed += HandleCategoriesPressed;
            view.CategorySelected += HandleCategorySelected;
            view.LessonSelected += HandleLessonSelected;
            view.MockModeChanged += HandleMockModeChanged;
            view.NoticeConfirmed += HandleNoticeConfirmed;

            SelectService(useMock);
            ResetView();
            activeService.Prepare();
            UpdateMicControlState();
            onboardingRequired = PlayerPrefs.GetInt(OnboardingPrefsKey, 0) == 0;
            if (onboardingRequired)
            {
                view.SetCategoryScreenVisible(false);
                ShowOnboardingWelcome();
            }
            else
            {
                view.SetCategoryScreenVisible(true);
            }
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
            view.CategoriesPressed -= HandleCategoriesPressed;
            view.CategorySelected -= HandleCategorySelected;
            view.LessonSelected -= HandleLessonSelected;
            view.MockModeChanged -= HandleMockModeChanged;
            view.NoticeConfirmed -= HandleNoticeConfirmed;
        }

        private void HandleMicPressed()
        {
            if (onboardingRequired)
                return;

            if (session.IsCancelling)
                return;

            if (activeService.IsListening)
            {
                activeService.StopListening();
                return;
            }

            if (!session.CanStartRecording)
                return;

            if (IsInteractionLocked())
            {
                if (session.Phase == SpeechSessionPhase.Preparing)
                    view.SetStatus(FluentEchoCopy.LoadingSpeechEngineStatus);
                UpdateMicControlState();
                return;
            }

            session.Reset();
            view.SetTranscript(string.Empty);
            view.SetWordMatches(new bool[currentExercise.GetDisplayWords().Length]);
            view.SetSuccess(false);
            view.SetStatus(useMock
                ? "Playing a demo attempt..."
                : "Listening. Speak naturally, then press Check Answer.");
            view.SetListening(true);
            UpdateMicControlState();
            session.BeginListening();
            attemptStartedAt = Time.realtimeSinceStartup;
            activeService.StartListening();
        }

        private void HandleDemoPressed()
        {
            if (onboardingRequired)
                return;

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
            if (onboardingRequired)
                return;

            CancelCurrentService("Resetting attempt...");
            session.Reset();
            ClearAttemptState();
            ResetView();
        }

        private void HandleListen()
        {
            if (onboardingRequired)
                return;

            if (IsInteractionLocked())
                return;

            if (currentExercise.ReferenceAudio != null)
                playReference?.Invoke(currentExercise.ReferenceAudio);
        }

        private void HandleMockModeChanged(bool value)
        {
            if (onboardingRequired)
                return;

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
            if (onboardingRequired)
                return;

            if (IsInteractionLocked())
                return;

            SwitchExercise(currentExerciseIndex - 1);
        }

        private void HandleNextExercise()
        {
            if (onboardingRequired)
                return;

            if (IsInteractionLocked())
                return;

            SwitchExercise(currentExerciseIndex + 1);
        }

        private void HandleCategoriesPressed()
        {
            if (onboardingRequired)
                return;

            if (IsInteractionLocked())
                return;

            view.SetCategoryScreenVisible(true);
        }

        private void HandleCategorySelected(int categoryIndex)
        {
            if (onboardingRequired)
                return;

            if (IsInteractionLocked())
                return;

            SwitchCategory(categoryIndex);
            view.SetCategoryScreenVisible(false);
        }

        private void HandleLessonSelected(int exerciseIndex)
        {
            if (onboardingRequired)
                return;

            if (IsInteractionLocked())
                return;

            SwitchExercise(exerciseIndex);
        }

        private void SwitchCategory(int requestedCategoryIndex)
        {
            if (exerciseCatalog == null || exerciseCatalog.CategoryCount == 0)
                return;

            int clampedIndex = Mathf.Clamp(requestedCategoryIndex, 0, exerciseCatalog.CategoryCount - 1);
            if (clampedIndex == currentCategoryIndex)
            {
                ClearAttemptState();
                BindView();
                LoadCurrentExercise();
                ConfigureActiveServiceForCurrentExercise();
                ResetView();
                persistSelection?.Invoke(currentCategoryIndex, currentExerciseIndex);
                return;
            }

            ClearAttemptState();
            currentCategoryIndex = clampedIndex;
            currentExerciseIndex = 0;
            currentExercise = exerciseCatalog.GetCategoryExercise(currentCategoryIndex, currentExerciseIndex);
            persistSelection?.Invoke(currentCategoryIndex, currentExerciseIndex);
            EnsureExercise();
            BindView();
            LoadCurrentExercise();
            ConfigureActiveServiceForCurrentExercise();
            ResetView();
        }

        private void SwitchExercise(int requestedIndex)
        {
            int exerciseCount = GetCurrentExerciseCount();
            if (exerciseCount == 0)
                return;

            int clampedIndex = Mathf.Clamp(requestedIndex, 0, exerciseCount - 1);
            if (clampedIndex == currentExerciseIndex)
                return;

            ClearAttemptState();
            currentExerciseIndex = clampedIndex;
            currentExercise = exerciseCatalog.GetCategoryExercise(currentCategoryIndex, currentExerciseIndex);
            persistSelection?.Invoke(currentCategoryIndex, currentExerciseIndex);
            EnsureExercise();
            BindView();
            LoadCurrentExercise();
            ConfigureActiveServiceForCurrentExercise();
            ResetView();
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

        private void ConfigureActiveServiceForCurrentExercise()
        {
            if (activeService == null)
                return;

            activeService.Configure(currentExercise);
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
                currentCategoryIndex = ResolveCategoryIndex(currentCategoryIndex);
                currentExerciseIndex = Mathf.Clamp(currentExerciseIndex, 0, Mathf.Max(0, GetCurrentExerciseCount() - 1));
                currentExercise = exerciseCatalog.GetCategoryExercise(currentCategoryIndex, currentExerciseIndex);
            }
        }

        private void BindView()
        {
            SpeechExerciseCategory category = exerciseCatalog != null
                ? exerciseCatalog.GetCategory(currentCategoryIndex)
                : null;
            string categoryName = category != null ? category.DisplayName : "Practice Menu";
            string categoryDescription = category != null ? category.Description : "Pick a lesson set and practice at your own pace.";
            string categoryProgress = BuildCategoryProgressSummary(currentCategoryIndex);
            int exerciseCount = GetCurrentExerciseCount();

            view.SetCategory(categoryName, categoryDescription);
            view.SetCategoryProgress(categoryProgress);
            view.SetLessonOptions(
                exerciseCatalog != null
                    ? exerciseCatalog.GetCategoryExerciseDisplayNames(currentCategoryIndex)
                    : Array.Empty<string>(),
                currentExerciseIndex);
            view.Build(currentExercise.Prompt, currentExercise.GetDisplayWords());
            view.SetNavigation(
                exerciseCatalog != null && currentExerciseIndex > 0,
                exerciseCatalog != null && currentExerciseIndex < exerciseCount - 1);
            view.SetLessonPosition(
                currentExerciseIndex + 1,
                exerciseCount > 0 ? exerciseCount : 1);
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

            if (!result.IsComplete || session.Phase == SpeechSessionPhase.Success)
                return;

            session.BeginSuccess();
            view.SetSuccess(true);
            view.SetStatus(FluentEchoCopy.ExcellentResultStatus);
            view.SetListening(false);

            if (activeService.IsListening)
                activeService.StopListening();
        }

        private void HandleAnalysisStarted()
        {
            if (session.Phase == SpeechSessionPhase.Success || session.IsCancelling)
                return;

            session.BeginAnalyzing();
            view.SetStatus(FluentEchoCopy.AnalyzingSpeechStatus);
            view.SetListening(false);
            UpdateMicControlState();
        }

        private void HandleListeningStopped()
        {
            if (session.IsCancelling)
                return;

            view.SetListening(false);
            if (session.Phase == SpeechSessionPhase.Success
                || session.Phase == SpeechSessionPhase.Retry
                || session.Phase == SpeechSessionPhase.Analyzing)
            {
                UpdatePronunciationScore();
                SaveProgress();
            }

            if (session.Phase == SpeechSessionPhase.Error || session.Phase == SpeechSessionPhase.Success)
            {
                UpdateMicControlState();
                return;
            }

            session.BeginRetry();
            view.SetStatus(string.IsNullOrWhiteSpace(session.Transcript)
                ? FluentEchoCopy.DidNotCatchThatDetailedStatus
                : FluentEchoCopy.AFewWordsAreMissingStatus);
            UpdateMicControlState();
        }

        private void HandleFailure(string message)
        {
            if (session.Phase == SpeechSessionPhase.Cancelling)
                return;

            UpdatePronunciationScore();
            session.BeginError();
            view.SetListening(false);
            ShowUserFacingFailure(message);
            UpdateMicControlState();
        }

        private void HandleServiceStatus(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (message.StartsWith("Loading", StringComparison.OrdinalIgnoreCase)
                || message.StartsWith("Warming", StringComparison.OrdinalIgnoreCase))
            {
                session.BeginPreparing();
                UpdateMicControlState();
            }
            else if (message.StartsWith("Whisper ready", StringComparison.OrdinalIgnoreCase))
            {
                if (session.Phase == SpeechSessionPhase.Preparing)
                    session.BeginIdle();
                UpdateMicControlState();
            }

            view.SetStatus(message);
        }

        private void ResetView()
        {
            session.Reset();
            ClearAttemptState();
            lastPronunciationScore = PronunciationScoreResult.Unavailable;
            view.SetTranscript(string.Empty);
            view.SetWordMatches(new bool[currentExercise.GetDisplayWords().Length]);
            view.SetProgress(progress?.GetSummaryText() ?? string.Empty);
            view.SetProgressDetails(BuildProgressDetailsText());
            view.SetPronunciation(
                FluentEchoCopy.FirstPronunciationSummary,
                FluentEchoCopy.BuildUnavailablePronunciationDetails());
            view.SetPronunciationConfidence(FluentEchoCopy.FirstConfidenceSummary);
            view.SetPronunciationBreakdown(
                FluentEchoCopy.FirstWordMatchSummary,
                FluentEchoCopy.FirstRhythmSummary);
            view.SetListening(false);
            view.SetSuccess(false);
            UpdateMicControlState();
            if (useMock)
                view.SetStatus(FluentEchoCopy.DemoModeReadyStatus);
            else if (activeService != null && activeService.IsReady)
                view.SetStatus(FluentEchoCopy.ReadyToPracticeStatus);
            else
                view.SetStatus(FluentEchoCopy.PreparingSpeechModelStatus);
        }

        private void HandleNoticeConfirmed()
        {
            switch (pendingNoticeAction)
            {
                case NoticeAction.CompleteOnboarding:
                    onboardingRequired = false;
                    PlayerPrefs.SetInt(OnboardingPrefsKey, 1);
                    PlayerPrefs.Save();
                    view.HideNotice();
                    view.SetCategoryScreenVisible(true);
                    view.SetStatus(useMock
                        ? FluentEchoCopy.DemoModeReadyStatus
                        : FluentEchoCopy.ReadyToPracticeStatus);
                    break;
                case NoticeAction.AdvanceOnboarding:
                    ShowOnboardingPracticePaths();
                    break;
                case NoticeAction.OpenSettings:
                    view.HideNotice();
                    view.SetSettingsPanelVisible(true);
                    break;
                case NoticeAction.RetryAttempt:
                    view.HideNotice();
                    HandleRetry();
                    break;
            }

            pendingNoticeAction = NoticeAction.None;
        }

        private void ShowOnboardingWelcome()
        {
            pendingNoticeAction = NoticeAction.AdvanceOnboarding;
            view.ShowNotice(
                FluentEchoCopy.OnboardingWelcomeTitle,
                FluentEchoCopy.OnboardingWelcomeBody,
                "CONTINUE");
        }

        private void ShowOnboardingPracticePaths()
        {
            pendingNoticeAction = NoticeAction.CompleteOnboarding;
            view.ShowNotice(
                FluentEchoCopy.OnboardingPracticePathsTitle,
                FluentEchoCopy.OnboardingPracticePathsBody,
                "CHOOSE PATH");
        }

        private void ShowUserFacingFailure(string message)
        {
            string normalized = message ?? string.Empty;

            if (normalized.IndexOf("microphone device was detected", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pendingNoticeAction = NoticeAction.OpenSettings;
                view.ShowNotice(
                    FluentEchoCopy.MicrophoneNotFoundTitle,
                    FluentEchoCopy.MicrophoneNotFoundBody,
                    "OPEN SETTINGS");
                view.SetStatus(FluentEchoCopy.MicrophoneNotFoundStatus);
                return;
            }

            if (normalized.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("could not start", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pendingNoticeAction = NoticeAction.OpenSettings;
                view.ShowNotice(
                    FluentEchoCopy.MicrophonePermissionNeededTitle,
                    FluentEchoCopy.MicrophonePermissionNeededBody,
                    "OPEN SETTINGS");
                view.SetStatus(FluentEchoCopy.MicrophonePermissionNeededStatus);
                return;
            }

            if (normalized.IndexOf("model is missing", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("could not load", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pendingNoticeAction = NoticeAction.OpenSettings;
                view.ShowNotice(
                    FluentEchoCopy.SpeechModelMissingTitle,
                    FluentEchoCopy.SpeechModelMissingBody,
                    "OPEN SETTINGS");
                view.SetStatus(FluentEchoCopy.SpeechModelMissingStatus);
                return;
            }

            if (normalized.IndexOf("analysis could not start", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("Listening failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pendingNoticeAction = NoticeAction.RetryAttempt;
                view.ShowNotice(
                    FluentEchoCopy.CouldNotCheckAttemptTitle,
                    "The recording stopped before analysis finished. Please try again.",
                    "TRY AGAIN");
                view.SetStatus(FluentEchoCopy.CouldNotCheckAttemptStatus);
                return;
            }

            if (normalized.IndexOf("no clear speech", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("did not catch", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pendingNoticeAction = NoticeAction.RetryAttempt;
                view.ShowNotice(
                    FluentEchoCopy.DidNotCatchThatTitle,
                    "Move closer to the microphone and try again.",
                    "TRY AGAIN");
                view.SetStatus(FluentEchoCopy.DidNotCatchThatStatus);
                return;
            }

            pendingNoticeAction = NoticeAction.RetryAttempt;
            view.ShowNotice(
                FluentEchoCopy.CouldNotCheckAttemptTitle,
                "Please try again.",
                "TRY AGAIN");
            view.SetStatus(normalized);
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
                lastPronunciationScore.SummaryText,
                lastPronunciationScore.ConfidenceBand,
                lastPronunciationScore.ConfidenceScore,
                lastPronunciationScore.ConfidenceReason,
                lastPronunciationScore.ExactWordCount,
                lastPronunciationScore.ApproximateWordCount,
                lastPronunciationScore.MissedWordCount);
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

        private bool IsRecordingLocked()
        {
            return activeService != null
                   && (activeService.IsListening
                       || session.Phase == SpeechSessionPhase.Cancelling
                       || session.Phase == SpeechSessionPhase.Analyzing);
        }

        private void CancelCurrentService(string statusMessage)
        {
            if (activeService == null)
                return;

            session.BeginCancelling();
            view.SetStatus(statusMessage);
            view.SetListening(false);
            UpdateMicControlState();
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
            lastPhonemeAlignment = UpdatePhonemeAlignmentEvidence();

            if (!lastPronunciationScore.IsAvailable)
            {
                view.SetPronunciation(
                    FluentEchoCopy.FirstPronunciationSummary,
                    FluentEchoCopy.BuildUnavailablePronunciationDetails());
                view.SetPronunciationConfidence(FluentEchoCopy.FirstConfidenceSummary);
                return;
            }

            view.SetPronunciation(
                lastPronunciationScore.SummaryText,
                BuildPronunciationDetails(lastPronunciationScore));
            view.SetPronunciationConfidence($"CONFIDENCE | {Capitalize(lastPronunciationScore.ConfidenceBand)}");
            view.SetPronunciationBreakdown(
                $"WORD MATCH | {lastPronunciationScore.MatchedWordCount}/{lastPronunciationScore.ExpectedWordCount}",
                $"RHYTHM | {lastPronunciationScore.TempoScore}/100");
        }

        private float GetAttemptDurationSeconds()
        {
            if (attemptStartedAt < 0f)
                return 0f;

            return Mathf.Max(0f, Time.realtimeSinceStartup - attemptStartedAt);
        }

        private PhonemeAlignmentResult UpdatePhonemeAlignmentEvidence()
        {
            if (phonemeAlignmentService == null || currentExercise == null)
                return PhonemeAlignmentResult.Unavailable;

            return phonemeAlignmentService.Align(
                new PhonemeAlignmentRequest(
                    currentExercise,
                    session.Transcript,
                    session.MatchResult,
                    GetAttemptDurationSeconds(),
                    currentExercise.GetDisplayWords(),
                    currentExercise.GetAcceptedPhrases()));
        }

        private void ClearAttemptState()
        {
            attemptStartedAt = -1f;
        }

        private string BuildProgressDetailsText()
        {
            if (session.Phase == SpeechSessionPhase.Success && lastPronunciationScore.IsAvailable)
                return BuildAcceptedResultDetailsText();

            var lines = new System.Collections.Generic.List<string>();

            if (lastPronunciationScore.IsAvailable)
            {
                lines.Add("Current attempt:");
                lines.Add($"Practice score: {lastPronunciationScore.OverallScore}/100 | {lastPronunciationScore.BandLabel}");
                lines.Add($"Confidence: {Capitalize(lastPronunciationScore.ConfidenceBand)}");
                if (!string.IsNullOrWhiteSpace(lastPronunciationScore.ConfidenceReason))
                    lines.Add($"Confidence reason: {lastPronunciationScore.ConfidenceReason}");
                lines.Add($"Word match: {lastPronunciationScore.MatchedWordCount}/{lastPronunciationScore.ExpectedWordCount}");
                lines.Add($"Recognition precision: {lastPronunciationScore.PrecisionScore}%");
                lines.Add($"Rhythm: {lastPronunciationScore.TempoScore}%");
                lines.Add($"Word focus: {lastPronunciationScore.WordQualityScore}%");
                string matchQuality = BuildMatchQualityText(lastPronunciationScore);
                if (!string.IsNullOrWhiteSpace(matchQuality))
                    lines.Add(matchQuality);
                string approximateMatches = BuildApproximateMatchText(lastPronunciationScore);
                if (!string.IsNullOrWhiteSpace(approximateMatches))
                    lines.Add(approximateMatches);
                if (!string.IsNullOrWhiteSpace(lastPronunciationScore.EstimateBasisText))
                    lines.Add(lastPronunciationScore.EstimateBasisText);

                string phonemeAlignment = PhonemeAlignmentTextFormatter.BuildPreviewText(lastPhonemeAlignment);
                if (!string.IsNullOrWhiteSpace(phonemeAlignment))
                    lines.Add(phonemeAlignment);

                string wordBreakdown = BuildWordBreakdown(lastPronunciationScore.WordScores);
                if (!string.IsNullOrWhiteSpace(wordBreakdown))
                    lines.Add(wordBreakdown);

                if (progress != null && progress.BestPronunciationScore > 0)
                {
                    bool isNewBestAttempt =
                        progress.BestPronunciationScore == lastPronunciationScore.OverallScore
                        && string.Equals(
                            progress.BestTranscript?.Trim(),
                            session.Transcript?.Trim(),
                            StringComparison.Ordinal);

                    lines.Add(isNewBestAttempt
                        ? "Best attempt so far: this attempt is the new best."
                        : "Best attempt so far:");

                    string bestAttempt = $"Practice score {progress.BestPronunciationScore}/100";
                    if (!string.IsNullOrWhiteSpace(progress.BestPronunciationConfidenceBand))
                        bestAttempt += $" | confidence {Capitalize(progress.BestPronunciationConfidenceBand)}";

                    if (!string.IsNullOrWhiteSpace(progress.BestTranscript))
                        bestAttempt += $" | \"{progress.BestTranscript}\"";

                    lines.Add(bestAttempt);
                }

                if (progress != null)
                {
                    lines.Add("Lesson progress:");
                    lines.Add(progress.GetSummaryText());
                }

                if (!string.IsNullOrWhiteSpace(lastPronunciationScore.FeedbackText))
                {
                    lines.Add(string.Empty);
                    lines.Add("Focus next:");
                    lines.Add(lastPronunciationScore.FeedbackText);
                }
            }

            string history = progress?.GetHistoryText(2);
            if (!string.IsNullOrWhiteSpace(history))
            {
                if (lines.Count > 0)
                    lines.Add(string.Empty);

                lines.Add(history);
            }

            return lines.Count == 0
                ? FluentEchoCopy.FirstLocalEstimateText
                : string.Join("\n", lines);
        }

        private string BuildAcceptedResultDetailsText()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                FluentEchoCopy.ResultAcceptedHeader,
                FluentEchoCopy.ResultWhatWeHeardHeader
            };

            lines.Add(string.IsNullOrWhiteSpace(session.Transcript)
                ? "Your transcript will appear here."
                : $"\"{session.Transcript.Trim()}\"");

            lines.Add(string.Empty);
            lines.Add(FluentEchoCopy.ResultEstimateHeader);
            lines.Add($"Practice score: {lastPronunciationScore.OverallScore}/100 | {lastPronunciationScore.BandLabel}");
            lines.Add($"Confidence: {Capitalize(lastPronunciationScore.ConfidenceBand)}");
            if (!string.IsNullOrWhiteSpace(lastPronunciationScore.ConfidenceReason))
                lines.Add($"Confidence reason: {lastPronunciationScore.ConfidenceReason}");
            if (!string.IsNullOrWhiteSpace(lastPronunciationScore.EstimateBasisText))
                lines.Add(lastPronunciationScore.EstimateBasisText);
            lines.Add(string.Empty);
            lines.Add(FluentEchoCopy.ResultSignalBreakdownHeader);
            lines.Add($"Coverage: {lastPronunciationScore.MatchedWordCount}/{lastPronunciationScore.ExpectedWordCount}");
            lines.Add($"Precision: {lastPronunciationScore.PrecisionScore}%");
            lines.Add($"Rhythm: {lastPronunciationScore.TempoScore}%");
            lines.Add($"Word focus: {lastPronunciationScore.WordQualityScore}%");
            string phonemeAlignment = PhonemeAlignmentTextFormatter.BuildPreviewText(lastPhonemeAlignment);
            if (!string.IsNullOrWhiteSpace(phonemeAlignment))
                lines.Add(phonemeAlignment);
            string matchQuality = BuildMatchQualityText(lastPronunciationScore);
            if (!string.IsNullOrWhiteSpace(matchQuality))
                lines.Add(matchQuality);
            string approximateMatches = BuildApproximateMatchText(lastPronunciationScore);
            if (!string.IsNullOrWhiteSpace(approximateMatches))
                lines.Add(approximateMatches);

            string wordBreakdown = BuildWordBreakdown(lastPronunciationScore.WordScores);
            if (!string.IsNullOrWhiteSpace(wordBreakdown))
                lines.Add(wordBreakdown);

            if (!string.IsNullOrWhiteSpace(lastPronunciationScore.FeedbackText))
            {
                lines.Add(string.Empty);
                lines.Add(FluentEchoCopy.ResultTakeawayHeader);
                lines.Add(lastPronunciationScore.FeedbackText);
            }

            string focusNext = BuildFocusNextText(lastPronunciationScore);
            if (!string.IsNullOrWhiteSpace(focusNext))
            {
                lines.Add(string.Empty);
                lines.Add("Focus next:");
                lines.Add(focusNext);
            }

            if (progress != null)
            {
                lines.Add(string.Empty);
                lines.Add(FluentEchoCopy.ResultLessonRecapHeader);
                lines.Add(progress.GetSummaryText());
            }

            string history = progress?.GetHistoryText(2);
            if (!string.IsNullOrWhiteSpace(history))
            {
                lines.Add(string.Empty);
                lines.Add(history);
            }

            lines.Add(string.Empty);
            lines.Add(FluentEchoCopy.ResultNextStepHeader);
            lines.Add(BuildResultNextStepPrompt());

            return string.Join("\n", lines);
        }

        private string BuildResultNextStepPrompt()
        {
            if (HasNextExercise())
                return FluentEchoCopy.NextMissionPrompt;

            return FluentEchoCopy.FinalLessonPrompt;
        }

        private string BuildCategoryProgressSummary(int categoryIndex)
        {
            if (exerciseCatalog == null || exerciseCatalog.CategoryCount == 0)
                return string.Empty;

            int exerciseCount = exerciseCatalog.GetCategoryExerciseCount(categoryIndex);
            if (exerciseCount <= 0)
                return string.Empty;

            int clearedLessons = 0;
            int bestScore = 0;
            int totalAttempts = 0;
            int attemptedLessons = 0;
            int totalBestScore = 0;
            string bestConfidenceBand = string.Empty;
            string bestLessonLabel = string.Empty;
            string startLessonLabel = string.Empty;
            string nextLessonLabel = string.Empty;

            for (int i = 0; i < exerciseCount; i++)
            {
                SpeechExerciseSO exercise = exerciseCatalog.GetCategoryExercise(categoryIndex, i);
                if (exercise == null)
                    continue;

                if (string.IsNullOrWhiteSpace(startLessonLabel))
                    startLessonLabel = BuildCategoryLessonLabel(exercise);

                LessonProgressState lessonProgress = LessonProgressRepository.Load(
                    exercise.ProgressKey,
                    exercise.GetDisplayWords().Length);

                totalAttempts += lessonProgress.Attempts;
                if (lessonProgress.Attempts > 0)
                {
                    attemptedLessons++;
                    totalBestScore += lessonProgress.BestPronunciationScore;

                    if (lessonProgress.SuccessfulAttempts <= 0 && string.IsNullOrWhiteSpace(nextLessonLabel))
                        nextLessonLabel = BuildCategoryLessonLabel(exercise);
                }

                if (lessonProgress.SuccessfulAttempts > 0)
                    clearedLessons++;

                if (lessonProgress.BestPronunciationScore > bestScore)
                {
                    bestScore = lessonProgress.BestPronunciationScore;
                    bestConfidenceBand = lessonProgress.BestPronunciationConfidenceBand;
                    bestLessonLabel = BuildCategoryLessonLabel(exercise);
                }
            }

            string summary = $"Progress: {clearedLessons}/{exerciseCount} lessons cleared";
            if (clearedLessons == exerciseCount && exerciseCount > 0)
                summary += " | all lessons cleared";
            if (attemptedLessons > 0)
                summary += $" | {attemptedLessons}/{exerciseCount} attempted";
            if (bestScore > 0)
                summary += $" | best score {bestScore}/100";
            if (!string.IsNullOrWhiteSpace(bestLessonLabel))
                summary += $" | top lesson {bestLessonLabel}";
            else if (attemptedLessons == 0 && !string.IsNullOrWhiteSpace(startLessonLabel))
                summary += $" | start with {startLessonLabel}";
            if (attemptedLessons > 0
                && !string.IsNullOrWhiteSpace(nextLessonLabel)
                && !string.Equals(nextLessonLabel, bestLessonLabel, StringComparison.Ordinal))
                summary += $" | next lesson {nextLessonLabel}";
            if (!string.IsNullOrWhiteSpace(bestConfidenceBand))
                summary += $" | confidence {bestConfidenceBand}";
            if (attemptedLessons > 0)
                summary += $" | avg best score {Mathf.RoundToInt((float) totalBestScore / attemptedLessons)}/100";
            if (totalAttempts > 0)
                summary += $" | {totalAttempts} attempts";

            return summary;
        }

        private string BuildPronunciationDetails(PronunciationScoreResult score)
        {
            if (!score.IsAvailable)
                return string.Empty;

            string focus = BuildFocusNextText(score);

            string wordBreakdown = BuildWordBreakdown(score.WordScores);

            var lines = new System.Collections.Generic.List<string>
            {
                "Current attempt:",
                $"Practice score: {score.OverallScore}/100 | {score.BandLabel}",
                $"Confidence: {Capitalize(score.ConfidenceBand)}",
                string.IsNullOrWhiteSpace(score.ConfidenceReason)
                    ? string.Empty
                    : $"Confidence reason: {score.ConfidenceReason}",
                $"Word match: {score.MatchedWordCount}/{score.ExpectedWordCount}",
                $"Recognition precision: {score.PrecisionScore}%",
                $"Rhythm: {score.TempoScore}%",
                $"Word focus: {score.WordQualityScore}%",
                $"Exact words: {score.ExactWordCount} | Approximate words: {score.ApproximateWordCount} | Missed words: {score.MissedWordCount}"
            };

            if (!string.IsNullOrWhiteSpace(score.EstimateBasisText))
                lines.Add(score.EstimateBasisText);

            string phonemeAlignment = PhonemeAlignmentTextFormatter.BuildPreviewText(lastPhonemeAlignment);
            if (!string.IsNullOrWhiteSpace(phonemeAlignment))
                lines.Add(phonemeAlignment);

            if (!string.IsNullOrWhiteSpace(wordBreakdown))
                lines.Add(wordBreakdown);

            lines.Add("Focus next:");
            lines.Add(focus);
            if (!string.IsNullOrWhiteSpace(score.FeedbackText))
                lines.Add(score.FeedbackText);

            return string.Join("\n", lines);
        }

        private static string BuildFocusNextText(PronunciationScoreResult score)
        {
            if (!score.IsAvailable)
                return string.Empty;

            if (score.MissingWordCount > 0)
            {
                if (score.MissingWordCount == 1)
                    return "Focus next: say the missing word slowly once, then repeat the full line.";

                return $"Focus next: say the {score.MissingWordCount} missing words slowly once, then repeat the full line.";
            }

            if (score.ExtraWordCount > 0)
            {
                if (score.ExtraWordCount == 1)
                    return "Focus next: drop the extra word and keep the line tighter.";

                return $"Focus next: trim the {score.ExtraWordCount} extra words and keep the line cleaner.";
            }

            if (score.TempoScore <= 45)
                return "Focus next: slow down a little so each word lands cleanly.";

            if (score.TempoScore >= 90 && score.OverallScore < 100)
                return "Focus next: keep the pace steady and natural.";

            if (string.Equals(score.ConfidenceBand, "low", StringComparison.OrdinalIgnoreCase))
                return "Focus next: try a cleaner full-line repeat.";

            return "Focus next: keep the same clear rhythm on the next mission.";
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }

        private static string BuildWordBreakdown(System.Collections.Generic.IReadOnlyList<PronunciationWordScore> wordScores)
        {
            if (wordScores == null || wordScores.Count == 0)
                return string.Empty;

            int limit = Mathf.Min(5, wordScores.Count);
            var parts = new System.Collections.Generic.List<string>(limit);
            for (int i = 0; i < limit; i++)
            {
                PronunciationWordScore wordScore = wordScores[i];
                string marker = wordScore.Kind == PronunciationMatchKind.Exact
                    ? "exact"
                    : wordScore.Kind == PronunciationMatchKind.Alternative
                        ? "alt"
                        : wordScore.Kind == PronunciationMatchKind.Fuzzy
                            ? "fuzzy"
                            : "miss";
                parts.Add($"{wordScore.Word}: {wordScore.Score}% {marker}");
            }

            return $"Word focus: {string.Join(" | ", parts)}";
        }

        private static string BuildApproximateMatchText(PronunciationScoreResult score)
        {
            if (score == null || !score.IsAvailable)
                return string.Empty;

            int approximateCount = score.ApproximateWordCount;

            if (approximateCount <= 0)
                return string.Empty;

            return approximateCount == 1
                ? "Approximate matches: 1 word"
                : $"Approximate matches: {approximateCount} words";
        }

        private static string BuildMatchQualityText(PronunciationScoreResult score)
        {
            if (score == null || !score.IsAvailable)
                return string.Empty;

            int exactCount = score.ExactWordCount;
            int approximateCount = score.ApproximateWordCount;
            int missingCount = score.MissedWordCount;

            if (exactCount == 0 && approximateCount == 0 && missingCount == 0)
                return string.Empty;

            return $"Match quality: {exactCount} exact | {approximateCount} approximate | {missingCount} missed";
        }

        private static string BuildCategoryLessonLabel(SpeechExerciseSO exercise)
        {
            if (exercise == null)
                return string.Empty;

            string raw = !string.IsNullOrWhiteSpace(exercise.Prompt)
                ? exercise.Prompt.Trim()
                : !string.IsNullOrWhiteSpace(exercise.name)
                    ? exercise.name.Trim()
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            const int maxLength = 28;
            if (raw.Length <= maxLength)
                return raw;

            return raw.Substring(0, maxLength - 3).TrimEnd() + "...";
        }

        private SpeechExerciseSO ResolveExercise(SpeechExerciseSO selectedExercise)
        {
            if (exerciseCatalog == null || exerciseCatalog.Count == 0)
                return selectedExercise;

            if (selectedExercise != null)
            {
                for (int categoryIndex = 0; categoryIndex < Mathf.Max(1, exerciseCatalog.CategoryCount); categoryIndex++)
                {
                    int count = exerciseCatalog.GetCategoryExerciseCount(categoryIndex);
                    for (int exerciseIndex = 0; exerciseIndex < count; exerciseIndex++)
                    {
                        if (exerciseCatalog.GetCategoryExercise(categoryIndex, exerciseIndex) == selectedExercise)
                        {
                            currentCategoryIndex = categoryIndex;
                            currentExerciseIndex = exerciseIndex;
                            return selectedExercise;
                        }
                    }
                }
            }

            currentCategoryIndex = ResolveCategoryIndex(currentCategoryIndex);
            currentExerciseIndex = Mathf.Clamp(currentExerciseIndex, 0, Mathf.Max(0, GetCurrentExerciseCount() - 1));
            return exerciseCatalog.GetCategoryExercise(currentCategoryIndex, currentExerciseIndex);
        }

        private void UpdateMicControlState()
        {
            if (view == null)
                return;

            bool canPressMic =
                activeService != null
                && (activeService.IsListening
                    || (session.CanStartRecording && (activeService.IsReady || useMock)));

            view.SetMicInteractable(canPressMic);
        }

        private int ResolveCategoryIndex(int selectedCategoryIndex)
        {
            if (exerciseCatalog == null || exerciseCatalog.CategoryCount == 0)
                return 0;

            return Mathf.Clamp(selectedCategoryIndex, 0, exerciseCatalog.CategoryCount - 1);
        }

        private int GetCurrentExerciseCount()
        {
            if (exerciseCatalog == null)
                return 0;

            int categoryCount = exerciseCatalog.GetCategoryExerciseCount(currentCategoryIndex);
            return categoryCount > 0 ? categoryCount : exerciseCatalog.Count;
        }

        private bool HasNextExercise()
        {
            int exerciseCount = GetCurrentExerciseCount();
            return exerciseCount > 0 && currentExerciseIndex < exerciseCount - 1;
        }
    }
}
