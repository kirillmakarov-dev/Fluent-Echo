using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluentEcho.Data
{
    [CreateAssetMenu(
        fileName = "SpeechExercise",
        menuName = "Fluent Echo/Speech Exercise")]
    public sealed class SpeechExerciseSO : ScriptableObject
    {
        [TextArea(2, 4)]
        [SerializeField] private string prompt = "Say the sentence in English.";

        [Tooltip("Each entry is one required word. Use | for alternatives, for example: twenty|20.")]
        [SerializeField] private string[] targetWords = { "the", "dog", "is", "big" };

        [Tooltip("Optional full-phrase alternatives. Use | between accepted phrases.")]
        [SerializeField] private string acceptedPhrases = "the dog is big";

        [Tooltip("Stable key used to persist lesson progress.")]
        [SerializeField] private string progressKey = "lesson_01_describe_the_dog";

        [SerializeField] private bool requireWordOrder = true;
        [SerializeField] private bool allowFuzzyMatch = true;
        [Min(0.5f)]
        [SerializeField] private float silenceTimeoutSeconds = 2.5f;
        [SerializeField] private AudioClip referenceAudio;

        public string Prompt => prompt;
        public bool RequireWordOrder => requireWordOrder;
        public bool AllowFuzzyMatch => allowFuzzyMatch;
        public float SilenceTimeoutSeconds => silenceTimeoutSeconds;
        public AudioClip ReferenceAudio => referenceAudio;
        public string ProgressKey => string.IsNullOrWhiteSpace(progressKey) ? name : progressKey;

        public string[] GetDisplayWords()
        {
            if (targetWords == null)
                return Array.Empty<string>();

            string[] result = new string[targetWords.Length];
            for (int i = 0; i < targetWords.Length; i++)
            {
                string[] alternatives = SplitAlternatives(targetWords[i]);
                result[i] = alternatives.Length > 0 ? alternatives[0] : string.Empty;
            }

            return result;
        }

        public string[][] GetAcceptedWordGroups()
        {
            if (targetWords == null)
                return Array.Empty<string[]>();

            string[][] result = new string[targetWords.Length][];
            for (int i = 0; i < targetWords.Length; i++)
                result[i] = SplitAlternatives(targetWords[i]);

            return result;
        }

        public string[] GetAcceptedPhrases() => SplitAlternatives(acceptedPhrases);

        public string GetRecognitionPrompt() => string.Join(" ", GetDisplayWords());

        public bool IsSingleWordExercise()
        {
            string[] words = GetDisplayWords();
            return words.Length == 1 && CountWords(words[0]) <= 1;
        }

        public bool IsShortUtteranceExercise()
        {
            string[] words = GetDisplayWords();
            return words.Length > 0 && words.Length <= 3;
        }

        public string GetPrimaryExpectedUtterance()
        {
            string[] phrases = GetAcceptedPhrases();
            if (phrases.Length > 0)
                return phrases[0];

            return GetRecognitionPrompt();
        }

        public string[] GetRecognitionCandidates()
        {
            var candidates = new List<string>();
            AppendDistinct(candidates, GetPrimaryExpectedUtterance());

            string[] phrases = GetAcceptedPhrases();
            for (int i = 0; i < phrases.Length; i++)
                AppendDistinct(candidates, phrases[i]);

            string[] displayWords = GetDisplayWords();
            for (int i = 0; i < displayWords.Length; i++)
                AppendDistinct(candidates, displayWords[i]);

            return candidates.ToArray();
        }

        private static string[] SplitAlternatives(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            string[] pieces = raw.Split('|');
            var cleaned = new System.Collections.Generic.List<string>(pieces.Length);
            for (int i = 0; i < pieces.Length; i++)
            {
                string value = pieces[i]?.Trim();
                if (!string.IsNullOrEmpty(value))
                    cleaned.Add(value);
            }

            return cleaned.ToArray();
        }

        private static void AppendDistinct(List<string> values, string raw)
        {
            string value = raw?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return;

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            values.Add(value);
        }

        private static int CountWords(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0;

            string[] parts = raw.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length;
        }
    }

}
