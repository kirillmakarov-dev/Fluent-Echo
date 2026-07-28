using System;

namespace FluentEcho.Presentation
{
    public interface IFluentEchoView
    {
        event Action NoticeConfirmed;
        event Action MicPressed;
        event Action DemoPressed;
        event Action RetryPressed;
        event Action ListenPressed;
        event Action PreviousPressed;
        event Action NextPressed;
        event Action CategoriesPressed;
        event Action<int> CategorySelected;
        event Action<int> LessonSelected;
        event Action<bool> MockModeChanged;

        void Build(string prompt, string[] targetWords);
        void SetProgress(string progress);
        void SetStatus(string status);
        void SetTranscript(string transcript);
        void SetWordMatches(bool[] matches);
        void SetListening(bool listening);
        void SetSuccess(bool success);
        void SetMode(bool mockMode);
        void SetNavigation(bool canGoPrevious, bool canGoNext);
        void SetLessonPosition(int currentLesson, int totalLessons);
        void SetLessonOptions(string[] lessonNames, int selectedIndex);
        void SetCategory(string categoryName, string categoryDescription);
        void SetCategoryScreenVisible(bool visible);
        void SetSettingsPanelVisible(bool visible);
        void ShowNotice(string title, string body, string primaryActionLabel);
        void HideNotice();
        void SetProgressDetails(string details);
        void SetPronunciation(string summary, string feedback);
    }

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
