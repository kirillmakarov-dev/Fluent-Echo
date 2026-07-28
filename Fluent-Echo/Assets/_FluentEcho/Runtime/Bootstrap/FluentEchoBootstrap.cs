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
        [SerializeField] private SpeechExerciseSO exercise;
        [SerializeField] private SpeechExerciseCatalogSO exerciseCatalog;
        [SerializeField] private FluentEchoView view;
        [SerializeField] private WhisperSpeechRecognitionService whisperService;
        [SerializeField] private MockSpeechRecognitionService mockService;
        [SerializeField] private Dropdown exerciseDropdown;
        [SerializeField] private Dropdown whisperProfileDropdown;
        [SerializeField] private string selectedExercisePrefsKey = "FluentEcho.SelectedExerciseIndex";
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
            EnsureMicrophoneDropdown();
            EnsureQualityDropdown();
            EnsureProgressLabel();
            EnsureProgressDetailsLabel();
            EnsurePronunciationLabels();
            presenter = new FluentEchoPresenter(
                exercise,
                exerciseCatalog,
                view,
                whisperService,
                mockService,
                useMockByDefault,
                PlayReference);
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

        private void EnsureMicrophoneDropdown()
        {
            if (whisperService == null)
                return;

            MicrophoneRecord microphone = whisperService.GetComponent<MicrophoneRecord>();
            Dropdown dropdown = microphone?.microphoneDropdown;
            if (dropdown == null)
            {
                dropdown = CreateDropdown(
                    view.transform,
                    "Microphone Dropdown",
                    "Default microphone",
                    new Vector2(0.06f, 0.08f),
                    new Vector2(0.42f, 0.15f));
                if (microphone == null)
                    return;

                microphone.microphoneDropdown = dropdown;
            }

            RectTransform rect = dropdown.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.03f);
            rect.anchorMax = new Vector2(0.42f, 0.09f);

            PopulateDropdown(microphone, dropdown);
        }

        private void EnsureExerciseDropdown()
        {
            if (exerciseCatalog == null || exerciseCatalog.Count == 0)
                return;

            Dropdown dropdown = exerciseDropdown;
            if (dropdown == null)
            {
                dropdown = CreateDropdown(
                    view.transform,
                    "Lesson Dropdown",
                    exerciseCatalog.GetDisplayNames()[0],
                    new Vector2(0.17f, 0.86f),
                    new Vector2(0.78f, 0.93f));
                exerciseDropdown = dropdown;
            }

            RectTransform rect = dropdown.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.17f, 0.86f);
            rect.anchorMax = new Vector2(0.78f, 0.93f);

            string[] names = exerciseCatalog.GetDisplayNames();
            var options = new List<Dropdown.OptionData>();
            for (int i = 0; i < names.Length; i++)
                options.Add(new Dropdown.OptionData(names[i]));

            dropdown.options = options;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(index =>
            {
                PlayerPrefs.SetInt(selectedExercisePrefsKey, index);
                PlayerPrefs.Save();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            dropdown.SetValueWithoutNotify(ResolveSelectedExerciseIndex());
        }

        private void EnsureQualityDropdown()
        {
            if (whisperService == null)
                return;

            Dropdown dropdown = whisperProfileDropdown;
            if (dropdown == null)
            {
                dropdown = CreateDropdown(
                    view.transform,
                    "Whisper Profile Dropdown",
                    "FAST",
                    new Vector2(0.46f, 0.08f),
                    new Vector2(0.82f, 0.15f));
                whisperProfileDropdown = dropdown;
            }

            WhisperQualityProfile currentProfile = whisperService.CurrentQualityProfile;
            RectTransform rect = dropdown.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.46f, 0.03f);
            rect.anchorMax = new Vector2(0.82f, 0.09f);

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
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            dropdown.SetValueWithoutNotify((int) currentProfile);
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
                    "PROGRESS | no attempts yet",
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

            Transform existing = view.transform.Find("Progress Details");
            TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (label == null)
            {
                label = CreateText(
                    view.transform,
                    "Progress Details",
                    "Recent attempts will appear here.",
                    11,
                    FontStyles.Normal,
                    new Color(0.20f, 0.20f, 0.20f, 1f),
                    new Vector2(0.62f, 0.67f),
                    new Vector2(0.94f, 0.74f),
                    TextAlignmentOptions.Right);
            }

            view.ConfigureProgressDetailsLabel(label);
        }

        private void EnsurePronunciationLabels()
        {
            if (view == null)
                return;

            Transform summaryExisting = view.transform.Find("Pronunciation Summary");
            TextMeshProUGUI summaryLabel = summaryExisting != null
                ? summaryExisting.GetComponent<TextMeshProUGUI>()
                : null;
            if (summaryLabel == null)
            {
                summaryLabel = CreateText(
                    view.transform,
                    "Pronunciation Summary",
                    string.Empty,
                    14,
                    FontStyles.Bold,
                    new Color(0.89f, 0.37f, 0.3f, 1f),
                    new Vector2(0.62f, 0.23f),
                    new Vector2(0.94f, 0.28f),
                    TextAlignmentOptions.Right);
            }

            Transform feedbackExisting = view.transform.Find("Pronunciation Feedback");
            TextMeshProUGUI feedbackLabel = feedbackExisting != null
                ? feedbackExisting.GetComponent<TextMeshProUGUI>()
                : null;
            if (feedbackLabel == null)
            {
                feedbackLabel = CreateText(
                    view.transform,
                    "Pronunciation Feedback",
                    "Score feedback will appear here.",
                    12,
                    FontStyles.Italic,
                    new Color(0.21f, 0.21f, 0.21f, 1f),
                    new Vector2(0.62f, 0.18f),
                    new Vector2(0.94f, 0.23f),
                    TextAlignmentOptions.Right);
            }

            view.ConfigurePronunciationLabels(summaryLabel, feedbackLabel);
        }

        private SpeechExerciseSO ResolveSelectedExercise()
        {
            int index = ResolveSelectedExerciseIndex();
            SpeechExerciseSO selected = exerciseCatalog != null ? exerciseCatalog.GetExercise(index) : null;
            return selected != null ? selected : exercise;
        }

        private int ResolveSelectedExerciseIndex()
        {
            if (exerciseCatalog == null || exerciseCatalog.Count == 0)
                return 0;

            int index = PlayerPrefs.GetInt(selectedExercisePrefsKey, 0);
            return Mathf.Clamp(index, 0, exerciseCatalog.Count - 1);
        }

        private static Dropdown CreateDropdown(
            Transform parent,
            string name,
            string caption,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null)
        {
            DefaultControls.Resources resources = new()
            {
                standard = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"),
                background = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd"),
                inputField = Resources.GetBuiltinResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = Resources.GetBuiltinResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = Resources.GetBuiltinResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = Resources.GetBuiltinResource<Sprite>("UI/Skin/UIMask.psd")
            };

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

            return dropdown;
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

        private static void PopulateDropdown(MicrophoneRecord microphone, Dropdown dropdown)
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
                    return;
                }

                int deviceIndex = index - 1;
                int current = 0;
                foreach (string device in microphone.AvailableMicDevices)
                {
                    if (current == deviceIndex)
                    {
                        microphone.SelectedMicDevice = device;
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
    }
}
