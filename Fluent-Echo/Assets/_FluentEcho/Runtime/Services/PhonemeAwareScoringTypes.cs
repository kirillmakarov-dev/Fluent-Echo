using System;
using System.Collections.Generic;
using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Presentation;
using UnityEngine;

namespace FluentEcho.Services
{
    public interface IPhonemeAlignmentService
    {
        PhonemeAlignmentResult Align(PhonemeAlignmentRequest request);
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
        public bool HasMatchResult => MatchResult.MatchedWords != null;
    }

    public sealed class PhonemeAlignmentResult
    {
        public static readonly PhonemeAlignmentResult Unavailable = new(
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
            MatchedPhonemes.Count > 0
            || MissingPhonemes.Count > 0
            || WeakPhonemes.Count > 0;
    }
}
