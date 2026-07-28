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
        private const string ExerciseDataPath = Root + "/Demo/Data";
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
        private const string AvaPortraitPath = Root + "/Demo/Visuals/AvaCoachPortrait.png";
        private const string RoundedSurfacePath = Root + "/Demo/Visuals/RoundedSurface.png";
        private const string IconRoot = Root + "/Demo/Visuals/Icons";
        private const string ChevronLeftIconPath = IconRoot + "/ChevronLeft.png";
        private const string ChevronRightIconPath = IconRoot + "/ChevronRight.png";
        private const string MicrophoneIconPath = IconRoot + "/Microphone.png";
        private const string ChatIconPath = IconRoot + "/Chat.png";
        private const string SpeakerIconPath = IconRoot + "/Speaker.png";
        private const string WaveformIconPath = IconRoot + "/Waveform.png";
        private const string ShieldIconPath = IconRoot + "/Shield.png";
        private const string SettingsIconPath = IconRoot + "/Settings.png";
        private const string StatusIconPath = IconRoot + "/Status.png";
        private const string CloseIconPath = IconRoot + "/Close.png";
        private const string RetryIconPath = IconRoot + "/Retry.png";
        private const string ArrowRightIconPath = IconRoot + "/ArrowRight.png";
        private const string ChevronDownIconPath = IconRoot + "/ChevronDown.png";
        private static readonly Color Background = Hex("07171D");
        private static readonly Color Surface = Hex("0D252C");
        private static readonly Color SurfaceRaised = Hex("173740");
        private static readonly Color Cream = Hex("F8F3E7");
        private static readonly Color Muted = Hex("A9BDC1");
        private static readonly Color Mint = Hex("45DFAE");
        private static readonly Color MintInk = Hex("0F7358");
        private static readonly Color Coral = Hex("FF8066");
        private static readonly Color CoralInk = Hex("B84938");
        private static readonly Color Ink = Hex("102226");
        private static readonly Color SettingsFill = Hex("102C34");
        private static readonly Color SettingsBorder = new(0.27f, 0.87f, 0.68f, 0.34f);
        private static readonly Color SettingsAccent = new(0.27f, 0.87f, 0.68f, 0.96f);
        private static readonly Color ResultFill = Hex("F8F3E7");
        private static readonly Color ResultBorder = new(0.06f, 0.14f, 0.16f, 0.22f);
        private static readonly Color ResultAccent = new(0.27f, 0.87f, 0.68f, 0.96f);

        private readonly struct ExerciseSpec
        {
            public ExerciseSpec(
                string fileName,
                string displayName,
                string prompt,
                string acceptedPhrases,
                string progressKey,
                bool requireWordOrder,
                params string[] targetWords)
            {
                FileName = fileName;
                DisplayName = displayName;
                Prompt = prompt;
                AcceptedPhrases = acceptedPhrases;
                ProgressKey = progressKey;
                RequireWordOrder = requireWordOrder;
                TargetWords = targetWords;
            }

            public string FileName { get; }
            public string DisplayName { get; }
            public string Prompt { get; }
            public string AcceptedPhrases { get; }
            public string ProgressKey { get; }
            public bool RequireWordOrder { get; }
            public string[] TargetWords { get; }
        }

        private readonly struct CategorySpec
        {
            public CategorySpec(string displayName, string description, ExerciseSpec[] exercises)
            {
                DisplayName = displayName;
                Description = description;
                Exercises = exercises;
            }

            public string DisplayName { get; }
            public string Description { get; }
            public ExerciseSpec[] Exercises { get; }
        }

        private static readonly CategorySpec[] GuidedCategories =
        {
            new(
                "Words",
                "Start with focused single-word practice.",
                new[]
                {
                    new ExerciseSpec("Words_01_Apple.asset", "Apple", "Say the word: Apple.", "apple", "words_01_apple", false, "apple"),
                    new ExerciseSpec("Words_02_Window.asset", "Window", "Say the word: Window.", "window", "words_02_window", false, "window"),
                    new ExerciseSpec("Words_03_Family.asset", "Family", "Say the word: Family.", "family", "words_03_family", false, "family"),
                    new ExerciseSpec("Words_04_School.asset", "School", "Say the word: School.", "school", "words_04_school", false, "school"),
                    new ExerciseSpec("Words_05_Water.asset", "Water", "Say the word: Water.", "water", "words_05_water", false, "water"),
                    new ExerciseSpec("Words_06_Friend.asset", "Friend", "Say the word: Friend.", "friend", "words_06_friend", false, "friend"),
                    new ExerciseSpec("Words_07_Morning.asset", "Morning", "Say the word: Morning.", "morning", "words_07_morning", false, "morning"),
                    new ExerciseSpec("Words_08_Garden.asset", "Garden", "Say the word: Garden.", "garden", "words_08_garden", false, "garden"),
                    new ExerciseSpec("Words_09_Question.asset", "Question", "Say the word: Question.", "question", "words_09_question", false, "question"),
                    new ExerciseSpec("Words_10_Practice.asset", "Practice", "Say the word: Practice.", "practice", "words_10_practice", false, "practice")
                }),
            new(
                "Short Sentences",
                "Build confidence with short, clear lines.",
                new[]
                {
                    new ExerciseSpec("Short_01_Dog.asset", "The Dog Is Big", "Say: The dog is big.", "the dog is big|the dog is large", "short_01_dog_is_big", true, "the", "dog", "is", "big|large"),
                    new ExerciseSpec("Short_02_Cat.asset", "The Cat Is Small", "Say: The cat is small.", "the cat is small|the cat is little", "short_02_cat_is_small", true, "the", "cat", "is", "small|little"),
                    new ExerciseSpec("Short_03_Book.asset", "I Like This Book", "Say: I like this book.", "i like this book|i like this one", "short_03_like_this_book", true, "i", "like", "this", "book|one"),
                    new ExerciseSpec("Short_04_Apple.asset", "The Apple Is Red", "Say: The apple is red.", "the apple is red|the apple is bright red", "short_04_apple_is_red", true, "the", "apple", "is", "red|bright red"),
                    new ExerciseSpec("Short_05_Ready.asset", "We Are Ready", "Say: We are ready to go.", "we are ready to go|we are all ready to go", "short_05_ready_to_go", true, "we", "are", "ready", "to", "go"),
                    new ExerciseSpec("Short_06_Time.asset", "What Time Is It", "Ask: What time is it?", "what time is it|could you tell me the time", "short_06_what_time_is_it", true, "what", "time", "is", "it"),
                    new ExerciseSpec("Short_07_Window.asset", "Open The Window", "Say: Please open the window.", "please open the window|open the window please", "short_07_open_window", true, "please", "open", "the", "window"),
                    new ExerciseSpec("Short_08_Water.asset", "Need Water", "Say: I need a glass of water.", "i need a glass of water|could i have some water", "short_08_need_water", true, "i", "need", "a", "glass", "of", "water"),
                    new ExerciseSpec("Short_09_School.asset", "I Walk To School", "Say: I walk to school.", "i walk to school|i go to school", "short_09_walk_to_school", true, "i", "walk|go", "to", "school"),
                    new ExerciseSpec("Short_10_Friend.asset", "My Friend Is Here", "Say: My friend is here.", "my friend is here|my friend is there", "short_10_friend_is_here", true, "my", "friend", "is", "here|there")
                }),
            new(
                "Challenge Sentences",
                "Practice longer lines with smoother rhythm.",
                new[]
                {
                    new ExerciseSpec("Challenge_01_Morning.asset", "Morning Tea", "Say: I usually drink tea before school.", "i usually drink tea before school|i usually have tea before school", "challenge_01_morning_tea", true, "i", "usually", "drink|have", "tea", "before", "school"),
                    new ExerciseSpec("Challenge_02_Park.asset", "Going To The Park", "Say: My friend and I are going to the park after lunch.", "my friend and i are going to the park after lunch|my friend and i go to the park after lunch", "challenge_02_park_after_lunch", true, "my", "friend", "and", "i", "are", "going|go", "to", "the", "park", "after", "lunch"),
                    new ExerciseSpec("Challenge_03_Window.asset", "Close The Window", "Say: Please close the window because it is cold outside.", "please close the window because it is cold outside|close the window because it is cold outside please", "challenge_03_close_window", true, "please", "close", "the", "window", "because", "it", "is", "cold", "outside"),
                    new ExerciseSpec("Challenge_04_Homework.asset", "Homework First", "Say: I want to finish my homework before dinner.", "i want to finish my homework before dinner|i would like to finish my homework before dinner", "challenge_04_homework", true, "i", "want|would like", "to", "finish", "my", "homework", "before", "dinner"),
                    new ExerciseSpec("Challenge_05_Question.asset", "Good Question", "Say: That is a good question, but I need more time.", "that is a good question but i need more time|that is a good question and i need more time", "challenge_05_good_question", true, "that", "is", "a", "good", "question", "but|and", "i", "need", "more", "time"),
                    new ExerciseSpec("Challenge_06_Directions.asset", "Train Station", "Say: Could you show me the way to the train station?", "could you show me the way to the train station|can you show me the way to the train station", "challenge_06_train_station", true, "could|can", "you", "show", "me", "the", "way", "to", "the", "train", "station"),
                    new ExerciseSpec("Challenge_07_Practice.asset", "Practice Every Day", "Say: I practice English every day to speak more clearly.", "i practice english every day to speak more clearly|i practise english every day to speak more clearly", "challenge_07_practice_every_day", true, "i", "practice|practise", "english", "every", "day", "to", "speak", "more", "clearly"),
                    new ExerciseSpec("Challenge_08_Movie.asset", "Movie After Class", "Say: After class, we can watch a movie together.", "after class we can watch a movie together|after the class we can watch a movie together", "challenge_08_movie_after_class", true, "after", "class", "we", "can", "watch", "a", "movie", "together")
                })
        };

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

        [MenuItem("Tools/Fluent Echo/Sync Current Prototype Scene UI")]
        public static void SyncCurrentPrototypeSceneUi()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                Debug.LogWarning("[FluentEcho] Open FluentEchoPrototype.unity before syncing scene UI.");
                return;
            }

            SpeechExerciseCatalogSO catalog = CreateExerciseCatalog(out _);
            WhisperSettingsSO settings = AssetDatabase.LoadAssetAtPath<WhisperSettingsSO>(WhisperSettingsPath);
            RectTransform teacherCard = FindSceneRect(scene, "Teacher Card");
            RectTransform lessonCard = FindSceneRect(scene, "Lesson Card");
            RectTransform canvasRoot = teacherCard != null ? teacherCard.parent as RectTransform : null;
            if (teacherCard == null || lessonCard == null)
            {
                Debug.LogWarning("[FluentEcho] Scene UI sync skipped because Teacher Card or Lesson Card is missing.");
                return;
            }

            ApplyRoundedSurface(teacherCard);
            ApplyRoundedSurface(lessonCard);
            ConfigureCoachPortrait(scene);

            RectTransform settingsPanel = FindSceneRect(scene, "Settings Panel");
            if (settingsPanel == null)
            {
                settingsPanel = CreatePanel(canvasRoot, "Settings Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), SettingsFill);
                SetCenteredSize(settingsPanel, new Vector2(860f, 440f));
            }

            settingsPanel.GetComponent<Image>().color = SettingsFill;
            ApplyRoundedSurface(settingsPanel);
            ApplyPanelFrame(settingsPanel, SettingsBorder);
            EnsureChildPanel(settingsPanel, "Settings Accent", new Vector2(0f, 0.975f), Vector2.one, SettingsAccent);
            EnsureChildPanel(settingsPanel, "Settings Divider", new Vector2(0.50f, 0.18f), new Vector2(0.502f, 0.72f), new Color(1f, 1f, 1f, 0.10f));
            EnsureText(settingsPanel, "Settings Title", "SPEECH SETTINGS", 28, FontStyles.Bold, Cream, new Vector2(0.06f, 0.78f), new Vector2(0.72f, 0.93f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Microphone Label", "INPUT DEVICE", 12, FontStyles.Bold, Mint, new Vector2(0.06f, 0.58f), new Vector2(0.45f, 0.70f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Microphone Value", "Default microphone", 16, FontStyles.Bold, Cream, new Vector2(0.06f, 0.46f), new Vector2(0.45f, 0.57f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Whisper Profile Label", "RECOGNITION PROFILE", 12, FontStyles.Bold, Mint, new Vector2(0.55f, 0.58f), new Vector2(0.94f, 0.70f), TextAlignmentOptions.Left);
            EnsureText(settingsPanel, "Whisper Profile Value", settings != null ? settings.QualityProfile.ToString().ToUpperInvariant() : "FAST", 16, FontStyles.Bold, Cream, new Vector2(0.55f, 0.46f), new Vector2(0.94f, 0.57f), TextAlignmentOptions.Left);
            Button settingsCloseButton = EnsureButton(settingsPanel, "Settings Close Button", "CLOSE", new Vector2(0.81f, 0.80f), new Vector2(0.94f, 0.92f), SurfaceRaised, Cream);
            EnsureIcon(settingsCloseButton.transform, "Close Icon", CloseIconPath,
                new Vector2(0.14f, 0.36f), new Vector2(0.26f, 0.64f), Cream);
            SetButtonLabelInsets(settingsCloseButton, 0.30f, 0.96f);

            Dropdown microphoneDropdown = FindSceneDropdown(scene, "Microphone Dropdown");
            bool createdMicrophoneDropdown = false;
            if (microphoneDropdown == null)
            {
                microphoneDropdown = CreateDropdown(settingsPanel, "Microphone Dropdown", new Vector2(0.06f, 0.18f), new Vector2(0.45f, 0.42f), "Default microphone");
                createdMicrophoneDropdown = true;
            }

            if (microphoneDropdown.transform.parent != settingsPanel)
                microphoneDropdown.transform.SetParent(settingsPanel, false);
            if (createdMicrophoneDropdown)
                SetAnchors(microphoneDropdown.GetComponent<RectTransform>(), new Vector2(0.06f, 0.18f), new Vector2(0.45f, 0.42f));
            SetDropdownOptions(microphoneDropdown, "Default microphone");
            StyleDropdown(microphoneDropdown, SettingsFill);

            Dropdown whisperProfileDropdown = FindSceneDropdown(scene, "Whisper Profile Dropdown");
            bool createdWhisperProfileDropdown = false;
            if (whisperProfileDropdown == null)
            {
                whisperProfileDropdown = CreateDropdown(settingsPanel, "Whisper Profile Dropdown", new Vector2(0.55f, 0.18f), new Vector2(0.94f, 0.42f), "FAST");
                createdWhisperProfileDropdown = true;
            }

            if (whisperProfileDropdown.transform.parent != settingsPanel)
                whisperProfileDropdown.transform.SetParent(settingsPanel, false);
            if (createdWhisperProfileDropdown)
                SetAnchors(whisperProfileDropdown.GetComponent<RectTransform>(), new Vector2(0.55f, 0.18f), new Vector2(0.94f, 0.42f));
            SetDropdownOptions(whisperProfileDropdown, "FAST", "BALANCED", "ACCURATE");
            StyleDropdown(whisperProfileDropdown, SettingsFill);
            settingsPanel.gameObject.SetActive(false);

            EnsureButton(canvasRoot, "Settings Toggle Button", "SETTINGS", new Vector2(0.83f, 0.90f), new Vector2(0.965f, 0.96f), SurfaceRaised, Cream);
            RectTransform categoryScreen = EnsureCategoryScreen(
                canvasRoot,
                out Button wordsCategoryButton,
                out Button shortSentencesCategoryButton,
                out Button challengeCategoryButton);

            Dropdown lessonDropdown = FindSceneDropdown(scene, "Lesson Dropdown");
            if (lessonDropdown == null)
            {
                lessonDropdown = CreateDropdown(lessonCard, "Lesson Dropdown", new Vector2(0.17f, 0.86f), new Vector2(0.78f, 0.93f), "Describe the Dog");
            }

            if (lessonDropdown.transform.parent != lessonCard)
                lessonDropdown.transform.SetParent(lessonCard, false);
            SetAnchors(lessonDropdown.GetComponent<RectTransform>(), new Vector2(0.35f, 0.86f), new Vector2(0.78f, 0.93f));
            SetDropdownOptions(lessonDropdown, catalog != null ? catalog.GetCategoryExerciseDisplayNames(0) : new[] { "Apple" });
            StyleDropdown(lessonDropdown, SurfaceRaised);
            lessonDropdown.gameObject.SetActive(true);

            Button categoriesButton = EnsureButton(
                lessonCard,
                "Category Back Button",
                "CATEGORIES",
                new Vector2(0.06f, 0.86f),
                new Vector2(0.24f, 0.93f),
                Hex("F2ECDF"),
                Ink);
            EnsureIcon(categoriesButton.transform, "Categories Icon", ChevronLeftIconPath,
                new Vector2(0.10f, 0.38f), new Vector2(0.18f, 0.62f), Ink);
            SetButtonLabelInsets(categoriesButton, 0.24f, 0.96f);

            RectTransform previousButtonRect = FindSceneRect(scene, "Lesson Previous Button");
            if (previousButtonRect != null)
                SetAnchors(previousButtonRect, new Vector2(0.25f, 0.86f), new Vector2(0.34f, 0.93f));

            Transform staleLessonTitle = lessonCard.Find("Lesson Title Panel");
            if (staleLessonTitle != null)
                staleLessonTitle.gameObject.SetActive(false);

            Transform micLabel = lessonCard.Find("Mic Label");
            if (micLabel != null)
                micLabel.gameObject.SetActive(false);

            Transform legacyWhisperLabel = lessonCard.Find("Whisper Profile Label");
            if (legacyWhisperLabel != null)
                legacyWhisperLabel.gameObject.SetActive(false);

            RectTransform resultPanel = FindSceneRect(scene, "Result Panel");
            if (resultPanel == null)
            {
                resultPanel = CreatePanel(canvasRoot, "Result Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ResultFill);
                SetCenteredSize(resultPanel, new Vector2(960f, 680f));
            }

            Image resultImage = resultPanel.GetComponent<Image>();
            if (resultImage != null)
                resultImage.color = ResultFill;
            ApplyRoundedSurface(resultPanel);
            CanvasGroup resultGroup = resultPanel.GetComponent<CanvasGroup>();
            if (resultGroup == null)
                resultGroup = resultPanel.gameObject.AddComponent<CanvasGroup>();

            ApplyPanelFrame(resultPanel, ResultBorder);
            EnsureChildPanel(resultPanel, "Result Accent", new Vector2(0f, 0.975f), Vector2.one, ResultAccent);
            EnsureChildPanel(resultPanel, "Result Divider", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.723f), new Color(0.06f, 0.14f, 0.16f, 0.12f));
            EnsureText(resultPanel, "Result Header", "MISSION COMPLETE", 28, FontStyles.Bold, Ink, new Vector2(0.06f, 0.80f), new Vector2(0.58f, 0.93f), TextAlignmentOptions.Left);
            EnsureText(resultPanel, "Result Badge", "CLEARED", 13, FontStyles.Bold, MintInk, new Vector2(0.68f, 0.80f), new Vector2(0.80f, 0.92f), TextAlignmentOptions.Center);
            Button resultCloseButton = EnsureButton(resultPanel, "Result Close Button", "CLOSE", new Vector2(0.82f, 0.80f), new Vector2(0.94f, 0.92f), SurfaceRaised, Cream);
            EnsureIcon(resultCloseButton.transform, "Close Icon", CloseIconPath,
                new Vector2(0.14f, 0.36f), new Vector2(0.26f, 0.64f), Cream);
            SetButtonLabelInsets(resultCloseButton, 0.30f, 0.96f);
            TextMeshProUGUI progressDetails = EnsureText(
                resultPanel,
                "Progress Details",
                "Your first recording will appear here.",
                17,
                FontStyles.Normal,
                Ink,
                new Vector2(0.06f, 0.50f),
                new Vector2(0.94f, 0.69f),
                TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationSummary = EnsureText(
                resultPanel,
                "Pronunciation Summary",
                string.Empty,
                30,
                FontStyles.Bold,
                CoralInk,
                new Vector2(0.06f, 0.34f),
                new Vector2(0.94f, 0.49f),
                TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationFeedback = EnsureText(
                resultPanel,
                "Pronunciation Feedback",
                "Your coach tip will appear here.",
                18,
                FontStyles.Italic,
                Ink,
                new Vector2(0.06f, 0.19f),
                new Vector2(0.94f, 0.33f),
                TextAlignmentOptions.Left);
            Button resultTryAgainButton = EnsureButton(
                resultPanel,
                "Result Try Again Button",
                "TRY AGAIN",
                new Vector2(0.06f, 0.05f),
                new Vector2(0.36f, 0.15f),
                SurfaceRaised,
                Cream);
            EnsureIcon(resultTryAgainButton.transform, "Retry Icon", RetryIconPath,
                new Vector2(0.12f, 0.34f), new Vector2(0.22f, 0.66f), Cream);
            SetButtonLabelInsets(resultTryAgainButton, 0.25f, 0.96f);
            Button resultNextButton = EnsureButton(
                resultPanel,
                "Result Next Mission Button",
                "NEXT MISSION",
                new Vector2(0.38f, 0.05f),
                new Vector2(0.72f, 0.15f),
                Mint,
                Ink);
            EnsureIcon(resultNextButton.transform, "Next Icon", ArrowRightIconPath,
                new Vector2(0.80f, 0.36f), new Vector2(0.89f, 0.64f), Ink);
            SetButtonLabelInsets(resultNextButton, 0.04f, 0.76f);
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
                Set(viewObject, "successPanelGroup", resultGroup);
                Set(viewObject, "successPanelRect", resultPanel);
                Set(viewObject, "resultCloseButton", resultCloseButton);
                Set(viewObject, "resultTryAgainButton", resultTryAgainButton);
                Set(viewObject, "resultNextButton", resultNextButton);
                Set(viewObject, "categoriesButton", categoriesButton);
                Set(viewObject, "wordsCategoryButton", wordsCategoryButton);
                Set(viewObject, "shortSentencesCategoryButton", shortSentencesCategoryButton);
                Set(viewObject, "challengeCategoryButton", challengeCategoryButton);
                Set(viewObject, "lessonDropdown", lessonDropdown);
                Set(viewObject, "categoryScreen", categoryScreen.gameObject);
                Set(viewObject, "selectedCategoryLabel", FindDeep(categoryScreen, "Selected Category Label")?.GetComponent<TextMeshProUGUI>());
                Set(viewObject, "selectedCategoryDescriptionLabel", FindDeep(categoryScreen, "Selected Category Description")?.GetComponent<TextMeshProUGUI>());
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
                Set(bootstrapObject, "exercise", catalog != null ? catalog.GetCategoryExercise(0, 0) : null);
                Set(bootstrapObject, "exerciseCatalog", catalog);
                Set(bootstrapObject, "exerciseDropdown", lessonDropdown);
                Set(bootstrapObject, "whisperProfileDropdown", whisperProfileDropdown);
                bootstrapObject.FindProperty("selectedCategoryPrefsKey").stringValue = "FluentEcho.SelectedCategoryIndex";
                bootstrapObject.FindProperty("selectedExercisePrefsKey").stringValue = "FluentEcho.SelectedExerciseIndex";
                bootstrapObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bootstrap);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FluentEcho] Current prototype scene UI synced for manual editing.");
        }

        public static void SyncPrototypeSceneAsset()
        {
            EnsureFolders();
            EnsureTmpResources();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SyncCurrentPrototypeSceneUi();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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

            CreateText(root, "Brand", "FLUENT ECHO", 32, FontStyles.Bold, Cream,
                new Vector2(0.045f, 0.90f), new Vector2(0.35f, 0.96f), TextAlignmentOptions.Left);
            CreateText(root, "Subtitle", "PRIVATE OFFLINE SPEECH PRACTICE", 13, FontStyles.Bold, Mint,
                new Vector2(0.045f, 0.855f), new Vector2(0.48f, 0.895f), TextAlignmentOptions.Left);

            RectTransform mentorCard = CreatePanel(
                root, "Teacher Card", new Vector2(0.045f, 0.12f), new Vector2(0.31f, 0.82f), Surface);
            ApplyRoundedSurface(mentorCard);
            RectTransform portraitField = CreatePanel(
                mentorCard, "Portrait Field", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.92f), SurfaceRaised);
            ApplyRoundedSurface(portraitField);
            Image avatar = CreatePanel(
                mentorCard, "Avatar", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.92f), Color.white)
                .GetComponent<Image>();
            avatar.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AvaPortraitPath);
            avatar.preserveAspect = true;
            avatar.raycastTarget = false;
            CreateText(mentorCard, "Teacher Name", "AVA  /  SPEECH COACH", 17, FontStyles.Bold, Mint,
                new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.38f), TextAlignmentOptions.Left);
            CreateText(mentorCard, "Teacher Note",
                "Practice privately. Your voice is processed locally and never leaves this device.",
                18, FontStyles.Normal, Cream,
                new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.29f), TextAlignmentOptions.TopLeft);
            Button settingsToggleButton = CreateButton(
                root, "Settings Toggle Button", "SETTINGS",
                new Vector2(0.83f, 0.90f), new Vector2(0.965f, 0.96f), SurfaceRaised, Cream, out _);
            CreateIcon(settingsToggleButton.transform, "Settings Icon", SettingsIconPath,
                new Vector2(0.12f, 0.34f), new Vector2(0.20f, 0.66f), Cream);
            SetButtonLabelInsets(settingsToggleButton, 0.32f, 0.96f);

            RectTransform settingsPanel = CreatePanel(
                root, "Settings Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), SettingsFill);
            SetCenteredSize(settingsPanel, new Vector2(860f, 440f));
            ApplyRoundedSurface(settingsPanel);
            ApplyPanelFrame(settingsPanel, SettingsBorder);
            CreatePanel(settingsPanel, "Settings Accent", new Vector2(0f, 0.975f), new Vector2(1f, 1f), SettingsAccent);
            CreatePanel(settingsPanel, "Settings Divider", new Vector2(0.50f, 0.18f), new Vector2(0.502f, 0.72f), new Color(1f, 1f, 1f, 0.10f));
            CreateText(settingsPanel, "Settings Title", "SPEECH SETTINGS", 28, FontStyles.Bold, Cream,
                new Vector2(0.06f, 0.78f), new Vector2(0.72f, 0.93f), TextAlignmentOptions.Left);
            Button settingsCloseButton = CreateButton(
                settingsPanel, "Settings Close Button", "CLOSE",
                new Vector2(0.81f, 0.80f), new Vector2(0.94f, 0.92f), SurfaceRaised, Cream, out _);
            CreateIcon(settingsCloseButton.transform, "Close Icon", CloseIconPath,
                new Vector2(0.14f, 0.36f), new Vector2(0.26f, 0.64f), Cream);
            SetButtonLabelInsets(settingsCloseButton, 0.30f, 0.96f);
            CreateText(settingsPanel, "Microphone Label", "INPUT DEVICE", 12, FontStyles.Bold, Mint,
                new Vector2(0.06f, 0.58f), new Vector2(0.45f, 0.70f), TextAlignmentOptions.Left);
            CreateText(settingsPanel, "Microphone Value", "Microphone Array", 16, FontStyles.Bold, Cream,
                new Vector2(0.06f, 0.46f), new Vector2(0.45f, 0.57f), TextAlignmentOptions.Left);
            Dropdown microphoneDropdown = CreateDropdown(
                settingsPanel,
                "Microphone Dropdown",
                new Vector2(0.06f, 0.18f),
                new Vector2(0.45f, 0.42f),
                "Default microphone");
            SetDropdownOptions(microphoneDropdown, "Default microphone");
            CreateText(settingsPanel, "Whisper Profile Label", "RECOGNITION PROFILE", 12, FontStyles.Bold, Mint,
                new Vector2(0.55f, 0.58f), new Vector2(0.94f, 0.70f), TextAlignmentOptions.Left);
            CreateText(settingsPanel, "Whisper Profile Value", "FAST", 16, FontStyles.Bold, Cream,
                new Vector2(0.55f, 0.46f), new Vector2(0.94f, 0.57f), TextAlignmentOptions.Left);
            Dropdown whisperProfileDropdown = CreateDropdown(
                settingsPanel,
                "Whisper Profile Dropdown",
                new Vector2(0.55f, 0.18f),
                new Vector2(0.94f, 0.42f),
                "FAST");
            SetDropdownOptions(whisperProfileDropdown, "FAST", "BALANCED", "ACCURATE");
            settingsPanel.gameObject.SetActive(false);

            RectTransform lessonCard = CreatePanel(
                root, "Lesson Card", new Vector2(0.335f, 0.075f), new Vector2(0.965f, 0.88f), Cream);
            ApplyRoundedSurface(lessonCard);
            Button lessonPreviousButton = CreateButton(
                lessonCard, "Lesson Previous Button", "PREV",
                new Vector2(0.25f, 0.86f), new Vector2(0.34f, 0.93f), Hex("F2ECDF"), Ink, out _);
            CreateIcon(lessonPreviousButton.transform, "Previous Icon", ChevronLeftIconPath,
                new Vector2(0.12f, 0.38f), new Vector2(0.23f, 0.62f), Ink);
            SetButtonLabelInsets(lessonPreviousButton, 0.30f, 0.96f);
            Button categoriesButton = CreateButton(
                lessonCard, "Category Back Button", "CATEGORIES",
                new Vector2(0.06f, 0.86f), new Vector2(0.24f, 0.93f), Hex("F2ECDF"), Ink, out _);
            CreateIcon(categoriesButton.transform, "Categories Icon", ChevronLeftIconPath,
                new Vector2(0.10f, 0.38f), new Vector2(0.18f, 0.62f), Ink);
            SetButtonLabelInsets(categoriesButton, 0.24f, 0.96f);
            lessonDropdown = CreateDropdown(
                lessonCard,
                "Lesson Dropdown",
                new Vector2(0.35f, 0.86f),
                new Vector2(0.78f, 0.93f),
                selectedExercise != null ? selectedExercise.name.ToUpperInvariant() : "LESSON 01");
            SetDropdownOptions(lessonDropdown, catalog.GetCategoryExerciseDisplayNames(0));
            Button lessonNextButton = CreateButton(
                lessonCard, "Lesson Next Button", "NEXT",
                new Vector2(0.80f, 0.86f), new Vector2(0.94f, 0.93f), SurfaceRaised, Cream, out _);
            CreateIcon(lessonNextButton.transform, "Next Icon", ChevronRightIconPath,
                new Vector2(0.78f, 0.38f), new Vector2(0.87f, 0.62f), Cream);
            SetButtonLabelInsets(lessonNextButton, 0.04f, 0.72f);

            TextMeshProUGUI progressLabel = CreateText(lessonCard, "Progress", "Lesson 1 / 10", 14, FontStyles.Bold, Ink,
                new Vector2(0.31f, 0.86f), new Vector2(0.43f, 0.93f), TextAlignmentOptions.Center);
            RectTransform progressTrack = CreatePanel(
                lessonCard, "Lesson Progress Track", new Vector2(0.52f, 0.88f), new Vector2(0.68f, 0.91f), Hex("DED7C8"));
            ApplyRoundedSurface(progressTrack);
            Image progressFill = CreatePanel(
                progressTrack, "Lesson Progress Fill", Vector2.zero, new Vector2(0.125f, 1f), Mint)
                .GetComponent<Image>();
            ApplyRoundedSurface(progressFill.rectTransform);
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
            ApplyRoundedSurface(transcriptPanel);
            CreateText(transcriptPanel, "Heard Label", "YOUR TRANSCRIPT", 13, FontStyles.Bold, CoralInk,
                new Vector2(0.10f, 0.64f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Left);
            CreateIcon(transcriptPanel, "Transcript Icon", WaveformIconPath,
                new Vector2(0.04f, 0.72f), new Vector2(0.07f, 0.88f), Coral);
            TextMeshProUGUI transcript = CreateText(
                transcriptPanel, "Transcript", "Your transcript will appear here.", 24, FontStyles.Italic, Ink,
                new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.64f), TextAlignmentOptions.Left);

            RectTransform statusRail = CreatePanel(
                lessonCard, "Status Rail", new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.34f), Hex("F2ECDF"));
            ApplyRoundedSurface(statusRail);
            CreateIcon(statusRail, "Status Dot", StatusIconPath,
                new Vector2(0.025f, 0.38f), new Vector2(0.055f, 0.62f), Coral);
            CreateIcon(statusRail, "Model Icon", WaveformIconPath,
                new Vector2(0.37f, 0.38f), new Vector2(0.40f, 0.62f), MintInk);
            CreateIcon(statusRail, "Privacy Icon", ShieldIconPath,
                new Vector2(0.71f, 0.36f), new Vector2(0.74f, 0.64f), MintInk);
            CreateText(statusRail, "Privacy Label", "PRIVATE  /  100% LOCAL", 13, FontStyles.Bold, Ink,
                new Vector2(0.75f, 0.18f), new Vector2(0.98f, 0.82f), TextAlignmentOptions.Left);

            TextMeshProUGUI status = CreateText(
                lessonCard, "Status", "Preparing speech model...", 14, FontStyles.Normal, Ink,
                new Vector2(0.11f, 0.23f), new Vector2(0.38f, 0.33f), TextAlignmentOptions.Left);
            TextMeshProUGUI modeLabel = CreateText(lessonCard, "Mode", "LOCAL WHISPER", 14, FontStyles.Bold, Ink,
                new Vector2(0.47f, 0.23f), new Vector2(0.68f, 0.33f), TextAlignmentOptions.Left);

            Image recordingIndicator = CreatePanel(
                lessonCard, "Recording Indicator", new Vector2(0.06f, 0.155f), new Vector2(0.075f, 0.185f), Coral)
                .GetComponent<Image>();
            recordingIndicator.gameObject.SetActive(false);

            Button micButton = CreateButton(
                lessonCard, "Mic Button", "START SPEAKING",
                new Vector2(0.06f, 0.07f), new Vector2(0.48f, 0.19f), Mint, Ink, out TextMeshProUGUI micLabel);
            CreateIcon(micButton.transform, "Microphone Icon", MicrophoneIconPath,
                new Vector2(0.13f, 0.37f), new Vector2(0.17f, 0.63f), Ink);
            SetButtonLabelInsets(micButton, 0.22f, 0.96f);
            Button demoButton = CreateButton(
                lessonCard, "Demo Button", "SHOW DEMO",
                new Vector2(0.50f, 0.07f), new Vector2(0.72f, 0.19f), Hex("F2ECDF"), Ink, out _);
            CreateIcon(demoButton.transform, "Demo Icon", ChatIconPath,
                new Vector2(0.13f, 0.39f), new Vector2(0.20f, 0.61f), Ink);
            SetButtonLabelInsets(demoButton, 0.25f, 0.96f);
            Button listenButton = CreateButton(
                lessonCard, "Listen Button", "LISTEN",
                new Vector2(0.74f, 0.07f), new Vector2(0.94f, 0.19f), Hex("F2ECDF"), Ink, out _);
            CreateIcon(listenButton.transform, "Listen Icon", SpeakerIconPath,
                new Vector2(0.15f, 0.38f), new Vector2(0.23f, 0.62f), Ink);
            SetButtonLabelInsets(listenButton, 0.28f, 0.96f);

            Toggle mockToggle = CreateToggle(
                settingsPanel, "Mock Mode Toggle", "Use demo engine",
                new Vector2(0.55f, 0.04f), new Vector2(0.94f, 0.13f));

            Button retryButton = CreateButton(
                lessonCard, "Retry Button", "NEW ATTEMPT",
                new Vector2(0.74f, 0.01f), new Vector2(0.94f, 0.07f), Coral, Ink, out _);
            retryButton.gameObject.SetActive(false);

            Image success = CreatePanel(
                root, "Result Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ResultFill)
                .GetComponent<Image>();
            SetCenteredSize(success.rectTransform, new Vector2(960f, 680f));
            ApplyRoundedSurface(success.rectTransform);
            CanvasGroup successGroup = success.gameObject.AddComponent<CanvasGroup>();
            ApplyPanelFrame(success.rectTransform, ResultBorder);
            CreatePanel(success.rectTransform, "Result Accent", new Vector2(0f, 0.975f), new Vector2(1f, 1f), ResultAccent);
            CreatePanel(success.rectTransform, "Result Divider", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.723f), new Color(0.06f, 0.14f, 0.16f, 0.12f));
            CreateText(success.rectTransform, "Result Header", "MISSION COMPLETE", 28, FontStyles.Bold, Ink,
                new Vector2(0.06f, 0.80f), new Vector2(0.58f, 0.93f), TextAlignmentOptions.Left);
            CreateText(success.rectTransform, "Result Badge", "CLEARED", 13, FontStyles.Bold, MintInk,
                new Vector2(0.68f, 0.80f), new Vector2(0.80f, 0.92f), TextAlignmentOptions.Center);
            Button resultCloseButton = CreateButton(
                success.rectTransform, "Result Close Button", "CLOSE",
                new Vector2(0.82f, 0.80f), new Vector2(0.94f, 0.92f), SurfaceRaised, Cream, out _);
            CreateIcon(resultCloseButton.transform, "Close Icon", CloseIconPath,
                new Vector2(0.14f, 0.36f), new Vector2(0.26f, 0.64f), Cream);
            SetButtonLabelInsets(resultCloseButton, 0.30f, 0.96f);
            TextMeshProUGUI progressDetails = CreateText(
                success.rectTransform, "Progress Details", "Your first recording will appear here.", 17, FontStyles.Normal, Ink,
                new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.69f), TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationSummary = CreateText(
                success.rectTransform, "Pronunciation Summary", string.Empty, 30, FontStyles.Bold, CoralInk,
                new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.49f), TextAlignmentOptions.Left);
            TextMeshProUGUI pronunciationFeedback = CreateText(
                success.rectTransform, "Pronunciation Feedback", "Your coach tip will appear here.", 18, FontStyles.Italic, Ink,
                new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.33f), TextAlignmentOptions.Left);
            Button resultTryAgainButton = CreateButton(
                success.rectTransform, "Result Try Again Button", "TRY AGAIN",
                new Vector2(0.06f, 0.05f), new Vector2(0.36f, 0.15f), SurfaceRaised, Cream, out _);
            CreateIcon(resultTryAgainButton.transform, "Retry Icon", RetryIconPath,
                new Vector2(0.12f, 0.34f), new Vector2(0.22f, 0.66f), Cream);
            SetButtonLabelInsets(resultTryAgainButton, 0.25f, 0.96f);
            Button resultNextButton = CreateButton(
                success.rectTransform, "Result Next Mission Button", "NEXT MISSION",
                new Vector2(0.38f, 0.05f), new Vector2(0.72f, 0.15f), Mint, Ink, out _);
            CreateIcon(resultNextButton.transform, "Next Icon", ArrowRightIconPath,
                new Vector2(0.80f, 0.36f), new Vector2(0.89f, 0.64f), Ink);
            SetButtonLabelInsets(resultNextButton, 0.04f, 0.76f);
            success.gameObject.SetActive(false);

            RectTransform categoryScreen = EnsureCategoryScreen(
                root,
                out Button wordsCategoryButton,
                out Button shortSentencesCategoryButton,
                out Button challengeCategoryButton);

            FluentEchoView view = lessonCard.gameObject.AddComponent<FluentEchoView>();
            SerializedObject serialized = new(view);
            Set(serialized, "promptLabel", prompt);
            Set(serialized, "progressLabel", progressLabel);
            Set(serialized, "lessonPositionLabel", progressLabel);
            Set(serialized, "lessonProgressFill", progressFill);
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
            Set(serialized, "categoriesButton", categoriesButton);
            Set(serialized, "resultCloseButton", resultCloseButton);
            Set(serialized, "resultNextButton", resultNextButton);
            Set(serialized, "resultTryAgainButton", resultTryAgainButton);
            Set(serialized, "wordsCategoryButton", wordsCategoryButton);
            Set(serialized, "shortSentencesCategoryButton", shortSentencesCategoryButton);
            Set(serialized, "challengeCategoryButton", challengeCategoryButton);
            Set(serialized, "lessonDropdown", lessonDropdown);
            Set(serialized, "mockModeToggle", mockToggle);
            Set(serialized, "recordingIndicator", recordingIndicator);
            Set(serialized, "successPanel", success);
            Set(serialized, "successPanelGroup", successGroup);
            Set(serialized, "successPanelRect", success.rectTransform);
            Set(serialized, "categoryScreen", categoryScreen.gameObject);
            Set(serialized, "selectedCategoryLabel", FindDeep(categoryScreen, "Selected Category Label")?.GetComponent<TextMeshProUGUI>());
            Set(serialized, "selectedCategoryDescriptionLabel", FindDeep(categoryScreen, "Selected Category Description")?.GetComponent<TextMeshProUGUI>());
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
            Transform settingsPanel = FindDeep(view.transform.root, "Settings Panel");
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
            bootstrapObject.FindProperty("selectedCategoryPrefsKey").stringValue = "FluentEcho.SelectedCategoryIndex";
            bootstrapObject.FindProperty("selectedExercisePrefsKey").stringValue = "FluentEcho.SelectedExerciseIndex";
            Set(bootstrapObject, "whisperProfileDropdown", whisperProfileDropdown);
            bootstrapObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpeechExerciseCatalogSO CreateExerciseCatalog(out SpeechExerciseSO selectedExercise)
        {
            var allExercises = new System.Collections.Generic.List<SpeechExerciseSO>();
            SpeechExerciseSO first = null;
            SpeechExerciseSO[][] categoryExercises = new SpeechExerciseSO[GuidedCategories.Length][];
            for (int categoryIndex = 0; categoryIndex < GuidedCategories.Length; categoryIndex++)
            {
                CategorySpec category = GuidedCategories[categoryIndex];
                categoryExercises[categoryIndex] = new SpeechExerciseSO[category.Exercises.Length];
                for (int exerciseIndex = 0; exerciseIndex < category.Exercises.Length; exerciseIndex++)
                {
                    ExerciseSpec spec = category.Exercises[exerciseIndex];
                    SpeechExerciseSO exercise = CreateExerciseAsset(ExerciseDataPath + "/" + spec.FileName, spec);
                    categoryExercises[categoryIndex][exerciseIndex] = exercise;
                    allExercises.Add(exercise);
                    first ??= exercise;
                }
            }

            SpeechExerciseCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SpeechExerciseCatalogSO>(ExerciseCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SpeechExerciseCatalogSO>();
                AssetDatabase.CreateAsset(catalog, ExerciseCatalogPath);
            }

            SerializedObject serialized = new(catalog);
            SerializedProperty exercises = serialized.FindProperty("exercises");
            exercises.arraySize = allExercises.Count;
            for (int i = 0; i < allExercises.Count; i++)
                exercises.GetArrayElementAtIndex(i).objectReferenceValue = allExercises[i];

            SerializedProperty categories = serialized.FindProperty("categories");
            categories.arraySize = GuidedCategories.Length;
            for (int categoryIndex = 0; categoryIndex < GuidedCategories.Length; categoryIndex++)
            {
                SerializedProperty categoryProperty = categories.GetArrayElementAtIndex(categoryIndex);
                categoryProperty.FindPropertyRelative("displayName").stringValue = GuidedCategories[categoryIndex].DisplayName;
                categoryProperty.FindPropertyRelative("description").stringValue = GuidedCategories[categoryIndex].Description;

                SerializedProperty categoryItems = categoryProperty.FindPropertyRelative("exercises");
                categoryItems.arraySize = categoryExercises[categoryIndex].Length;
                for (int exerciseIndex = 0; exerciseIndex < categoryExercises[categoryIndex].Length; exerciseIndex++)
                    categoryItems.GetArrayElementAtIndex(exerciseIndex).objectReferenceValue = categoryExercises[categoryIndex][exerciseIndex];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            selectedExercise = first;
            return catalog;
        }

        private static SpeechExerciseSO CreateExerciseAsset(string path, ExerciseSpec spec)
        {
            return CreateExerciseAsset(
                path,
                spec.Prompt,
                spec.AcceptedPhrases,
                spec.ProgressKey,
                spec.DisplayName,
                spec.RequireWordOrder,
                spec.TargetWords);
        }

        private static SpeechExerciseSO CreateExerciseAsset(
            string path,
            string prompt,
            string acceptedPhrases,
            string progressKey,
            string displayName,
            bool requireWordOrder,
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
            serialized.FindProperty("requireWordOrder").boolValue = requireWordOrder;
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
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSurfacePath);
            Sprite flatSprite = CreateUiSprite();
            if (sprite == null)
                sprite = flatSprite;

            return new DefaultControls.Resources
            {
                standard = sprite,
                background = sprite,
                inputField = sprite,
                knob = flatSprite,
                checkmark = flatSprite,
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

        private static Image CreateIcon(
            Transform parent,
            string name,
            string assetPath,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            Image image = CreatePanel(parent, name, anchorMin, anchorMax, color).GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Image EnsureIcon(
            Transform parent,
            string name,
            string assetPath,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            Transform existing = parent.Find(name);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
                return CreateIcon(parent, name, assetPath, anchorMin, anchorMax, color);

            if (image.transform.parent != parent)
                image.transform.SetParent(parent, false);

            SetAnchors(image.rectTransform, anchorMin, anchorMax);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void SetButtonLabelInsets(Button button, float left, float right)
        {
            TextMeshProUGUI label = button != null
                ? button.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            if (label == null)
                return;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(left, rect.anchorMin.y);
            rect.anchorMax = new Vector2(right, rect.anchorMax.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            bool created = false;
            if (rect == null)
            {
                rect = CreatePanel(parent, name, anchorMin, anchorMax, color);
                created = true;
            }

            if (rect.parent != parent)
                rect.SetParent(parent, false);
            if (created)
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

            if (text.transform.parent != parent)
                text.transform.SetParent(parent, false);
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

            if (button.transform.parent != parent)
                button.transform.SetParent(parent, false);
            SetAnchors(button.GetComponent<RectTransform>(), anchorMin, anchorMax);
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = background;
                ApplyRoundedSurface(button.GetComponent<RectTransform>());
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.color = foreground;
            }

            return button;
        }

        private static RectTransform EnsureCategoryScreen(
            Transform root,
            out Button wordsButton,
            out Button shortSentencesButton,
            out Button challengeButton)
        {
            RectTransform screen = root.Find("Category Screen") as RectTransform;
            if (screen == null)
            {
                screen = CreatePanel(root, "Category Screen", new Vector2(0.335f, 0.075f), new Vector2(0.965f, 0.88f), Cream);
            }

            SetAnchors(screen, new Vector2(0.335f, 0.075f), new Vector2(0.965f, 0.88f));
            Image image = screen.GetComponent<Image>();
            if (image != null)
                image.color = Cream;
            ApplyRoundedSurface(screen);
            ApplyPanelFrame(screen, new Color(0.06f, 0.14f, 0.16f, 0.12f));

            EnsureText(screen, "Category Eyebrow", "PRACTICE MENU", 13, FontStyles.Bold, MintInk,
                new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.90f), TextAlignmentOptions.Left);
            EnsureText(screen, "Category Title", "Choose your practice path.", 42, FontStyles.Bold, Ink,
                new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.84f), TextAlignmentOptions.Left);
            EnsureText(
                screen,
                "Category Description",
                "Start with words, build rhythm with short sentences, then move into longer fluency challenges.",
                18,
                FontStyles.Normal,
                Ink,
                new Vector2(0.06f, 0.63f),
                new Vector2(0.88f, 0.72f),
                TextAlignmentOptions.Left);
            EnsureText(screen, "Selected Category Label", "Practice Menu", 15, FontStyles.Bold, MintInk,
                new Vector2(0.06f, 0.05f), new Vector2(0.36f, 0.10f), TextAlignmentOptions.Left);
            EnsureText(
                screen,
                "Selected Category Description",
                "Pick a path to begin.",
                13,
                FontStyles.Normal,
                Ink,
                new Vector2(0.38f, 0.05f),
                new Vector2(0.94f, 0.10f),
                TextAlignmentOptions.Right);

            wordsButton = EnsureButton(
                screen,
                "Words Category Button",
                "WORDS",
                new Vector2(0.06f, 0.26f),
                new Vector2(0.33f, 0.56f),
                Mint,
                Ink);
            EnsureText(wordsButton.transform, "Description", "10 focused words", 14, FontStyles.Bold, Ink,
                new Vector2(0.10f, 0.16f), new Vector2(0.90f, 0.34f), TextAlignmentOptions.Center);
            EnsureIcon(wordsButton.transform, "Category Icon", MicrophoneIconPath,
                new Vector2(0.42f, 0.58f), new Vector2(0.58f, 0.78f), Ink);

            shortSentencesButton = EnsureButton(
                screen,
                "Short Sentences Category Button",
                "SHORT SENTENCES",
                new Vector2(0.365f, 0.26f),
                new Vector2(0.635f, 0.56f),
                Hex("F2ECDF"),
                Ink);
            EnsureText(shortSentencesButton.transform, "Description", "10 short lines", 14, FontStyles.Bold, Ink,
                new Vector2(0.10f, 0.16f), new Vector2(0.90f, 0.34f), TextAlignmentOptions.Center);
            EnsureIcon(shortSentencesButton.transform, "Category Icon", ChatIconPath,
                new Vector2(0.42f, 0.58f), new Vector2(0.58f, 0.78f), Ink);

            challengeButton = EnsureButton(
                screen,
                "Challenge Category Button",
                "CHALLENGE",
                new Vector2(0.67f, 0.26f),
                new Vector2(0.94f, 0.56f),
                SurfaceRaised,
                Cream);
            EnsureText(challengeButton.transform, "Description", "8 fluency lines", 14, FontStyles.Bold, Cream,
                new Vector2(0.10f, 0.16f), new Vector2(0.90f, 0.34f), TextAlignmentOptions.Center);
            EnsureIcon(challengeButton.transform, "Category Icon", WaveformIconPath,
                new Vector2(0.42f, 0.58f), new Vector2(0.58f, 0.78f), Cream);

            screen.gameObject.SetActive(true);
            screen.SetAsLastSibling();
            return screen;
        }

        private static void ConfigureCoachPortrait(Scene scene)
        {
            Transform avatarTransform = FindSceneTransform(scene, "Avatar");
            if (avatarTransform == null)
                return;

            TextMeshProUGUI placeholder = avatarTransform.GetComponent<TextMeshProUGUI>();
            if (placeholder != null)
                Object.DestroyImmediate(placeholder);

            Image avatar = avatarTransform.GetComponent<Image>();
            if (avatar == null)
                avatar = avatarTransform.gameObject.AddComponent<Image>();

            avatar.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AvaPortraitPath);
            avatar.color = Color.white;
            avatar.preserveAspect = true;
            avatar.raycastTarget = false;
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

        private static void SetCenteredSize(RectTransform rect, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
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
            {
                dropdown.targetGraphic.color = fillColor;
                if (dropdown.targetGraphic is Image image)
                {
                    image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSurfacePath);
                    image.type = Image.Type.Sliced;
                }
            }

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

            Transform arrowTransform = dropdown.transform.Find("Arrow");
            Image arrow = arrowTransform != null ? arrowTransform.GetComponent<Image>() : null;
            if (arrow != null)
            {
                arrow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChevronDownIconPath);
                arrow.color = Cream;
                arrow.preserveAspect = true;
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
            ApplyRoundedSurface(rect);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.16f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.38f);
            colors.fadeDuration = 0.18f;
            button.colors = colors;
            text = CreateText(rect, "Label", label, 17, FontStyles.Bold, foreground,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), TextAlignmentOptions.Center);
            return button;
        }

        private static void ApplyRoundedSurface(RectTransform rect)
        {
            if (rect == null)
                return;

            Image image = rect.GetComponent<Image>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSurfacePath);
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
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
