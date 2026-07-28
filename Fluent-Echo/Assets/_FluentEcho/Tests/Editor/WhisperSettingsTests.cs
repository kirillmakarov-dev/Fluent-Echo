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
                "PRONUNCIATION ESTIMATE | 67/100 | HIGH",
                "high",
                81,
                "All target words matched cleanly, with no extra words.");
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
                Assert.That(reloaded.BestPronunciationConfidenceBand, Is.EqualTo("high"));
                Assert.That(reloaded.BestPronunciationConfidenceScore, Is.EqualTo(81));
                Assert.That(reloaded.BestTranscript, Is.EqualTo("the dog is"));
                Assert.That(reloaded.GetSummaryText(), Does.Contain("score 67/100"));
                Assert.That(reloaded.GetSummaryText(), Does.Contain("confidence high"));
                Assert.That(reloaded.TotalWords, Is.EqualTo(4));
                Assert.That(reloaded.GetSummaryText(), Does.Contain("best 3/4"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("67/100"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("Best transcript: the dog is"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("confidence high"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("Best reason: All target words matched cleanly"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("Last reason: All target words matched cleanly"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("All target words matched cleanly"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("History: 1 attempt"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("Recent attempts:"));
                Assert.That(reloaded.GetHistoryText(), Does.Contain("1."));
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
                "PRONUNCIATION ESTIMATE | 94/100 | HIGH",
                "high",
                92,
                "All target words matched cleanly, with no extra words.");

                Assert.That(state.SuccessfulAttempts, Is.EqualTo(1));
                Assert.That(state.GetSummaryText(), Does.Contain("cleared 1"));
                Assert.That(state.GetHistoryText(), Does.Contain("cleared"));
                Assert.That(state.GetHistoryText(), Does.Contain("Best transcript: the dog is big"));
                Assert.That(state.GetHistoryText(), Does.Contain("confidence high"));
                Assert.That(state.GetHistoryText(), Does.Contain("Best reason: All target words matched cleanly"));
                Assert.That(state.GetHistoryText(), Does.Contain("Last reason: All target words matched cleanly"));
                Assert.That(state.GetHistoryText(), Does.Contain("All target words matched cleanly"));
                Assert.That(state.GetHistoryText(), Does.Contain("History: 1 attempt"));
                Assert.That(state.GetHistoryText(), Does.Contain("Recent attempts:"));
                Assert.That(state.LastPronunciationSummary, Does.Contain("94/100"));

            LessonProgressRepository.Clear(key);
        }

        [Test]
        public void HistoryText_WithMultipleAttempts_ShowsVisibleCountSummary()
        {
            string key = $"lesson_test_{Guid.NewGuid():N}";
            LessonProgressRepository.Clear(key);

            LessonProgressState state = LessonProgressRepository.Load(key, 4);

            try
            {
                state.RecordAttempt(
                    "the dog is big",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    4,
                    94,
                    "strong",
                    "PRONUNCIATION ESTIMATE | 94/100 | HIGH",
                    "high",
                    92,
                    "All target words matched cleanly, with no extra words.");
                state.RecordAttempt(
                    "the dog is",
                    new SpeechMatchResult(false, new[] { true, true, true, false }),
                    4,
                    68,
                    "steady",
                    "PRONUNCIATION ESTIMATE | 68/100 | HIGH",
                    "high",
                    79,
                    "Missing 1 word, so confidence stays cautious.");
                state.RecordAttempt(
                    "dog is big",
                    new SpeechMatchResult(false, new[] { false, true, true, true }),
                    4,
                    55,
                    "developing",
                    "PRONUNCIATION ESTIMATE | 55/100 | MEDIUM",
                    "medium",
                    64,
                    "Missing 1 word, so confidence stays cautious.");

                string history = state.GetHistoryText(2);
                Assert.That(history, Does.Contain("History: 3 attempts | showing last 2 | cleared 1"));
                Assert.That(history, Does.Contain("Recent attempts:"));
                Assert.That(history, Does.Contain("1."));
                Assert.That(history, Does.Contain("2."));
                Assert.That(history, Does.Not.Contain("3."));

                string fullHistory = state.GetHistoryText(5);
                Assert.That(fullHistory, Does.Contain("History: 3 attempts | showing all 3 | cleared 1"));
                Assert.That(fullHistory, Does.Contain("Recent attempts:"));
                Assert.That(fullHistory, Does.Contain("1."));
                Assert.That(fullHistory, Does.Contain("2."));
                Assert.That(fullHistory, Does.Contain("3."));
            }
            finally
            {
                LessonProgressRepository.Clear(key);
            }
        }

        [Test]
        public void EmptyProgressState_ShowsReadyCopy()
        {
            string key = $"lesson_test_{Guid.NewGuid():N}";
            LessonProgressRepository.Clear(key);

            LessonProgressState state = LessonProgressRepository.Load(key, 4);

            try
            {
                Assert.That(state.GetSummaryText(), Is.EqualTo(FluentEchoCopy.BuildProgressSummary(4)));
                Assert.That(state.GetHistoryText(), Does.Contain(FluentEchoCopy.FirstLocalEstimateText));
            }
            finally
            {
                LessonProgressRepository.Clear(key);
            }
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
                Assert.That(score.SummaryText, Does.Contain("PRONUNCIATION ESTIMATE"));
                Assert.That(score.ConfidenceBand, Is.EqualTo("high"));
                Assert.That(score.ConfidenceReason, Does.Contain("matched cleanly"));
                Assert.That(score.EstimateBasisText, Does.Contain("not phoneme-level scoring"));
                Assert.That(score.FeedbackText, Does.Contain("pronunciation estimate"));
                Assert.That(score.WordScores, Has.Length.EqualTo(4));
                Assert.That(score.WordScores[0].Score, Is.GreaterThanOrEqualTo(90));
                Assert.That(score.WordQualityScore, Is.GreaterThanOrEqualTo(90));
                Assert.That(score.WordScores[0].Kind, Is.EqualTo(PronunciationMatchKind.Exact));
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
                Assert.That(score.ConfidenceBand, Is.EqualTo("medium"));
                Assert.That(score.ConfidenceReason, Does.Contain("Missing 1 word"));
                Assert.That(score.FeedbackText, Is.EqualTo("Missing 1 word. Repeat \"big\" once, then say the full line again."));
                Assert.That(score.WordScores[3].Score, Is.LessThan(score.WordScores[0].Score));
                Assert.That(score.WordScores[3].Kind, Is.EqualTo(PronunciationMatchKind.Missing));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void NoTargetWordsMatched_ProducesStartWithFirstWordFeedback()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "banana banana",
                    new SpeechMatchResult(false, new[] { false, false, false, false }),
                    2f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.MissingWordCount, Is.EqualTo(4));
                Assert.That(score.FeedbackText, Is.EqualTo("I heard speech, but none of the target words matched. Start with \"the\" and try the full line again."));
                Assert.That(score.ConfidenceReason, Does.Contain("No target words matched"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void AcceptedAlternative_ProducesHighPerWordScore()
        {
            SpeechExerciseSO exercise = CreateExercise("dog|hound");

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "The hound is big.",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    3f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.WordScores[1].Word, Is.EqualTo("dog"));
                Assert.That(score.WordScores[1].Score, Is.GreaterThanOrEqualTo(95));
                Assert.That(score.WordScores[1].Matched, Is.True);
                Assert.That(score.WordScores[1].Kind, Is.EqualTo(PronunciationMatchKind.Alternative));
                Assert.That(score.ConfidenceReason, Does.Contain("approximately"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void FuzzyWord_ProducesFuzzyKind()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "The dug is big.",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    3f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.WordScores[1].Kind, Is.EqualTo(PronunciationMatchKind.Fuzzy));
                Assert.That(score.WordScores[1].Score, Is.LessThan(100));
                Assert.That(score.WordQualityScore, Is.LessThan(100));
                Assert.That(score.ConfidenceBand, Is.EqualTo("high"));
                Assert.That(score.ConfidenceReason, Does.Contain("approximately"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void ApproximateMatch_LowersConfidenceScoreComparedToExactMatch()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult exactScore = scorer.Score(
                    exercise,
                    "The dog is big.",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    3f);

                PronunciationScoreResult fuzzyScore = scorer.Score(
                    exercise,
                    "The dug is big.",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    3f);

                Assert.That(exactScore.IsAvailable, Is.True);
                Assert.That(fuzzyScore.IsAvailable, Is.True);
                Assert.That(exactScore.ConfidenceScore, Is.GreaterThan(fuzzyScore.ConfidenceScore));
                Assert.That(fuzzyScore.ConfidenceReason, Does.Contain("approximately"));
                Assert.That(fuzzyScore.ConfidenceBand, Is.EqualTo("high"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void EmptyTranscript_ProducesLowConfidenceEstimate()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    string.Empty,
                    new SpeechMatchResult(false, new[] { false, false, false, false }),
                    0f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.OverallScore, Is.EqualTo(0));
                Assert.That(score.ConfidenceBand, Is.EqualTo("low"));
                Assert.That(score.ConfidenceReason, Does.Contain("No speech was transcribed"));
                Assert.That(score.SummaryText, Does.Contain("PRONUNCIATION ESTIMATE"));
                Assert.That(score.SummaryText, Does.Contain("LOW"));
                Assert.That(score.EstimateBasisText, Does.Contain("No speech was transcribed"));
                Assert.That(score.FeedbackText, Is.EqualTo(FluentEchoCopy.DidNotCatchThatDetailedStatus));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void ExtraWords_ProduceMediumConfidence()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "The dog is big with extra words",
                    new SpeechMatchResult(false, new[] { true, true, true, true }),
                    4.4f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.ExtraWordCount, Is.GreaterThan(0));
                Assert.That(score.ConfidenceBand, Is.EqualTo("medium"));
                Assert.That(score.ConfidenceReason, Does.Contain("Extra words"));
                Assert.That(score.FeedbackText, Is.EqualTo("Good coverage. Trim the 3 extra words and keep the line cleaner."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void SlowRecording_ProducesQuickerRhythmFeedback()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "The dog is big.",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    7.5f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.TempoScore, Is.LessThan(70));
                Assert.That(score.FeedbackText, Does.Contain("quicker"));
                Assert.That(score.ConfidenceReason, Does.Contain("pacing").Or.Contain("rushed"));
                Assert.That(score.EstimateBasisText, Does.Contain("not phoneme-level scoring"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void FastRecording_ProducesSlowerRhythmFeedback()
        {
            SpeechExerciseSO exercise = CreateExercise();

            try
            {
                PronunciationScoreResult score = scorer.Score(
                    exercise,
                    "The dog is big.",
                    new SpeechMatchResult(true, new[] { true, true, true, true }),
                    0.8f);

                Assert.That(score.IsAvailable, Is.True);
                Assert.That(score.TempoScore, Is.LessThan(80));
                Assert.That(score.FeedbackText, Does.Contain("Slow down"));
                Assert.That(score.ConfidenceReason, Does.Contain("pacing").Or.Contain("slow"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(exercise);
            }
        }

        [Test]
        public void UnavailableScore_UsesFriendlyCopy()
        {
            Assert.That(PronunciationScoreResult.Unavailable.IsAvailable, Is.False);
            Assert.That(PronunciationScoreResult.Unavailable.SummaryText, Does.Contain(FluentEchoCopy.FirstPronunciationSummary));
            Assert.That(PronunciationScoreResult.Unavailable.FeedbackText, Does.Contain(FluentEchoCopy.FirstEstimatePrompt));
        }

        [Test]
        public void SharedCopy_HelperBuildsExpectedFirstStateText()
        {
            Assert.That(FluentEchoCopy.BuildProgressSummary(0), Is.EqualTo(FluentEchoCopy.FirstProgressSummary));
            Assert.That(
                FluentEchoCopy.BuildProgressSummary(4),
                Is.EqualTo($"{FluentEchoCopy.FirstProgressSummary} | 0/4 matched"));

            string unavailableDetails = FluentEchoCopy.BuildUnavailablePronunciationDetails();
            Assert.That(unavailableDetails, Does.Contain(FluentEchoCopy.FirstEstimatePrompt));
            Assert.That(unavailableDetails, Does.Contain(FluentEchoCopy.FirstCoachingTipText));
            Assert.That(unavailableDetails, Does.Contain(FluentEchoCopy.PhonemeRoadmapText));
            Assert.That(unavailableDetails, Does.Not.Contain("Alignment preview (demo only):"));
        }

        private static SpeechExerciseSO CreateExercise(string secondWord = "dog")
        {
            SpeechExerciseSO exercise = ScriptableObject.CreateInstance<SpeechExerciseSO>();
            SerializedObject serialized = new(exercise);
            serialized.FindProperty("prompt").stringValue = "Say the sentence in English.";
            serialized.FindProperty("targetWords").arraySize = 4;
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(0).stringValue = "the";
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(1).stringValue = secondWord;
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(2).stringValue = "is";
            serialized.FindProperty("targetWords").GetArrayElementAtIndex(3).stringValue = "big";
            serialized.FindProperty("acceptedPhrases").stringValue = "the dog is big|the hound is big";
            serialized.FindProperty("progressKey").stringValue = "lesson_01_describe_the_dog";
            serialized.FindProperty("requireWordOrder").boolValue = true;
            serialized.FindProperty("allowFuzzyMatch").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return exercise;
        }
    }
}
