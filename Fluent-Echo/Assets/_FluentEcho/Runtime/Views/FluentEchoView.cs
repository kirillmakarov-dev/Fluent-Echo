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
        [SerializeField] private Toggle mockModeToggle;
        [SerializeField] private Image recordingIndicator;
        [SerializeField] private Image successPanel;

        private readonly List<WordChipView> chips = new();

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

            mockModeToggle.onValueChanged.AddListener(value => MockModeChanged?.Invoke(value));
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
            if (progressLabel != null)
                progressLabel.text = progress;
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

        public void SetStatus(string status) => statusLabel.text = status;

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
            successPanel.gameObject.SetActive(success);
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
        }
    }
}
