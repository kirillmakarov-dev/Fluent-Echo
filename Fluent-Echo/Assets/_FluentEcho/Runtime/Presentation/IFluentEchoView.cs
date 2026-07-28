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
        void SetMicInteractable(bool interactable);
        void SetSuccess(bool success);
        void SetMode(bool mockMode);
        void SetNavigation(bool canGoPrevious, bool canGoNext);
        void SetLessonPosition(int currentLesson, int totalLessons);
        void SetLessonOptions(string[] lessonNames, int selectedIndex);
        void SetCategory(string categoryName, string categoryDescription);
        void SetCategoryProgress(string categoryProgress);
        void SetCategoryScreenVisible(bool visible);
        void SetSettingsPanelVisible(bool visible);
        void ShowNotice(string title, string body, string primaryActionLabel);
        void HideNotice();
        void SetProgressDetails(string details);
        void SetPronunciation(string summary, string feedback);
        void SetPronunciationConfidence(string confidence);
        void SetPronunciationBreakdown(string wordMatch, string rhythm);
    }
}
