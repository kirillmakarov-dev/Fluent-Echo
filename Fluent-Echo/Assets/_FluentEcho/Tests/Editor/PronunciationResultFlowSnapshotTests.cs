using FluentEcho.Domain;
using FluentEcho.Presentation;
using FluentEcho.Services;
using NUnit.Framework;

namespace FluentEcho.Tests
{
    public sealed class PronunciationResultFlowSnapshotTests
    {
        [Test]
        public void Create_WithProgress_RendersCurrentAttemptAndAcceptedFlow()
        {
            PronunciationScoreResult score = CreateScore(
                overallScore: 91,
                confidenceBand: "high",
                exact: 3,
                approximate: 1,
                missed: 0);

            LessonProgressState progress = new();
            progress.Configure("lesson_01", 4);
            progress.RecordAttempt(
                "the dog is big",
                new SpeechMatchResult(true, new[] { true, true, true, true }),
                4,
                91,
                "strong",
                "Summary",
                "high",
                88,
                "Good coverage.",
                3,
                1,
                0);

            PronunciationResultFlowSnapshot snapshot = PronunciationResultFlowSnapshot.Create(
                score,
                "The dog is big.",
                "Alignment preview (demo only):",
                progress,
                "History: 1 attempt",
                "Choose NEXT MISSION to continue.");

            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("Current attempt:"));
            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("Lesson progress:"));
            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("History: 1 attempt"));
            Assert.That(snapshot.CurrentAttemptLines, Contains.Item("Focus next:"));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item(FluentEchoCopy.ResultAcceptedHeader));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item(FluentEchoCopy.ResultLessonRecapHeader));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item(FluentEchoCopy.ResultNextStepHeader));
            Assert.That(snapshot.AcceptedResultLines, Contains.Item("Choose NEXT MISSION to continue."));
        }

        [Test]
        public void Create_WithUnavailableScore_ReturnsEmptyFlow()
        {
            PronunciationResultFlowSnapshot snapshot = PronunciationResultFlowSnapshot.Create(
                PronunciationScoreResult.Unavailable,
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                string.Empty);

            Assert.That(snapshot.CurrentAttemptLines, Is.Empty);
            Assert.That(snapshot.AcceptedResultLines, Is.Empty);
        }

        private static PronunciationScoreResult CreateScore(
            int overallScore = 80,
            string confidenceBand = "medium",
            int exact = 4,
            int approximate = 0,
            int missed = 0)
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
                70,
                90,
                88,
                2f,
                System.Array.Empty<PronunciationWordScore>());
        }
    }
}
