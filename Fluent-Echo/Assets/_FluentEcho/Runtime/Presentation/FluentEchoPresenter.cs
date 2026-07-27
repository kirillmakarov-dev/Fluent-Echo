using System;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Services;
using UnityEngine;

namespace FluentEcho.Presentation
{
    public sealed class FluentEchoPresenter : IDisposable
    {
        private readonly SpeechExerciseSO exercise;
        private readonly IFluentEchoView view;
        private readonly ISpeechRecognitionService realService;
        private readonly ISpeechRecognitionService mockService;
        private readonly SpeechAnswerMatcher matcher = new();
        private readonly SpeechSession session = new();
        private readonly Action<AudioClip> playReference;

        private ISpeechRecognitionService activeService;
        private LessonProgressState progress;
        private bool useMock;
        private bool success;

        public FluentEchoPresenter(
            SpeechExerciseSO exercise,
            IFluentEchoView view,
            ISpeechRecognitionService realService,
            ISpeechRecognitionService mockService,
            bool useMockByDefault,
            Action<AudioClip> playReference)
        {
            this.exercise = exercise;
            this.view = view;
            this.realService = realService;
            this.mockService = mockService;
            useMock = useMockByDefault;
            this.playReference = playReference;
        }

        public void Initialize()
        {
            view.Build(exercise.Prompt, exercise.GetDisplayWords());
            progress = LessonProgressRepository.Load(exercise.ProgressKey, exercise.GetDisplayWords().Length);
            view.SetProgress(progress.GetSummaryText());
            view.MicPressed += HandleMicPressed;
            view.DemoPressed += HandleDemoPressed;
            view.RetryPressed += HandleRetry;
            view.ListenPressed += HandleListen;
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
            view.MockModeChanged -= HandleMockModeChanged;
        }

        private void HandleMicPressed()
        {
            if (success)
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
            view.SetWordMatches(new bool[exercise.GetDisplayWords().Length]);
            view.SetSuccess(false);
            view.SetStatus(useMock
                ? "Running deterministic demo..."
                : "Listening... Transcription appears in short local-processing chunks.");
            view.SetListening(true);
            session.SetPhase(SpeechSessionPhase.Listening);
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
            if (IsInteractionLocked())
                return;

            activeService?.Cancel();
            success = false;
            session.Reset();
            ResetView();
        }

        private void HandleListen()
        {
            if (IsInteractionLocked())
                return;

            if (exercise.ReferenceAudio != null)
                playReference?.Invoke(exercise.ReferenceAudio);
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

        private void SelectService(bool mock)
        {
            activeService?.Cancel();
            UnbindService();
            activeService = mock ? mockService : realService;
            if (activeService == null)
                throw new InvalidOperationException("Selected speech service is missing.");

            activeService.Configure(exercise);
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

        private void HandleTranscript(string transcript)
        {
            SpeechMatchResult result = matcher.Match(
                transcript,
                exercise.GetAcceptedWordGroups(),
                exercise.GetAcceptedPhrases(),
                exercise.RequireWordOrder,
                exercise.AllowFuzzyMatch);

            session.UpdateTranscript(transcript, result);
            view.SetTranscript(transcript);
            view.SetWordMatches(result.MatchedWords);

            if (!result.IsComplete || success)
                return;

            success = true;
            session.SetPhase(SpeechSessionPhase.Success);
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

            session.SetPhase(SpeechSessionPhase.Analyzing);
            view.SetStatus("Analyzing speech locally...");
            view.SetListening(false);
        }

        private void HandleListeningStopped()
        {
            view.SetListening(false);
            if (session.Phase == SpeechSessionPhase.Success
                || session.Phase == SpeechSessionPhase.Retry
                || session.Phase == SpeechSessionPhase.Analyzing)
            {
                SaveProgress();
            }

            if (success || session.Phase == SpeechSessionPhase.Error)
                return;

            session.SetPhase(SpeechSessionPhase.Retry);
            view.SetStatus(string.IsNullOrWhiteSpace(session.Transcript)
                ? "No speech was recognized. Check the microphone and try again."
                : "Some words are missing. Review the highlights and retry.");
        }

        private void HandleFailure(string message)
        {
            session.SetPhase(SpeechSessionPhase.Error);
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
                session.SetPhase(SpeechSessionPhase.Preparing);
            }
            else if (message.StartsWith("Whisper ready", StringComparison.OrdinalIgnoreCase))
            {
                if (session.Phase == SpeechSessionPhase.Preparing)
                    session.SetPhase(SpeechSessionPhase.Idle);
            }

            view.SetStatus(message);
        }

        private void ResetView()
        {
            session.Reset();
            success = false;
            view.SetTranscript(string.Empty);
            view.SetWordMatches(new bool[exercise.GetDisplayWords().Length]);
            view.SetProgress(progress?.GetSummaryText() ?? string.Empty);
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

            progress.RecordAttempt(session.Transcript, session.MatchResult, exercise.GetDisplayWords().Length);
            LessonProgressRepository.Save(progress);
            view.SetProgress(progress.GetSummaryText());
        }

        private bool IsInteractionLocked()
        {
            return activeService == null
                ? false
                : session.Phase == SpeechSessionPhase.Preparing
                  || session.Phase == SpeechSessionPhase.Analyzing
                  || activeService.IsListening;
        }
    }
}
