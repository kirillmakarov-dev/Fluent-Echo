using System;
using System.Collections.Generic;
using FluentEcho.Data;
using FluentEcho.Presentation;
using FluentEcho.Services;
using FluentEcho.Views;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Whisper.Utils;

namespace FluentEcho.Bootstrap
{
    public sealed class FluentEchoBootstrap : MonoBehaviour
    {
        private static readonly Color SettingsFill = new(0.11f, 0.24f, 0.28f, 1f);
        private static readonly Color SettingsBorder = new(0.20f, 0.90f, 0.68f, 0.24f);
        private static readonly Color SettingsAccent = new(0.20f, 0.90f, 0.68f, 0.9f);
        private static readonly Color ResultFill = new(0.94f, 0.89f, 0.80f, 1f);
        private static readonly Color ResultBorder = new(0.89f, 0.37f, 0.30f, 0.20f);
        private static readonly Color ResultAccent = new(0.89f, 0.37f, 0.30f, 0.92f);

        [SerializeField] private SpeechExerciseSO exercise;
        [SerializeField] private SpeechExerciseCatalogSO exerciseCatalog;
        [SerializeField] private FluentEchoView view;
        [SerializeField] private WhisperSpeechRecognitionService whisperService;
        [SerializeField] private MockSpeechRecognitionService mockService;
        [SerializeField] private Dropdown exerciseDropdown;
        [SerializeField] private Dropdown whisperProfileDropdown;
        [SerializeField] private string selectedCategoryPrefsKey = "FluentEcho.SelectedCategoryIndex";
        [SerializeField] private string selectedExercisePrefsKey = "FluentEcho.SelectedExerciseIndex";
        [SerializeField] private bool repairSceneUiOnStart;
        [SerializeField] private bool useMockByDefault;
        [SerializeField] private AudioSource audioSource;

        private FluentEchoPresenter presenter;

        public void Configure(
            SpeechExerciseSO configuredExercise,
            SpeechExerciseCatalogSO configuredExerciseCatalog,
            FluentEchoView configuredView,
            WhisperSpeechRecognitionService configuredWhisperService,
            MockSpeechRecognitionService configuredMockService,
            AudioSource configuredAudioSource,
            bool mockByDefault)
        {
            exercise = configuredExercise;
            exerciseCatalog = configuredExerciseCatalog;
            view = configuredView;
            whisperService = configuredWhisperService;
            mockService = configuredMockService;
            audioSource = configuredAudioSource;
            useMockByDefault = mockByDefault;
        }

        private void Awake()
        {
            if (exerciseCatalog != null && exerciseCatalog.Count > 0)
                exercise = ResolveSelectedExercise();

            if (exercise == null || view == null || whisperService == null || mockService == null)
            {
                var missing = new List<string>();
                if (exercise == null)
                    missing.Add(nameof(exercise));
                if (view == null)
                    missing.Add(nameof(view));
                if (whisperService == null)
                    missing.Add(nameof(whisperService));
                if (mockService == null)
                    missing.Add(nameof(mockService));

                Debug.LogError(
                    $"[FluentEchoBootstrap] Missing references: {string.Join(", ", missing)}.",
                    this);
                enabled = false;
                return;
            }

            EnsureExerciseDropdown();
            if (repairSceneUiOnStart)
            {
                EnsureMicrophoneDropdown();
                EnsureQualityDropdown();
                EnsureProgressLabel();
                EnsureProgressDetailsLabel();
                EnsurePronunciationLabels();
            }
            else
            {
                WireSceneOwnedUi();
            }

            presenter = new FluentEchoPresenter(
                exercise,
                exerciseCatalog,
                view,
                whisperService,
                mockService,
                useMockByDefault,
                PlayReference,
                ResolveSelectedCategoryIndex(),
                ResolveSelectedExerciseIndex(),
                PersistPracticeSelection);
            presenter.Initialize();
        }

        private void PlayReference(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            presenter = null;
        }

        private void WireSceneOwnedUi()
        {
            BindSceneMicrophoneDropdown();
            BindSceneWhisperProfileDropdown();
            WireExistingSettingsPanel();
            WireExistingResultPanel();
        }

