using FluentEcho.Bootstrap;
using FluentEcho.Data;
using FluentEcho.Services;
using FluentEcho.Views;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;

namespace FluentEcho.Editor
{
    public static class FluentEchoPrototypeBuilder
    {
        private const string Root = "Assets/_FluentEcho";
        private const string ScenePath = Root + "/Demo/Scenes/FluentEchoPrototype.unity";
        private const string ExerciseCatalogPath = Root + "/Demo/Data/ExerciseCatalog.asset";
        private const string FirstExercisePath = Root + "/Demo/Data/FirstLesson.asset";
        private const string SecondExercisePath = Root + "/Demo/Data/SecondLesson.asset";
        private const string ThirdExercisePath = Root + "/Demo/Data/ThirdLesson.asset";
        private const string FourthExercisePath = Root + "/Demo/Data/FourthLesson.asset";
        private const string FifthExercisePath = Root + "/Demo/Data/FifthLesson.asset";
        private const string WhisperSettingsPath = Root + "/Demo/Data/WhisperSettings.asset";
        private const string ChipPrefabPath = Root + "/Demo/Prefabs/WordChip.prefab";

        private static readonly Color Background = Hex("08171D");
        private static readonly Color Surface = Hex("10272E");
        private static readonly Color SurfaceRaised = Hex("17353D");
        private static readonly Color Cream = Hex("F6F0DF");
        private static readonly Color Muted = Hex("A8BCC0");
        private static readonly Color Mint = Hex("33D69F");
        private static readonly Color Coral = Hex("FF8066");
        private static readonly Color Ink = Hex("071416");

