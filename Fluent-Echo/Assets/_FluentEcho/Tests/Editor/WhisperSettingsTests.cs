using System;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FluentEcho.Tests
{
    public sealed class WhisperSettingsTests
    {
        [Test]
        public void RuntimeProfile_ResolvesExpectedModelPath()
        {
            string prefsKey = $"FluentEcho.Test.WhisperProfile.{Guid.NewGuid():N}";
            WhisperSettingsSO settings = CreateSettings(prefsKey);

            try
            {
                settings.SetRuntimeProfile(WhisperQualityProfile.Balanced);

                Assert.That(settings.GetEffectiveProfile(), Is.EqualTo(WhisperQualityProfile.Balanced));
                Assert.That(settings.ResolveModelPath(), Is.EqualTo("Whisper/ggml-base.en.bin"));

                settings.SetRuntimeProfile(WhisperQualityProfile.Accurate);
                Assert.That(settings.ResolveModelPath(), Is.EqualTo("Whisper/ggml-small.en.bin"));
            }
            finally
            {
                PlayerPrefs.DeleteKey(prefsKey);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void StoredProfile_OverridesSerializedDefault()
        {
            string prefsKey = $"FluentEcho.Test.WhisperProfile.{Guid.NewGuid():N}";
            WhisperSettingsSO settings = CreateSettings(prefsKey);

            try
            {
                settings.SetRuntimeProfile(WhisperQualityProfile.Accurate);

                WhisperSettingsSO reloaded = CreateSettings(prefsKey);
                Assert.That(reloaded.GetEffectiveProfile(), Is.EqualTo(WhisperQualityProfile.Accurate));
                Assert.That(reloaded.ResolveModelPath(), Is.EqualTo("Whisper/ggml-small.en.bin"));
            }
            finally
            {
                PlayerPrefs.DeleteKey(prefsKey);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void ProgressState_RoundTripsThroughPlayerPrefs()
        {
            string key = $"lesson_test_{Guid.NewGuid():N}";
            LessonProgressRepository.Clear(key);

            LessonProgressState state = LessonProgressRepository.Load(key, 4);
            state.RecordAttempt(
                "the dog is",
                new SpeechMatchResult(false, new[] { true, true, true, false }),
                4,
                67,
                "steady",
                "PRONUNCIATION | 67/100 | STEADY");
            LessonProgressRepository.Save(state);

            LessonProgressState reloaded = LessonProgressRepository.Load(key, 4);
            try
            {
                Assert.That(reloaded.Attempts, Is.EqualTo(1));
                Assert.That(reloaded.SuccessfulAttempts, Is.EqualTo(0));
                Assert.That(reloaded.BestMatchedWords, Is.EqualTo(3));
                Assert.That(reloaded.BestPronunciationScore, Is.EqualTo(67));
                Assert.That(reloaded.BestPronunciationBand, Is.EqualTo("steady"));
                Assert.That(reloaded.BestPronunciationSummary, Does.Contain("67/100"));
                Assert.That(reloaded.TotalWords, Is.EqualTo(4));
                Assert.That(reloaded.GetSummaryText(), Does.Contain("best 3/4"));
                Assert.That(reloaded.GetSummaryText(), Does.Contain("score 67/100"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("67/100"));
            }
            finally
            {
                LessonProgressRepository.Clear(key);
            }
        }

        [Test]
        public void CompletedAttempt_IncrementsSuccessCounter()
        {
            string key = $"lesson_test_{Guid.NewGuid():N}";
            LessonProgressRepository.Clear(key);

            LessonProgressState state = LessonProgressRepository.Load(key, 4);
            state.RecordAttempt(
                "the dog is big",
                new SpeechMatchResult(true, new[] { true, true, true, true }),
                4,
                94,
                "strong",
                "PRONUNCIATION | 94/100 | STRONG");

                Assert.That(state.SuccessfulAttempts, Is.EqualTo(1));
                Assert.That(state.GetSummaryText(), Does.Contain("cleared 1"));
                Assert.That(state.GetHistoryText(), Does.Contain("cleared"));
                Assert.That(state.LastPronunciationSummary, Does.Contain("94/100"));

            LessonProgressRepository.Clear(key);
        }

        private static WhisperSettingsSO CreateSettings(string prefsKey)
        {
            WhisperSettingsSO settings = ScriptableObject.CreateInstance<WhisperSettingsSO>();
            SerializedObject serialized = new(settings);
            serialized.FindProperty("qualityProfile").enumValueIndex = (int) WhisperQualityProfile.Fast;
            serialized.FindProperty("fastModelPath").stringValue = "Whisper/ggml-tiny.en.bin";
            serialized.FindProperty("balancedModelPath").stringValue = "Whisper/ggml-base.en.bin";
            serialized.FindProperty("accurateModelPath").stringValue = "Whisper/ggml-small.en.bin";
            serialized.FindProperty("qualityProfilePrefsKey").stringValue = prefsKey;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }
    }

    public sealed class PronunciationScoringServiceTests
    {
        private readonly HeuristicPronunciationScoringService scorer = new();

        [Test]
        public void ExactSentence_ProducesStrongScore()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "The dog is big.",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    3f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.OverallScore, Is.GreaterThanOrEqualTo(90));
                Assert.That(score.BandLabel, Is.EqualTo("strong"));
                Assert.That(score.SummaryText, Does.Contain("PRONUNCIATION"));
                Assert.That(score.FeedbackText, Does.Contain("Strong delivery"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void MissingWord_ProducesFocusedFeedback()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "The dog is",
                    new SpeechMatchResult(false, new[] { true, true, true, false }),
                    2.5f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.OverallScore, Is.LessThan(90));
                Assert.That(score.MissingWordCount, Is.EqualTo(1));
                Assert.That(score.FeedbackText, Does.Contain("Missing 1 word"));
                Assert.That(score.FeedbackText, Does.Contain("Focus on big"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        private static SpeechExerciseSO CreateExercise()
        {
            SpeechExerciseSO exercise = ScriptableObject.CreateInstance<SpeechExerciseSO>();
            SerializedObject serialized = new(exercise);
            serialized.FindProperty("prompt").stringValue = "Say the sentence in English.";
            serialized.FindProperty("targetWords").arraySize = 4;
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(0).stringValue = "the";
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(1).stringValue = "dog";
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(2).stringValue = "is";
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(3).stringValue = "big";
            serialized.FindProperty("acceptedPhrases").stringValue = "the dog is big";
            serialized.FindProperty("progressKey").stringValue = "lesson_01_describe_the_dog";
            serialized.FindProperty("requireWordOrder").boolValue = true;
            serialized.FindProperty("allowFuzzyMatch").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return exercise;
        }
    }
}
