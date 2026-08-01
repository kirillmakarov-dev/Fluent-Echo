using System;
using FluentEcho.Services;
using UnityEngine;

namespace FluentEcho.Presentation
{
    public sealed class PronunciationScoreNarrativeSnapshot
    {
        private PronunciationScoreNarrativeSnapshot(
            string[] currentAttemptLines,
            string[] acceptedResultLines)
        {
            CurrentAttemptLines = currentAttemptLines ?? Array.Empty<string>();
            AcceptedResultLines = acceptedResultLines ?? Array.Empty<string>();
        }

        public System.Collections.Generic.IReadOnlyList<string> CurrentAttemptLines { get; }
        public System.Collections.Generic.IReadOnlyList<string> AcceptedResultLines { get; }

        public static PronunciationScoreNarrativeSnapshot Create(
            PronunciationScoreResult score,
            string transcript,
            string phonemeAlignmentText)
        {
            if (score == null || !score.IsAvailable)
                return new PronunciationScoreNarrativeSnapshot(Array.Empty<string>(), Array.Empty<string>());

            string focusNext = PronunciationScoreNarrativeFormatter.BuildFocusNextText(score);
            string matchQuality = PronunciationScoreNarrativeFormatter.BuildMatchQualityText(score);
            string approximateMatches = PronunciationScoreNarrativeFormatter.BuildApproximateMatchText(score);

            var currentAttemptLines = new System.Collections.Generic.List<string>
            {
                "Current attempt:",
                $"Practice score: {score.OverallScore}/100 | {score.BandLabel}",
                $"Confidence: {Capitalize(score.ConfidenceBand)}",
                string.IsNullOrWhiteSpace(score.ConfidenceReason)
                    ? string.Empty
                    : $"Confidence reason: {score.ConfidenceReason}",
                $"Word match: {score.MatchedWordCount}/{score.ExpectedWordCount}",
                $"Recognition precision: {score.PrecisionScore}%",
                $"Rhythm: {score.TempoScore}%",
                $"Word focus: {score.WordQualityScore}%",
                $"Exact words: {score.ExactWordCount} | Approximate words: {score.ApproximateWordCount} | Missed words: {score.MissedWordCount}"
            };

            if (!string.IsNullOrWhiteSpace(score.EstimateBasisText))
                currentAttemptLines.Add(score.EstimateBasisText);

            if (!string.IsNullOrWhiteSpace(phonemeAlignmentText))
                currentAttemptLines.Add(phonemeAlignmentText);

            if (!string.IsNullOrWhiteSpace(matchQuality))
                currentAttemptLines.Add(matchQuality);

            if (!string.IsNullOrWhiteSpace(approximateMatches))
                currentAttemptLines.Add(approximateMatches);

            currentAttemptLines.Add("Focus next:");
            currentAttemptLines.Add(focusNext);

            if (!string.IsNullOrWhiteSpace(score.FeedbackText))
                currentAttemptLines.Add(score.FeedbackText);

            var acceptedResultLines = new System.Collections.Generic.List<string>
            {
                FluentEchoCopy.ResultAcceptedHeader,
                FluentEchoCopy.ResultWhatWeHeardHeader,
                string.IsNullOrWhiteSpace(transcript)
                    ? "Your transcript will appear here."
                    : $"\"{transcript.Trim()}\"",
                string.Empty,
                FluentEchoCopy.ResultEstimateHeader,
                $"Practice score: {score.OverallScore}/100 | {score.BandLabel}",
                $"Confidence: {Capitalize(score.ConfidenceBand)}",
                string.IsNullOrWhiteSpace(score.ConfidenceReason)
                    ? string.Empty
                    : $"Confidence reason: {score.ConfidenceReason}",
                string.IsNullOrWhiteSpace(score.EstimateBasisText)
                    ? string.Empty
                    : score.EstimateBasisText,
                string.Empty,
                FluentEchoCopy.ResultSignalBreakdownHeader,
                $"Coverage: {score.MatchedWordCount}/{score.ExpectedWordCount} | Precision: {score.PrecisionScore}% | Rhythm: {score.TempoScore}% | Word focus: {score.WordQualityScore}%"
            };

            if (!string.IsNullOrWhiteSpace(phonemeAlignmentText))
                acceptedResultLines.Add(phonemeAlignmentText);

            if (!string.IsNullOrWhiteSpace(matchQuality))
                acceptedResultLines.Add(matchQuality);

            if (!string.IsNullOrWhiteSpace(score.FeedbackText))
            {
                acceptedResultLines.Add(string.Empty);
                acceptedResultLines.Add(FluentEchoCopy.ResultTakeawayHeader);
                acceptedResultLines.Add(score.FeedbackText);
            }

            acceptedResultLines.Add(string.Empty);
            acceptedResultLines.Add("Focus next:");
            acceptedResultLines.Add(focusNext);

            return new PronunciationScoreNarrativeSnapshot(
                currentAttemptLines.ToArray(),
                acceptedResultLines.ToArray());
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }
    }
}
