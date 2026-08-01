using System;
using FluentEcho.Domain;
using FluentEcho.Services;
using UnityEngine;

namespace FluentEcho.Presentation
{
    public sealed class PronunciationResultFlowSnapshot
    {
        private PronunciationResultFlowSnapshot(
            string[] currentAttemptLines,
            string[] acceptedResultLines)
        {
            CurrentAttemptLines = currentAttemptLines ?? Array.Empty<string>();
            AcceptedResultLines = acceptedResultLines ?? Array.Empty<string>();
        }

        public System.Collections.Generic.IReadOnlyList<string> CurrentAttemptLines { get; }
        public System.Collections.Generic.IReadOnlyList<string> AcceptedResultLines { get; }

        public static PronunciationResultFlowSnapshot Create(
            PronunciationScoreResult score,
            string transcript,
            string phonemeAlignmentText,
            LessonProgressState progress,
            string historyText,
            string nextStepPrompt)
        {
            PronunciationScoreNarrativeSnapshot scoreSnapshot = PronunciationScoreNarrativeSnapshot.Create(
                score,
                transcript,
                phonemeAlignmentText);

            var currentAttemptLines = new System.Collections.Generic.List<string>(scoreSnapshot.CurrentAttemptLines);
            if (score != null && score.IsAvailable)
            {
                AppendProgressSummary(currentAttemptLines, progress, score, transcript);
                AppendHistory(currentAttemptLines, historyText);
            }

            var acceptedResultLines = new System.Collections.Generic.List<string>(scoreSnapshot.AcceptedResultLines);
            if (score != null && score.IsAvailable)
            {
                AppendAcceptedProgress(acceptedResultLines, progress);
                AppendNextStep(acceptedResultLines, nextStepPrompt);
            }

            return new PronunciationResultFlowSnapshot(
                currentAttemptLines.ToArray(),
                acceptedResultLines.ToArray());
        }

        private static void AppendProgressSummary(
            System.Collections.Generic.List<string> lines,
            LessonProgressState progress,
            PronunciationScoreResult score,
            string transcript)
        {
            if (lines == null || progress == null || score == null || !score.IsAvailable)
                return;

            if (progress.BestPronunciationScore > 0)
            {
                string bestAttempt = $"Best so far: {progress.BestPronunciationScore}/100";
                if (!string.IsNullOrWhiteSpace(progress.BestPronunciationConfidenceBand))
                    bestAttempt += $" | confidence {Capitalize(progress.BestPronunciationConfidenceBand)}";

                if (!string.IsNullOrWhiteSpace(progress.BestTranscript))
                    bestAttempt += $" | \"{progress.BestTranscript}\"";

                lines.Add(bestAttempt);
            }

            lines.Add("Lesson progress:");
            lines.Add(progress.GetSummaryText());
        }

        private static void AppendAcceptedProgress(
            System.Collections.Generic.List<string> lines,
            LessonProgressState progress)
        {
            if (lines == null || progress == null)
                return;

            lines.Add(string.Empty);
            lines.Add(FluentEchoCopy.ResultLessonRecapHeader);
            lines.Add(progress.GetSummaryText());
        }

        private static void AppendHistory(System.Collections.Generic.List<string> lines, string historyText)
        {
            if (lines == null || string.IsNullOrWhiteSpace(historyText))
                return;

            if (lines.Count > 0)
                lines.Add(string.Empty);

            lines.Add(historyText);
        }

        private static void AppendNextStep(System.Collections.Generic.List<string> lines, string nextStepPrompt)
        {
            if (lines == null)
                return;

            if (!string.IsNullOrWhiteSpace(nextStepPrompt))
            {
                lines.Add(string.Empty);
                lines.Add(FluentEchoCopy.ResultNextStepHeader);
                lines.Add(nextStepPrompt);
            }
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }
    }
}
