using System;
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
                4);
            LessonProgressRepository.Save(state);

            LessonProgressState reloaded = LessonProgressRepository.Load(key, 4);
            try
            {
                Assert.That(reloaded.Attempts, Is.EqualTo(1));
                Assert.That(reloaded.SuccessfulAttempts, Is.EqualTo(0));
                Assert.That(reloaded.BestMatchedWords, Is.EqualTo(3));
                Assert.That(reloaded.TotalWords, Is.EqualTo(4));
                Assert.That(reloaded.GetSummaryText(), Does.Contain("best 3/4"));
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
                4);

            Assert.That(state.SuccessfulAttempts, Is.EqualTo(1));
            Assert.That(state.GetSummaryText(), Does.Contain("cleared 1"));

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
}
