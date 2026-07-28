using System;
using System.Collections.Generic;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Presentation;
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

    public enum PronunciationMatchKind
    {
        Missing = 0,
        Exact = 1,
        Alternative = 2,
        Fuzzy = 3
    }

    public readonly struct PronunciationWordScore
    {
        public PronunciationWordScore(string word, int score, bool matched, PronunciationMatchKind kind)
        {
            Word = word ?? string.Empty;
            Score = Mathf.Clamp(score, 0, 100);
            Matched = matched;
            Kind = kind;
        }

        public string Word { get; }
        public int Score { get; }
        public bool Matched { get; }
        public PronunciationMatchKind Kind { get; }
    }

    public sealed class PronunciationScoreResult
    {
        public static readonly PronunciationScoreResult Unavailable = new(
            false,
            0,
            "UNAVAILABLE",
            "UNAVAILABLE",
            FluentEchoCopy.FirstPronunciationSummary,
            FluentEchoCopy.FirstEstimatePrompt,
            string.Empty,
            string.Empty,
            0,
            0,
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
            string confidenceBand,
            string summaryText,
            string feedbackText,
            string confidenceReason,
            string estimateBasisText,
            int matchedWordCount,
            int expectedWordCount,
            int missingWordCount,
            int extraWordCount,
            int coverageScore,
            int precisionScore,
            int tempoScore,
            int wordQualityScore,
            int confidenceScore,
            float recordingSeconds,
            PronunciationWordScore[] wordScores)
        {
            IsAvailable = isAvailable;
            OverallScore = Mathf.Clamp(overallScore, 0, 100);
            BandLabel = bandLabel ?? string.Empty;
            ConfidenceBand = confidenceBand ?? string.Empty;
            SummaryText = summaryText ?? string.Empty;
            FeedbackText = feedbackText ?? string.Empty;
            ConfidenceReason = confidenceReason ?? string.Empty;
            EstimateBasisText = estimateBasisText ?? string.Empty;
            MatchedWordCount = Mathf.Max(0, matchedWordCount);
            ExpectedWordCount = Mathf.Max(0, expectedWordCount);
            MissingWordCount = Mathf.Max(0, missingWordCount);
            ExtraWordCount = Mathf.Max(0, extraWordCount);
            CoverageScore = Mathf.Clamp(coverageScore, 0, 100);
            PrecisionScore = Mathf.Clamp(precisionScore, 0, 100);
            TempoScore = Mathf.Clamp(tempoScore, 0, 100);
            WordQualityScore = Mathf.Clamp(wordQualityScore, 0, 100);
            ConfidenceScore = Mathf.Clamp(confidenceScore, 0, 100);
            RecordingSeconds = Mathf.Max(0f, recordingSeconds);
            WordScores = wordScores ?? Array.Empty<PronunciationWordScore>();
        }

        public bool IsAvailable { get; }
        public int OverallScore { get; }
        public string BandLabel { get; }
        public string ConfidenceBand { get; }
        public string SummaryText { get; }
        public string FeedbackText { get; }
        public string ConfidenceReason { get; }
        public string EstimateBasisText { get; }
        public int MatchedWordCount { get; }
        public int ExpectedWordCount { get; }
        public int MissingWordCount { get; }
        public int ExtraWordCount { get; }
        public int CoverageScore { get; }
        public int PrecisionScore { get; }
        public int TempoScore { get; }
        public int WordQualityScore { get; }
        public int ConfidenceScore { get; }
        public float RecordingSeconds { get; }
        public IReadOnlyList<PronunciationWordScore> WordScores { get; }
    }
}
