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
        private const string SixthExercisePath = Root + "/Demo/Data/SixthLesson.asset";
        private const string SeventhExercisePath = Root + "/Demo/Data/SeventhLesson.asset";
        private const string EighthExercisePath = Root + "/Demo/Data/EighthLesson.asset";
        private const string WhisperSettingsPath = Root + "/Demo/Data/WhisperSettings.asset";
        private const string ChipPrefabPath = Root + "/Demo/Prefabs/WordChip.prefab";
        private const string SyncSessionKey = "FluentEcho.SyncPrototypeSceneUiOnce.v2";

        private static readonly Color Background = Hex("08171D");
        private static readonly Color Surface = Hex("10272E");
        private static readonly Color SurfaceRaised = Hex("17353D");
        private static readonly Color Cream = Hex("F6F0DF");
        private static readonly Color Muted = Hex("A8BCC0");
        private static readonly Color Mint = Hex("33D69F");
        private static readonly Color Coral = Hex("FF8066");
        private static readonly Color Ink = Hex("071416");
        private static readonly Color SettingsFill = new(0.11f, 0.24f, 0.28f, 1f);
        private static readonly Color SettingsBorder = new(0.20f, 0.90f, 0.68f, 0.24f);
        private static readonly Color SettingsAccent = new(0.20f, 0.90f, 0.68f, 0.9f);
        private static readonly Color ResultFill = new(0.94f, 0.89f, 0.80f, 1f);
        private static readonly Color ResultBorder = new(0.89f, 0.37f, 0.30f, 0.20f);
        private static readonly Color ResultAccent = new(0.89f, 0.37f, 0.30f, 0.92f);

        [InitializeOnLoadMethod]
        private static void BuildOnFirstImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                    BuildPrototype();
            };
        }

        [InitializeOnLoadMethod]
        private static void SyncOpenPrototypeSceneOnReload()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (SessionState.GetBool(SyncSessionKey, false))
                    return;

                Scene scene = SceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                    return;

                SessionState.SetBool(SyncSessionKey, true);
                SyncCurrentPrototypeSceneUi();
            };
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;

            SyncCurrentPrototypeSceneUi();
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

        [MenuItem("Tools/Fluent Echo/Sync Current Prototype Scene UI")]
        public static void SyncCurrentPrototypeSceneUi()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                Debug.LogWarning("[FluentEcho] Open FluentEchoPrototype.unity before syncing scene UI.");
                return;
            }

            SpeechExerciseCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SpeechExerciseCatalogSO>(ExerciseCatalogPath);
            WhisperSettingsSO settings = AssetDatabase.LoadAssetAtPath<WhisperSettingsSO>(WhisperSettingsPath);
            RectTransform teacherCard = FindSceneRect(scene, "Teacher Card");
            RectTransform lessonCard = FindSceneRect(scene, "Lesson Card");
            if (teacherCard == null || lessonCard == null)
            {
                Debug.LogWarning("[FluentEcho] Scene UI sync skipped because Teacher Card or Lesson Card is missing.");
                return;
            }

            RectTransform settingsPanel = FindSceneRect(scene, "Settings Panel");
            if (settingsPanel == null)
                settingsPanel = CreatePanel(teacherCard, "Settings Panel", new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.34f), SettingsFill);

            settingsPanel.SetParent(teacherCard, false);
            SetAnchors(settingsPanel, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.34f));
            settingsPanel.GetComponent<Image>().color = SettingsFill;
            ApplyPanelFrame(settingsPanel, SettingsBorder);
            EnsureChildPanel(settingsPanel, "Settings Accent", new Vector2(0f, 0.92f), Vector2.one, SettingsAccent);
            EnsureChildPanel(settingsPanel, "Settings Divider", new Vector2(0.50f, 0.20f), new Vector2(0.505f, 0.84f), new Color(1f, 1f, 1f, 0.10f));
            EnsureText(settingsPanel, "Settings Title", "SPEECH SETTINGS", 11, FontStyles.Bold, Mint, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.94f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Microphone Label", "MICROPHONE", 11, FontStyles.Bold, Coral, new Vector2(0.05f, 0.58f), new Vector2(0.46f, 0.74f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Microphone Value", "Default microphone", 14, FontStyles.Bold, Cream, new Vector2(0.05f, 0.44f), new Vector2(0.46f, 0.56f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Whisper Profile Label", "WHISPER PROFILE", 11, FontStyles.Bold, Coral, new Vector2(0.54f, 0.58f), new Vector2(0.95f, 0.74f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Whisper Profile Value", settings != null ? settings.QualityProfile.ToString().ToUpperInvariant() : "FAST", 14, FontStyles.Bold, Cream, new Vector2(0.54f, 0.44f), new Vector2(0.95f, 0.56f), TextAlignmentOptions.Left);
            EnsureButton(settingsPanel, "Settings Close Button", "CLOSE", new Vector2(0.84f, 0.83f), new Vector2(0.96f, 0.95f), Coral, Ink);

            Dropdown microphoneDropdown = FindSceneDropdown(scene, "Microphone Dropdown");
            if (microphoneDropdown == null)
                microphoneDropdown = CreateDropdown(settingsPanel, "Microphone Dropdown", new Vector2(0.05f, 0.10f), new Vector2(0.46f, 0.42f), "Default microphone");

            microphoneDropdown.transform.SetParent(settingsPanel, false);
            SetAnchors(microphoneDropdown.GetComponent<RectTransform>(), new Vector2(0.05f, 0.10f), new Vector2(0.46f, 0.42f));
            SetDropdownOptions(microphoneDropdown, "Default microphone");
            StyleDropdown(microphoneDropdown, SettingsFill);

            Dropdown whisperProfileDropdown = FindSceneDropdown(scene, "Whisper Profile Dropdown");
            if (whisperProfileDropdown == null)
                whisperProfileDropdown = CreateDropdown(settingsPanel, "Whisper Profile Dropdown", new Vector2(0.54f, 0.10f), new Vector2(0.95f, 0.42f), "FAST");

            whisperProfileDropdown.transform.SetParent(settingsPanel, false);
            SetAnchors(whisperProfileDropdown.GetComponent<RectTransform>(), new Vector2(0.54f, 0.10f), new Vector2(0.95f, 0.42f));
            SetDropdownOptions(whisperProfileDropdown, "FAST", "BALANCED", "ACCURATE");
            StyleDropdown(whisperProfileDropdown, SettingsFill);
            settingsPanel.gameObject.SetActive(false);

            EnsureButton(teacherCard, "Settings Toggle Button", "SETTINGS", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.11f), SettingsFill, Cream);

            Dropdown lessonDropdown = FindSceneDropdown(scene, "Lesson Dropdown");
            if (lessonDropdown == null)
                lessonDropdown = CreateDropdown(lessonCard, "Lesson Dropdown", new Vector2(0.17f, 0.86f), new Vector2(0.78f, 0.93f), "Describe the Dog");

            lessonDropdown.transform.SetParent(lessonCard, false);
            SetAnchors(lessonDropdown.GetComponent<RectTransform>(), new Vector2(0.17f, 0.86f), new Vector2(0.78f, 0.93f));
            SetDropdownOptions(lessonDropdown, catalog != null ? catalog.GetDisplayNames() : new[] { "Describe the Dog" });
            StyleDropdown(lessonDropdown, SurfaceRaised);
            lessonDropdown.gameObject.SetActive(true);

            Transform staleLessonTitle = lessonCard.Find("Lesson Title Panel");
            if (staleLessonTitle != null)
                staleLessonTitle.gameObject.SetActive(false);

            Transform micLabel = lessonCard.Find("Mic Label");
            if (micLabel != null)
                micLabel.gameObject.SetActive(false);

            RectTransform resultPanel = FindSceneRect(scene, "Result Panel");
            if (resultPanel == null)
                resultPanel = CreatePanel(lessonCard, "Result Panel", new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.38f), ResultFill);

            resultPanel.SetParent(lessonCard, false);
            SetAnchors(resultPanel, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.38f));
            Image resultImage = resultPanel.GetComponent<Image>();
            if (resultImage != null)
                resultImage.color = ResultFill;

            ApplyPanelFrame(resultPanel, ResultBorder);
            EnsureChildPanel(resultPanel, "Result Accent", new Vector2(0f, 0.92f), Vector2.one, ResultAccent);
            EnsureChildPanel(resultPanel, "Result Divider", new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.75f), new Color(0.89f, 0.37f, 0.30f, 0.16f));
            EnsureText(resultPanel, "Result Header", "FINAL FEEDBACK", 10, FontStyles.Bold, Mint, new Vector2(0.04f, 0.79f), new Vector2(0.34f, 0.92f), TextAlignmentOptions.Left);
            EnsureText(resultPanel, "Result Badge", "CLEARED", 10, FontStyles.Bold, Coral, new Vector2(0.78f, 0.79f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Right);
            EnsureButton(resultPanel, "Result Close Button", "CLOSE", new Vector2(0.84f, 0.83f), new Vector2(0.96f, 0.95f), Coral, Ink);
            TextMeshProUGUI progressDetails = EnsureText(
                resultPanel,
                "Progress Details",
                "Recent attempts will appear here.",
                11,
                FontStyles.Normal,
                Ink,
                new Vector2(0.04f, 0.50f),
                new Vector2(0.96f, 0.72f),
                TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationSummary = EnsureText(
                resultPanel,
                "Pronunciation Summary",
                string.Empty,
                14,
                FontStyles.Bold,
                Coral,
                new Vector2(0.04f, 0.34f),
                new Vector2(0.96f, 0.48f),
                TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationFeedback = EnsureText(
                resultPanel,
                "Pronunciation Feedback",
                "Score feedback will appear here.",
                12,
                FontStyles.Italic,
                Ink,
                new Vector2(0.04f, 0.10f),
                new Vector2(0.96f, 0.30f),
                TextAlignmentOptions.Left);
            resultPanel.gameObject.SetActive(false);

            FluentEchoView view = Object.FindFirstObjectByType<FluentEchoView>(FindObjectsInactive.Include);
            if (view != null)
            {
                SerializedObject viewObject = new(view);
                Set(viewObject, "progressDetailsLabel", progressDetails);
                Set(viewObject, "pronunciationSummaryLabel", pronunciationSummary);
                Set(viewObject, "pronunciationFeedbackLabel", pronunciationFeedback);
                if (resultImage != null)
                    Set(viewObject, "successPanel", resultImage);
                viewObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
            }

            MicrophoneRecord microphone = Object.FindFirstObjectByType<MicrophoneRecord>(FindObjectsInactive.Include);
            if (microphone != null)
            {
                SerializedObject microphoneObject = new(microphone);
                Set(microphoneObject, "microphoneDropdown", microphoneDropdown);
                microphoneObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(microphone);
            }

            FluentEchoBootstrap bootstrap = Object.FindFirstObjectByType<FluentEchoBootstrap>(FindObjectsInactive.Include);
            if (bootstrap != null)
            {
                SerializedObject bootstrapObject = new(bootstrap);
                Set(bootstrapObject, "exerciseDropdown", lessonDropdown);
                Set(bootstrapObject, "whisperProfileDropdown", whisperProfileDropdown);
                bootstrapObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bootstrap);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FluentEcho] Current prototype scene UI synced for manual editing.");
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
                20, FontStyles.Normal, Cream,
                new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.29f), TextAlignmentOptions.TopLeft);

            RectTransform settingsPanel = CreatePanel(
                mentorCard, "Settings Panel", new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.34f), SettingsFill);
            ApplyPanelFrame(settingsPanel, SettingsBorder);
            CreatePanel(settingsPanel, "Settings Accent", new Vector2(0f, 0.92f), new Vector2(1f, 1f), SettingsAccent);
            CreatePanel(settingsPanel, "Settings Divider", new Vector2(0.50f, 0.20f), new Vector2(0.505f, 0.84f), new Color(1f, 1f, 1f, 0.10f));
            CreateText(settingsPanel, "Settings Title", "SPEECH SETTINGS", 11, FontStyles.Bold, Mint,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.94f), TextAlignmentOptions.Left);
            Button settingsCloseButton = CreateButton(
                settingsPanel, "Settings Close Button", "CLOSE",
                new Vector2(0.84f, 0.83f), new Vector2(0.96f, 0.95f), Coral, Ink, out _);
            CreateText(settingsPanel, "Microphone Label", "MICROPHONE", 11, FontStyles.Bold, Coral,
                new Vector2(0.05f, 0.58f), new Vector2(0.46f, 0.74f), TextAlignmentOptions.Left);
            CreateText(settingsPanel, "Microphone Value", "Microphone Array", 14, FontStyles.Bold, Cream,
                new Vector2(0.05f, 0.44f), new Vector2(0.46f, 0.56f), TextAlignmentOptions.Left);
            Dropdown microphoneDropdown = CreateDropdown(
                settingsPanel,
                "Microphone Dropdown",
                new Vector2(0.05f, 0.10f),
                new Vector2(0.46f, 0.42f),
                "Default microphone");
            SetDropdownOptions(microphoneDropdown, "Default microphone");
            CreateText(settingsPanel, "Whisper Profile Label", "WHISPER PROFILE", 11, FontStyles.Bold, Coral,
                new Vector2(0.54f, 0.58f), new Vector2(0.95f, 0.74f), TextAlignmentOptions.Left);
            CreateText(settingsPanel, "Whisper Profile Value", "FAST", 14, FontStyles.Bold, Cream,
                new Vector2(0.54f, 0.44f), new Vector2(0.95f, 0.56f), TextAlignmentOptions.Left);
            Dropdown whisperProfileDropdown = CreateDropdown(
                settingsPanel,
                "Whisper Profile Dropdown",
                new Vector2(0.54f, 0.10f),
                new Vector2(0.95f, 0.42f),
                "FAST");
            SetDropdownOptions(whisperProfileDropdown, "FAST", "BALANCED", "ACCURATE");
            settingsPanel.gameObject.SetActive(false);

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
            SetDropdownOptions(lessonDropdown, catalog.GetDisplayNames());
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
                lessonCard, "Result Panel", new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.38f), ResultFill)
                .GetComponent<Image>();
            ApplyPanelFrame(success.rectTransform, ResultBorder);
            CreatePanel(success.rectTransform, "Result Accent", new Vector2(0f, 0.92f), new Vector2(1f, 1f), ResultAccent);
            CreatePanel(success.rectTransform, "Result Divider", new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.75f), new Color(0.89f, 0.37f, 0.30f, 0.16f));
            CreateText(success.rectTransform, "Result Header", "FINAL FEEDBACK", 10, FontStyles.Bold, Mint,
                new Vector2(0.04f, 0.79f), new Vector2(0.34f, 0.92f), TextAlignmentOptions.Left);
            CreateText(success.rectTransform, "Result Badge", "CLEARED", 10, FontStyles.Bold, Coral,
                new Vector2(0.78f, 0.79f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Right);
            Button resultCloseButton = CreateButton(
                success.rectTransform, "Result Close Button", "CLOSE",
                new Vector2(0.84f, 0.83f), new Vector2(0.96f, 0.95f), Coral, Ink, out _);
            CreateText(success.rectTransform, "Result Title", "LEVEL COMPLETE", 14, FontStyles.Bold, Coral,
                new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Left);
            TextMeshProUGUI progressDetails = CreateText(
                success.rectTransform, "Progress Details", "Recent attempts will appear here.", 11, FontStyles.Normal, Ink,
                new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.72f), TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationSummary = CreateText(
                success.rectTransform, "Pronunciation Summary", string.Empty, 14, FontStyles.Bold, Coral,
                new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.48f), TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationFeedback = CreateText(
                success.rectTransform, "Pronunciation Feedback", "Score feedback will appear here.", 12, FontStyles.Italic, Ink,
                new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.30f), TextAlignmentOptions.Left);
            success.gameObject.SetActive(false);

            FluentEchoView view = lessonCard.gameObject.AddComponent<FluentEchoView>();
            SerializedObject serialized = new(view);
            Set(serialized, "promptLabel", prompt);
            Set(serialized, "progressLabel", progressLabel);
            Set(serialized, "progressDetailsLabel", progressDetails);
            Set(serialized, "pronunciationSummaryLabel", pronunciationSummary);
            Set(serialized, "pronunciationFeedbackLabel", pronunciationFeedback);
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
            Transform teacherCard = view.transform.parent != null ? view.transform.parent.Find("Teacher Card") : null;
            Transform settingsPanel = teacherCard != null ? teacherCard.Find("Settings Panel") : null;
            Dropdown microphoneDropdown = settingsPanel != null
                ? settingsPanel.Find("Microphone Dropdown")?.GetComponent<Dropdown>()
                : null;
            Set(microphoneObject, "microphoneDropdown", microphoneDropdown);
            microphoneObject.FindProperty("microphoneDefaultLabel").stringValue = "Default microphone";
            microphoneObject.FindProperty("rememberSelectedDevice").boolValue = true;
            microphoneObject.FindProperty("selectedDevicePrefsKey").stringValue = "FluentEcho.SelectedMicrophone";
            microphoneObject.ApplyModifiedPropertiesWithoutUndo();

            if (settingsPanel != null)
            {
                Transform existingLabel = settingsPanel.Find("Whisper Profile Label");
                if (existingLabel == null)
                {
                    CreateText(
                        settingsPanel,
                        "Whisper Profile Label",
                        "WHISPER PROFILE",
                        11,
                        FontStyles.Bold,
                        Coral,
                        new Vector2(0.54f, 0.48f),
                        new Vector2(0.95f, 0.68f),
                        TextAlignmentOptions.Left);
                }
            }

            Dropdown whisperProfileDropdown = settingsPanel != null
                ? settingsPanel.Find("Whisper Profile Dropdown")?.GetComponent<Dropdown>()
                : null;
            if (whisperProfileDropdown == null && settingsPanel != null)
            {
                whisperProfileDropdown = CreateDropdown(
                    settingsPanel,
                    "Whisper Profile Dropdown",
                    new Vector2(0.54f, 0.10f),
                    new Vector2(0.95f, 0.42f),
                    settings.QualityProfile.ToString().ToUpperInvariant());
            }
            else if (whisperProfileDropdown != null)
            {
                whisperProfileDropdown.captionText.text = settings.QualityProfile.ToString().ToUpperInvariant();
                SetDropdownOptions(whisperProfileDropdown, "FAST", "BALANCED", "ACCURATE");
            }

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
                "Describe the Dog",
                "the",
                "dog",
                "is",
                "big|large");

            SpeechExerciseSO second = CreateExerciseAsset(
                SecondExercisePath,
                "Say: The cat is small.",
                "the cat is small|the cat is little",
                "lesson_02_describe_the_cat",
                "Describe the Cat",
                "the",
                "cat",
                "is",
                "small|little");

            SpeechExerciseSO third = CreateExerciseAsset(
                ThirdExercisePath,
                "Say: I like this book.",
                "i like this book|i like this one",
                "lesson_03_like_this_book",
                "Like This Book",
                "i",
                "like",
                "this",
                "book|one");

            SpeechExerciseSO fourth = CreateExerciseAsset(
                FourthExercisePath,
                "Say: The apple is red.",
                "the apple is red|the apple is bright red",
                "lesson_04_the_apple_is_red",
                "Describe the Apple",
                "the",
                "apple",
                "is",
                "red|bright red");

            SpeechExerciseSO fifth = CreateExerciseAsset(
                FifthExercisePath,
                "Say: We are ready to go.",
                "we are ready to go|we are all ready to go",
                "lesson_05_ready_to_go",
                "Ready to Go",
                "we",
                "are",
                "ready",
                "to",
                "go");

            SpeechExerciseSO sixth = CreateExerciseAsset(
                SixthExercisePath,
                "Ask: What time is it?",
                "what time is it|could you tell me the time",
                "lesson_06_ask_the_time",
                "Ask the Time",
                "what",
                "time",
                "is",
                "it");

            SpeechExerciseSO seventh = CreateExerciseAsset(
                SeventhExercisePath,
                "Say: Please open the window.",
                "please open the window|open the window please",
                "lesson_07_open_the_window",
                "Open the Window",
                "please",
                "open",
                "the",
                "window");

            SpeechExerciseSO eighth = CreateExerciseAsset(
                EighthExercisePath,
                "Say: I need a glass of water.",
                "i need a glass of water|could i have some water",
                "lesson_08_need_water",
                "Need Water",
                "i",
                "need",
                "a",
                "glass",
                "of",
                "water");

            SpeechExerciseCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SpeechExerciseCatalogSO>(ExerciseCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SpeechExerciseCatalogSO>();
                AssetDatabase.CreateAsset(catalog, ExerciseCatalogPath);
            }

            SerializedObject serialized = new(catalog);
            SerializedProperty exercises = serialized.FindProperty("exercises");
            exercises.arraySize = 8;
            exercises.GetArrayElementAtIndex(0).objectReferenceValue = first;
            exercises.GetArrayElementAtIndex(1).objectReferenceValue = second;
            exercises.GetArrayElementAtIndex(2).objectReferenceValue = third;
            exercises.GetArrayElementAtIndex(3).objectReferenceValue = fourth;
            exercises.GetArrayElementAtIndex(4).objectReferenceValue = fifth;
            exercises.GetArrayElementAtIndex(5).objectReferenceValue = sixth;
            exercises.GetArrayElementAtIndex(6).objectReferenceValue = seventh;
            exercises.GetArrayElementAtIndex(7).objectReferenceValue = eighth;
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
            string displayName,
            params string[] words)
        {
            SpeechExerciseSO exercise = AssetDatabase.LoadAssetAtPath<SpeechExerciseSO>(path);
            if (exercise == null)
            {
                exercise = ScriptableObject.CreateInstance<SpeechExerciseSO>();
                AssetDatabase.CreateAsset(exercise, path);
            }

            exercise.name = displayName;

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
            serialized.FindProperty("useGpu").boolValue = false;
            serialized.FindProperty("flashAttention").boolValue = false;
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

            StyleDropdown(dropdown, SurfaceRaised);
            SetDropdownOptions(dropdown, caption);

            return dropdown;
        }

        private static void SetDropdownOptions(Dropdown dropdown, params string[] labels)
        {
            if (dropdown == null)
                return;

            dropdown.ClearOptions();
            var options = new System.Collections.Generic.List<Dropdown.OptionData>();
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

        private static RectTransform EnsureChildPanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            Transform existing = parent.Find(name);
            RectTransform rect = existing != null ? existing.GetComponent<RectTransform>() : null;
            if (rect == null)
                rect = CreatePanel(parent, name, anchorMin, anchorMax, color);

            rect.SetParent(parent, false);
            SetAnchors(rect, anchorMin, anchorMax);
            Image image = rect.GetComponent<Image>();
            if (image != null)
                image.color = color;
            return rect;
        }

        private static TextMeshProUGUI EnsureText(
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
            Transform existing = parent.Find(name);
            TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (text == null)
                return CreateText(parent, name, value, size, style, color, anchorMin, anchorMax, alignment);

            text.transform.SetParent(parent, false);
            SetAnchors(text.GetComponent<RectTransform>(), anchorMin, anchorMax);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color background,
            Color foreground)
        {
            Transform existing = parent.Find(name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
                return CreateButton(parent, name, label, anchorMin, anchorMax, background, foreground, out _);

            button.transform.SetParent(parent, false);
            SetAnchors(button.GetComponent<RectTransform>(), anchorMin, anchorMax);
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = background;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.color = foreground;
            }

            return button;
        }

        private static RectTransform FindSceneRect(Scene scene, string name)
        {
            Transform transform = FindSceneTransform(scene, name);
            return transform != null ? transform.GetComponent<RectTransform>() : null;
        }

        private static Dropdown FindSceneDropdown(Scene scene, string name)
        {
            Transform transform = FindSceneTransform(scene, name);
            return transform != null ? transform.GetComponent<Dropdown>() : null;
        }

        private static Transform FindSceneTransform(Scene scene, string name)
        {
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(name))
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindDeep(roots[i].transform, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindDeep(root.GetChild(i), name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyPanelFrame(RectTransform panel, Color borderColor)
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

        private static void StyleDropdown(Dropdown dropdown, Color fillColor)
        {
            if (dropdown == null)
                return;

            if (dropdown.targetGraphic != null)
                dropdown.targetGraphic.color = fillColor;

            if (dropdown.captionText != null)
            {
                dropdown.captionText.color = new Color(0.96f, 0.94f, 0.88f, 1f);
                dropdown.captionText.fontSize = 12;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = new Color(0.10f, 0.12f, 0.13f, 1f);
                dropdown.itemText.fontSize = 12;
            }
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
