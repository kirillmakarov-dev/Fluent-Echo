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
        [SerializeField] private Button resultCloseButton;
        [SerializeField] private Button resultNextButton;
        [SerializeField] private Button resultTryAgainButton;
        [SerializeField] private Toggle mockModeToggle;
        [SerializeField] private Image recordingIndicator;
        [SerializeField] private Image successPanel;
        [SerializeField] private CanvasGroup successPanelGroup;
        [SerializeField] private RectTransform successPanelRect;

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
        public event Action<bool> MockModeChanged;

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
            micButton.onClick.AddListener(() => MicPressed?.Invoke());
            demoButton.onClick.AddListener(() => DemoPressed?.Invoke());
            retryButton.onClick.AddListener(() => RetryPressed?.Invoke());
            listenButton.onClick.AddListener(() => ListenPressed?.Invoke());
            if (previousButton != null)
                previousButton.onClick.AddListener(() => PreviousPressed?.Invoke());

            if (nextButton != null)
                nextButton.onClick.AddListener(() => NextPressed?.Invoke());

            if (resultCloseButton != null)
                resultCloseButton.onClick.AddListener(HideResultPanel);

            if (resultNextButton != null)
                resultNextButton.onClick.AddListener(() => NextPressed?.Invoke());

            if (resultTryAgainButton != null)
                resultTryAgainButton.onClick.AddListener(() => RetryPressed?.Invoke());

            mockModeToggle.onValueChanged.AddListener(value => MockModeChanged?.Invoke(value));
            EnsureResultAnimationReferences();
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
                progressLabel.text = progress;
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

        public void SetProgressDetails(string details)
        {
            if (progressDetailsLabel != null)
                progressDetailsLabel.text = string.IsNullOrWhiteSpace(details)
                    ? "Recent attempts will appear here."
                    : details;
        }

        public void SetPronunciation(string summary, string feedback)
        {
            if (pronunciationSummaryLabel != null)
                pronunciationSummaryLabel.text = summary ?? string.Empty;

            if (pronunciationFeedbackLabel != null)
                pronunciationFeedbackLabel.text = feedback ?? string.Empty;
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

            if (status.StartsWith("Whisper ready", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = "Ready when you are.";
            else if (status.StartsWith("Loading", StringComparison.OrdinalIgnoreCase)
                     || status.StartsWith("Warming", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = "Preparing local model...";
            else if (status.StartsWith("Analyzing", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = "Analyzing your speech...";
            else if (status.StartsWith("Excellent", StringComparison.OrdinalIgnoreCase))
                statusLabel.text = "Answer accepted.";
            else
                statusLabel.text = status;
        }

        public void SetTranscript(string transcript)
        {
            transcriptLabel.text = string.IsNullOrWhiteSpace(transcript)
                ? "Your recognized sentence will appear here."
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
            micButtonLabel.text = listening ? "STOP & CHECK" : "START SPEAKING";
        }

        public void SetSuccess(bool success)
        {
            SetResultPanelVisible(success);
            retryButton.gameObject.SetActive(success);
        }

        public void SetMode(bool mockMode)
        {
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
