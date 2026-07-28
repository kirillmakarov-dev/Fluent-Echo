using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluentEcho.Views
{
    public sealed class WordChipView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI label;

        [Header("Visual States")]
        [SerializeField] private Color pendingBackground = new(0.09f, 0.22f, 0.25f, 1f);
        [SerializeField] private Color matchedBackground = new(0.27f, 0.87f, 0.68f, 1f);
        [SerializeField] private Color pendingText = new(0.72f, 0.78f, 0.80f, 1f);
        [SerializeField] private Color matchedText = new(0.06f, 0.13f, 0.15f, 1f);

        public void Configure(string word)
        {
            label.text = word;
            SetMatched(false);
        }

        public void SetMatched(bool matched)
        {
            background.color = matched ? matchedBackground : pendingBackground;
            label.color = matched ? matchedText : pendingText;
        }
    }
}
