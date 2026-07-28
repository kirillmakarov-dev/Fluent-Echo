using System;
using System.Collections.Generic;
using FluentEcho.Data;
using FluentEcho.Domain;
using UnityEngine;

namespace FluentEcho.Services
{
    public interface ISpeechRecognitionService
    {
        event Action<string> TranscriptUpdated;
        event Action AnalysisStarted;
        event Action ListeningStopped;
        event Action<string> Failed;
        event Action<string> StatusChanged;

        bool IsListening { get; }
        bool IsReady { get; }

        void Configure(SpeechExerciseSO exercise);
        void Prepare();
        void StartListening();
        void StopListening();
        void Cancel();
    }

    public interface IPronunciationScoringService
    {
        PronunciationScoreResult Score(
            SpeechExerciseSO exercise,
            string transcript,
            SpeechMatchResult matchResult,
            float recordingSeconds);
    }

    public readonly struct PronunciationWordScore
    {
        public PronunciationWordScore(string word, int score, bool matched)
        {
            Word = word ?? string.Empty;
            Score = Mathf.Clamp(score, 0, 100);
            Matched = matched;
        }

        public string Word { get; }
        public int Score { get; }
        public bool Matched { get; }
    }

    public sealed class PronunciationScoreResult
    {
        public static readonly PronunciationScoreResult Unavailable = new(
            false,
            0,
            "UNAVAILABLE",
            "Pronunciation score is not available yet.",
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0f,
            Array.Empty<PronunciationWordScore>());

        public PronunciationScoreResult(
            bool isAvailable,
            int overallScore,
            string bandLabel,
            string summaryText,
            string feedbackText,
            int matchedWordCount,
            int expectedWordCount,
            int missingWordCount,
            int extraWordCount,
            int coverageScore,
            int precisionScore,
            int tempoScore,
            float recordingSeconds,
            PronunciationWordScore[] wordScores)
        {
            IsAvailable = isAvailable;
            OverallScore = Mathf.Clamp(overallScore, 0, 100);
            BandLabel = bandLabel ?? string.Empty;
            SummaryText = summaryText ?? string.Empty;
            FeedbackText = feedbackText ?? string.Empty;
            MatchedWordCount = Mathf.Max(0, matchedWordCount);
            ExpectedWordCount = Mathf.Max(0, expectedWordCount);
            MissingWordCount = Mathf.Max(0, missingWordCount);
            ExtraWordCount = Mathf.Max(0, extraWordCount);
            CoverageScore = Mathf.Clamp(coverageScore, 0, 100);
            PrecisionScore = Mathf.Clamp(precisionScore, 0, 100);
            TempoScore = Mathf.Clamp(tempoScore, 0, 100);
            RecordingSeconds = Mathf.Max(0f, recordingSeconds);
            WordScores = wordScores ?? Array.Empty<PronunciationWordScore>();
        }

        public bool IsAvailable { get; }
        public int OverallScore { get; }
        public string BandLabel { get; }
        public string SummaryText { get; }
        public string FeedbackText { get; }
        public int MatchedWordCount { get; }
        public int ExpectedWordCount { get; }
        public int MissingWordCount { get; }
        public int ExtraWordCount { get; }
        public int CoverageScore { get; }
        public int PrecisionScore { get; }
        public int TempoScore { get; }
        public float RecordingSeconds { get; }
        public IReadOnlyList<PronunciationWordScore> WordScores { get; }
    }

    public sealed class HeuristicPronunciationScoringService : IPronunciationScoringService
    {
        public PronunciationScoreResult Score(
            SpeechExerciseSO exercise,
            string transcript,
            SpeechMatchResult matchResult,
            float recordingSeconds)
        {
            if (exercise == null)
                return PronunciationScoreResult.Unavailable;

            string[] expectedWords = exercise.GetDisplayWords();
            if (expectedWords.Length == 0)
                return PronunciationScoreResult.Unavailable;

            bool[] matchedWordFlags = matchResult.MatchedWords ?? Array.Empty<bool>();
            List<string> spokenWords = Tokenize(transcript);
            int matchedWords = CountMatchedWords(matchedWordFlags);
            int expectedCount = expectedWords.Length;
            int spokenCount = spokenWords.Count;
            int missingCount = Mathf.Max(0, expectedCount - matchedWords);
            int extraCount = Mathf.Max(0, spokenCount - matchedWords);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                int emptyScore = 0;
                string emptySummary = BuildSummary(emptyScore, "needs work");
                return new PronunciationScoreResult(
                    true,
                    emptyScore,
                    "needs work",
                    emptySummary,
                    "Say the sentence once, then pause so the score can be calculated.",
                    matchedWords,
                    expectedCount,
                    missingCount,
                    extraCount,
                    0,
                    0,
                    0,
                    recordingSeconds,
                    BuildWordScores(expectedWords, matchedWordFlags));
            }

            float coverage = Mathf.Clamp01((float) matchedWords / expectedCount);
            float precision = spokenCount <= 0
                ? 0f
                : Mathf.Clamp01(1f - (float) extraCount / Mathf.Max(1f, spokenCount));
            float tempo = ScoreTempo(recordingSeconds, spokenCount, expectedCount);
            float completenessBonus = matchResult.IsComplete ? 0.12f : 0f;
            int coverageScore = Mathf.RoundToInt(coverage * 100f);
            int precisionScore = Mathf.RoundToInt(precision * 100f);
            int tempoScore = Mathf.RoundToInt(tempo * 100f);

