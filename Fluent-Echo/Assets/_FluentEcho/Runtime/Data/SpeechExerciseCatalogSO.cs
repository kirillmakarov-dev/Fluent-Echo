using System;
using UnityEngine;

namespace FluentEcho.Data
{
    [CreateAssetMenu(
        fileName = "SpeechExerciseCatalog",
        menuName = "Fluent Echo/Speech Exercise Catalog")]
    public sealed class SpeechExerciseCatalogSO : ScriptableObject
    {
        [SerializeField] private SpeechExerciseSO[] exercises = Array.Empty<SpeechExerciseSO>();
        [SerializeField] private SpeechExerciseCategory[] categories = Array.Empty<SpeechExerciseCategory>();

        public int Count => exercises == null ? 0 : exercises.Length;
        public int CategoryCount => categories == null ? 0 : categories.Length;

        public SpeechExerciseSO GetExercise(int index)
        {
            if (Count == 0)
                return null;

            index = Mathf.Clamp(index, 0, Count - 1);
            return exercises[index];
        }

        public SpeechExerciseCategory GetCategory(int index)
        {
            if (CategoryCount == 0)
                return null;

            index = Mathf.Clamp(index, 0, CategoryCount - 1);
            return categories[index];
        }

        public int GetCategoryExerciseCount(int categoryIndex)
        {
            SpeechExerciseCategory category = GetCategory(categoryIndex);
            return category != null ? category.Count : Count;
        }

        public SpeechExerciseSO GetCategoryExercise(int categoryIndex, int exerciseIndex)
        {
            SpeechExerciseCategory category = GetCategory(categoryIndex);
            if (category != null && category.Count > 0)
                return category.GetExercise(exerciseIndex);

            return GetExercise(exerciseIndex);
        }

        public string[] GetCategoryDisplayNames()
        {
            if (CategoryCount == 0)
                return Array.Empty<string>();

            string[] names = new string[CategoryCount];
            for (int i = 0; i < CategoryCount; i++)
            {
                SpeechExerciseCategory category = categories[i];
                names[i] = category != null && !string.IsNullOrWhiteSpace(category.DisplayName)
                    ? category.DisplayName
                    : $"Category {i + 1}";
            }

            return names;
        }

        public string[] GetCategoryExerciseDisplayNames(int categoryIndex)
        {
            SpeechExerciseCategory category = GetCategory(categoryIndex);
            if (category != null && category.Count > 0)
                return category.GetDisplayNames();

            return GetDisplayNames();
        }

        public string[] GetDisplayNames()
        {
            if (Count == 0)
                return Array.Empty<string>();

            string[] names = new string[Count];
            for (int i = 0; i < Count; i++)
            {
                SpeechExerciseSO exercise = exercises[i];
                names[i] = exercise != null && !string.IsNullOrWhiteSpace(exercise.name)
                    ? exercise.name
                    : $"Lesson {i + 1}";
            }

            return names;
        }
    }

    [Serializable]
    public sealed class SpeechExerciseCategory
    {
        [SerializeField] private string displayName = "Words";
        [SerializeField] private string description = "Warm up with clear single words.";
        [SerializeField] private SpeechExerciseSO[] exercises = Array.Empty<SpeechExerciseSO>();

        public string DisplayName => displayName;
        public string Description => description;
        public int Count => exercises == null ? 0 : exercises.Length;

        public SpeechExerciseSO GetExercise(int index)
        {
            if (Count == 0)
                return null;

            index = Mathf.Clamp(index, 0, Count - 1);
            return exercises[index];
        }

        public string[] GetDisplayNames()
        {
            if (Count == 0)
                return Array.Empty<string>();

            string[] names = new string[Count];
            for (int i = 0; i < Count; i++)
            {
                SpeechExerciseSO exercise = exercises[i];
                names[i] = exercise != null && !string.IsNullOrWhiteSpace(exercise.name)
                    ? exercise.name
                    : $"Lesson {i + 1}";
            }

            return names;
        }
    }
}
