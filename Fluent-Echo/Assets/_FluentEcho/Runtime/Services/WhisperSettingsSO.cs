using System;
using System.Linq;
using UnityEngine;

namespace FluentEcho.Services
{
    public enum WhisperQualityProfile
    {
        Fast,
        Balanced,
        Accurate
    }

    [CreateAssetMenu(
        fileName = "WhisperSettings",
        menuName = "Fluent Echo/Whisper Settings")]
    public sealed class WhisperSettingsSO : ScriptableObject
    {
        [Header("Profile")]
        [SerializeField] private WhisperQualityProfile qualityProfile = WhisperQualityProfile.Fast;
        [SerializeField] private string fastModelPath = "Whisper/ggml-tiny.en.bin";
        [SerializeField] private string balancedModelPath = "Whisper/ggml-base.en.bin";
        [SerializeField] private string accurateModelPath = "Whisper/ggml-small.en.bin";

        [Header("Whisper")]
        [SerializeField] private string language = "en";
        [SerializeField] private bool useGpu = true;
        [SerializeField] private bool flashAttention = true;
        [SerializeField, Min(0.1f)] private float streamingStepSeconds = 1.25f;
        [SerializeField, Min(0.05f)] private float keepSeconds = 0.2f;
        [SerializeField, Min(0.1f)] private float streamLengthSeconds = 10f;
        [SerializeField] private bool updatePrompt = true;
        [SerializeField] private bool dropOldBuffer;
        [SerializeField] private bool useVad = true;

        [Header("Microphone")]
        [SerializeField, Min(0.05f)] private float microphoneChunkSeconds = 0.25f;
        [SerializeField, Min(0f)] private float warmupSeconds = 1.25f;
        [SerializeField] private bool warmUpOnPrepare = true;
        [SerializeField] private string qualityProfilePrefsKey = "FluentEcho.WhisperQualityProfile";

        public WhisperQualityProfile QualityProfile => qualityProfile;
        public string Language => language;
        public bool UseGpu => useGpu;
        public bool FlashAttention => flashAttention;
        public float StreamingStepSeconds => streamingStepSeconds;
        public float KeepSeconds => keepSeconds;
        public float StreamLengthSeconds => streamLengthSeconds;
        public bool UpdatePrompt => updatePrompt;
        public bool DropOldBuffer => dropOldBuffer;
        public bool UseVad => useVad;
        public float MicrophoneChunkSeconds => microphoneChunkSeconds;
        public float WarmupSeconds => warmupSeconds;
        public bool WarmUpOnPrepare => warmUpOnPrepare;
        public string QualityProfilePrefsKey => qualityProfilePrefsKey;

        public WhisperQualityProfile GetEffectiveProfile()
        {
            if (PlayerPrefs.HasKey(qualityProfilePrefsKey))
                return (WhisperQualityProfile) PlayerPrefs.GetInt(qualityProfilePrefsKey, (int) qualityProfile);

            return qualityProfile;
        }

        public void SetRuntimeProfile(WhisperQualityProfile profile)
        {
            qualityProfile = profile;
            PlayerPrefs.SetInt(qualityProfilePrefsKey, (int) profile);
            PlayerPrefs.Save();
        }

        public string ResolveModelPath() => GetEffectiveProfile() switch
        {
            WhisperQualityProfile.Balanced => balancedModelPath,
            WhisperQualityProfile.Accurate => accurateModelPath,
            _ => fastModelPath
        };

        public bool TryResolveModelPath(out string modelPath)
        {
            modelPath = ResolveModelPath();
            return !string.IsNullOrWhiteSpace(modelPath);
        }
    }
}

namespace FluentEcho.Domain
{
    [Serializable]
    public sealed class LessonProgressState
    {
        [SerializeField] private string lessonKey;
        [SerializeField] private int totalWords;
        [SerializeField] private int attempts;
        [SerializeField] private int successfulAttempts;
        [SerializeField] private int bestMatchedWords;
        [SerializeField] private string bestTranscript = string.Empty;
        [SerializeField] private string lastTranscript = string.Empty;
        [SerializeField] private string lastUpdatedUtc = string.Empty;

        public string LessonKey => lessonKey;
        public int TotalWords => totalWords;
        public int Attempts => attempts;
        public int SuccessfulAttempts => successfulAttempts;
        public int BestMatchedWords => bestMatchedWords;
        public string BestTranscript => bestTranscript;
        public string LastTranscript => lastTranscript;
        public string LastUpdatedUtc => lastUpdatedUtc;

        public void Configure(string key, int expectedTotalWords)
        {
            lessonKey = key ?? string.Empty;
            totalWords = Math.Max(0, expectedTotalWords);
            bestMatchedWords = Math.Min(bestMatchedWords, totalWords);
        }

        public void RecordAttempt(string transcript, FluentEcho.Domain.SpeechMatchResult result, int expectedTotalWords)
        {
            Configure(lessonKey, expectedTotalWords);

            attempts++;
            lastTranscript = transcript?.Trim() ?? string.Empty;
            int matchedWords = CountMatchedWords(result.MatchedWords);

            if (matchedWords >= bestMatchedWords)
            {
                bestMatchedWords = matchedWords;
                bestTranscript = lastTranscript;
            }

            if (result.IsComplete)
                successfulAttempts++;

            lastUpdatedUtc = DateTime.UtcNow.ToString("O");
        }

        public string GetSummaryText()
        {
            if (totalWords <= 0)
                return $"PROGRESS | {attempts} attempts";

            string completion = $"{Math.Min(bestMatchedWords, totalWords)}/{totalWords}";
            string attemptsText = attempts == 0 ? "no attempts yet" : $"{attempts} attempts";
            string successText = successfulAttempts > 0 ? $" | cleared {successfulAttempts}" : string.Empty;
            return $"PROGRESS | best {completion} | {attemptsText}{successText}";
        }

        private static int CountMatchedWords(bool[] matches)
        {
            if (matches == null || matches.Length == 0)
                return 0;

            return matches.Count(match => match);
        }
    }
}

namespace FluentEcho.Services
{
    public static class LessonProgressRepository
    {
        private const string Prefix = "FluentEcho.LessonProgress.";

        public static FluentEcho.Domain.LessonProgressState Load(string lessonKey, int expectedTotalWords)
        {
            FluentEcho.Domain.LessonProgressState state = new();
            state.Configure(lessonKey, expectedTotalWords);

            if (string.IsNullOrWhiteSpace(lessonKey))
                return state;

            string raw = PlayerPrefs.GetString(BuildPrefsKey(lessonKey), string.Empty);
            if (!string.IsNullOrWhiteSpace(raw))
                JsonUtility.FromJsonOverwrite(raw, state);

            state.Configure(lessonKey, expectedTotalWords);
            return state;
        }

        public static void Save(FluentEcho.Domain.LessonProgressState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.LessonKey))
                return;

            PlayerPrefs.SetString(BuildPrefsKey(state.LessonKey), JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public static void Clear(string lessonKey)
        {
            if (string.IsNullOrWhiteSpace(lessonKey))
                return;

            PlayerPrefs.DeleteKey(BuildPrefsKey(lessonKey));
            PlayerPrefs.Save();
        }

        private static string BuildPrefsKey(string lessonKey) => Prefix + lessonKey;
    }
}
