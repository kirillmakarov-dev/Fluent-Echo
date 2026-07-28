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
        [SerializeField] private GameObject settingsPanelObject;
        [SerializeField] private Button settingsToggleButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private GameObject resultPanelObject;
        [SerializeField] private Button resultCloseButton;
        [SerializeField] private TextMeshProUGUI microphoneValueLabel;
        [SerializeField] private TextMeshProUGUI whisperProfileValueLabel;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private TextMeshProUGUI progressDetailsLabel;
        [SerializeField] private TextMeshProUGUI pronunciationSummaryLabel;
        [SerializeField] private TextMeshProUGUI pronunciationFeedbackLabel;
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
            WireSceneOwnedUi();

            presenter = new FluentEchoPresenter(
                exercise,
                exerciseCatalog,
                view,
                whisperService,
                mockService,
                useMockByDefault,
                PlayReference,
                null,
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
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Scene-owned Microphone Dropdown is not assigned.",
                    this);
                return;
            }

            microphone.microphoneDropdown = dropdown;
            PopulateDropdown(microphone, dropdown, microphoneValueLabel);
            UpdateMicrophoneValueLabel(microphone, microphoneValueLabel);
        }

        private void BindSceneWhisperProfileDropdown()
        {
            if (whisperService == null || view == null)
                return;

            Dropdown dropdown = whisperProfileDropdown;
            if (dropdown == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Scene-owned Whisper Profile Dropdown is not assigned.",
                    this);
                return;
            }

            whisperProfileDropdown = dropdown;
            WhisperQualityProfile currentProfile = whisperService.CurrentQualityProfile;

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
                if (whisperProfileValueLabel != null)
                    whisperProfileValueLabel.text = profile.ToString().ToUpperInvariant();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            dropdown.SetValueWithoutNotify((int) currentProfile);
            if (dropdown.captionText != null)
                dropdown.captionText.text = currentProfile.ToString().ToUpperInvariant();

            if (whisperProfileValueLabel != null)
                whisperProfileValueLabel.text = currentProfile.ToString().ToUpperInvariant();
        }

        private void WireExistingSettingsPanel()
        {
            if (view == null)
                return;

            if (settingsPanelObject == null || settingsToggleButton == null || settingsCloseButton == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Settings panel, toggle button, or close button is not assigned.",
                    this);
                return;
            }

            settingsToggleButton.onClick.RemoveAllListeners();
            settingsToggleButton.onClick.AddListener(() =>
            {
                bool shouldOpen = !settingsPanelObject.activeSelf;
                settingsPanelObject.SetActive(shouldOpen);
            });

            settingsCloseButton.onClick.RemoveAllListeners();
            settingsCloseButton.onClick.AddListener(() => settingsPanelObject.SetActive(false));
        }

        private void WireExistingResultPanel()
        {
            if (view == null)
                return;

            if (resultPanelObject == null || resultCloseButton == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Result panel or close button is not assigned.",
                    this);
                return;
            }

            resultCloseButton.onClick.RemoveAllListeners();
            resultCloseButton.onClick.AddListener(() => resultPanelObject.SetActive(false));
        }

        private void EnsureMicrophoneDropdown()
        {
            if (whisperService == null)
                return;

            MicrophoneRecord microphone = whisperService.GetComponent<MicrophoneRecord>();
            Dropdown dropdown = microphone?.microphoneDropdown;

            if (dropdown == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Scene-owned Microphone Dropdown is not assigned.",
                    this);
                return;
            }

            if (microphone != null)
                microphone.microphoneDropdown = dropdown;

            StyleDropdown(dropdown, SettingsFill);
            PopulateDropdown(microphone, dropdown, microphoneValueLabel);
        }

        private void EnsureExerciseDropdown()
        {
            if (exerciseCatalog == null || exerciseCatalog.Count == 0)
                return;

            Dropdown dropdown = exerciseDropdown;
            if (dropdown == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Scene-owned Lesson Dropdown is not assigned.",
                    this);
                return;
            }

            string[] names = exerciseCatalog.GetDisplayNames();
            var options = new List<Dropdown.OptionData>();
            for (int i = 0; i < names.Length; i++)
                options.Add(new Dropdown.OptionData(names[i]));

            dropdown.options = options;
            dropdown.onValueChanged.RemoveAllListeners();

            dropdown.SetValueWithoutNotify(ResolveSelectedExerciseIndex());
            dropdown.gameObject.SetActive(true);
        }

        private void EnsureQualityDropdown()
        {
            if (whisperService == null)
                return;

            Dropdown dropdown = whisperProfileDropdown;

            if (dropdown == null)
            {
                Debug.LogWarning(
                    "[FluentEchoBootstrap] Scene-owned Whisper Profile Dropdown is not assigned.",
                    this);
                return;
            }

            whisperProfileDropdown = dropdown;

            WhisperQualityProfile currentProfile = whisperService.CurrentQualityProfile;
            TextMeshProUGUI currentProfileLabel = whisperProfileValueLabel;

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
        }

        private void EnsureProgressLabel()
        {
            if (view == null)
                return;

            if (progressLabel == null)
            {
                Debug.LogWarning("[FluentEchoBootstrap] Progress label is not assigned in the scene.", this);
                return;
            }

            progressLabel.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            progressLabel.fontStyle = FontStyles.Normal;
            view.ConfigureProgressLabel(progressLabel);
        }

        private void EnsureProgressDetailsLabel()
        {
            if (view == null)
                return;

            if (progressDetailsLabel == null)
            {
                Debug.LogWarning("[FluentEchoBootstrap] Progress Details label is not assigned in the scene.", this);
                return;
            }

            progressDetailsLabel.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            progressDetailsLabel.fontStyle = FontStyles.Normal;
            view.ConfigureProgressDetailsLabel(progressDetailsLabel);
        }

        private void EnsurePronunciationLabels()
        {
            if (view == null)
                return;

            if (pronunciationSummaryLabel == null || pronunciationFeedbackLabel == null)
            {
                Debug.LogWarning("[FluentEchoBootstrap] Pronunciation summary or feedback labels are not assigned in the scene.", this);
                return;
            }

            pronunciationSummaryLabel.color = new Color(0.89f, 0.37f, 0.30f, 1f);
            pronunciationFeedbackLabel.color = new Color(0.21f, 0.21f, 0.21f, 1f);
            view.ConfigurePronunciationLabels(pronunciationSummaryLabel, pronunciationFeedbackLabel);
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
            if (settingsPanelObject == null)
                return null;

            return settingsPanelObject.transform;
        }

        private Transform GetOrCreateResultPanel()
        {
            if (resultPanelObject == null)
                return null;

            return resultPanelObject.transform;
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

            Button button = settingsToggleButton;

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (settingsPanelObject == null)
                    return;

                bool shouldOpen = !settingsPanelObject.activeSelf;
                settingsPanelObject.SetActive(shouldOpen);
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
