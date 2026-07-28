using FluentEcho.Data;
using FluentEcho.Domain;
using FluentEcho.Presentation;
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
            Assert.That(result.Source, Is.EqualTo(PhonemeAlignmentSource.Unavailable));
            Assert.That(result.DisplayTitle, Is.EqualTo("Alignment unavailable:"));
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
            Assert.That(result.Source, Is.EqualTo(PhonemeAlignmentSource.Unavailable));
            Assert.That(result.DisplayTitle, Is.EqualTo("Alignment unavailable:"));
            Assert.That(result.AlignmentScore, Is.EqualTo(0));
            Assert.That(result.SummaryText, Does.Contain("not active yet"));
            Assert.That(result.FeedbackText, Does.Contain("roadmap item"));
            Assert.That(result.HasEvidence, Is.False);
        }

        [Test]
        public void Factory_ReturnsPreviewServiceWhenEnabled()
        {
            IPhonemeAlignmentService service = PhonemeAlignmentServiceFactory.Create(true);

            Assert.That(service, Is.TypeOf<InspectorPhonemeAlignmentService>());
        }

        [Test]
        public void Factory_ReturnsNoOpServiceWhenDisabled()
        {
            IPhonemeAlignmentService service = PhonemeAlignmentServiceFactory.Create(false);

            Assert.That(service, Is.SameAs(NoOpPhonemeAlignmentService.Instance));
        }

        [Test]
        public void PreviewService_ReturnsInspectableAlignmentResult()
        {
            var service = new InspectorPhonemeAlignmentService();
            PhonemeAlignmentRequest request = new(
                (SpeechExerciseSO) null,
                "apple",
                new SpeechMatchResult(true, new[] { true }),
                2f,
                new[] { "apple" },
                new[] { "apple" });

            PhonemeAlignmentResult result = service.Align(request);

            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.Source, Is.EqualTo(PhonemeAlignmentSource.Preview));
            Assert.That(result.DisplayTitle, Is.EqualTo("Alignment preview (demo only):"));
            Assert.That(result.AlignmentScore, Is.GreaterThan(0));
            Assert.That(result.SummaryText, Does.Contain("preview"));
            Assert.That(result.EvidenceText, Does.Contain("Preview mode"));
            Assert.That(result.FeedbackText, Does.Contain("Preview only"));
            Assert.That(result.HasEvidence, Is.True);
        }

        [Test]
        public void PreviewFormatter_ReturnsCompactInspectableBlock()
        {
            PhonemeAlignmentResult alignment = new(
                PhonemeAlignmentSource.Preview,
                true,
                82,
                "high",
                "Preview summary",
                "Preview only: keep going.",
                "Preview mode: 4/5 target words matched from the transcript.",
                System.Array.Empty<string>(),
                System.Array.Empty<string>(),
                System.Array.Empty<string>(),
                1.75f);

            string text = PhonemeAlignmentTextFormatter.BuildPreviewText(alignment);

            Assert.That(text, Does.Contain("Alignment preview (demo only):"));
            Assert.That(text, Does.Contain("Preview score: 82/100 | High"));
            Assert.That(text, Does.Contain("Preview mode: 4/5 target words matched from the transcript."));
            Assert.That(text, Does.Contain("Preview only: keep going."));
        }
    }
}
