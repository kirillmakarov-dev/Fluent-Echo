using FluentEcho.Presentation;
using FluentEcho.Services;
using NUnit.Framework;

namespace FluentEcho.Tests
{
    public sealed class PronunciationScoreNarrativeFormatterTests
    {
        [Test]
        public void MatchQualityText_UsesExplicitCounts()
        {
            PronunciationScoreResult score = CreateScore(
                exact: 3,
                approximate: 1,
                missed: 1);

            string text = PronunciationScoreNarrativeFormatter.BuildMatchQualityText(score);

            Assert.That(text, Is.EqualTo("Match quality: 3 exact | 1 approximate | 1 missed"));
        }

        [Test]
        public void ApproximateMatchText_UsesExplicitCounts()
        {
            PronunciationScoreResult score = CreateScore(approximate: 2);

            string text = PronunciationScoreNarrativeFormatter.BuildApproximateMatchText(score);

            Assert.That(text, Is.EqualTo("Approximate matches: 2 words"));
        }

        [Test]
        public void FocusNext_PrioritizesMissingWords()
        {
            PronunciationScoreResult score = CreateScore(missing: 2, exact: 2, missed: 2);

            string text = PronunciationScoreNarrativeFormatter.BuildFocusNextText(score);

            Assert.That(text, Is.EqualTo("Focus next: say the 2 missing words slowly once, then repeat the full line."));
        }

        [Test]
        public void FocusNext_UsesRhythmWhenNoWordIssuesRemain()
        {
            PronunciationScoreResult score = CreateScore(
                overallScore: 96,
                confidenceBand: "high",
                tempoScore: 96,
                exact: 4,
                missed: 0);

            string text = PronunciationScoreNarrativeFormatter.BuildFocusNextText(score);

            Assert.That(text, Is.EqualTo("Focus next: keep the pace steady and natural."));
        }

        [Test]
        public void FocusNext_ReturnsCleanerRepeatForLowConfidence()
        {
            PronunciationScoreResult score = CreateScore(
                overallScore: 72,
                confidenceBand: "low",
                tempoScore: 70,
                exact: 4,
                missed: 0);

            string text = PronunciationScoreNarrativeFormatter.BuildFocusNextText(score);

            Assert.That(text, Is.EqualTo("Focus next: try a cleaner full-line repeat."));
        }

        private static PronunciationScoreResult CreateScore(
            int overallScore = 80,
            string confidenceBand = "medium",
            int tempoScore = 70,
            int exact = 4,
            int approximate = 0,
            int missed = 0,
            int missing = 0,
            int extra = 0)
        {
            int matched = exact + approximate;
            int expected = exact + approximate + missed;

            return new PronunciationScoreResult(
                true,
                overallScore,
                "steady",
                confidenceBand,
                "Summary",
                "Feedback",
                "Reason",
                "Basis",
                matched,
                expected,
                missing,
                extra,
                exact,
                approximate,
                missed,
                80,
                75,
                tempoScore,
                90,
                88,
                2f,
                System.Array.Empty<PronunciationWordScore>());
        }
    }
}
