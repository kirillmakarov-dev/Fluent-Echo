using System;

namespace FluentEcho.Domain
{
    public enum SpeechSessionPhase
    {
        Idle,
        Preparing,
        Listening,
        Analyzing,
        Cancelling,
        Success,
        Retry,
        Error
    }

    public sealed class SpeechSession
    {
        public event Action Changed;

        public SpeechSessionPhase Phase { get; private set; } = SpeechSessionPhase.Idle;
        public string Transcript { get; private set; } = string.Empty;
        public SpeechMatchResult MatchResult { get; private set; }
        public bool IsBusy => Phase is SpeechSessionPhase.Preparing
            or SpeechSessionPhase.Listening
            or SpeechSessionPhase.Analyzing
            or SpeechSessionPhase.Cancelling;

        public void BeginPreparing() => SetPhase(SpeechSessionPhase.Preparing);
        public void BeginListening() => SetPhase(SpeechSessionPhase.Listening);
        public void BeginAnalyzing() => SetPhase(SpeechSessionPhase.Analyzing);
        public void BeginCancelling() => SetPhase(SpeechSessionPhase.Cancelling);
        public void BeginSuccess() => SetPhase(SpeechSessionPhase.Success);
        public void BeginRetry() => SetPhase(SpeechSessionPhase.Retry);
        public void BeginError() => SetPhase(SpeechSessionPhase.Error);
        public void BeginIdle() => SetPhase(SpeechSessionPhase.Idle);

        public void SetPhase(SpeechSessionPhase phase)
        {
            if (Phase == phase)
                return;

            Phase = phase;
            Changed?.Invoke();
        }

        public void UpdateTranscript(string transcript, SpeechMatchResult result)
        {
            Transcript = transcript?.Trim() ?? string.Empty;
            MatchResult = result;
            Changed?.Invoke();
        }

        public void Reset()
        {
            Transcript = string.Empty;
            MatchResult = default;
            BeginIdle();
        }
    }
}
