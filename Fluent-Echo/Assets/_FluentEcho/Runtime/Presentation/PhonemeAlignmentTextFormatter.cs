using System.Collections.Generic;
using FluentEcho.Services;

namespace FluentEcho.Presentation
{
    public static class PhonemeAlignmentTextFormatter
    {
        public static string BuildPreviewText(PhonemeAlignmentResult alignment)
        {
            if (alignment == null)
                return string.Empty;

            if (!alignment.IsAvailable)
                return string.Empty;

            var lines = new List<string>
            {
                "Alignment preview:",
                $"Preview score: {alignment.AlignmentScore}/100 | {Capitalize(alignment.ConfidenceBand)}"
            };

            if (!string.IsNullOrWhiteSpace(alignment.EvidenceText))
                lines.Add(alignment.EvidenceText);

            if (!string.IsNullOrWhiteSpace(alignment.FeedbackText))
                lines.Add(alignment.FeedbackText);

            return string.Join("\n", lines);
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (value.Length == 1)
                return value.ToUpperInvariant();

            return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
        }
    }
}
