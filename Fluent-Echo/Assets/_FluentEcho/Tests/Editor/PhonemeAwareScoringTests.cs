using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Services;
using NUnit.Framework;

namespace FluentEcho.Tests
{
    public sealed class PhonemeAwareScoringTests
    {
        [Test]
        public void UnavailableResult_StaysRoadmapOnlyAndEmpty()
        {
            PhonemeAlignmentResult result = PhonemeAlignmentResult.Unavailable;

            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.AlignmentScore, Is.EqualTo(0));
            Assert.That(result.ConfidenceBand, Is.EqualTo("unavailable"));
            Assert.That(result.SummaryText, Does.Contain("not active yet"));
            Assert.That(result.FeedbackText, Does.Contain("roadmap item"));
            Assert.That(result.HasEvidence, Is.False);
            Assert.That(result.MatchedPhonemes, Is.Empty);
            Assert.That(result.MissingPhonemes, Is.Empty);
            Assert.That(result.WeakPhonemes, Is.Empty);
        }

        [Test]
        public void Request_NormalizesNullValuesAndClampsDuration()
        {
            SpeechMatchResult matchResult = new(false, new[] { true, false });
            PhonemeAlignmentRequest request = new(
                (SpeechExerciseSO) null,
                null,
                matchResult,
                -3.5f,
                null,
                null);

            Assert.That(request.Exercise, Is.Null);
            Assert.That(request.Transcript, Is.Empty);
            Assert.That(request.MatchResult, Is.SameAs(matchResult));
            Assert.That(request.RecordingSeconds, Is.EqualTo(0f));
            Assert.That(request.TargetWords, Is.Empty);
            Assert.That(request.AcceptedPhrases, Is.Empty);
            Assert.That(request.HasTranscript, Is.False);
        }

        [Test]
        public void NoOpService_ReturnsUnavailableRoadmapResult()
        {
            var service = NoOpPhonemeAlignmentService.Instance;
            PhonemeAlignmentRequest request = new(
                (SpeechExerciseSO) null,
                "apple",
                new SpeechMatchResult(true, new[] { true }),
                1.5f,
                new[] { "apple" },
                new[] { "apple" });

            PhonemeAlignmentResult result = service.Align(request);

            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.AlignmentScore, Is.EqualTo(0));
            Assert.That(result.SummaryText, Does.Contain("not active yet"));
            Assert.That(result.FeedbackText, Does.Contain("roadmap item"));
            Assert.That(result.HasEvidence, Is.False);
        }
    }
}