        private void BindSceneMicrophoneDropdown()
        {
            if (whisperService == null || view == null)
                return;

            MicrophoneRecord microphone = whisperService.GetComponent<MicrophoneRecord>();
            if (microphone == null)
                return;

            Dropdown dropdown = microphone.microphoneDropdown;
            if (dropdown == null)
                dropdown = FindDeepTransform(view.transform.root, "Microphone Dropdown")?.GetComponent<Dropdown>();

            if (dropdown == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Scene-owned Microphone Dropdown is not assigned.",
                    this);
                return;
            }

            microphone.microphoneDropdown = dropdown;
            TextMeshProUGUI valueLabel = FindDeepTransform(view.transform.root, "Microphone Value")
                ?.GetComponent<TextMeshProUGUI>();
            PopulateDropdown(microphone, dropdown, valueLabel);
            UpdateMicrophoneValueLabel(microphone, valueLabel);
        }

        private void BindSceneWhisperProfileDropdown()
        {
            if (whisperService == null || view == null)
                return;

            Dropdown dropdown = whisperProfileDropdown;
            if (dropdown == null)
                dropdown = FindDeepTransform(view.transform.root, "Whisper Profile Dropdown")?.GetComponent<Dropdown>();

            if (dropdown == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Scene-owned Whisper Profile Dropdown is not assigned.",
                    this);
                return;
            }

            whisperProfileDropdown = dropdown;
            WhisperQualityProfile currentProfile = whisperService.CurrentQualityProfile;
            TextMeshProUGUI valueLabel = FindDeepTransform(view.transform.root, "Whisper Profile Value")
                ?.GetComponent<TextMeshProUGUI>();

            var options = new List<Dropdown.OptionData>();
            foreach (string name in Enum.GetNames(typeof(WhisperQualityProfile)))
                options.Add(new Dropdown.OptionData(name.ToUpperInvariant()));

            dropdown.options = options;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(index =>
            {
                WhisperQualityProfile profile = index switch
                {
                    1 => WhisperQualityProfile.Balanced,
                    2 => WhisperQualityProfile.Accurate,
                    _ => WhisperQualityProfile.Fast
                };

                whisperService.SetQualityProfile(profile);
                if (valueLabel != null)
                    valueLabel.text = profile.ToString().ToUpperInvariant();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            dropdown.SetValueWithoutNotify((int) currentProfile);
            if (dropdown.captionText != null)
                dropdown.captionText.text = currentProfile.ToString().ToUpperInvariant();

            if (valueLabel != null)
                valueLabel.text = currentProfile.ToString().ToUpperInvariant();
        }

        private void WireExistingSettingsPanel()
        {
            if (view == null)
                return;

            Transform settingsPanel = FindDeepTransform(view.transform.root, "Settings Panel");
            Button toggleButton = FindDeepTransform(view.transform.root, "Settings Toggle Button")
                ?.GetComponent<Button>();
            Button closeButton = settingsPanel != null
                ? settingsPanel.Find("Settings Close Button")?.GetComponent<Button>()
                : null;

            if (toggleButton != null && settingsPanel != null)
            {
                toggleButton.onClick.RemoveAllListeners();
                toggleButton.onClick.AddListener(() =>
                {
                    bool shouldOpen = !settingsPanel.gameObject.activeSelf;
                    settingsPanel.gameObject.SetActive(shouldOpen);
                });
            }

            if (closeButton != null && settingsPanel != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => settingsPanel.gameObject.SetActive(false));
            }
        }

