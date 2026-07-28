using System;
using System.Collections.Generic;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Presentation;
using UnityEngine;

namespace FluentEcho.Services
{
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
            string[][] acceptedWordGroups = exercise.GetAcceptedWordGroups();
            List<string> spokenWords = Tokenize(transcript);
            bool transcriptWasEmpty = string.IsNullOrWhiteSpace(transcript);
            PronunciationWordScore[] wordScores = BuildWordScores(
                expectedWords,
                acceptedWordGroups,
                spokenWords,
                exercise.RequireWordOrder,
                exercise.AllowFuzzyMatch);
            int matchedWords = CountMatchedWords(matchedWordFlags);
            int expectedCount = expectedWords.Length;
            int spokenCount = spokenWords.Count;
            int missingCount = Mathf.Max(0, expectedCount - matchedWords);
            int extraCount = Mathf.Max(0, spokenCount - matchedWords);

            if (transcriptWasEmpty)
            {
                int emptyScore = 0;
                string emptyConfidence = GetConfidenceBand(emptyScore);
                string emptySummary = BuildSummary(emptyScore, "needs work", emptyConfidence);
                return new PronunciationScoreResult(
                    true,
                    emptyScore,
                    "needs work",
                    emptyConfidence,
                    emptySummary,
                    "Say the sentence once, then pause so the pronunciation estimate can be calculated.",
                    BuildConfidenceReason(
                        matchedWords,
                        expectedCount,
                        missingCount,
                        extraCount,
                        emptyConfidence,
                        true,
                        transcriptWasEmpty),
                    BuildEstimateBasisText(true, transcriptWasEmpty),
                    matchedWords,
                    expectedCount,
                    missingCount,
                    extraCount,
                    0,
                    0,
                    0,
                    ComputeWordQualityScore(wordScores),
                    0,
                    recordingSeconds,
                    wordScores);
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
            int wordQualityScore = ComputeWordQualityScore(wordScores);
            int confidenceScore = ComputeConfidenceScore(
                matchedWords,
                expectedCount,
                missingCount,
                extraCount,
                tempoScore,
                wordQualityScore,
                matchResult.IsComplete,
                transcript);
            string confidenceBand = GetConfidenceBand(confidenceScore);
            string confidenceReason = BuildConfidenceReason(
                matchedWords,
                expectedCount,
                missingCount,
                extraCount,
                confidenceBand,
                matchResult.IsComplete,
                transcriptWasEmpty);

            float raw = (coverage * 0.40f)
                + (precision * 0.15f)
                + (tempo * 0.15f)
                + ((wordQualityScore / 100f) * 0.30f)
                + completenessBonus;
            int overallScore = Mathf.Clamp(Mathf.RoundToInt(raw * 100f), 0, 100);
            string band = GetBandLabel(overallScore);
            string summary = BuildSummary(overallScore, band, confidenceBand);
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
                confidenceBand,
                summary,
                feedback,
                confidenceReason,
                BuildEstimateBasisText(matchResult.IsComplete, transcriptWasEmpty),
                matchedWords,
                expectedCount,
                missingCount,
                extraCount,
                coverageScore,
                precisionScore,
                tempoScore,
                wordQualityScore,
                confidenceScore,
                recordingSeconds,
                wordScores);
        }

        private static string BuildSummary(int score, string band, string confidenceBand)
        {
            if (score <= 0)
                return $"PRONUNCIATION ESTIMATE | 0/100 | {confidenceBand.ToUpperInvariant()}";

            return $"PRONUNCIATION ESTIMATE | {score:0}/100 | {confidenceBand.ToUpperInvariant()}";
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
                return FluentEchoCopy.DidNotCatchThatDetailedStatus;

            string focus = BuildFocusText(expectedWords, matchResult.MatchedWords);
            if (missingCount > 0)
            {
                if (!HasAnyMatchedWord(matchResult.MatchedWords) && expectedWords != null && expectedWords.Length > 0)
                    return $"I heard speech, but none of the target words matched. Start with \"{expectedWords[0]}\" and try the full line again.";

                if (missingCount == 1 && !string.IsNullOrWhiteSpace(focus))
                    return $"Missing 1 word. Repeat \"{focus}\" once, then say the full line again.";

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
                return $"Strong transcript match. {band.ToUpperInvariant()} pronunciation estimate.";

            return $"Good transcript match. Keep the rhythm steady and natural for the next attempt.";
        }

        private static string BuildConfidenceReason(
            int matchedWords,
            int expectedCount,
            int missingCount,
            int extraCount,
            string confidenceBand,
            bool isComplete,
            bool transcriptWasEmpty)
        {
            if (expectedCount <= 0)
                return string.Empty;

            if (transcriptWasEmpty)
                return "No speech was transcribed, so confidence stays low.";

            if (matchedWords <= 0)
                return "No target words matched, so confidence stays low.";

            if (missingCount > 0)
                return $"Missing {missingCount} word{PluralSuffix(missingCount)}, so confidence stays cautious.";

            if (extraCount > 0)
                return "Extra words were heard, so confidence stays a little cautious.";

            if (isComplete && confidenceBand == "high")
                return "All target words matched cleanly, with no extra words.";

            return "The match is useful, but confidence still stays cautious.";
        }

        private static string BuildEstimateBasisText(bool isComplete)
        {
            return BuildEstimateBasisText(isComplete, false);
        }

        private static string BuildEstimateBasisText(bool isComplete, bool transcriptWasEmpty)
        {
            string basis = "Estimate basis: transcript coverage, word quality, and pacing.";
            if (transcriptWasEmpty)
                return $"{basis} No speech was transcribed.";

            return isComplete
                ? $"{basis} This is not phoneme-level scoring."
                : $"{basis} Missing words keep the estimate conservative.";
        }

        private static int ComputeConfidenceScore(
            int matchedWords,
            int expectedCount,
            int missingCount,
            int extraCount,
            int tempoScore,
            int wordQualityScore,
            bool isComplete,
            string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript) || expectedCount <= 0)
                return 0;

            float matchRatio = expectedCount <= 0
                ? 0f
                : Mathf.Clamp01((float) matchedWords / expectedCount);
            float extraPenalty = Mathf.Clamp01(extraCount / Mathf.Max(1f, expectedCount * 0.6f));
            float missingPenalty = Mathf.Clamp01(missingCount / Mathf.Max(1f, expectedCount));
            float rhythm = Mathf.Clamp01(tempoScore / 100f);
            float clarity = Mathf.Clamp01(wordQualityScore / 100f);

            float score = (matchRatio * 0.48f)
                + (clarity * 0.26f)
                + (rhythm * 0.16f)
                + (isComplete ? 0.12f : 0f)
                - (extraPenalty * 0.18f)
                - (missingPenalty * 0.10f);

            int confidenceScore = Mathf.RoundToInt(Mathf.Clamp01(score) * 100f);

            if (missingCount > 0 || extraCount > 0)
                confidenceScore = Mathf.Min(confidenceScore, 74);

            if (extraCount >= Mathf.Max(2, expectedCount / 2))
                confidenceScore = Mathf.Min(confidenceScore, 44);

            return confidenceScore;
        }

        private static string GetConfidenceBand(int score)
        {
            if (score >= 75)
                return "high";

            if (score >= 45)
                return "medium";

            return "low";
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

        private static PronunciationWordScore[] BuildWordScores(
            string[] expectedWords,
            string[][] acceptedWordGroups,
            List<string> spokenWords,
            bool requireWordOrder,
            bool allowFuzzyMatch)
        {
            if (expectedWords == null || expectedWords.Length == 0)
                return Array.Empty<PronunciationWordScore>();

            var scores = new PronunciationWordScore[expectedWords.Length];
            bool[] consumed = spokenWords == null ? Array.Empty<bool>() : new bool[spokenWords.Count];
            for (int i = 0; i < expectedWords.Length; i++)
            {
                string expectedWord = expectedWords[i];
                string[] acceptedWords = acceptedWordGroups != null && i < acceptedWordGroups.Length
                    ? acceptedWordGroups[i]
                    : Array.Empty<string>();
                WordScoreResult scoreResult = ScoreWord(
                    expectedWord,
                    acceptedWords,
                    spokenWords,
                    i,
                    requireWordOrder,
                    allowFuzzyMatch,
                    consumed);

                scores[i] = new PronunciationWordScore(
                    expectedWord,
                    scoreResult.Score,
                    scoreResult.Kind != PronunciationMatchKind.Missing,
                    scoreResult.Kind);
            }

            return scores;
        }

        private readonly struct WordScoreResult
        {
            public WordScoreResult(int score, PronunciationMatchKind kind)
            {
                Score = score;
                Kind = kind;
            }

            public int Score { get; }
            public PronunciationMatchKind Kind { get; }
        }

        private static WordScoreResult ScoreWord(
            string expectedWord,
            string[] acceptedWords,
            List<string> spokenWords,
            int expectedIndex,
            bool requireWordOrder,
            bool allowFuzzyMatch,
            bool[] consumed)
        {
            if (spokenWords == null || spokenWords.Count == 0)
                return new WordScoreResult(0, PronunciationMatchKind.Missing);

            int bestScore = 0;
            int bestTokenIndex = -1;
            PronunciationMatchKind bestKind = PronunciationMatchKind.Missing;
            for (int tokenIndex = 0; tokenIndex < spokenWords.Count; tokenIndex++)
            {
                if (consumed != null && tokenIndex < consumed.Length && consumed[tokenIndex])
                    continue;

                string token = spokenWords[tokenIndex];
                WordScoreResult candidate = ScoreTokenAgainstCandidates(token, expectedWord, acceptedWords, allowFuzzyMatch);
                int score = candidate.Score;
                if (score <= 0)
                    continue;

                if (requireWordOrder)
                {
                    int distancePenalty = Mathf.Min(24, Mathf.Abs(tokenIndex - expectedIndex) * 6);
                    score = Mathf.Max(0, score - distancePenalty);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTokenIndex = tokenIndex;
                    bestKind = candidate.Kind;
                }
            }

            if (bestScore <= 0)
                return new WordScoreResult(0, PronunciationMatchKind.Missing);

            if (consumed != null && bestTokenIndex >= 0 && bestTokenIndex < consumed.Length)
                consumed[bestTokenIndex] = true;

            return new WordScoreResult(bestScore, bestKind);
        }

        private static WordScoreResult ScoreTokenAgainstCandidates(
            string token,
            string expectedWord,
            string[] acceptedWords,
            bool allowFuzzyMatch)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new WordScoreResult(0, PronunciationMatchKind.Missing);

            string normalizedExpected = NormalizeToken(expectedWord);
            int bestScore = ScoreCandidate(token, expectedWord, normalizedExpected, allowFuzzyMatch, out PronunciationMatchKind kind);
            if (acceptedWords == null)
                return new WordScoreResult(bestScore, kind);

            for (int i = 0; i < acceptedWords.Length; i++)
            {
                int candidateScore = ScoreCandidate(token, acceptedWords[i], normalizedExpected, allowFuzzyMatch, out PronunciationMatchKind candidateKind);
                if (candidateScore > bestScore)
                {
                    bestScore = candidateScore;
                    kind = candidateKind;
                }
            }

            return new WordScoreResult(bestScore, kind);
        }

        private static int ScoreCandidate(
            string token,
            string candidate,
            string normalizedExpected,
            bool allowFuzzyMatch,
            out PronunciationMatchKind kind)
        {
            kind = PronunciationMatchKind.Missing;
            string normalizedCandidate = NormalizeToken(candidate);
            if (string.IsNullOrEmpty(normalizedCandidate))
                return 0;

            if (token == normalizedCandidate)
            {
                kind = string.Equals(normalizedCandidate, normalizedExpected, StringComparison.Ordinal)
                    ? PronunciationMatchKind.Exact
                    : PronunciationMatchKind.Alternative;
                return 100;
            }

            if (!allowFuzzyMatch)
                return 0;

            int distance = ComputeLevenshteinDistance(token, normalizedCandidate);
            int longest = Mathf.Max(token.Length, normalizedCandidate.Length);
            if (longest <= 0)
                return 0;

            float similarity = 1f - ((float) distance / longest);
            if (similarity <= 0f)
                return 0;

            kind = PronunciationMatchKind.Fuzzy;
            return Mathf.RoundToInt(Mathf.Lerp(48f, 92f, similarity));
        }

        private static int ComputeWordQualityScore(PronunciationWordScore[] wordScores)
        {
            if (wordScores == null || wordScores.Length == 0)
                return 0;

            int total = 0;
            for (int i = 0; i < wordScores.Length; i++)
                total += wordScores[i].Score;

            return Mathf.RoundToInt((float) total / wordScores.Length);
        }

        private static int ComputeLevenshteinDistance(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
                return string.IsNullOrEmpty(right) ? 0 : right.Length;

            if (string.IsNullOrEmpty(right))
                return left.Length;

            int[,] matrix = new int[left.Length + 1, right.Length + 1];
            for (int i = 0; i <= left.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= right.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= left.Length; i++)
            {
                for (int j = 1; j <= right.Length; j++)
                {
                    int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    matrix[i, j] = Mathf.Min(
                        Mathf.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[left.Length, right.Length];
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

        private static bool HasAnyMatchedWord(bool[] matches)
        {
            if (matches == null || matches.Length == 0)
                return false;

            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i])
                    return true;
            }

            return false;
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
