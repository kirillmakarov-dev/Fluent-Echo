using FluentEcho.Domain;
using NUnit.Framework;

namespace FluentEcho.Tests
{
    public sealed class SpeechAnswerMatcherTests
    {
        private readonly SpeechAnswerMatcher matcher = new();
        private readonly string[][] targets =
        {
            new[] { "the" },
            new[] { "dog" },
            new[] { "is" },
            new[] { "big", "large" }
        };

        [Test]
        public void ExactSentence_CompletesExercise()
        {
            SpeechMatchResult result = matcher.Match(
                "The dog is big.",
                targets,
                new[] { "the dog is big" },
                true,
                true);

            Assert.That(result.IsComplete, Is.True);
        }

        [Test]
        public void AlternativeWord_CompletesExercise()
        {
            SpeechMatchResult result = matcher.Match(
                "the dog is large",
                targets,
                new[] { "the dog is big", "the dog is large" },
                true,
                true);

            Assert.That(result.IsComplete, Is.True);
        }

        [Test]
        public void MissingWord_ReturnsPartialHighlights()
        {
            SpeechMatchResult result = matcher.Match(
                "the dog big",
                targets,
                System.Array.Empty<string>(),
                true,
                false);

            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.MatchedWords, Is.EqualTo(new[] { true, true, false, true }));
        }

        [Test]
        public void WrongOrder_FailsWhenOrderIsRequired()
        {
            SpeechMatchResult result = matcher.Match(
                "big dog is the",
                targets,
                System.Array.Empty<string>(),
                true,
                false);

            Assert.That(result.IsComplete, Is.False);
        }

        [Test]
        public void ShortWhisperTypo_IsAcceptedWhenFuzzyEnabled()
        {
            SpeechMatchResult result = matcher.Match(
                "the dog is bog",
                targets,
                System.Array.Empty<string>(),
                true,
                true);

            Assert.That(result.IsComplete, Is.True);
        }
    }
}