        private void WireExistingResultPanel()
        {
            if (view == null)
                return;

            Transform resultPanel = FindDeepTransform(view.transform.root, "Result Panel");
            Button closeButton = resultPanel != null
                ? resultPanel.Find("Result Close Button")?.GetComponent<Button>()
                : null;
            if (closeButton == null || resultPanel == null)
                return;

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => resultPanel.gameObject.SetActive(false));
        }

        private void EnsureMicrophoneDropdown()
        {
            if (whisperService == null)
                return;

            MicrophoneRecord microphone = whisperService.GetComponent<MicrophoneRecord>();
            Transform settingsPanel = GetOrCreateSettingsPanel();
            if (settingsPanel == null)
                return;

            EnsureSettingsButton(settingsPanel.parent);
            EnsurePanelCloseButton(settingsPanel, "Settings Close Button", "CLOSE");

            TextMeshProUGUI title = MoveOrCreateText(
                settingsPanel,
                "Settings Title",
                "SPEECH SETTINGS",
                11,
                FontStyles.Bold,
                new Color(0.20f, 0.90f, 0.68f, 1f),
                new Vector2(0.05f, 0.72f),
                new Vector2(0.95f, 0.94f),
                TextAlignmentOptions.Left,
                Array.Empty<string>());
            if (title != null)
                title.transform.SetAsFirstSibling();

            TextMeshProUGUI label = MoveOrCreateText(
                settingsPanel,
                "Microphone Label",
                "MICROPHONE",
                11,
                FontStyles.Bold,
                new Color(0.89f, 0.37f, 0.3f, 1f),
                new Vector2(0.05f, 0.58f),
                new Vector2(0.46f, 0.74f),
                TextAlignmentOptions.Left,
                new[] { "Mic Label" });

            TextMeshProUGUI selectedDeviceLabel = MoveOrCreateText(
                settingsPanel,
                "Microphone Value",
                string.Empty,
                14,
                FontStyles.Bold,
                new Color(0.96f, 0.94f, 0.88f, 1f),
                new Vector2(0.05f, 0.44f),
                new Vector2(0.46f, 0.56f),
                TextAlignmentOptions.Left,
                Array.Empty<string>());

            Dropdown dropdown = microphone?.microphoneDropdown;
            if (dropdown == null)
                dropdown = FindDeepTransform(view.transform.root, "Microphone Dropdown")?.GetComponent<Dropdown>();

            if (dropdown == null)
            {
                dropdown = CreateDropdown(
                    settingsPanel,
                    "Microphone Dropdown",
                    "Default microphone",
                    new Vector2(0.05f, 0.12f),
                    new Vector2(0.46f, 0.40f));
                if (microphone == null)
                    return;
            }

            if (microphone != null)
                microphone.microphoneDropdown = dropdown;

            dropdown.transform.SetParent(settingsPanel, false);
            RectTransform rect = dropdown.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.12f);
            rect.anchorMax = new Vector2(0.46f, 0.40f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (label != null)
                label.transform.SetAsLastSibling();

            StyleDropdown(dropdown, SettingsFill);
            EnsureSettingsDivider(settingsPanel);
            PopulateDropdown(microphone, dropdown, selectedDeviceLabel);
            UpdateMicrophoneValueLabel(microphone, selectedDeviceLabel);
        }

        private void EnsureExerciseDropdown()
        {
            if (exerciseCatalog == null || exerciseCatalog.Count == 0)
                return;

            Dropdown dropdown = exerciseDropdown;
            bool createdDropdown = false;
            if (dropdown == null)
            {
                dropdown = CreateDropdown(
                    view.transform,
                    "Lesson Dropdown",
                    exerciseCatalog.GetDisplayNames()[0],
                    new Vector2(0.17f, 0.86f),
                    new Vector2(0.78f, 0.93f));
                exerciseDropdown = dropdown;
                createdDropdown = true;
            }

            if (createdDropdown)
            {
                RectTransform rect = dropdown.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.17f, 0.86f);
                rect.anchorMax = new Vector2(0.78f, 0.93f);
            }

            string[] names = exerciseCatalog.GetDisplayNames();
            var options = new List<Dropdown.OptionData>();
            for (int i = 0; i < names.Length; i++)
                options.Add(new Dropdown.OptionData(names[i]));

            dropdown.options = options;
            dropdown.onValueChanged.RemoveAllListeners();

            dropdown.SetValueWithoutNotify(ResolveSelectedExerciseIndex());
            dropdown.gameObject.SetActive(true);
            HideLegacyExerciseTitle();
        }

        private void HideLegacyExerciseTitle()
        {
            if (view == null)
                return;

            Transform existing = view.transform.Find("Lesson Title Panel");
            if (existing != null)
                existing.gameObject.SetActive(false);
        }

        private void EnsureQualityDropdown()
        {
            if (whisperService == null)
                return;

            Transform settingsPanel = GetOrCreateSettingsPanel();
            if (settingsPanel == null)
                return;

            EnsureSettingsButton(settingsPanel.parent);
            EnsurePanelCloseButton(settingsPanel, "Settings Close Button", "CLOSE");

            Dropdown dropdown = whisperProfileDropdown;
            if (dropdown == null)
                dropdown = FindDeepTransform(view.transform.root, "Whisper Profile Dropdown")?.GetComponent<Dropdown>();

            if (dropdown == null)
            {
                dropdown = CreateDropdown(
                    settingsPanel,
                    "Whisper Profile Dropdown",
                    "FAST",
                    new Vector2(0.54f, 0.10f),
                    new Vector2(0.95f, 0.42f));
            }

            whisperProfileDropdown = dropdown;

            WhisperQualityProfile currentProfile = whisperService.CurrentQualityProfile;
            MoveOrCreateText(
                settingsPanel,
                "Whisper Profile Label",
                "WHISPER PROFILE",
                11,
                FontStyles.Bold,
                new Color(0.89f, 0.37f, 0.3f, 1f),
                new Vector2(0.54f, 0.58f),
                new Vector2(0.95f, 0.74f),
                TextAlignmentOptions.Left,
                new[] { "Whisper Profile Label" });

            TextMeshProUGUI currentProfileLabel = MoveOrCreateText(
                settingsPanel,
                "Whisper Profile Value",
                currentProfile.ToString().ToUpperInvariant(),
                14,
                FontStyles.Bold,
                new Color(0.96f, 0.94f, 0.88f, 1f),
                new Vector2(0.54f, 0.44f),
                new Vector2(0.95f, 0.56f),
                TextAlignmentOptions.Left,
                Array.Empty<string>());

            dropdown.transform.SetParent(settingsPanel, false);
            RectTransform rect = dropdown.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.54f, 0.12f);
            rect.anchorMax = new Vector2(0.95f, 0.40f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var options = new List<Dropdown.OptionData>();
            foreach (string name in Enum.GetNames(typeof(WhisperQualityProfile)))
                options.Add(new Dropdown.OptionData(name.ToUpperInvariant()));

            dropdown.options = options;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(index =>
            {
                WhisperQualityProfile profile = index switch
                {
                    1 => WhisperQualityProfile.Balanced,
                    2 => WhisperQualityProfile.Accurate,
                    _ => WhisperQualityProfile.Fast
                };

                whisperService.SetQualityProfile(profile);
                if (currentProfileLabel != null)
                    currentProfileLabel.text = profile.ToString().ToUpperInvariant();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            dropdown.SetValueWithoutNotify((int) currentProfile);
            StyleDropdown(dropdown, SettingsFill);
            EnsureSettingsDivider(settingsPanel);
        }

        private void EnsureProgressLabel()
        {
            if (view == null)
                return;

            Transform existing = view.transform.Find("Progress");
            TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (label == null)
            {
                label = CreateText(
                    view.transform,
                    "Progress",
                    "Progress: no attempts yet",
                    14,
                    FontStyles.Bold,
                    new Color(0.89f, 0.37f, 0.3f, 1f),
                    new Vector2(0.62f, 0.80f),
                    new Vector2(0.94f, 0.85f),
                    TextAlignmentOptions.Right);
            }

            view.ConfigureProgressLabel(label);
        }

        private void EnsureProgressDetailsLabel()
        {
            if (view == null)
                return;

            Transform resultPanel = GetOrCreateResultPanel();
            if (resultPanel == null)
                return;

            TextMeshProUGUI label = MoveOrCreateText(
                resultPanel,
                "Progress Details",
                "Your first local estimate will appear here.",
                11,
                FontStyles.Normal,
                new Color(0.20f, 0.20f, 0.20f, 1f),
                new Vector2(0.04f, 0.50f),
                new Vector2(0.96f, 0.72f),
                TextAlignmentOptions.Left,
                new[] { "Progress Details" });

            label.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            label.fontStyle = FontStyles.Normal;
            EnsureResultHeader(resultPanel);
            EnsureResultCloseButton(resultPanel);
            view.ConfigureProgressDetailsLabel(label);
        }

        private void EnsurePronunciationLabels()
        {
            if (view == null)
                return;

            Transform resultPanel = GetOrCreateResultPanel();
            if (resultPanel == null)
                return;

            TextMeshProUGUI summaryLabel = MoveOrCreateText(
                resultPanel,
                "Practice Score",
                string.Empty,
                14,
                FontStyles.Bold,
                new Color(0.89f, 0.37f, 0.3f, 1f),
                new Vector2(0.04f, 0.34f),
                new Vector2(0.96f, 0.48f),
                TextAlignmentOptions.Left,
                new[] { "Pronunciation Summary", "Practice Score" });

            summaryLabel.color = new Color(0.89f, 0.37f, 0.30f, 1f);
            TextMeshProUGUI feedbackLabel = MoveOrCreateText(
                resultPanel,
                "Coach Tip",
                "Your coach tip will appear here.",
                12,
                FontStyles.Italic,
                new Color(0.21f, 0.21f, 0.21f, 1f),
                new Vector2(0.04f, 0.10f),
                new Vector2(0.96f, 0.30f),
                TextAlignmentOptions.Left,
                new[] { "Pronunciation Feedback", "Coach Tip" });

            feedbackLabel.color = new Color(0.21f, 0.21f, 0.21f, 1f);
            EnsureResultHeader(resultPanel);
            EnsureResultCloseButton(resultPanel);
            view.ConfigurePronunciationLabels(summaryLabel, feedbackLabel);
        }

        private SpeechExerciseSO ResolveSelectedExercise()
        {
            int categoryIndex = ResolveSelectedCategoryIndex();
            int index = ResolveSelectedExerciseIndex();
            SpeechExerciseSO selected = exerciseCatalog != null
                ? exerciseCatalog.GetCategoryExercise(categoryIndex, index)
                : null;
            return selected != null ? selected : exercise;
        }

        private int ResolveSelectedCategoryIndex()
        {
            if (exerciseCatalog == null || exerciseCatalog.CategoryCount == 0)
                return 0;

            int index = PlayerPrefs.GetInt(selectedCategoryPrefsKey, 0);
            return Mathf.Clamp(index, 0, exerciseCatalog.CategoryCount - 1);
        }

        private int ResolveSelectedExerciseIndex()
        {
            if (exerciseCatalog == null)
                return 0;

            int exerciseCount = exerciseCatalog.GetCategoryExerciseCount(ResolveSelectedCategoryIndex());
            if (exerciseCount == 0)
                exerciseCount = exerciseCatalog.Count;

            int index = PlayerPrefs.GetInt(selectedExercisePrefsKey, 0);
            return Mathf.Clamp(index, 0, Mathf.Max(0, exerciseCount - 1));
        }

        private void PersistPracticeSelection(int categoryIndex, int exerciseIndex)
        {
            PlayerPrefs.SetInt(selectedCategoryPrefsKey, categoryIndex);
            PlayerPrefs.SetInt(selectedExercisePrefsKey, exerciseIndex);
            PlayerPrefs.Save();
        }

        private static Dropdown CreateDropdown(
            Transform parent,
            string name,
            string caption,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null)
        {
            DefaultControls.Resources resources = CreateUiResources();

            GameObject dropdownObject = DefaultControls.CreateDropdown(resources);
            dropdownObject.name = name;
            dropdownObject.transform.SetParent(parent, false);

            RectTransform rect = dropdownObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.06f, 0.08f);
            rect.anchorMax = anchorMax ?? new Vector2(0.42f, 0.15f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            if (dropdown.captionText != null)
                dropdown.captionText.text = caption;

            TextMeshProUGUI label = dropdownObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = caption;

            SetDropdownOptions(dropdown, caption);

            return dropdown;
        }

        private static void SetDropdownOptions(Dropdown dropdown, params string[] labels)
        {
            if (dropdown == null)
                return;

            dropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>();
            if (labels != null)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    string label = string.IsNullOrWhiteSpace(labels[i])
                        ? $"Option {i + 1}"
                        : labels[i];
                    options.Add(new Dropdown.OptionData(label));
                }
            }

            if (options.Count == 0)
                options.Add(new Dropdown.OptionData("Select"));

            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(0);
            if (dropdown.captionText != null)
                dropdown.captionText.text = options[0].text;

            if (dropdown.itemText != null)
                dropdown.itemText.text = options[0].text;
        }

        private static DefaultControls.Resources CreateUiResources()
        {
            Sprite sprite = CreateUiSprite();
            return new DefaultControls.Resources
            {
                standard = sprite,
                background = sprite,
                inputField = sprite,
                knob = sprite,
                checkmark = sprite,
                dropdown = sprite,
                mask = sprite
            };
        }

        private static Sprite CreateUiSprite()
        {
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false)
            {
                name = "FluentEcho.UI.WhiteTexture",
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixels(new[]
            {
                Color.white,
                Color.white,
                Color.white,
                Color.white
            });
            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        private static TextMeshProUGUI CreateText(
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

        private Transform GetOrCreateSettingsPanel()
        {
            if (view == null)
                return null;

            Transform mentorCard = view.transform.parent != null ? view.transform.parent.Find("Teacher Card") : null;
            if (mentorCard == null)
                return null;

            Transform existing = FindDeepTransform(view.transform.root, "Settings Panel");
            if (existing == null)
            {
                RectTransform rect = CreatePanel(
                    view.transform.root,
                    "Settings Panel",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(0.11f, 0.24f, 0.28f, 1f));
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(860f, 440f);
                return rect;
            }

            Image image = existing.GetComponent<Image>();
            if (image != null)
                image.color = SettingsFill;

            EnsurePanelAccent(existing, "Settings Accent", SettingsAccent);
            ApplyPanelFrame(existing, SettingsBorder);
            existing.gameObject.SetActive(false);
            return existing;
        }

        private Transform GetOrCreateResultPanel()
        {
            if (view == null)
                return null;

            Transform existing = view.transform.Find("Result Panel") ?? view.transform.Find("Success Badge");
            if (existing == null)
            {
                RectTransform rect = CreatePanel(
                    view.transform,
                    "Result Panel",
                    new Vector2(0.06f, 0.02f),
                    new Vector2(0.94f, 0.24f),
                    new Color(0.90f, 0.86f, 0.78f, 1f));
                return rect;
            }

            RectTransform existingRect = existing.GetComponent<RectTransform>();
            if (existingRect == null)
                return existing;

            Image image = existing.GetComponent<Image>();
            if (image != null)
                image.color = ResultFill;

            EnsurePanelAccent(existing, "Result Accent", ResultAccent);
            ApplyPanelFrame(existing, ResultBorder);
            existing.gameObject.SetActive(false);

            existing.name = "Result Panel";
            return existing;
        }

        private static TextMeshProUGUI MoveOrCreateText(
            Transform parent,
            string name,
            string value,
            float size,
            FontStyles style,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAlignmentOptions alignment,
            IReadOnlyList<string> legacyNames)
        {
            Transform existing = parent.Find(name);
            if (existing == null && legacyNames != null)
            {
                for (int i = 0; i < legacyNames.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(legacyNames[i]))
                        continue;

                    existing = FindDeepTransform(parent.root, legacyNames[i]);
                    if (existing != null)
                        break;
                }
            }

            TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (label == null)
            {
                label = CreateText(parent, name, value, size, style, color, anchorMin, anchorMax, alignment);
                return label;
            }

            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.name = name;
            return label;
        }

        private static void ApplyPanelFrame(Transform panel, Color borderColor)
        {
            if (panel == null)
                return;

            Outline outline = panel.GetComponent<Outline>();
            if (outline == null)
                outline = panel.gameObject.AddComponent<Outline>();

            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
        }

        private static void EnsurePanelAccent(Transform panel, string name, Color accentColor)
        {
            if (panel == null)
                return;

            Transform existing = panel.Find(name);
            RectTransform accent = existing != null ? existing.GetComponent<RectTransform>() : null;
            if (accent == null)
            {
                GameObject accentObject = new(name, typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(panel, false);
                accent = accentObject.GetComponent<RectTransform>();
            }

            accent.anchorMin = new Vector2(0f, 0.92f);
            accent.anchorMax = new Vector2(1f, 1f);
            accent.offsetMin = Vector2.zero;
            accent.offsetMax = Vector2.zero;
            Image image = accent.GetComponent<Image>();
            image.color = accentColor;
            accent.SetAsFirstSibling();
        }

        private void EnsureSettingsDivider(Transform settingsPanel)
        {
            if (settingsPanel == null)
                return;

            Transform existing = settingsPanel.Find("Settings Divider");
            RectTransform divider = existing != null ? existing.GetComponent<RectTransform>() : null;
            if (divider == null)
            {
                divider = CreatePanel(
                    settingsPanel,
                    "Settings Divider",
                    new Vector2(0.50f, 0.20f),
                    new Vector2(0.505f, 0.84f),
                    new Color(1f, 1f, 1f, 0.10f));
                return;
            }

            divider.anchorMin = new Vector2(0.50f, 0.20f);
            divider.anchorMax = new Vector2(0.505f, 0.84f);
            divider.offsetMin = Vector2.zero;
            divider.offsetMax = Vector2.zero;
            Image image = divider.GetComponent<Image>();
            if (image != null)
                image.color = new Color(1f, 1f, 1f, 0.10f);
        }

        private void EnsureResultHeader(Transform resultPanel)
        {
            if (resultPanel == null)
                return;

            MoveOrCreateText(
                resultPanel,
                "Result Header",
                "FINAL FEEDBACK",
                10,
                FontStyles.Bold,
                new Color(0.20f, 0.90f, 0.68f, 1f),
                new Vector2(0.04f, 0.79f),
                new Vector2(0.34f, 0.92f),
                TextAlignmentOptions.Left,
                new[] { "Result Header" });

            MoveOrCreateText(
                resultPanel,
                "Result Badge",
                "CLEARED",
                10,
                FontStyles.Bold,
                new Color(0.89f, 0.37f, 0.30f, 1f),
                new Vector2(0.78f, 0.79f),
                new Vector2(0.96f, 0.92f),
                TextAlignmentOptions.Right,
                new[] { "Result Badge" });

            Transform existing = resultPanel.Find("Result Divider");
            RectTransform divider = existing != null ? existing.GetComponent<RectTransform>() : null;
            if (divider == null)
            {
                divider = CreatePanel(
                    resultPanel,
                    "Result Divider",
                    new Vector2(0.04f, 0.74f),
                    new Vector2(0.96f, 0.75f),
                    new Color(0.89f, 0.37f, 0.30f, 0.16f));
                return;
            }

            divider.anchorMin = new Vector2(0.04f, 0.74f);
            divider.anchorMax = new Vector2(0.96f, 0.75f);
            divider.offsetMin = Vector2.zero;
            divider.offsetMax = Vector2.zero;
            Image image = divider.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.89f, 0.37f, 0.30f, 0.16f);
        }

        private void EnsureResultCloseButton(Transform resultPanel)
        {
            if (resultPanel == null)
                return;

            Button button = GetOrCreateButton(
                resultPanel,
                "Result Close Button",
                "CLOSE",
                new Vector2(0.84f, 0.83f),
                new Vector2(0.96f, 0.95f),
                new Color(0.89f, 0.37f, 0.30f, 1f),
                new Color(0.08f, 0.10f, 0.11f, 1f));

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => resultPanel.gameObject.SetActive(false));
        }

        private void EnsureSettingsButton(Transform uiRoot)
        {
            if (uiRoot == null)
                return;

            Button button = GetOrCreateButton(
                uiRoot,
                "Settings Toggle Button",
                "SETTINGS",
                new Vector2(0.83f, 0.90f),
                new Vector2(0.965f, 0.96f),
                new Color(0.11f, 0.24f, 0.28f, 1f),
                new Color(0.96f, 0.94f, 0.88f, 1f));

            if (button == null)
                return;

            Transform settingsPanel = FindDeepTransform(view.transform.root, "Settings Panel");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (settingsPanel == null)
                    return;

                bool shouldOpen = !settingsPanel.gameObject.activeSelf;
                settingsPanel.gameObject.SetActive(shouldOpen);
            });
        }

        private void EnsurePanelCloseButton(
            Transform panel,
            string name,
            string labelText)
        {
            if (panel == null)
                return;

            Button button = GetOrCreateButton(
                panel,
                name,
                labelText,
                new Vector2(0.84f, 0.83f),
                new Vector2(0.96f, 0.95f),
                new Color(0.89f, 0.37f, 0.30f, 1f),
                new Color(0.08f, 0.10f, 0.11f, 1f));

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => panel.gameObject.SetActive(false));
        }

        private static void StyleDropdown(Dropdown dropdown, Color fillColor)
        {
            if (dropdown == null)
                return;

            if (dropdown.targetGraphic != null)
                dropdown.targetGraphic.color = fillColor;

            if (dropdown.captionText != null)
            {
                dropdown.captionText.color = new Color(0.96f, 0.94f, 0.88f, 1f);
                dropdown.captionText.fontSize = 15;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = new Color(0.10f, 0.12f, 0.13f, 1f);
                dropdown.itemText.fontSize = 14;
            }
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

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private static Button GetOrCreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color background,
            Color foreground)
        {
            if (parent == null)
                return null;

            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Button existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                    return existingButton;
            }

            RectTransform rect = CreatePanel(parent, name, anchorMin, anchorMax, background);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.16f);
            button.colors = colors;
            CreateText(
                rect,
                "Label",
                label,
                13,
                FontStyles.Bold,
                foreground,
                new Vector2(0.05f, 0.12f),
                new Vector2(0.95f, 0.90f),
                TextAlignmentOptions.Center);
            return button;
        }

        private static void PopulateDropdown(
            MicrophoneRecord microphone,
            Dropdown dropdown,
            TextMeshProUGUI selectedDeviceLabel)
        {
            var options = new List<Dropdown.OptionData>();
            options.Add(new Dropdown.OptionData("Default microphone"));
            foreach (string device in microphone.AvailableMicDevices)
                options.Add(new Dropdown.OptionData(device));

            dropdown.options = options;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(index =>
            {
                if (index <= 0)
                {
                    microphone.SelectedMicDevice = null;
                    UpdateMicrophoneValueLabel(microphone, selectedDeviceLabel);
                    return;
                }

                int deviceIndex = index - 1;
                int current = 0;
                foreach (string device in microphone.AvailableMicDevices)
                {
                    if (current == deviceIndex)
                    {
                        microphone.SelectedMicDevice = device;
                        UpdateMicrophoneValueLabel(microphone, selectedDeviceLabel);
                        return;
                    }

                    current++;
                }
            });

            string selected = microphone.SelectedMicDevice;
            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(selected))
            {
                int index = 1;
                foreach (string device in microphone.AvailableMicDevices)
                {
                    if (device == selected)
                    {
                        selectedIndex = index;
                        break;
                    }

                    index++;
                }
            }

            dropdown.SetValueWithoutNotify(selectedIndex);
        }

        private static void UpdateMicrophoneValueLabel(
            MicrophoneRecord microphone,
            TextMeshProUGUI selectedDeviceLabel)
        {
            if (selectedDeviceLabel == null)
                return;

            string selected = microphone != null ? microphone.SelectedMicDevice : null;
            selectedDeviceLabel.text = string.IsNullOrWhiteSpace(selected)
                ? "Default microphone"
                : selected;
        }
    }
}
