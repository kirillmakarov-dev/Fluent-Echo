using System;
using System.Collections.Generic;
using FluentEcho.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FluentEcho.Views
{
    public sealed class FluentEchoView : MonoBehaviour, IFluentEchoView
    {
        private static readonly Color NoticeFill = new(0.10f, 0.16f, 0.18f, 0.96f);
        private static readonly Color NoticeBorder = new(0.20f, 0.90f, 0.68f, 0.35f);
        private static readonly Color NoticeAccent = new(0.20f, 0.90f, 0.68f, 0.88f);
        private static readonly Color NoticeActionFill = new(0.20f, 0.90f, 0.68f, 1f);
        private static readonly Color NoticeActionText = new(0.06f, 0.13f, 0.15f, 1f);

        [SerializeField] private TextMeshProUGUI promptLabel;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private TextMeshProUGUI lessonPositionLabel;
        [SerializeField] private Image lessonProgressFill;
        [SerializeField] private TextMeshProUGUI progressDetailsLabel;
        [SerializeField] private TextMeshProUGUI pronunciationSummaryLabel;
        [SerializeField] private TextMeshProUGUI pronunciationFeedbackLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI transcriptLabel;
        [SerializeField] private TextMeshProUGUI micButtonLabel;
        [SerializeField] private TextMeshProUGUI modeLabel;
        [SerializeField] private RectTransform wordContainer;
        [SerializeField] private WordChipView wordChipPrefab;
        [SerializeField] private Button micButton;
        [SerializeField] private Button demoButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button listenButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button categoriesButton;
        [SerializeField] private Button resultCloseButton;
        [SerializeField] private Button resultNextButton;
        [SerializeField] private Button resultTryAgainButton;
        [SerializeField] private GameObject noticePanel;
        [SerializeField] private CanvasGroup noticePanelGroup;
        [SerializeField] private TextMeshProUGUI noticeTitleLabel;
        [SerializeField] private TextMeshProUGUI noticeBodyLabel;
        [SerializeField] private Button noticeActionButton;
        [SerializeField] private TextMeshProUGUI noticeActionLabel;
        [SerializeField] private Button wordsCategoryButton;
        [SerializeField] private Button shortSentencesCategoryButton;
        [SerializeField] private Button challengeCategoryButton;
        [SerializeField] private Dropdown lessonDropdown;
        [SerializeField] private Toggle mockModeToggle;
        [SerializeField] private Image recordingIndicator;
        [SerializeField] private Image successPanel;
        [SerializeField] private CanvasGroup successPanelGroup;
        [SerializeField] private RectTransform successPanelRect;
        [SerializeField] private GameObject categoryScreen;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TextMeshProUGUI selectedCategoryLabel;
        [SerializeField] private TextMeshProUGUI selectedCategoryProgressLabel;
        [SerializeField] private TextMeshProUGUI selectedCategoryDescriptionLabel;
        [SerializeField] private RectTransform noticeCard;

        [Header("Result Motion")]
        [SerializeField, Min(0f)] private float resultEnterDuration = 0.22f;
        [SerializeField, Range(0.8f, 1f)] private float resultEnterScale = 0.965f;

        private readonly List<WordChipView> chips = new();
        private Coroutine resultAnimation;

        public event Action MicPressed;
        public event Action DemoPressed;
        public event Action RetryPressed;
        public event Action ListenPressed;
        public event Action PreviousPressed;
        public event Action NextPressed;
        public event Action CategoriesPressed;
        public event Action<int> CategorySelected;
        public event Action<int> LessonSelected;
        public event Action<bool> MockModeChanged;
        public event Action NoticeConfirmed;

        public void ConfigureWordChipPrefab(WordChipView prefab)
        {
            wordChipPrefab = prefab;
        }
        public void ConfigureProgressLabel(TextMeshProUGUI label)
        {
            progressLabel = label;
        }

        public void ConfigureProgressDetailsLabel(TextMeshProUGUI label)
        {
            progressDetailsLabel = label;
        }

        public void ConfigurePronunciationLabels(
            TextMeshProUGUI summaryLabel,
            TextMeshProUGUI feedbackLabel)
        {
            pronunciationSummaryLabel = summaryLabel;
            pronunciationFeedbackLabel = feedbackLabel;
        }

        private void Awake()
        {
            EnsureCategoriesButton();
            micButton.onClick.AddListener(() => MicPressed?.Invoke());
            demoButton.onClick.AddListener(() => DemoPressed?.Invoke());
            retryButton.onClick.AddListener(() => RetryPressed?.Invoke());
            listenButton.onClick.AddListener(() => ListenPressed?.Invoke());
            if (previousButton != null)
                previousButton.onClick.AddListener(() => PreviousPressed?.Invoke());

            if (nextButton != null)
                nextButton.onClick.AddListener(() => NextPressed?.Invoke());

            if (categoriesButton != null)
                categoriesButton.onClick.AddListener(() => CategoriesPressed?.Invoke());

            if (resultCloseButton != null)
                resultCloseButton.onClick.AddListener(HideResultPanel);

            if (resultNextButton != null)
                resultNextButton.onClick.AddListener(() => NextPressed?.Invoke());

            if (resultTryAgainButton != null)
                resultTryAgainButton.onClick.AddListener(() => RetryPressed?.Invoke());

            if (wordsCategoryButton != null)
                wordsCategoryButton.onClick.AddListener(() => CategorySelected?.Invoke(0));

            if (shortSentencesCategoryButton != null)
                shortSentencesCategoryButton.onClick.AddListener(() => CategorySelected?.Invoke(1));

            if (challengeCategoryButton != null)
                challengeCategoryButton.onClick.AddListener(() => CategorySelected?.Invoke(2));

            if (lessonDropdown != null)
                lessonDropdown.onValueChanged.AddListener(index => LessonSelected?.Invoke(index));

            if (mockModeToggle != null)
                mockModeToggle.onValueChanged.AddListener(value => MockModeChanged?.Invoke(value));
            EnsureResultAnimationReferences();
            EnsureNoticePanel();
        }

        private void EnsureCategoriesButton()
        {
            if (categoriesButton == null)
            {
                Debug.LogWarning(
                    "[FluentEchoView] Category Back Button is not assigned in the scene.",
                    this);
                return;
            }

            ConfigureCategoriesButton(categoriesButton);
        }

        private static void ConfigureCategoriesButton(Button button)
        {
            if (button == null)
                return;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "CATEGORIES";
                label.fontSize = 14;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        public void Build(string prompt, string[] targetWords)
        {
            if (wordChipPrefab == null)
                throw new InvalidOperationException(
                    "FluentEchoView requires a WordChipView prefab. Rebuild the prototype scene.");

            promptLabel.text = prompt;
            for (int i = wordContainer.childCount - 1; i >= 0; i--)
                Destroy(wordContainer.GetChild(i).gameObject);

            chips.Clear();
            for (int i = 0; i < targetWords.Length; i++)
            {
                WordChipView chip = Instantiate(wordChipPrefab, wordContainer);
                chip.gameObject.SetActive(true);
                chip.Configure(targetWords[i]);
                chips.Add(chip);
            }
        }

        public void SetProgress(string progress)
        {
            if (progressLabel != null && progressLabel != lessonPositionLabel)
                progressLabel.text = string.IsNullOrWhiteSpace(progress)
                    ? FluentEchoCopy.FirstProgressSummary
                    : progress;
        }

        public void SetLessonPosition(int currentLesson, int totalLessons)
        {
            int safeTotal = Mathf.Max(1, totalLessons);
            int safeCurrent = Mathf.Clamp(currentLesson, 1, safeTotal);

            if (lessonPositionLabel != null)
                lessonPositionLabel.text = $"Lesson {safeCurrent} / {safeTotal}";

            if (lessonProgressFill != null)
            {
                RectTransform fillRect = lessonProgressFill.rectTransform;
                Vector2 anchorMax = fillRect.anchorMax;
                anchorMax.x = safeCurrent / (float) safeTotal;
                fillRect.anchorMax = anchorMax;
            }
        }

        public void SetLessonOptions(string[] lessonNames, int selectedIndex)
        {
            if (lessonDropdown == null)
                return;

            lessonDropdown.options.Clear();
            if (lessonNames != null)
            {
                for (int i = 0; i < lessonNames.Length; i++)
                    lessonDropdown.options.Add(new Dropdown.OptionData(lessonNames[i]));
            }

            int safeIndex = lessonDropdown.options.Count == 0
                ? 0
                : Mathf.Clamp(selectedIndex, 0, lessonDropdown.options.Count - 1);
            lessonDropdown.SetValueWithoutNotify(safeIndex);
            if (lessonDropdown.captionText != null && lessonDropdown.options.Count > 0)
                lessonDropdown.captionText.text = lessonDropdown.options[safeIndex].text;
        }

        public void SetCategory(string categoryName, string categoryDescription)
        {
            if (selectedCategoryLabel != null)
                selectedCategoryLabel.text = string.IsNullOrWhiteSpace(categoryName)
                    ? "Practice Menu"
                    : categoryName;

            if (selectedCategoryDescriptionLabel != null)
                selectedCategoryDescriptionLabel.text = string.IsNullOrWhiteSpace(categoryDescription)
                    ? "Pick a lesson set and practice at your own pace."
                    : categoryDescription;
        }

        public void SetCategoryProgress(string categoryProgress)
        {
            if (selectedCategoryProgressLabel != null)
                selectedCategoryProgressLabel.text = string.IsNullOrWhiteSpace(categoryProgress)
                    ? FluentEchoCopy.FirstProgressSummary
                    : categoryProgress;
        }

        public void SetCategoryScreenVisible(bool visible)
        {
            if (categoryScreen != null)
                categoryScreen.SetActive(visible);
        }

        public void SetSettingsPanelVisible(bool visible)
        {
            if (settingsPanel == null)
            {
                Debug.LogWarning("[FluentEchoView] Settings Panel is not assigned in the scene.", this);
                return;
            }

            settingsPanel.SetActive(visible);
        }

        public void ShowNotice(string title, string body, string primaryActionLabel)
        {
            EnsureNoticePanel();
            if (noticePanel == null)
                return;

            if (noticeTitleLabel != null)
                noticeTitleLabel.text = string.IsNullOrWhiteSpace(title) ? "Notice" : title;

            if (noticeBodyLabel != null)
                noticeBodyLabel.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body;

            if (noticeActionLabel != null)
                noticeActionLabel.text = string.IsNullOrWhiteSpace(primaryActionLabel) ? "CONTINUE" : primaryActionLabel;

            noticePanel.SetActive(true);
            if (noticePanelGroup != null)
            {
                noticePanelGroup.alpha = 1f;
                noticePanelGroup.interactable = true;
                noticePanelGroup.blocksRaycasts = true;
            }
        }

        public void HideNotice()
        {
            if (noticePanel == null)
                return;

            if (noticePanelGroup != null)
            {
                noticePanelGroup.alpha = 0f;
                noticePanelGroup.interactable = false;
                noticePanelGroup.blocksRaycasts = false;
            }

            noticePanel.SetActive(false);
        }

        public void SetProgressDetails(string details)
        {
            if (progressDetailsLabel != null)
                progressDetailsLabel.text = string.IsNullOrWhiteSpace(details)
                    ? FluentEchoCopy.FirstLocalEstimateText
                    : details;
        }

        public void SetPronunciation(string summary, string feedback)
        {
            if (pronunciationSummaryLabel != null)
                pronunciationSummaryLabel.text = string.IsNullOrWhiteSpace(summary)
                    ? FluentEchoCopy.FirstPronunciationSummary
                    : summary;

            if (pronunciationFeedbackLabel != null)
                pronunciationFeedbackLabel.text = string.IsNullOrWhiteSpace(feedback)
                    ? FluentEchoCopy.BuildUnavailablePronunciationDetails()
                    : feedback;
        }

        public void SetStatus(string status)
        {
            if (statusLabel == null)
                return;

            if (string.IsNullOrWhiteSpace(status))
            {
                statusLabel.text = string.Empty;
                return;
            }

            if (status.StartsWith("No microphone device", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.text = FluentEchoCopy.MicrophoneNotFoundStatus;
                return;
            }

            if (status.Contains("permission", StringComparison.OrdinalIgnoreCase)
                || status.StartsWith("The microphone could not start", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.text = FluentEchoCopy.MicrophonePermissionNeededStatus;
                return;
            }

            if (status.StartsWith("Speech model is missing", StringComparison.OrdinalIgnoreCase)
                || status.StartsWith("Speech model failed to load", StringComparison.OrdinalIgnoreCase)
                || status.StartsWith("The selected speech model could not load", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.text = FluentEchoCopy.SpeechModelMissingStatus;
                return;
            }

            if (status.StartsWith("Could not check this attempt", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.text = FluentEchoCopy.CouldNotCheckAttemptStatus;
                return;
            }

            if (status.StartsWith("I did not catch that", StringComparison.OrdinalIgnoreCase)
                || status.StartsWith("No clear speech", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.text = FluentEchoCopy.DidNotCatchThatStatus;
                return;
            }

            if (status.StartsWith("Whisper ready", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = FluentEchoCopy.ReadyToPracticeStatus;
            else if (status.StartsWith("Loading", StringComparison.OrdinalIgnoreCase)
                     || status.StartsWith("Warming", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = FluentEchoCopy.PreparingSpeechModelStatus;
            else if (status.StartsWith("Analyzing", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = FluentEchoCopy.CheckingPronunciationStatus;
            else if (status.StartsWith("Excellent", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = FluentEchoCopy.GreatWorkAcceptedStatus;
            else
                statusLabel.text = status;
        }

        public void SetTranscript(string transcript)
        {
            transcriptLabel.text = string.IsNullOrWhiteSpace(transcript)
                ? "Your transcript will appear here."
                : $"\"{transcript.Trim()}\"";
        }

        public void SetWordMatches(bool[] matches)
        {
            for (int i = 0; i < chips.Count; i++)
                chips[i].SetMatched(matches != null && i < matches.Length && matches[i]);
        }

        public void SetListening(bool listening)
        {
            recordingIndicator.gameObject.SetActive(listening);
            micButtonLabel.text = listening ? "CHECK ANSWER" : "START SPEAKING";
        }

        public void SetMicInteractable(bool interactable)
        {
            if (micButton != null)
                micButton.interactable = interactable;
        }

        public void SetSuccess(bool success)
        {
            SetResultPanelVisible(success);
            retryButton.gameObject.SetActive(success);
        }

        public void SetMode(bool mockMode)
        {
            if (mockModeToggle != null)
                mockModeToggle.SetIsOnWithoutNotify(mockMode);

            modeLabel.text = mockMode ? "DEMO ENGINE" : "LOCAL WHISPER";
        }

        public void SetNavigation(bool canGoPrevious, bool canGoNext)
        {
            if (previousButton != null)
                previousButton.interactable = canGoPrevious;

            if (nextButton != null)
                nextButton.interactable = canGoNext;

            if (resultNextButton != null)
                resultNextButton.interactable = canGoNext;
        }

        private void HideResultPanel()
        {
            SetResultPanelVisible(false);
        }

        private void SetResultPanelVisible(bool visible)
        {
            if (successPanel == null)
                return;

            EnsureResultAnimationReferences();
            if (resultAnimation != null)
                StopCoroutine(resultAnimation);

            if (!visible)
            {
                if (successPanelGroup != null)
                    successPanelGroup.alpha = 0f;
                successPanel.gameObject.SetActive(false);
                return;
            }

            successPanel.gameObject.SetActive(true);
            if (successPanelGroup != null)
                successPanelGroup.alpha = 0f;
            if (successPanelRect != null)
                successPanelRect.localScale = new Vector3(resultEnterScale, resultEnterScale, 1f);

            resultAnimation = StartCoroutine(AnimateResultPanelIn());
        }

        private void EnsureResultAnimationReferences()
        {
            if (successPanel == null)
                return;

            if (successPanelRect == null)
                successPanelRect = successPanel.GetComponent<RectTransform>();

            if (successPanelGroup == null)
            {
                successPanelGroup = successPanel.GetComponent<CanvasGroup>();
                if (successPanelGroup == null)
                    successPanelGroup = successPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void EnsureNoticePanel()
        {
            if (noticePanel == null)
            {
                Debug.LogWarning("[FluentEchoView] Notice Panel is not assigned in the scene.", this);
                return;
            }

            if (noticePanelGroup == null)
            {
                Debug.LogWarning("[FluentEchoView] Notice Panel is missing a CanvasGroup.", this);
                return;
            }

            if (noticeCard == null)
            {
                Debug.LogWarning("[FluentEchoView] Notice Card is not assigned in the scene.", this);
                return;
            }

            Image image = noticeCard.GetComponent<Image>();
            if (image != null)
                image.color = NoticeFill;

            Outline outline = noticeCard.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = NoticeBorder;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            CanvasGroup cardGroup = noticeCard.GetComponent<CanvasGroup>();
            if (cardGroup != null)
            {
                cardGroup.interactable = true;
                cardGroup.blocksRaycasts = true;
            }

            Transform accent = noticeCard.Find("Notice Accent");
            if (accent != null && accent.TryGetComponent(out Image accentImage))
                accentImage.color = NoticeAccent;

            if (noticeTitleLabel == null || noticeBodyLabel == null || noticeActionButton == null || noticeActionLabel == null)
            {
                Debug.LogWarning("[FluentEchoView] Notice Panel is missing one or more child UI objects.", this);
                return;
            }

            noticeActionButton.onClick.RemoveAllListeners();
            noticeActionButton.onClick.AddListener(() => NoticeConfirmed?.Invoke());

            HideNotice();
        }

        private static TextMeshProUGUI CreateNoticeText(
            Transform parent,
            string name,
            string value,
            float size,
            FontStyles style,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateNoticeButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color background,
            Color foreground)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = background;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.16f);
            button.colors = colors;

            CreateNoticeText(
                buttonObject.transform,
                "Label",
                label,
                14,
                FontStyles.Bold,
                foreground,
                new Vector2(0.03f, 0.12f),
                new Vector2(0.97f, 0.90f),
                TextAlignmentOptions.Center);
            return button;
        }

        private static Transform FindDeepTransform(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Transform match = FindDeepTransform(child, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private System.Collections.IEnumerator AnimateResultPanelIn()
        {
            float duration = Mathf.Max(0.01f, resultEnterDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (successPanelGroup != null)
                    successPanelGroup.alpha = eased;

                if (successPanelRect != null)
                {
                    float scale = Mathf.Lerp(resultEnterScale, 1f, eased);
                    successPanelRect.localScale = new Vector3(scale, scale, 1f);
                }

                yield return null;
            }

            if (successPanelGroup != null)
                successPanelGroup.alpha = 1f;
            if (successPanelRect != null)
                successPanelRect.localScale = Vector3.one;
            resultAnimation = null;
        }
    }
}