            float raw = (coverage * 0.60f) + (precision * 0.20f) + (tempo * 0.20f) + completenessBonus;
            int overallScore = Mathf.Clamp(Mathf.RoundToInt(raw * 100f), 0, 100);
            string band = GetBandLabel(overallScore);
            string summary = BuildSummary(overallScore, band);
            string feedback = BuildFeedback(
                matchResult,
                transcript,
                recordingSeconds,
                expectedWords,
                missingCount,
                extraCount,
                overallScore,
                band);

            return new PronunciationScoreResult(
                true,
                overallScore,
                band,
                summary,
                feedback,
                matchedWords,
                expectedCount,
                missingCount,
                extraCount,
                coverageScore,
                precisionScore,
                tempoScore,
                recordingSeconds,
                BuildWordScores(expectedWords, matchedWordFlags));
        }

        private static string BuildSummary(int score, string band)
        {
            if (score <= 0)
                return "PRONUNCIATION | 0/100 | needs work";

            return $"PRONUNCIATION | {score:0}/100 | {band.ToUpperInvariant()}";
        }

        private static string BuildFeedback(
            SpeechMatchResult matchResult,
            string transcript,
            float recordingSeconds,
            string[] expectedWords,
            int missingCount,
            int extraCount,
            int score,
            string band)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return "Try saying the sentence once, then let the model finish the turn.";

            string focus = BuildFocusText(expectedWords, matchResult.MatchedWords);
            if (missingCount > 0)
            {
                string suffix = string.IsNullOrWhiteSpace(focus) ? string.Empty : $" Focus on {focus}.";
                return $"Missing {missingCount} word{PluralSuffix(missingCount)}.{suffix}";
            }

            if (extraCount > 0)
                return $"Good coverage, but there are {extraCount} extra word{PluralSuffix(extraCount)}.";

            float expectedSeconds = Mathf.Max(1.5f, expectedWords.Length * 0.7f);
            if (recordingSeconds > expectedSeconds * 1.4f)
                return "Clear word match. Try a slightly quicker, more natural rhythm.";

            if (recordingSeconds > 0f && recordingSeconds < expectedSeconds * 0.7f)
                return "Good word match. Slow down a little so each word lands cleanly.";

            if (score >= 85)
                return $"Strong delivery. {band.ToUpperInvariant()} pronunciation.";

            return $"Good transcript match. Keep the rhythm steady and natural.";
        }

        private static string BuildFocusText(string[] expectedWords, bool[] matches)
        {
            if (expectedWords == null || expectedWords.Length == 0)
                return string.Empty;

            var focus = new List<string>();
            for (int i = 0; i < expectedWords.Length && i < matches.Length; i++)
            {
                if (matches[i])
                    continue;

                string word = expectedWords[i];
                if (!string.IsNullOrWhiteSpace(word))
                    focus.Add(word);

                if (focus.Count == 2)
                    break;
            }

            return focus.Count == 0 ? string.Empty : string.Join(", ", focus);
        }

        private static PronunciationWordScore[] BuildWordScores(string[] expectedWords, bool[] matches)
        {
            if (expectedWords == null || expectedWords.Length == 0)
                return Array.Empty<PronunciationWordScore>();

            var scores = new PronunciationWordScore[expectedWords.Length];
            for (int i = 0; i < expectedWords.Length; i++)
            {
                bool matched = matches != null && i < matches.Length && matches[i];
                int score = matched ? 90 : 35;
                scores[i] = new PronunciationWordScore(expectedWords[i], score, matched);
            }

            return scores;
        }

        private static string GetBandLabel(int score)
        {
            if (score >= 85)
                return "strong";

            if (score >= 60)
                return "steady";

            if (score >= 35)
                return "developing";

            return "needs work";
        }

        private static float ScoreTempo(float recordingSeconds, int spokenCount, int expectedCount)
        {
            if (recordingSeconds <= 0f || spokenCount <= 0)
                return 0.5f;

            float idealSeconds = Mathf.Max(1.5f, expectedCount * 0.75f);
            float ratio = recordingSeconds / idealSeconds;
            float distance = Mathf.Abs(ratio - 1f);
            return Mathf.Clamp01(1f - (distance * 0.85f));
        }

        private static int CountMatchedWords(bool[] matches)
        {
            if (matches == null || matches.Length == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i])
                    count++;
            }

            return count;
        }

        private static List<string> Tokenize(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            string[] pieces = text.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pieces.Length; i++)
            {
                string token = NormalizeToken(pieces[i]);
                if (!string.IsNullOrEmpty(token))
                    result.Add(token);
            }

            return result;
        }

        private static string NormalizeToken(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return string.Empty;

            word = word.Trim().ToLowerInvariant();
            int start = 0;
            int end = word.Length - 1;

            while (start <= end && ShouldStrip(word[start]))
                start++;
            while (end >= start && ShouldStrip(word[end]))
                end--;

            return start <= end ? word.Substring(start, end - start + 1) : string.Empty;
        }

        private static bool ShouldStrip(char value) =>
            char.IsPunctuation(value) || char.IsSymbol(value);

        private static string PluralSuffix(int count) => count == 1 ? string.Empty : "s";
    }
}
