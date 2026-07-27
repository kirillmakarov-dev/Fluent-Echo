using System;

namespace FluentEcho.Domain
{
    public enum SpeechSessionPhase
    {
        Idle,
        Preparing,
        Listening,
        Analyzing,
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

        public void SetPhase(SpeechSessionPhase phase)
        {
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
            Phase = SpeechSessionPhase.Idle;
            Changed?.Invoke();
        }
    }
}
