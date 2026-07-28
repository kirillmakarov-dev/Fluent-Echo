using FluentEcho.Domain;
using NUnit.Framework;

namespace FluentEcho.Tests
{
    public sealed class SpeechSessionTests
    {
        [Test]
        public void CancellationPhase_IsReportedAsBusy()
        {
            SpeechSession session = new();

            session.BeginCancelling();

            Assert.That(session.Phase, Is.EqualTo(SpeechSessionPhase.Cancelling));
            Assert.That(session.IsBusy, Is.True);
            Assert.That(session.CanStartRecording, Is.False);
            Assert.That(session.IsCancelling, Is.True);
        }

        [Test]
        public void Reset_FromCancellation_ReturnsToIdleAndAllowsNewRecording()
        {
            SpeechSession session = new();

            session.BeginCancelling();
            session.Reset();

            Assert.That(session.Phase, Is.EqualTo(SpeechSessionPhase.Idle));
            Assert.That(session.IsBusy, Is.False);
            Assert.That(session.CanStartRecording, Is.True);
            Assert.That(session.IsCancelling, Is.False);
        }

        [Test]
        public void Reset_ReturnsToIdleAndClearsTranscript()
        {
            SpeechSession session = new();

            session.BeginListening();
            session.UpdateTranscript("hello", new SpeechMatchResult(false, new[] { true }));
            session.Reset();

            Assert.That(session.Phase, Is.EqualTo(SpeechSessionPhase.Idle));
            Assert.That(session.Transcript, Is.EqualTo(string.Empty));
            Assert.That(session.MatchResult.IsComplete, Is.False);
            Assert.That(session.IsBusy, Is.False);
        }
    }
}
