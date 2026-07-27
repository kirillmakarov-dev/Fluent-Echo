using System;

namespace FluentEcho.Domain
{
    public readonly struct SpeechMatchResult
    {
        public SpeechMatchResult(bool isComplete, bool[] matchedWords)
        {
            IsComplete = isComplete;
            MatchedWords = matchedWords ?? Array.Empty<bool>();
        }

        public bool IsComplete { get; }
        public bool[] MatchedWords { get; }
    }
}
