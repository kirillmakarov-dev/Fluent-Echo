using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluentEcho.Views
{
    public sealed class WordChipView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI label;

        private static readonly Color PendingColor = new(0.12f, 0.18f, 0.22f, 1f);
        private static readonly Color MatchedColor = new(0.14f, 0.74f, 0.52f, 1f);

        public void Configure(string word)
        {
            label.text = word;
            SetMatched(false);
        }

        public void SetMatched(bool matched)
        {
            background.color = matched ? MatchedColor : PendingColor;
            label.color = matched ? Color.white : new Color(0.72f, 0.78f, 0.8f, 1f);
        }
    }
}
