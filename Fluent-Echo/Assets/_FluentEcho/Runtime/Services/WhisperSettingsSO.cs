using System;
using System.Collections.Generic;
using System.Linq;
using FluentEcho.Presentation;
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
    public sealed class LessonAttemptRecord
    {
        [SerializeField] private string timestampUtc = string.Empty;
        [SerializeField] private string transcript = string.Empty;
        [SerializeField] private int matchedWords;
        [SerializeField] private int expectedWords;
        [SerializeField] private bool isComplete;
        [SerializeField] private int pronunciationScore;
        [SerializeField] private string pronunciationBand = string.Empty;
        [SerializeField] private string pronunciationSummary = string.Empty;
        [SerializeField] private string pronunciationConfidenceBand = string.Empty;
        [SerializeField] private int pronunciationConfidenceScore;

        public string TimestampUtc => timestampUtc;
        public string Transcript => transcript;
        public int MatchedWords => matchedWords;
        public int ExpectedWords => expectedWords;
        public bool IsComplete => isComplete;
        public int PronunciationScore => pronunciationScore;
        public string PronunciationBand => pronunciationBand;
        public string PronunciationSummary => pronunciationSummary;
        public string PronunciationConfidenceBand => pronunciationConfidenceBand;
        public int PronunciationConfidenceScore => pronunciationConfidenceScore;

        public void Configure(
            string utcTimestamp,
            string transcriptValue,
            int matchedWordCount,
            int expectedWordCount,
            bool complete,
            int score,
            string band,
            string summary,
            string confidenceBand = "",
            int confidenceScore = 0)
        {
            timestampUtc = utcTimestamp ?? string.Empty;
            transcript = transcriptValue ?? string.Empty;
            matchedWords = Math.Max(0, matchedWordCount);
            expectedWords = Math.Max(0, expectedWordCount);
            isComplete = complete;
            pronunciationScore = Mathf.Clamp(score, 0, 100);
            pronunciationBand = band ?? string.Empty;
            pronunciationSummary = summary ?? string.Empty;
            pronunciationConfidenceBand = confidenceBand ?? string.Empty;
            pronunciationConfidenceScore = Mathf.Clamp(confidenceScore, 0, 100);
        }
    }

    [Serializable]
    public sealed class LessonProgressState
    {
        private const int HistoryLimit = 5;

        [SerializeField] private string lessonKey;
        [SerializeField] private int totalWords;
        [SerializeField] private int attempts;
        [SerializeField] private int successfulAttempts;
        [SerializeField] private int bestMatchedWords;
        [SerializeField] private int bestPronunciationScore;
        [SerializeField] private string bestTranscript = string.Empty;
        [SerializeField] private string bestPronunciationBand = string.Empty;
        [SerializeField] private string bestPronunciationSummary = string.Empty;
        [SerializeField] private string bestPronunciationConfidenceBand = string.Empty;
        [SerializeField] private int bestPronunciationConfidenceScore;
        [SerializeField] private string lastTranscript = string.Empty;
        [SerializeField] private int lastPronunciationScore;
        [SerializeField] private string lastPronunciationBand = string.Empty;
        [SerializeField] private string lastPronunciationSummary = string.Empty;
        [SerializeField] private string lastPronunciationConfidenceBand = string.Empty;
        [SerializeField] private int lastPronunciationConfidenceScore;
        [SerializeField] private string lastUpdatedUtc = string.Empty;
        [SerializeField] private List<LessonAttemptRecord> attemptHistory = new();

        public string LessonKey => lessonKey;
        public int TotalWords => totalWords;
        public int Attempts => attempts;
        public int SuccessfulAttempts => successfulAttempts;
        public int BestMatchedWords => bestMatchedWords;
        public int BestPronunciationScore => bestPronunciationScore;
        public string BestTranscript => bestTranscript;
        public string BestPronunciationBand => bestPronunciationBand;
        public string BestPronunciationSummary => bestPronunciationSummary;
        public string BestPronunciationConfidenceBand => bestPronunciationConfidenceBand;
        public int BestPronunciationConfidenceScore => bestPronunciationConfidenceScore;
        public string LastTranscript => lastTranscript;
        public int LastPronunciationScore => lastPronunciationScore;
        public string LastPronunciationBand => lastPronunciationBand;
        public string LastPronunciationSummary => lastPronunciationSummary;
        public string LastPronunciationConfidenceBand => lastPronunciationConfidenceBand;
        public int LastPronunciationConfidenceScore => lastPronunciationConfidenceScore;
        public string LastUpdatedUtc => lastUpdatedUtc;
        public IReadOnlyList<LessonAttemptRecord> AttemptHistory => attemptHistory;

        public void Configure(string key, int expectedTotalWords)
        {
            lessonKey = key ?? string.Empty;
            totalWords = Math.Max(0, expectedTotalWords);
            bestMatchedWords = Math.Min(bestMatchedWords, totalWords);
        }

        public void RecordAttempt(
            string transcript,
            FluentEcho.Domain.SpeechMatchResult result,
            int expectedTotalWords,
            int pronunciationScore = 0,
            string pronunciationBand = "",
            string pronunciationSummary = "",
            string confidenceBand = "",
            int confidenceScore = 0)
        {
            Configure(lessonKey, expectedTotalWords);

            attempts++;
            lastTranscript = transcript?.Trim() ?? string.Empty;
            int matchedWords = CountMatchedWords(result.MatchedWords);
            lastPronunciationScore = Mathf.Clamp(pronunciationScore, 0, 100);
            lastPronunciationBand = pronunciationBand ?? string.Empty;
            lastPronunciationSummary = pronunciationSummary ?? string.Empty;
            lastPronunciationConfidenceBand = confidenceBand ?? string.Empty;
            lastPronunciationConfidenceScore = Mathf.Clamp(confidenceScore, 0, 100);

            if (matchedWords > bestMatchedWords
                || (matchedWords == bestMatchedWords && lastPronunciationScore >= bestPronunciationScore))
            {
                bestMatchedWords = matchedWords;
                bestTranscript = lastTranscript;
                bestPronunciationScore = lastPronunciationScore;
                bestPronunciationBand = lastPronunciationBand;
                bestPronunciationSummary = lastPronunciationSummary;
                bestPronunciationConfidenceBand = lastPronunciationConfidenceBand;
                bestPronunciationConfidenceScore = lastPronunciationConfidenceScore;
            }

            if (result.IsComplete)
                successfulAttempts++;

            AddAttemptHistory(
                transcript,
                matchedWords,
                expectedTotalWords,
                result.IsComplete,
                pronunciationScore,
                pronunciationBand,
                pronunciationSummary,
                confidenceBand,
                confidenceScore);
            lastUpdatedUtc = DateTime.UtcNow.ToString("O");
        }

        public string GetSummaryText()
        {
            if (totalWords <= 0)
            {
                if (attempts <= 0)
                    return FluentEchoCopy.FirstProgressSummary;

                return $"Progress: {attempts} attempts";
            }

            if (attempts <= 0 && bestPronunciationScore <= 0)
                return FluentEchoCopy.BuildProgressSummary(totalWords);

            string completion = $"{Math.Min(bestMatchedWords, totalWords)}/{totalWords}";
            string score = bestPronunciationScore > 0 ? $" | estimate {bestPronunciationScore}/100" : string.Empty;
            string confidence = !string.IsNullOrWhiteSpace(bestPronunciationConfidenceBand)
                ? $" | confidence {bestPronunciationConfidenceBand}"
                : string.Empty;
            string attemptsText = $"{attempts} attempts";
            string successText = successfulAttempts > 0 ? $" | cleared {successfulAttempts}" : string.Empty;
            return $"Progress: best {completion}{score}{confidence} | {attemptsText}{successText}";
        }

        public string GetHistoryText(int maxEntries = 3)
        {
            if (attemptHistory == null || attemptHistory.Count == 0)
            {
                string emptyHeader = BuildHistoryHeader();
                return string.IsNullOrWhiteSpace(emptyHeader)
                    ? FluentEchoCopy.FirstLocalEstimateText
                    : $"{emptyHeader}\n{FluentEchoCopy.FirstLocalEstimateText}";
            }

            int limit = Math.Max(1, maxEntries);
            int count = Math.Min(limit, attemptHistory.Count);
            var lines = new List<string>(count + 1);
            string header = BuildHistoryHeader();
            if (!string.IsNullOrWhiteSpace(header))
                lines.Add(header);

            for (int i = 0; i < count; i++)
            {
                LessonAttemptRecord record = attemptHistory[i];
                string prefix = !string.IsNullOrWhiteSpace(record.PronunciationSummary)
                    ? record.PronunciationSummary
                    : record.PronunciationScore > 0
                        ? $"{record.PronunciationScore}/100"
                        : $"{record.MatchedWords}/{Math.Max(1, record.ExpectedWords)}";
                string confidence = !string.IsNullOrWhiteSpace(record.PronunciationConfidenceBand)
                    ? $" | confidence {record.PronunciationConfidenceBand}"
                    : string.Empty;
                string status = record.IsComplete ? "cleared" : "needs retry";
                string transcriptPreview = Truncate(record.Transcript, 34);
                lines.Add($"- {prefix}{confidence} | {status} | {transcriptPreview}");
            }

            return string.Join("\n", lines);
        }

        private static int CountMatchedWords(bool[] matches)
        {
            if (matches == null || matches.Length == 0)
                return 0;

            return matches.Count(match => match);
        }

        private void AddAttemptHistory(
            string transcript,
            int matchedWords,
            int expectedTotalWords,
            bool complete,
            int pronunciationScore,
            string pronunciationBand,
            string pronunciationSummary,
            string confidenceBand = "",
            int confidenceScore = 0)
        {
            if (attemptHistory == null)
                attemptHistory = new List<LessonAttemptRecord>();

            LessonAttemptRecord record = new();
            record.Configure(
                DateTime.UtcNow.ToString("O"),
                transcript?.Trim() ?? string.Empty,
                matchedWords,
                expectedTotalWords,
                complete,
                pronunciationScore,
                pronunciationBand,
                pronunciationSummary,
                confidenceBand,
                confidenceScore);
            attemptHistory.Insert(0, record);

            while (attemptHistory.Count > HistoryLimit)
                attemptHistory.RemoveAt(attemptHistory.Count - 1);
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "(empty)";

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, Math.Max(0, maxLength - 3)) + "...";
        }

        private string BuildHistoryHeader()
        {
            if (attempts <= 0 && bestPronunciationScore <= 0)
                return string.Empty;

            var parts = new List<string>();

            string bestText = bestPronunciationScore > 0
                ? $"Best score: {bestPronunciationScore}/100 {bestPronunciationBand}".Trim()
                : $"Best match: {bestMatchedWords}/{Math.Max(1, totalWords)}";
            parts.Add(bestText);

            if (!string.IsNullOrWhiteSpace(bestTranscript))
                parts.Add($"Best transcript: {bestTranscript}");

            if (!string.IsNullOrWhiteSpace(bestPronunciationConfidenceBand))
                parts.Add($"Best confidence: {bestPronunciationConfidenceBand}");

            string lastText = !string.IsNullOrWhiteSpace(lastPronunciationSummary)
                ? $"Last attempt: {lastPronunciationSummary}"
                : lastPronunciationScore > 0
                    ? $"Last attempt: {lastPronunciationScore}/100 {lastPronunciationBand}".Trim()
                    : (string.IsNullOrWhiteSpace(lastTranscript) ? "Last attempt pending" : $"Last attempt: {lastTranscript}");
            parts.Add(lastText);

            if (!string.IsNullOrWhiteSpace(lastPronunciationConfidenceBand))
                parts.Add($"Last confidence: {lastPronunciationConfidenceBand}");

            return string.Join(" | ", parts);
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
