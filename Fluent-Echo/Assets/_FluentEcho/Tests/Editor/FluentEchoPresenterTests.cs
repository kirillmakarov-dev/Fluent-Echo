using System;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Presentation;
using FluentEcho.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FluentEcho.Tests
{
    public sealed class FluentEchoPresenterTests
    {
        private const string OnboardingPrefsKey = "FluentEcho.OnboardingSeenV1";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.SetInt(OnboardingPrefsKey, 1);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(OnboardingPrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void MicPress_WhenAlreadyListening_StopsCurrentRecording()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_01");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true, IsListening = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();
                view.RaiseMicPressed();

                Assert.That(service.StopListeningCalls, Is.EqualTo(1));
                Assert.That(service.StartListeningCalls, Is.EqualTo(0));
                Assert.That(view.LastStatus, Does.Contain("Ready to practice").Or.Contain("Listening"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void FirstLaunch_ShowsOnboardingBeforePractice()
        {
            PlayerPrefs.DeleteKey(OnboardingPrefsKey);
            PlayerPrefs.Save();

            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_00");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();

                Assert.That(view.CategoryScreenVisible, Is.False);
                Assert.That(view.NoticeVisible, Is.True);
                Assert.That(view.NoticeTitle, Is.EqualTo(FluentEchoCopy.OnboardingWelcomeTitle));
                Assert.That(view.NoticeBody, Does.Contain("local speech model"));

                view.RaiseNoticeConfirmed();

                Assert.That(view.NoticeVisible, Is.True);
                Assert.That(view.NoticeTitle, Is.EqualTo(FluentEchoCopy.OnboardingPracticePathsTitle));
                Assert.That(view.NoticeBody, Does.Contain("Words build clarity"));

                view.RaiseNoticeConfirmed();

                Assert.That(view.NoticeVisible, Is.False);
                Assert.That(view.CategoryScreenVisible, Is.True);
                Assert.That(PlayerPrefs.HasKey(OnboardingPrefsKey), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ReturningUser_SkipsOnboardingAndShowsCategoryScreen()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_00_returning");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();

                Assert.That(view.NoticeVisible, Is.False);
                Assert.That(view.CategoryScreenVisible, Is.True);
                Assert.That(view.LastStatus, Does.Contain("Ready").Or.Contain("Preparing"));
                Assert.That(view.LastProgress, Does.Contain(FluentEchoCopy.FirstProgressSummary));
                Assert.That(view.LastPronunciationSummary, Does.Contain(FluentEchoCopy.FirstPronunciationSummary));
                Assert.That(view.LastPronunciationConfidence, Does.Contain(FluentEchoCopy.FirstConfidenceSummary));
                Assert.That(view.LastPronunciationFeedback, Does.Contain(FluentEchoCopy.PhonemeRoadmapText));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void LoadingStatus_DisablesMicUntilSpeechModelIsReady()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_loading_status");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = false };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();

                Assert.That(view.LastMicInteractable, Is.False);

                service.RaiseStatus("Loading speech engine...");
                Assert.That(view.LastMicInteractable, Is.False);

                service.IsReady = true;
                service.RaiseStatus("Whisper ready.");
                Assert.That(view.LastMicInteractable, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void MissingMicrophoneFailure_OpensSettingsNotice()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_02");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();
                service.RaiseFailure("No microphone device was detected.");

                Assert.That(view.NoticeVisible, Is.True);
                Assert.That(view.NoticeTitle, Is.EqualTo("Microphone not found"));
                Assert.That(view.NoticeActionLabel, Is.EqualTo("OPEN SETTINGS"));

                view.RaiseNoticeConfirmed();

                Assert.That(view.SettingsPanelVisible, Is.True);
                Assert.That(view.NoticeVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void MicrophonePermissionFailure_OpensSettingsNotice()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_02_permission");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();
                service.RaiseFailure("The microphone could not start. Check operating-system permission.");

                Assert.That(view.NoticeVisible, Is.True);
                Assert.That(view.NoticeTitle, Is.EqualTo("Microphone permission needed"));
                Assert.That(view.NoticeActionLabel, Is.EqualTo("OPEN SETTINGS"));

                view.RaiseNoticeConfirmed();

                Assert.That(view.SettingsPanelVisible, Is.True);
                Assert.That(view.NoticeVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void SpeechModelMissingFailure_OpensSettingsNotice()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_02_model");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();
                service.RaiseFailure("Speech model is missing.");

                Assert.That(view.NoticeVisible, Is.True);
                Assert.That(view.NoticeTitle, Is.EqualTo("Speech model missing"));
                Assert.That(view.NoticeActionLabel, Is.EqualTo("OPEN SETTINGS"));

                view.RaiseNoticeConfirmed();

                Assert.That(view.SettingsPanelVisible, Is.True);
                Assert.That(view.NoticeVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void AnalysisFailure_ConfirmationCancelsServiceAndResetsFlow()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_03");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();
                view.RaiseMicPressed();
                service.RaiseFailure("Speech analysis could not start.");

                Assert.That(view.NoticeVisible, Is.True);
                Assert.That(view.NoticeTitle, Is.EqualTo(FluentEchoCopy.CouldNotCheckAttemptTitle));
                Assert.That(view.NoticeActionLabel, Is.EqualTo("TRY AGAIN"));

                view.RaiseNoticeConfirmed();

                Assert.That(service.CancelCalls, Is.EqualTo(1));
                Assert.That(view.NoticeVisible, Is.False);
                Assert.That(view.StatusHistory, Has.Some.Contains("Resetting attempt"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void NoSpeechDetectedFailure_OffersRetryNotice()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the word.", "lesson_test_word_03_no_speech");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                presenter.Initialize();
                service.RaiseFailure(FluentEchoCopy.DidNotCatchThatDetailedStatus);

                Assert.That(view.NoticeVisible, Is.True);
                Assert.That(view.NoticeTitle, Is.EqualTo(FluentEchoCopy.DidNotCatchThatTitle));
                Assert.That(view.NoticeActionLabel, Is.EqualTo("TRY AGAIN"));

                view.RaiseNoticeConfirmed();

                Assert.That(service.CancelCalls, Is.EqualTo(1));
                Assert.That(view.NoticeVisible, Is.False);
                Assert.That(view.LastStatus, Does.Contain("Resetting attempt"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void NextPress_SwitchesToFollowingExerciseAndPersistsSelection()
        {
            SpeechExerciseSO first = CreateExercise("word_01", "Say the first word.", "lesson_test_word_04");
            SpeechExerciseSO second = CreateExercise("word_02", "Say the second word.", "lesson_test_word_05");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { first, second });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            int persistedCategory = -1;
            int persistedExercise = -1;
            var presenter = new FluentEchoPresenter(
                first,
                catalog,
                view,
                service,
                mockService,
                false,
                null,
                0,
                0,
                (categoryIndex, exerciseIndex) =>
                {
                    persistedCategory = categoryIndex;
                    persistedExercise = exerciseIndex;
                });

            try
            {
                presenter.Initialize();
                string initialPrompt = view.LastPrompt;

                view.RaiseNextPressed();

                Assert.That(view.LastPrompt, Is.Not.EqualTo(initialPrompt));
                Assert.That(view.LastPrompt, Is.EqualTo("Say the second word."));
                Assert.That(service.ConfigureCalls, Is.EqualTo(2));
                Assert.That(persistedCategory, Is.EqualTo(0));
                Assert.That(persistedExercise, Is.EqualTo(1));
                Assert.That(view.LessonOptions, Is.EqualTo(new[] { "word_01", "word_02" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CategorySelection_SwitchesCategoryAndResetsExerciseIndex()
        {
            SpeechExerciseSO wordsOne = CreateExercise("word_01", "Say the first word.", "lesson_test_word_06");
            SpeechExerciseSO wordsTwo = CreateExercise("word_02", "Say the second word.", "lesson_test_word_07");
            SpeechExerciseSO sentencesOne = CreateExercise("sentence_01", "Say the sentence.", "lesson_test_sentence_01");
            SpeechExerciseSO sentencesTwo = CreateExercise("sentence_02", "Say the next sentence.", "lesson_test_sentence_02");
            SpeechExerciseCatalogSO catalog = CreateCategorizedCatalog(
                new[]
                {
                    ("Words", "Practice one word at a time.", new[] { wordsOne, wordsTwo }),
                    ("Sentences", "Practice short sentence lines.", new[] { sentencesOne, sentencesTwo })
                });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            int persistedCategory = -1;
            int persistedExercise = -1;
            var presenter = new FluentEchoPresenter(
                wordsTwo,
                catalog,
                view,
                service,
                mockService,
                false,
                null,
                0,
                1,
                (categoryIndex, exerciseIndex) =>
                {
                    persistedCategory = categoryIndex;
                    persistedExercise = exerciseIndex;
                });

            try
            {
                presenter.Initialize();
                Assert.That(view.LastPrompt, Is.EqualTo("Say the second word."));

                view.RaiseCategorySelected(1);

                Assert.That(view.LastPrompt, Is.EqualTo("Say the sentence."));
                Assert.That(view.LastTargetWords, Is.EqualTo(new[] { "sentence_01" }));
                Assert.That(view.CategoryScreenVisible, Is.False);
                Assert.That(persistedCategory, Is.EqualTo(1));
                Assert.That(persistedExercise, Is.EqualTo(0));
                Assert.That(service.ConfigureCalls, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wordsOne);
                UnityEngine.Object.DestroyImmediate(wordsTwo);
                UnityEngine.Object.DestroyImmediate(sentencesOne);
                UnityEngine.Object.DestroyImmediate(sentencesTwo);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void SavedSelection_RestoresCategoryAndExerciseOnInitialize()
        {
            SpeechExerciseSO wordsOne = CreateExercise("word_01", "Say the first word.", "lesson_test_saved_selection_01");
            SpeechExerciseSO wordsTwo = CreateExercise("word_02", "Say the second word.", "lesson_test_saved_selection_02");
            SpeechExerciseSO sentencesOne = CreateExercise("sentence_01", "Say the sentence.", "lesson_test_saved_selection_03");
            SpeechExerciseCatalogSO catalog = CreateCategorizedCatalog(
                new[]
                {
                    ("Words", "Practice one word at a time.", new[] { wordsOne, wordsTwo }),
                    ("Sentences", "Practice short sentence lines.", new[] { sentencesOne })
                });

            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = new FluentEchoPresenter(
                null,
                catalog,
                view,
                service,
                mockService,
                false,
                null,
                1,
                0,
                null);

            try
            {
                presenter.Initialize();

                Assert.That(view.LastPrompt, Is.EqualTo("Say the sentence."));
                Assert.That(view.LastTargetWords, Is.EqualTo(new[] { "sentence_01" }));
                Assert.That(view.LessonOptions, Is.EqualTo(new[] { "sentence_01" }));
                Assert.That(view.CategoryScreenVisible, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wordsOne);
                UnityEngine.Object.DestroyImmediate(wordsTwo);
                UnityEngine.Object.DestroyImmediate(sentencesOne);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CategoryScreen_ShowsProgressSummaryForSavedLessons()
        {
            SpeechExerciseSO wordsOne = CreateExercise("word_01", "Say the first word.", "lesson_test_category_progress_01");
            SpeechExerciseSO wordsTwo = CreateExercise("word_02", "Say the second word.", "lesson_test_category_progress_02");
            SpeechExerciseCatalogSO catalog = CreateCategorizedCatalog(
                new[]
                {
                    ("Words", "Practice one word at a time.", new[] { wordsOne, wordsTwo })
                });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(wordsOne, catalog, view, service, mockService);

            try
            {
                LessonProgressRepository.Clear(wordsOne.ProgressKey);
                LessonProgressRepository.Clear(wordsTwo.ProgressKey);

                LessonProgressState savedProgress = LessonProgressRepository.Load(wordsOne.ProgressKey, 1);
                savedProgress.RecordAttempt(
                    "word_01",
                    new SpeechMatchResult(true, new[] { true }),
                    1,
                    92,
                    "strong",
                    "PRACTICE SCORE | 92/100 | HIGH",
                    "high",
                    90);
                LessonProgressRepository.Save(savedProgress);

                presenter.Initialize();

                Assert.That(view.LastCategoryName, Is.EqualTo("Words"));
                Assert.That(view.LastCategoryDescription, Is.EqualTo("Practice one word at a time."));
                Assert.That(view.LastCategoryProgress, Does.Contain("Progress: 1/2 lessons cleared"));
                Assert.That(view.LastCategoryProgress, Does.Contain("best score 92/100"));
            }
            finally
            {
                LessonProgressRepository.Clear(wordsOne.ProgressKey);
                LessonProgressRepository.Clear(wordsTwo.ProgressKey);
                UnityEngine.Object.DestroyImmediate(wordsOne);
                UnityEngine.Object.DestroyImmediate(wordsTwo);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void GuidedCatalog_ContainsThreeCategoriesWithExpectedLessonCounts()
        {
            SpeechExerciseCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SpeechExerciseCatalogSO>("Assets/_FluentEcho/Demo/Data/ExerciseCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.CategoryCount, Is.EqualTo(3));
            Assert.That(catalog.GetCategoryDisplayNames(), Is.EqualTo(new[] { "Words", "Short Sentences", "Challenge Sentences" }));
            Assert.That(catalog.GetCategoryExerciseCount(0), Is.EqualTo(10));
            Assert.That(catalog.GetCategoryExerciseCount(1), Is.EqualTo(10));
            Assert.That(catalog.GetCategoryExerciseCount(2), Is.EqualTo(8));
        }

        [Test]
        public void CompleteTranscript_SavesProgressAndShowsSuccess()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the sentence.", "lesson_test_word_complete");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                LessonProgressRepository.Clear(exercise.ProgressKey);

                presenter.Initialize();
                view.RaiseMicPressed();

                service.RaiseTranscript("word_01");
                Assert.That(service.StopListeningCalls, Is.EqualTo(1));

                service.RaiseListeningStopped();

                LessonProgressState saved = LessonProgressRepository.Load(exercise.ProgressKey, 1);
                try
                {
                    Assert.That(view.LastStatus, Does.Contain("Excellent"));
                    Assert.That(view.LastSuccessState, Is.True);
                    Assert.That(view.LastListeningState, Is.False);
                    Assert.That(view.LastProgressDetails, Does.Contain(FluentEchoCopy.PhonemeRoadmapText));
                    Assert.That(view.LastProgressDetails, Does.Contain("Current attempt:"));
                    Assert.That(view.LastProgressDetails, Does.Contain("Best attempt so far"));
                    Assert.That(view.LastProgressDetails, Does.Contain("this attempt is the new best"));
                    Assert.That(view.LastProgressDetails, Does.Contain("word_01"));
                    Assert.That(view.LastProgressDetails, Does.Contain("Lesson progress:"));
                    Assert.That(view.LastProgressDetails, Does.Contain(saved.GetSummaryText()));
                    Assert.That(view.LastProgressDetails, Does.Contain(FluentEchoCopy.NextMissionPrompt));
                    Assert.That(view.LastProgressDetails, Does.Contain("Coach tip:"));
                    Assert.That(view.LastPronunciationSummary, Does.Contain("Pronunciation estimate"));
                    Assert.That(view.LastPronunciationConfidence, Does.Contain("High").Or.Contain("Medium").Or.Contain("Low"));
                    Assert.That(view.LastPronunciationFeedback, Does.Contain(FluentEchoCopy.PhonemeRoadmapText));
                    Assert.That(saved.Attempts, Is.EqualTo(1));
                    Assert.That(saved.SuccessfulAttempts, Is.EqualTo(1));
                    Assert.That(saved.BestPronunciationScore, Is.GreaterThan(0));
                    Assert.That(saved.GetSummaryText(), Does.Contain("cleared 1"));
                }
                finally
                {
                    LessonProgressRepository.Clear(exercise.ProgressKey);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void NextMissionAfterSuccess_SwitchesLessonAndClearsResultState()
        {
            SpeechExerciseSO first = CreateExercise("word_01", "Say the first word.", "lesson_test_result_next_01");
            SpeechExerciseSO second = CreateExercise("word_02", "Say the second word.", "lesson_test_result_next_02");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { first, second });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            int persistedCategory = -1;
            int persistedExercise = -1;
            var presenter = new FluentEchoPresenter(
                first,
                catalog,
                view,
                service,
                mockService,
                false,
                null,
                0,
                0,
                (categoryIndex, exerciseIndex) =>
                {
                    persistedCategory = categoryIndex;
                    persistedExercise = exerciseIndex;
                });

            try
            {
                LessonProgressRepository.Clear(first.ProgressKey);
                LessonProgressRepository.Clear(second.ProgressKey);

                presenter.Initialize();
                view.RaiseMicPressed();
                service.RaiseTranscript("word_01");
                service.RaiseListeningStopped();

                Assert.That(view.LastSuccessState, Is.True);
                Assert.That(view.LastStatus, Does.Contain("Excellent"));

                view.RaiseNextPressed();

                Assert.That(view.LastPrompt, Is.EqualTo("Say the second word."));
                Assert.That(view.LastSuccessState, Is.False);
                Assert.That(view.LastListeningState, Is.False);
                Assert.That(view.LastTranscript, Is.EqualTo(string.Empty));
                Assert.That(view.LastStatus, Does.Contain("Ready to practice").Or.Contain("Preparing"));
                Assert.That(persistedCategory, Is.EqualTo(0));
                Assert.That(persistedExercise, Is.EqualTo(1));
                Assert.That(service.ConfigureCalls, Is.EqualTo(2));
            }
            finally
            {
                LessonProgressRepository.Clear(first.ProgressKey);
                LessonProgressRepository.Clear(second.ProgressKey);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void RetryAfterSuccess_ResetsViewButKeepsSavedProgress()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the sentence.", "lesson_test_word_retry_after_success");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                LessonProgressRepository.Clear(exercise.ProgressKey);

                presenter.Initialize();
                view.RaiseMicPressed();
                service.RaiseTranscript("word_01");
                service.RaiseListeningStopped();

                view.RaiseRetryPressed();

                LessonProgressState saved = LessonProgressRepository.Load(exercise.ProgressKey, 1);
                try
                {
                    Assert.That(service.CancelCalls, Is.EqualTo(1));
                    Assert.That(view.LastSuccessState, Is.False);
                    Assert.That(view.LastListeningState, Is.False);
                    Assert.That(view.LastTranscript, Is.EqualTo(string.Empty));
                    Assert.That(view.LastStatus, Does.Contain("Ready to practice"));
                    Assert.That(saved.Attempts, Is.EqualTo(1));
                    Assert.That(saved.SuccessfulAttempts, Is.EqualTo(1));
                    Assert.That(saved.GetSummaryText(), Does.Contain("cleared 1"));
                }
                finally
                {
                    LessonProgressRepository.Clear(exercise.ProgressKey);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void MockModeToggle_SwitchesToDemoServiceAndResetsFlow()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the sentence.", "lesson_test_mock_mode");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var realService = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, realService, mockService);

            try
            {
                presenter.Initialize();
                view.RaiseMockModeChanged(true);

                Assert.That(realService.ConfigureCalls, Is.EqualTo(1));
                Assert.That(mockService.ConfigureCalls, Is.EqualTo(1));
                Assert.That(mockService.PrepareCalls, Is.EqualTo(1));
                Assert.That(view.LastMockMode, Is.True);
                Assert.That(view.LastStatus, Does.Contain("Demo mode is ready"));
                Assert.That(view.LastTranscript, Is.EqualTo(string.Empty));
                Assert.That(view.LastListeningState, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void MockModeToggle_WhileListening_RevertsToggleWithoutSwitchingServices()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the sentence.", "lesson_test_mock_mode_locked");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var realService = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, realService, mockService);

            try
            {
                presenter.Initialize();
                realService.IsListening = true;

                view.RaiseMockModeChanged(true);

                Assert.That(view.LastMockMode, Is.False);
                Assert.That(realService.ConfigureCalls, Is.EqualTo(1));
                Assert.That(mockService.ConfigureCalls, Is.EqualTo(0));
                Assert.That(mockService.PrepareCalls, Is.EqualTo(0));
                Assert.That(view.LastStatus, Does.Contain("Ready to practice").Or.Contain("Preparing"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PartialTranscript_SavesRetryProgressWithoutSuccess()
        {
            SpeechExerciseSO exercise = CreateExercise("word_01", "Say the sentence.", "lesson_test_word_retry");
            SpeechExerciseCatalogSO catalog = CreateCatalog(new[] { exercise });
            var view = new FakeView();
            var service = new FakeSpeechService { IsReady = true };
            var mockService = new FakeSpeechService { IsReady = true };
            var presenter = CreatePresenter(exercise, catalog, view, service, mockService);

            try
            {
                LessonProgressRepository.Clear(exercise.ProgressKey);

                presenter.Initialize();
                view.RaiseMicPressed();

                service.RaiseTranscript("word_01");
                Assert.That(service.StopListeningCalls, Is.EqualTo(0));

                service.RaiseListeningStopped();

                LessonProgressState saved = LessonProgressRepository.Load(exercise.ProgressKey, 1);
                try
                {
                    Assert.That(view.LastSuccessState, Is.False);
                    Assert.That(view.LastListeningState, Is.False);
                    Assert.That(view.LastStatus, Does.Contain("missing").Or.Contain("retry"));
                    Assert.That(saved.Attempts, Is.EqualTo(1));
                    Assert.That(saved.SuccessfulAttempts, Is.EqualTo(0));
                    Assert.That(saved.GetSummaryText(), Does.Contain("1 attempts"));
                    Assert.That(saved.GetSummaryText(), Does.Not.Contain("cleared 1"));
                }
                finally
                {
                    LessonProgressRepository.Clear(exercise.ProgressKey);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        private static FluentEchoPresenter CreatePresenter(
            SpeechExerciseSO exercise,
            SpeechExerciseCatalogSO catalog,
            FakeView view,
            FakeSpeechService service,
            FakeSpeechService mockService)
        {
            return new FluentEchoPresenter(
                exercise,
                catalog,
                view,
                service,
                mockService,
                false,
                null,
                0,
                0,
                null);
        }

        private static SpeechExerciseSO CreateExercise(string name, string prompt, string progressKey)
        {
            SpeechExerciseSO exercise = ScriptableObject.CreateInstance<SpeechExerciseSO>();
            exercise.name = name;

            SerializedObject serialized = new(exercise);
            serialized.FindProperty("prompt").stringValue = prompt;
            serialized.FindProperty("targetWords").arraySize = 1;
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(0).stringValue = name;
            serialized.FindProperty("acceptedPhrases").stringValue = name;
            serialized.FindProperty("progressKey").stringValue = progressKey;
            serialized.FindProperty("requireWordOrder").boolValue = true;
            serialized.FindProperty("allowFuzzyMatch").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return exercise;
        }

        private static SpeechExerciseCatalogSO CreateCatalog(SpeechExerciseSO[] exercises)
        {
            SpeechExerciseCatalogSO catalog = ScriptableObject.CreateInstance<SpeechExerciseCatalogSO>();
            SerializedObject serialized = new(catalog);

            SerializedProperty exercisesProperty = serialized.FindProperty("exercises");
            exercisesProperty.arraySize = exercises.Length;
            for (int i = 0; i < exercises.Length; i++)
                exercisesProperty.GetArrayElementAtIndex(i).objectReferenceValue = exercises[i];

            SerializedProperty categoriesProperty = serialized.FindProperty("categories");
            categoriesProperty.arraySize = 1;
            SerializedProperty category = categoriesProperty.GetArrayElementAtIndex(0);
            category.FindPropertyRelative("displayName").stringValue = "Words";
            category.FindPropertyRelative("description").stringValue = "Practice one word at a time.";

            SerializedProperty categoryExercises = category.FindPropertyRelative("exercises");
            categoryExercises.arraySize = exercises.Length;
            for (int i = 0; i < exercises.Length; i++)
                categoryExercises.GetArrayElementAtIndex(i).objectReferenceValue = exercises[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static SpeechExerciseCatalogSO CreateCategorizedCatalog(
            (string displayName, string description, SpeechExerciseSO[] exercises)[] categories)
        {
            SpeechExerciseCatalogSO catalog = ScriptableObject.CreateInstance<SpeechExerciseCatalogSO>();
            SerializedObject serialized = new(catalog);

            SerializedProperty categoriesProperty = serialized.FindProperty("categories");
            categoriesProperty.arraySize = categories.Length;

            for (int i = 0; i < categories.Length; i++)
            {
                SerializedProperty category = categoriesProperty.GetArrayElementAtIndex(i);
                category.FindPropertyRelative("displayName").stringValue = categories[i].displayName;
                category.FindPropertyRelative("description").stringValue = categories[i].description;

                SerializedProperty categoryExercises = category.FindPropertyRelative("exercises");
                categoryExercises.arraySize = categories[i].exercises.Length;
                for (int j = 0; j < categories[i].exercises.Length; j++)
                    categoryExercises.GetArrayElementAtIndex(j).objectReferenceValue = categories[i].exercises[j];
            }

            serialized.FindProperty("exercises").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private sealed class FakeSpeechService : ISpeechRecognitionService
        {
            public event Action<string> TranscriptUpdated;
            public event Action AnalysisStarted;
            public event Action ListeningStopped;
            public event Action<string> Failed;
            public event Action<string> StatusChanged;

            public bool IsListening { get; set; }
            public bool IsReady { get; set; }
            public int PrepareCalls { get; private set; }
            public int StartListeningCalls { get; private set; }
            public int StopListeningCalls { get; private set; }
            public int CancelCalls { get; private set; }
            public int ConfigureCalls { get; private set; }
            public SpeechExerciseSO LastConfiguredExercise { get; private set; }

            public void Configure(SpeechExerciseSO exercise)
            {
                ConfigureCalls++;
                LastConfiguredExercise = exercise;
            }

            public void Prepare()
            {
                PrepareCalls++;
            }

            public void StartListening()
            {
                StartListeningCalls++;
                IsListening = true;
            }

            public void StopListening()
            {
                StopListeningCalls++;
                IsListening = false;
            }

            public void Cancel()
            {
                CancelCalls++;
                IsListening = false;
            }

            public void RaiseFailure(string message) => Failed?.Invoke(message);

            public void RaiseStatus(string message) => StatusChanged?.Invoke(message);

            public void RaiseTranscript(string transcript) => TranscriptUpdated?.Invoke(transcript);

            public void RaiseAnalysisStarted() => AnalysisStarted?.Invoke();

            public void RaiseListeningStopped() => ListeningStopped?.Invoke();
        }

        private sealed class FakeView : IFluentEchoView
        {
            private readonly System.Collections.Generic.List<string> statusHistory = new();

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

            public string LastPrompt { get; private set; }
            public string[] LastTargetWords { get; private set; } = Array.Empty<string>();
            public string LastStatus { get; private set; } = string.Empty;
            public string LastTranscript { get; private set; } = string.Empty;
            public string LastProgress { get; private set; } = string.Empty;
            public string LastCategoryName { get; private set; } = string.Empty;
            public string LastCategoryDescription { get; private set; } = string.Empty;
            public string LastCategoryProgress { get; private set; } = string.Empty;
            public string[] LessonOptions { get; private set; } = Array.Empty<string>();
            public string LastProgressDetails { get; private set; } = string.Empty;
            public string LastPronunciationSummary { get; private set; } = string.Empty;
            public string LastPronunciationConfidence { get; private set; } = string.Empty;
            public string LastPronunciationFeedback { get; private set; } = string.Empty;
            public bool CategoryScreenVisible { get; private set; }
            public bool SettingsPanelVisible { get; private set; }
            public bool NoticeVisible { get; private set; }
            public bool LastSuccessState { get; private set; }
            public bool LastListeningState { get; private set; }
            public bool LastMockMode { get; private set; }
            public bool LastMicInteractable { get; private set; }
            public string NoticeTitle { get; private set; } = string.Empty;
            public string NoticeBody { get; private set; } = string.Empty;
            public string NoticeActionLabel { get; private set; } = string.Empty;
            public System.Collections.Generic.IReadOnlyList<string> StatusHistory => statusHistory;

            public void RaiseMicPressed() => MicPressed?.Invoke();
            public void RaiseDemoPressed() => DemoPressed?.Invoke();
            public void RaiseRetryPressed() => RetryPressed?.Invoke();
            public void RaiseListenPressed() => ListenPressed?.Invoke();
            public void RaisePreviousPressed() => PreviousPressed?.Invoke();
            public void RaiseNextPressed() => NextPressed?.Invoke();
            public void RaiseCategoriesPressed() => CategoriesPressed?.Invoke();
            public void RaiseCategorySelected(int index) => CategorySelected?.Invoke(index);
            public void RaiseLessonSelected(int index) => LessonSelected?.Invoke(index);
            public void RaiseMockModeChanged(bool value) => MockModeChanged?.Invoke(value);
            public void RaiseNoticeConfirmed() => NoticeConfirmed?.Invoke();

            public void Build(string prompt, string[] targetWords)
            {
                LastPrompt = prompt;
                LastTargetWords = targetWords ?? Array.Empty<string>();
            }

            public void SetProgress(string progress)
            {
                LastProgress = progress ?? string.Empty;
            }

            public void SetStatus(string status)
            {
                LastStatus = status ?? string.Empty;
                statusHistory.Add(LastStatus);
            }

            public void SetTranscript(string transcript)
            {
                LastTranscript = transcript ?? string.Empty;
            }

            public void SetWordMatches(bool[] matches) { }
            public void SetListening(bool listening)
            {
                LastListeningState = listening;
            }

            public void SetMicInteractable(bool interactable)
            {
                LastMicInteractable = interactable;
            }

            public void SetSuccess(bool success)
            {
                LastSuccessState = success;
            }
            public void SetMode(bool mockMode)
            {
                LastMockMode = mockMode;
            }

            public void SetNavigation(bool canGoPrevious, bool canGoNext) { }

            public void SetLessonPosition(int currentLesson, int totalLessons) { }

            public void SetLessonOptions(string[] lessonNames, int selectedIndex)
            {
                LessonOptions = lessonNames ?? Array.Empty<string>();
            }

            public void SetCategory(string categoryName, string categoryDescription)
            {
                LastCategoryName = categoryName ?? string.Empty;
                LastCategoryDescription = categoryDescription ?? string.Empty;
            }

            public void SetCategoryProgress(string categoryProgress)
            {
                LastCategoryProgress = categoryProgress ?? string.Empty;
            }

            public void SetCategoryScreenVisible(bool visible)
            {
                CategoryScreenVisible = visible;
            }

            public void SetSettingsPanelVisible(bool visible)
            {
                SettingsPanelVisible = visible;
            }

            public void ShowNotice(string title, string body, string primaryActionLabel)
            {
                NoticeVisible = true;
                NoticeTitle = title ?? string.Empty;
                NoticeBody = body ?? string.Empty;
                NoticeActionLabel = primaryActionLabel ?? string.Empty;
            }

            public void HideNotice()
            {
                NoticeVisible = false;
            }

            public void SetProgressDetails(string details)
            {
                LastProgressDetails = details ?? string.Empty;
            }

            public void SetPronunciation(string summary, string feedback)
            {
                LastPronunciationSummary = summary ?? string.Empty;
                LastPronunciationFeedback = feedback ?? string.Empty;
            }

            public void SetPronunciationConfidence(string confidence)
            {
                LastPronunciationConfidence = confidence ?? string.Empty;
            }
        }
    }
}
