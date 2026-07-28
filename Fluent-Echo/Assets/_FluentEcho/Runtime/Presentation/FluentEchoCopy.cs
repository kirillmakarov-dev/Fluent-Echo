namespace FluentEcho.Presentation
{
    public static class FluentEchoCopy
    {
        public const string FirstProgressSummary = "Progress: ready for first recording";
        public const string FirstLocalEstimateText = "Your first local estimate will appear here.";
        public const string FirstCoachingTipText = "Your first coaching tip will appear here.";
        public const string FirstPronunciationSummary = "PRACTICE SCORE | READY FOR YOUR FIRST ATTEMPT";
        public const string FirstEstimatePrompt = "Speak once to generate the first local estimate.";
        public const string PhonemeRoadmapText = "Phoneme roadmap: local heuristic estimate only, with true phoneme scoring planned next.";

        public static string BuildProgressSummary(int totalWords)
        {
            if (totalWords <= 0)
                return FirstProgressSummary;

            return $"{FirstProgressSummary} | 0/{totalWords} matched";
        }

        public static string BuildUnavailablePronunciationDetails() =>
            $"{FirstEstimatePrompt}\n{FirstCoachingTipText}\n{PhonemeRoadmapText}";
    }
}
