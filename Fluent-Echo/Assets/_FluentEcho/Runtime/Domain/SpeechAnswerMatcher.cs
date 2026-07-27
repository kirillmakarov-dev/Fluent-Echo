using System;
using System.Collections.Generic;

namespace FluentEcho.Domain
{
    public sealed class SpeechAnswerMatcher
    {
        public SpeechMatchResult Match(
            string transcript,
            string[][] targetWordGroups,
            string[] acceptedPhrases,
            bool requireWordOrder,
            bool allowFuzzyMatch)
        {
            List<string> spokenWords = Tokenize(transcript);
            int targetCount = targetWordGroups?.Length ?? 0;
            if (targetCount == 0)
                return new SpeechMatchResult(true, Array.Empty<bool>());

            if (ContainsAcceptedPhrase(spokenWords, acceptedPhrases))
                return new SpeechMatchResult(true, CreateFilledResult(targetCount));

            bool[] matched = requireWordOrder
                ? MatchInOrder(spokenWords, targetWordGroups, allowFuzzyMatch)
                : MatchInAnyOrder(spokenWords, targetWordGroups, allowFuzzyMatch);

            bool isComplete = true;
            for (int i = 0; i < matched.Length; i++)
                isComplete &= matched[i];

            return new SpeechMatchResult(isComplete, matched);
        }

        public static string NormalizeToken(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return string.Empty;

            word = word.Trim().ToLowerInvariant();
            int start = 0;
            int end = word.Length - 1;

            while (start <= end && ShouldStrip(word[start]))
                start++;
            while (end >= start && ShouldStrip(word[end]))
                end--;

            return start <= end ? word.Substring(start, end - start + 1) : string.Empty;
        }

        private static bool[] MatchInOrder(
            IReadOnlyList<string> spoken,
            IReadOnlyList<string[]> targets,
            bool fuzzy)
        {
            bool[] matched = new bool[targets.Count];
            int targetCursor = 0;

            for (int spokenIndex = 0; spokenIndex < spoken.Count; spokenIndex++)
            {
                for (int targetIndex = targetCursor; targetIndex < targets.Count; targetIndex++)
                {
                    if (!MatchesAny(spoken[spokenIndex], targets[targetIndex], fuzzy))
                        continue;

                    matched[targetIndex] = true;
                    targetCursor = targetIndex + 1;
                    break;
                }
            }

            return matched;
        }

        private static bool[] MatchInAnyOrder(
            IReadOnlyList<string> spoken,
            IReadOnlyList<string[]> targets,
            bool fuzzy)
        {
            bool[] matched = new bool[targets.Count];
            bool[] used = new bool[spoken.Count];

            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                for (int spokenIndex = 0; spokenIndex < spoken.Count; spokenIndex++)
                {
                    if (used[spokenIndex] || !MatchesAny(spoken[spokenIndex], targets[targetIndex], fuzzy))
                        continue;

                    matched[targetIndex] = true;
                    used[spokenIndex] = true;
                    break;
                }
            }

            return matched;
        }

        private static bool ContainsAcceptedPhrase(
            IReadOnlyList<string> spoken,
            IReadOnlyList<string> phrases)
        {
            if (phrases == null)
                return false;

            for (int phraseIndex = 0; phraseIndex < phrases.Count; phraseIndex++)
            {
                List<string> phrase = Tokenize(phrases[phraseIndex]);
                if (phrase.Count == 0 || phrase.Count > spoken.Count)
                    continue;

                for (int start = 0; start <= spoken.Count - phrase.Count; start++)
                {
                    bool matches = true;
                    for (int i = 0; i < phrase.Count; i++)
                        matches &= spoken[start + i] == phrase[i];

                    if (matches)
                        return true;
                }
            }

            return false;
        }

        private static bool MatchesAny(string spoken, IReadOnlyList<string> alternatives, bool fuzzy)
        {
            if (alternatives == null || alternatives.Count == 0)
                return true;

            for (int i = 0; i < alternatives.Count; i++)
            {
                string target = NormalizeToken(alternatives[i]);
                if (spoken == target)
                    return true;

                if (fuzzy && target.Length <= 4 && LevenshteinDistance(spoken, target) <= 1)
                    return true;
            }

            return false;
        }

        private static List<string> Tokenize(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            string[] pieces = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pieces.Length; i++)
            {
                string token = NormalizeToken(pieces[i]);
                if (!string.IsNullOrEmpty(token))
                    result.Add(token);
            }

            return result;
        }

        private static bool[] CreateFilledResult(int count)
        {
            bool[] result = new bool[count];
            for (int i = 0; i < result.Length; i++)
                result[i] = true;
            return result;
        }

        private static bool ShouldStrip(char value) =>
            char.IsPunctuation(value) || char.IsSymbol(value);

        private static int LevenshteinDistance(string a, string b)
        {
            if (a.Length == 0)
                return b.Length;
            if (b.Length == 0)
                return a.Length;

            int[] costs = new int[b.Length + 1];
            for (int j = 0; j < costs.Length; j++)
                costs[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                costs[0] = i;
                int diagonal = i - 1;

                for (int j = 1; j <= b.Length; j++)
                {
                    int previous = costs[j];
                    int substitution = a[i - 1] == b[j - 1] ? 0 : 1;
                    costs[j] = Math.Min(
                        Math.Min(costs[j] + 1, costs[j - 1] + 1),
                        diagonal + substitution);
                    diagonal = previous;
                }
            }

            return costs[b.Length];
        }
    }
}