        [InitializeOnLoadMethod]
        private static void BuildOnFirstImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                    BuildPrototype();
            };
        }

        [MenuItem("Tools/Fluent Echo/Rebuild Prototype")]
        public static void BuildPrototype()
        {
            EnsureFolders();
            EnsureTmpResources();

            SpeechExerciseCatalogSO catalog = CreateExerciseCatalog(out SpeechExerciseSO exercise);
            WordChipView chipPrefab = CreateWordChipPrefab();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "FluentEchoPrototype";

            Camera camera = CreateWorld();
            CreateEventSystem();
            FluentEchoView view = CreateInterface(camera, chipPrefab, catalog, exercise, out Dropdown lessonDropdown);
            CreateRuntime(catalog, exercise, lessonDropdown, view);

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"[FluentEcho] Prototype rebuilt at {ScenePath}");
        }

        private static Camera CreateWorld()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = false;
            camera.fieldOfView = 40f;
            camera.transform.position = new Vector3(0f, 2.5f, -10f);
            camera.transform.LookAt(new Vector3(0f, 1.5f, 0f));
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.92f, 0.78f);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

            GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Plane);
            stage.name = "Future 3D Stage";
            stage.transform.position = new Vector3(0f, -1.4f, 2f);
            stage.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            stage.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial("Stage Material", Surface);

            GameObject teacher = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            teacher.name = "Future Teacher Placeholder";
            teacher.transform.position = new Vector3(-3.25f, 0f, 1.5f);
            teacher.transform.localScale = new Vector3(0.85f, 1.25f, 0.85f);
            teacher.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial("Teacher Material", Coral);

            return camera;
        }

        private static FluentEchoView CreateInterface(
            Camera camera,
            WordChipView chipPrefab,
            SpeechExerciseCatalogSO catalog,
            SpeechExerciseSO selectedExercise,
            out Dropdown lessonDropdown)
        {
            GameObject canvasObject = new("Fluent Echo UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            Stretch(root);

            CreatePanel(root, "Backdrop", Vector2.zero, Vector2.one, Background);
            CreatePanel(root, "Top Accent", new Vector2(0f, 0.982f), Vector2.one, Mint);

            CreateText(root, "Brand", "FLUENT ECHO", 34, FontStyles.Bold, Cream,
                new Vector2(0.05f, 0.89f), new Vector2(0.35f, 0.96f), TextAlignmentOptions.Left);
            CreateText(root, "Subtitle", "OFFLINE SPEECH PRACTICE / UNITY + WHISPER", 15, FontStyles.Bold, Mint,
                new Vector2(0.05f, 0.85f), new Vector2(0.48f, 0.89f), TextAlignmentOptions.Left);

            RectTransform mentorCard = CreatePanel(
                root, "Teacher Card", new Vector2(0.05f, 0.13f), new Vector2(0.34f, 0.80f), Surface);
            CreatePanel(mentorCard, "Portrait Field", new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.92f), SurfaceRaised);
            CreateText(mentorCard, "Avatar", "A", 112, FontStyles.Bold, Coral,
                new Vector2(0.18f, 0.48f), new Vector2(0.82f, 0.86f), TextAlignmentOptions.Center);
            CreateText(mentorCard, "Teacher Name", "AVA / SPEECH COACH", 18, FontStyles.Bold, Mint,
                new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.36f), TextAlignmentOptions.Left);
            CreateText(mentorCard, "Teacher Note",
                "Speak naturally. Your voice stays on this device and is processed by a local Whisper model.",
                24, FontStyles.Normal, Cream,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.27f), TextAlignmentOptions.TopLeft);

            RectTransform lessonCard = CreatePanel(
                root, "Lesson Card", new Vector2(0.37f, 0.10f), new Vector2(0.95f, 0.86f), Cream);
            CreateText(lessonCard, "Step", "LESSON 01 / DESCRIBE THE DOG", 16, FontStyles.Bold, Coral,
                new Vector2(0.06f, 0.86f), new Vector2(0.35f, 0.93f), TextAlignmentOptions.Left);

            Button lessonPreviousButton = CreateButton(
                lessonCard, "Lesson Previous Button", "PREV",
                new Vector2(0.06f, 0.86f), new Vector2(0.15f, 0.93f), SurfaceRaised, Cream, out _);
            lessonDropdown = CreateDropdown(
                lessonCard,
                "Lesson Dropdown",
                new Vector2(0.17f, 0.86f),
                new Vector2(0.78f, 0.93f),
                selectedExercise != null ? selectedExercise.name.ToUpperInvariant() : "LESSON 01");
            Button lessonNextButton = CreateButton(
                lessonCard, "Lesson Next Button", "NEXT",
                new Vector2(0.80f, 0.86f), new Vector2(0.94f, 0.93f), SurfaceRaised, Cream, out _);

            TextMeshProUGUI modeLabel = CreateText(lessonCard, "Mode", "LOCAL WHISPER", 14, FontStyles.Bold, Ink,
                new Vector2(0.71f, 0.80f), new Vector2(0.94f, 0.85f), TextAlignmentOptions.Right);
            TextMeshProUGUI progressLabel = CreateText(lessonCard, "Progress", "PROGRESS | no attempts yet", 14, FontStyles.Bold, Coral,
                new Vector2(0.62f, 0.74f), new Vector2(0.94f, 0.79f), TextAlignmentOptions.Right);
            TextMeshProUGUI prompt = CreateText(lessonCard, "Prompt", "Say the sentence in English.", 42, FontStyles.Bold, Ink,
                new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.83f), TextAlignmentOptions.BottomLeft);

            RectTransform words = CreatePanel(
                lessonCard, "Word Chips", new Vector2(0.06f, 0.54f), new Vector2(0.94f, 0.65f), new Color(0f, 0f, 0f, 0f));
            HorizontalLayoutGroup wordLayout = words.gameObject.AddComponent<HorizontalLayoutGroup>();
            wordLayout.spacing = 12f;
            wordLayout.childAlignment = TextAnchor.MiddleLeft;
            wordLayout.childControlWidth = false;
            wordLayout.childControlHeight = true;
            wordLayout.childForceExpandWidth = false;
            wordLayout.childForceExpandHeight = true;

            RectTransform transcriptPanel = CreatePanel(
                lessonCard, "Transcript Panel", new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.52f), Hex("E7E0CF"));
            CreateText(transcriptPanel, "Heard Label", "WHISPER HEARD", 13, FontStyles.Bold, Coral,
                new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Left);
            TextMeshProUGUI transcript = CreateText(
                transcriptPanel, "Transcript", "Your recognized sentence will appear here.", 24, FontStyles.Italic, Ink,
                new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.64f), TextAlignmentOptions.Left);

            TextMeshProUGUI status = CreateText(
                lessonCard, "Status", "Loading speech engine...", 21, FontStyles.Normal, Ink,
                new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.36f), TextAlignmentOptions.Left);

            CreateText(
                lessonCard, "Mic Label", "MICROPHONE DEVICE", 13, FontStyles.Bold, Coral,
                new Vector2(0.06f, 0.15f), new Vector2(0.33f, 0.19f), TextAlignmentOptions.Left);
            Dropdown microphoneDropdown = CreateDropdown(
                lessonCard, "Microphone Dropdown",
                new Vector2(0.06f, 0.08f), new Vector2(0.42f, 0.15f),
                "Default microphone");

            Image recordingIndicator = CreatePanel(
                lessonCard, "Recording Indicator", new Vector2(0.06f, 0.155f), new Vector2(0.075f, 0.185f), Coral)
                .GetComponent<Image>();
            recordingIndicator.gameObject.SetActive(false);

            Button micButton = CreateButton(
                lessonCard, "Mic Button", "START SPEAKING",
                new Vector2(0.06f, 0.18f), new Vector2(0.48f, 0.30f), Ink, Mint, out TextMeshProUGUI micLabel);
            Button demoButton = CreateButton(
                lessonCard, "Demo Button", "RUN DEMO ANSWER",
                new Vector2(0.50f, 0.18f), new Vector2(0.72f, 0.30f), SurfaceRaised, Cream, out _);
            Button listenButton = CreateButton(
                lessonCard, "Listen Button", "LISTEN",
                new Vector2(0.74f, 0.18f), new Vector2(0.94f, 0.30f), SurfaceRaised, Cream, out _);

            Toggle mockToggle = CreateToggle(
                lessonCard, "Mock Mode Toggle", "Use deterministic demo engine",
                new Vector2(0.06f, 0.01f), new Vector2(0.46f, 0.07f));

            Button retryButton = CreateButton(
                lessonCard, "Retry Button", "NEW ATTEMPT",
                new Vector2(0.74f, 0.01f), new Vector2(0.94f, 0.07f), Coral, Ink, out _);
            retryButton.gameObject.SetActive(false);

            Image success = CreatePanel(
                lessonCard, "Success Badge", new Vector2(0.49f, 0.01f), new Vector2(0.72f, 0.07f), Mint)
                .GetComponent<Image>();
            CreateText(success.rectTransform, "Success Text", "ANSWER ACCEPTED", 14, FontStyles.Bold, Ink,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            success.gameObject.SetActive(false);

            FluentEchoView view = lessonCard.gameObject.AddComponent<FluentEchoView>();
            SerializedObject serialized = new(view);
            Set(serialized, "promptLabel", prompt);
            Set(serialized, "progressLabel", progressLabel);
            Set(serialized, "statusLabel", status);
            Set(serialized, "transcriptLabel", transcript);
            Set(serialized, "micButtonLabel", micLabel);
            Set(serialized, "modeLabel", modeLabel);
            Set(serialized, "wordContainer", words);
            Set(serialized, "wordChipPrefab", chipPrefab);
            Set(serialized, "micButton", micButton);
            Set(serialized, "demoButton", demoButton);
            Set(serialized, "retryButton", retryButton);
            Set(serialized, "listenButton", listenButton);
            Set(serialized, "previousButton", lessonPreviousButton);
            Set(serialized, "nextButton", lessonNextButton);
            Set(serialized, "mockModeToggle", mockToggle);
            Set(serialized, "recordingIndicator", recordingIndicator);
            Set(serialized, "successPanel", success);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.ConfigureWordChipPrefab(chipPrefab);
            EditorUtility.SetDirty(view);

            return view;
        }

        private static void CreateRuntime(
            SpeechExerciseCatalogSO catalog,
            SpeechExerciseSO exercise,
            Dropdown lessonDropdown,
            FluentEchoView view)
        {
            GameObject runtime = new("Speech Runtime");
            WhisperManager manager = runtime.AddComponent<WhisperManager>();
            MicrophoneRecord microphone = runtime.AddComponent<MicrophoneRecord>();
            WhisperSpeechRecognitionService whisper = runtime.AddComponent<WhisperSpeechRecognitionService>();
            MockSpeechRecognitionService mock = runtime.AddComponent<MockSpeechRecognitionService>();
            AudioSource audioSource = runtime.AddComponent<AudioSource>();
            FluentEchoBootstrap bootstrap = runtime.AddComponent<FluentEchoBootstrap>();
            WhisperSettingsSO settings = CreateWhisperSettings();

            SerializedObject managerObject = new(manager);
            managerObject.FindProperty("modelPath").stringValue = settings.ResolveModelPath();
            managerObject.FindProperty("isModelPathInStreamingAssets").boolValue = true;
            managerObject.FindProperty("initOnAwake").boolValue = false;
            managerObject.FindProperty("useGpu").boolValue = settings.UseGpu;
            managerObject.FindProperty("flashAttention").boolValue = settings.FlashAttention;
            managerObject.FindProperty("language").stringValue = settings.Language;
            managerObject.FindProperty("stepSec").floatValue = settings.StreamingStepSeconds;
            managerObject.FindProperty("keepSec").floatValue = settings.KeepSeconds;
            managerObject.FindProperty("lengthSec").floatValue = settings.StreamLengthSeconds;
            managerObject.FindProperty("updatePrompt").boolValue = settings.UpdatePrompt;
            managerObject.FindProperty("dropOldBuffer").boolValue = settings.DropOldBuffer;
            managerObject.FindProperty("useVad").boolValue = settings.UseVad;
            managerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject whisperObject = new(whisper);
            Set(whisperObject, "whisperManager", manager);
            Set(whisperObject, "microphone", microphone);
            Set(whisperObject, "whisperSettings", settings);
            whisperObject.FindProperty("legacyModelPath").stringValue = settings.ResolveModelPath();
            whisperObject.FindProperty("legacyStreamingStepSeconds").floatValue = settings.StreamingStepSeconds;
            whisperObject.FindProperty("legacyMicrophoneChunkSeconds").floatValue = settings.MicrophoneChunkSeconds;
            whisperObject.FindProperty("legacyWarmupSeconds").floatValue = settings.WarmupSeconds;
            whisperObject.ApplyModifiedPropertiesWithoutUndo();

            bootstrap.Configure(
                exercise,
                catalog,
                view,
                whisper,
                mock,
                audioSource,
                false);
            EditorUtility.SetDirty(bootstrap);

            SerializedObject microphoneObject = new(microphone);
            Set(microphoneObject, "microphoneDropdown", microphone.microphoneDropdown);
            microphoneObject.FindProperty("microphoneDefaultLabel").stringValue = "Default microphone";
            microphoneObject.FindProperty("rememberSelectedDevice").boolValue = true;
            microphoneObject.FindProperty("selectedDevicePrefsKey").stringValue = "FluentEcho.SelectedMicrophone";
            microphoneObject.ApplyModifiedPropertiesWithoutUndo();

            CreateText(
                view.transform,
                "Whisper Profile Label",
                "WHISPER PROFILE",
                13,
                FontStyles.Bold,
                Coral,
                new Vector2(0.46f, 0.23f),
                new Vector2(0.82f, 0.27f),
                TextAlignmentOptions.Left);

            Dropdown whisperProfileDropdown = CreateDropdown(
                view.transform,
                "Whisper Profile Dropdown",
                new Vector2(0.46f, 0.18f),
                new Vector2(0.82f, 0.26f),
                settings.QualityProfile.ToString().ToUpperInvariant());

            SerializedObject bootstrapObject = new(bootstrap);
            Set(bootstrapObject, "exerciseCatalog", catalog);
            Set(bootstrapObject, "exerciseDropdown", lessonDropdown);
            bootstrapObject.FindProperty("selectedExercisePrefsKey").stringValue = "FluentEcho.SelectedExerciseIndex";
            Set(bootstrapObject, "whisperProfileDropdown", whisperProfileDropdown);
            bootstrapObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpeechExerciseCatalogSO CreateExerciseCatalog(out SpeechExerciseSO selectedExercise)
        {
            SpeechExerciseSO first = CreateExerciseAsset(
                FirstExercisePath,
                "Look at Ava and say: The dog is big.",
                "the dog is big|the dog is large",
                "lesson_01_describe_the_dog",
                "the",
                "dog",
                "is",
                "big|large");

            SpeechExerciseSO second = CreateExerciseAsset(
                SecondExercisePath,
                "Say: The cat is small.",
                "the cat is small|the cat is little",
                "lesson_02_describe_the_cat",
                "the",
                "cat",
                "is",
                "small|little");

            SpeechExerciseSO third = CreateExerciseAsset(
                ThirdExercisePath,
                "Say: I like this book.",
                "i like this book|i like this one",
                "lesson_03_like_this_book",
                "i",
                "like",
                "this",
                "book|one");

            SpeechExerciseSO fourth = CreateExerciseAsset(
                FourthExercisePath,
                "Say: The apple is red.",
                "the apple is red|the apple is bright red",
                "lesson_04_the_apple_is_red",
                "the",
                "apple",
                "is",
                "red|bright red");

            SpeechExerciseSO fifth = CreateExerciseAsset(
                FifthExercisePath,
                "Say: We are ready to go.",
                "we are ready to go|we are all ready to go",
                "lesson_05_ready_to_go",
                "we",
                "are",
                "ready",
                "to",
                "go");

            SpeechExerciseCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SpeechExerciseCatalogSO>(ExerciseCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SpeechExerciseCatalogSO>();
                AssetDatabase.CreateAsset(catalog, ExerciseCatalogPath);
            }

            SerializedObject serialized = new(catalog);
            SerializedProperty exercises = serialized.FindProperty("exercises");
            exercises.arraySize = 5;
            exercises.GetArrayElementAtIndex(0).objectReferenceValue = first;
            exercises.GetArrayElementAtIndex(1).objectReferenceValue = second;
            exercises.GetArrayElementAtIndex(2).objectReferenceValue = third;
            exercises.GetArrayElementAtIndex(3).objectReferenceValue = fourth;
            exercises.GetArrayElementAtIndex(4).objectReferenceValue = fifth;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            selectedExercise = first;
            return catalog;
        }

        private static SpeechExerciseSO CreateExerciseAsset(
            string path,
            string prompt,
            string acceptedPhrases,
            string progressKey,
            params string[] words)
        {
            SpeechExerciseSO exercise = AssetDatabase.LoadAssetAtPath<SpeechExerciseSO>(path);
            if (exercise == null)
            {
                exercise = ScriptableObject.CreateInstance<SpeechExerciseSO>();
                AssetDatabase.CreateAsset(exercise, path);
            }

            SerializedObject serialized = new(exercise);
            serialized.FindProperty("prompt").stringValue = prompt;
            serialized.FindProperty("acceptedPhrases").stringValue = acceptedPhrases;
            serialized.FindProperty("progressKey").stringValue = progressKey;
            serialized.FindProperty("requireWordOrder").boolValue = true;
            serialized.FindProperty("allowFuzzyMatch").boolValue = true;
            serialized.FindProperty("silenceTimeoutSeconds").floatValue = 2.5f;

            SerializedProperty targetWords = serialized.FindProperty("targetWords");
            targetWords.arraySize = words.Length;
            for (int i = 0; i < words.Length; i++)
                targetWords.GetArrayElementAtIndex(i).stringValue = words[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(exercise);
            return exercise;
        }

        private static WhisperSettingsSO CreateWhisperSettings()
        {
            WhisperSettingsSO settings = AssetDatabase.LoadAssetAtPath<WhisperSettingsSO>(WhisperSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<WhisperSettingsSO>();
                AssetDatabase.CreateAsset(settings, WhisperSettingsPath);
            }

            SerializedObject serialized = new(settings);
            serialized.FindProperty("qualityProfile").enumValueIndex = (int) WhisperQualityProfile.Fast;
            serialized.FindProperty("fastModelPath").stringValue = "Whisper/ggml-tiny.en.bin";
            serialized.FindProperty("balancedModelPath").stringValue = "Whisper/ggml-base.en.bin";
            serialized.FindProperty("accurateModelPath").stringValue = "Whisper/ggml-small.en.bin";
            serialized.FindProperty("language").stringValue = "en";
            serialized.FindProperty("useGpu").boolValue = true;
            serialized.FindProperty("flashAttention").boolValue = true;
            serialized.FindProperty("streamingStepSeconds").floatValue = 1.25f;
            serialized.FindProperty("keepSeconds").floatValue = 0.2f;
            serialized.FindProperty("streamLengthSeconds").floatValue = 10f;
            serialized.FindProperty("updatePrompt").boolValue = true;
            serialized.FindProperty("dropOldBuffer").boolValue = false;
            serialized.FindProperty("useVad").boolValue = true;
            serialized.FindProperty("microphoneChunkSeconds").floatValue = 0.25f;
            serialized.FindProperty("warmupSeconds").floatValue = 1.25f;
            serialized.FindProperty("warmUpOnPrepare").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static WordChipView CreateWordChipPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChipPrefabPath);
            if (existing != null)
                return existing.GetComponent<WordChipView>();

            GameObject root = new("Word Chip", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 60f);
            Image background = root.GetComponent<Image>();
            background.color = SurfaceRaised;

            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = 150f;
            layout.preferredHeight = 60f;

            TextMeshProUGUI label = CreateText(
                rect, "Label", "word", 23, FontStyles.Bold, Cream,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);

            WordChipView chip = root.AddComponent<WordChipView>();
            SerializedObject serialized = new(chip);
            Set(serialized, "background", background);
            Set(serialized, "label", label);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ChipPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(ChipPrefabPath, ImportAssetOptions.ForceUpdate);

            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChipPrefabPath);
            WordChipView savedChip = savedPrefab != null
                ? savedPrefab.GetComponent<WordChipView>()
                : null;

            if (savedChip == null)
                throw new System.InvalidOperationException(
                    $"Unable to load WordChipView from {ChipPrefabPath}.");

            return savedChip;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Dropdown CreateDropdown(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string caption)
        {
            DefaultControls.Resources resources = CreateUiResources();
            GameObject dropdownObject = DefaultControls.CreateDropdown(resources);
            dropdownObject.name = name;
            dropdownObject.transform.SetParent(parent, false);

            RectTransform rect = dropdownObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            if (dropdown.captionText != null)
                dropdown.captionText.text = caption;

            TextMeshProUGUI label = dropdownObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = caption;

            return dropdown;
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

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color background,
            Color foreground,
            out TextMeshProUGUI text)
        {
            RectTransform rect = CreatePanel(parent, name, anchorMin, anchorMax, background);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.16f);
            button.colors = colors;
            text = CreateText(rect, "Label", label, 17, FontStyles.Bold, foreground,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), TextAlignmentOptions.Center);
            return button;
        }

        private static Toggle CreateToggle(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RectTransform box = CreatePanel(rect, "Box", new Vector2(0f, 0.18f), new Vector2(0.12f, 0.82f), SurfaceRaised);
            Image checkmark = CreatePanel(box, "Checkmark", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Mint)
                .GetComponent<Image>();
            CreateText(rect, "Label", label, 15, FontStyles.Normal, Ink,
                new Vector2(0.15f, 0f), Vector2.one, TextAlignmentOptions.Left);

            Toggle toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = checkmark;
            return toggle;
        }

        private static Material CreateRuntimeMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name,
                color = color
            };
            return material;
        }

        private static DefaultControls.Resources CreateUiResources()
        {
            return new DefaultControls.Resources
            {
                standard = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"),
                background = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd"),
                inputField = Resources.GetBuiltinResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = Resources.GetBuiltinResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = Resources.GetBuiltinResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = Resources.GetBuiltinResource<Sprite>("UI/Skin/UIMask.psd")
            };
        }

        private static void EnsureTmpResources()
        {
            if (TMP_Settings.instance == null)
                TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_FluentEcho");
            EnsureFolder(Root, "Demo");
            EnsureFolder(Root + "/Demo", "Scenes");
            EnsureFolder(Root + "/Demo", "Data");
            EnsureFolder(Root + "/Demo", "Prefabs");
            EnsureFolder(Root, "Docs");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void Set(SerializedObject serialized, string propertyName, Object value)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }
    }
}
