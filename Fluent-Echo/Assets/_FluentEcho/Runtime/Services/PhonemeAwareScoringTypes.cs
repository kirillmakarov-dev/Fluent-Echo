using System;
using System.Collections.Generic;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Presentation;
using UnityEngine;

namespace FluentEcho.Services
{
    public enum PhonemeAlignmentSource
    {
        Unavailable,
        Preview
    }

    public interface IPhonemeAlignmentService
    {
        PhonemeAlignmentResult Align(PhonemeAlignmentRequest request);
    }

    public sealed class NoOpPhonemeAlignmentService : IPhonemeAlignmentService
    {
        public static readonly NoOpPhonemeAlignmentService Instance = new();

        private NoOpPhonemeAlignmentService()
        {
        }

        public PhonemeAlignmentResult Align(PhonemeAlignmentRequest request) => PhonemeAlignmentResult.Unavailable;
    }

    public static class PhonemeAlignmentServiceFactory
    {
        public static IPhonemeAlignmentService Create(bool enablePreview)
        {
            return enablePreview
                ? new InspectorPhonemeAlignmentService()
                : NoOpPhonemeAlignmentService.Instance;
        }
    }

    public sealed class InspectorPhonemeAlignmentService : IPhonemeAlignmentService
    {
        public PhonemeAlignmentResult Align(PhonemeAlignmentRequest request)
        {
            if (request == null || !request.HasTranscript || request.TargetWords.Count == 0)
                return PhonemeAlignmentResult.Unavailable;

            int expectedCount = request.TargetWords.Count;
            int matchedCount = request.MatchResult.MatchedWords != null
                ? CountMatchedWords(request.MatchResult.MatchedWords)
                : 0;
            int missingCount = Mathf.Max(0, expectedCount - matchedCount);
            int previewScore = Mathf.Clamp(Mathf.RoundToInt((matchedCount / (float) expectedCount) * 100f), 0, 100);
            string confidenceBand = previewScore >= 75 ? "high" : previewScore >= 45 ? "medium" : "low";
            string evidenceText = $"Preview mode: {matchedCount}/{expectedCount} target words matched from the transcript.";
            string summaryText = $"Phoneme alignment preview | {previewScore}/100 | {confidenceBand.ToUpperInvariant()}";
            string feedbackText = missingCount > 0
                ? $"Preview only: focus on the {missingCount} missing target word{PluralSuffix(missingCount)}."
                : "Preview only: the current transcript is clean enough to inspect alignment flow.";

            return new PhonemeAlignmentResult(
                PhonemeAlignmentSource.Preview,
                true,
                previewScore,
                confidenceBand,
                summaryText,
                feedbackText,
                evidenceText,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                request.RecordingSeconds);
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

        private static string PluralSuffix(int count) => count == 1 ? string.Empty : "s";
    }

    public sealed class PhonemeAlignmentRequest
    {
        public PhonemeAlignmentRequest(
            SpeechExerciseSO exercise,
            string transcript,
            SpeechMatchResult matchResult,
            float recordingSeconds,
            string[] targetWords,
            string[] acceptedPhrases)
        {
            Exercise = exercise;
            Transcript = transcript ?? string.Empty;
            MatchResult = matchResult;
            RecordingSeconds = Mathf.Max(0f, recordingSeconds);
            TargetWords = targetWords ?? Array.Empty<string>();
            AcceptedPhrases = acceptedPhrases ?? Array.Empty<string>();
        }

        public SpeechExerciseSO Exercise { get; }
        public string Transcript { get; }
        public SpeechMatchResult MatchResult { get; }
        public float RecordingSeconds { get; }
        public IReadOnlyList<string> TargetWords { get; }
        public IReadOnlyList<string> AcceptedPhrases { get; }

        public bool HasTranscript => !string.IsNullOrWhiteSpace(Transcript);
    }

    public sealed class PhonemeAlignmentResult
    {
        public static readonly PhonemeAlignmentResult Unavailable = new(
            PhonemeAlignmentSource.Unavailable,
            false,
            0,
            "unavailable",
            "Phoneme alignment is not active yet.",
            "This scorer is a roadmap item and stays separate from the current heuristic estimate.",
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            0f);

        public PhonemeAlignmentResult(
            PhonemeAlignmentSource source,
            bool isAvailable,
            int alignmentScore,
            string confidenceBand,
            string summaryText,
            string feedbackText,
            string evidenceText,
            string[] matchedPhonemes,
            string[] missingPhonemes,
            string[] weakPhonemes,
            float recordingSeconds)
        {
            Source = source;
            IsAvailable = isAvailable;
            AlignmentScore = Mathf.Clamp(alignmentScore, 0, 100);
            ConfidenceBand = confidenceBand ?? string.Empty;
            SummaryText = summaryText ?? string.Empty;
            FeedbackText = feedbackText ?? string.Empty;
            EvidenceText = evidenceText ?? string.Empty;
            MatchedPhonemes = matchedPhonemes ?? Array.Empty<string>();
            MissingPhonemes = missingPhonemes ?? Array.Empty<string>();
            WeakPhonemes = weakPhonemes ?? Array.Empty<string>();
            RecordingSeconds = Mathf.Max(0f, recordingSeconds);
        }

        public PhonemeAlignmentSource Source { get; }
        public bool IsAvailable { get; }
        public int AlignmentScore { get; }
        public string ConfidenceBand { get; }
        public string SummaryText { get; }
        public string FeedbackText { get; }
        public string EvidenceText { get; }
        public IReadOnlyList<string> MatchedPhonemes { get; }
        public IReadOnlyList<string> MissingPhonemes { get; }
        public IReadOnlyList<string> WeakPhonemes { get; }
        public float RecordingSeconds { get; }

        public bool HasEvidence =>
            !string.IsNullOrWhiteSpace(EvidenceText)
            || MatchedPhonemes.Count > 0
            || MissingPhonemes.Count > 0
            || WeakPhonemes.Count > 0;

        public bool IsPreview => Source == PhonemeAlignmentSource.Preview;
    }
}
