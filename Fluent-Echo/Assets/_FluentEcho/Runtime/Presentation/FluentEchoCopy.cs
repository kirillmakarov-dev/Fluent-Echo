namespace FluentEcho.Presentation
{
    public static class FluentEchoCopy
    {
        public const string FirstProgressSummary = "Progress: ready for first recording";
        public const string FirstLocalEstimateText = "Your first practice score will appear here.";
        public const string FirstCoachingTipText = "Your first coaching tip will appear here.";
        public const string FirstPronunciationSummary = "PRACTICE SCORE | READY FOR YOUR FIRST ATTEMPT";
        public const string FirstEstimatePrompt = "Speak once to generate the first practice score.";
        public const string OnboardingWelcomeTitle = "Practice English privately";
        public const string OnboardingWelcomeBody = "Fluent Echo listens on this device and uses a local speech model.";
        public const string OnboardingPracticePathsTitle = "Choose your practice path";
        public const string OnboardingPracticePathsBody = "Words build clarity, short sentences build rhythm, and challenge sentences build fluency. Your voice stays local either way.";
        public const string PhonemeRoadmapText = "True phoneme scoring is planned for a future sprint.";
        public const string ReadyToPracticeStatus = "Ready to practice.";
        public const string DemoModeReadyStatus = "Demo mode is ready. Press Start Speaking to preview a correct answer.";
        public const string PreparingSpeechModelStatus = "Preparing speech model...";
        public const string LoadingSpeechEngineStatus = "Loading speech engine...";
        public const string ExcellentResultStatus = "Excellent. Every target word was recognized.";
        public const string AnalyzingSpeechStatus = "Analyzing speech locally...";
        public const string CheckingPronunciationStatus = "Checking your pronunciation...";
        public const string GreatWorkAcceptedStatus = "Great work. Answer accepted.";
        public const string CouldNotCheckAttemptStatus = "Could not check this attempt.";
        public const string CouldNotCheckAttemptTitle = "Could not check this attempt";
        public const string DidNotCatchThatStatus = "I did not catch that.";
        public const string DidNotCatchThatTitle = "I did not catch that";
        public const string DidNotCatchThatDetailedStatus = "I did not catch that. Check the microphone and try again.";
        public const string AFewWordsAreMissingStatus = "A few words are missing. Review the highlights and try again.";
        public const string MicrophoneNotFoundStatus = "Microphone not found.";
        public const string MicrophoneNotFoundTitle = "Microphone not found";
        public const string MicrophonePermissionNeededStatus = "Microphone permission needed.";
        public const string MicrophonePermissionNeededTitle = "Microphone permission needed";
        public const string SpeechModelMissingStatus = "Speech model missing.";
        public const string SpeechModelMissingTitle = "Speech model missing";
        public const string NextMissionPrompt = "Choose NEXT MISSION to continue, or TRY AGAIN to improve the score.";

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
