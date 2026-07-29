using FluentEcho.Presentation;
using FluentEcho.Services;
using NUnit.Framework;

namespace FluentEcho.Tests
{
    public sealed class PronunciationScoreNarrativeSnapshotTests
    {
        [Test]
        public void CurrentAttemptLines_IncludeSnapshotDetails()
        {
            PronunciationScoreResult score = CreateScore(
                exact: 3,
                approximate: 1,
                missed: 1,
                confidenceBand: "high");

            PronunciationScoreNarrativeSnapshot snapshot = PronunciationScoreNarrativeSnapshot.Create(
                score,
                string.Empty,
                "Alignment preview (demo only):");

            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("Current attempt:"));
            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("Exact words: 3 | Approximate words: 1 | Missed words: 1"));
            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("Alignment preview (demo only):"));
            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("Focus next:"));
            Assert.That(snapshot.CurrentAttemptLines, Does.Contain("Feedback"));
        }

        [Test]
        public void AcceptedResultLines_IncludeCoachStyleResultSections()
        {
            PronunciationScoreResult score = CreateScore(
                exact: 4,
                approximate: 0,
                missed: 0,
                confidenceBand: "high",
                overallScore: 95);

            PronunciationScoreNarrativeSnapshot snapshot = PronunciationScoreNarrativeSnapshot.Create(
                score,
                "The dog is big.",
                "Alignment preview (demo only):");

            Assert.That(snapshot.AcceptedResultLines, Contains.Item(FluentEchoCopy.ResultAcceptedHeader));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item("\"The dog is big.\""));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item(FluentEchoCopy.ResultEstimateHeader));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item(FluentEchoCopy.ResultSignalBreakdownHeader));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item("Coverage: 4/4"));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item("Focus next:"));
            Assert.That(snapshot.AcceptedResultLines, Does.Contain("Feedback"));
        }

        [Test]
        public void Create_WithUnavailableScore_ReturnsEmptyLines()
        {
            PronunciationScoreNarrativeSnapshot snapshot = PronunciationScoreNarrativeSnapshot.Create(
                PronunciationScoreResult.Unavailable,
                "anything",
                "alignment");

            Assert.That(snapshot.CurrentAttemptLines, Is.Empty);
            Assert.That(snapshot.AcceptedResultLines, Is.Empty);
        }

        private static PronunciationScoreResult CreateScore(
            int overallScore = 80,
            string confidenceBand = "medium",
            int exact = 4,
            int approximate = 0,
            int missed = 0,
            int tempoScore = 70)
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
                missed,
                0,
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
