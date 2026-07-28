using FluentEcho.Domain;
using FluentEcho.Presentation;
using FluentEcho.Services;
using NUnit.Framework;

namespace FluentEcho.Tests
{
    public sealed class LessonProgressStateTests
    {
        [Test]
        public void EmptyHistory_ReturnsFriendlyMessage()
        {
            LessonProgressState state = LessonProgressRepository.Load("lesson_test_empty_history", 4);

            try
            {
                Assert.That(state.GetSummaryText(), Does.Contain("ready for first recording"));
                Assert.That(state.GetHistoryText(), Does.Contain(FluentEchoCopy.FirstLocalEstimateText));
            }
            finally
            {
                LessonProgressRepository.Clear("lesson_test_empty_history");
            }
        }

        [Test]
        public void History_IsTrimmedToLatestFiveAttempts()
        {
            LessonProgressState state = LessonProgressRepository.Load("lesson_test_history_limit", 4);

            try
            {
                for (int i = 0; i < 6; i++)
                {
                    state.RecordAttempt(
                        $"attempt {i}",
                        new SpeechMatchResult(i == 5, new[] { true, true, true, i == 5 }),
                        4,
                        60 + i,
                        i >= 5 ? "high" : "medium",
                        $"PRACTICE SCORE | {60 + i}/100 | {(i >= 5 ? "HIGH" : "MEDIUM")}");
                }

                Assert.That(state.AttemptHistory, Has.Count.EqualTo(5));
                Assert.That(state.AttemptHistory[0].Transcript, Is.EqualTo("attempt 5"));
                Assert.That(state.AttemptHistory[4].Transcript, Is.EqualTo("attempt 1"));
                Assert.That(state.GetHistoryText(10), Does.Contain("attempt 5"));
                Assert.That(state.GetHistoryText(10), Does.Not.Contain("attempt 0"));
                Assert.That(state.GetSummaryText(), Does.Contain("estimate 65/100"));
                Assert.That(state.GetSummaryText(), Does.Contain("cleared 1"));
            }
            finally
            {
                LessonProgressRepository.Clear("lesson_test_history_limit");
            }
        }
    }
}
