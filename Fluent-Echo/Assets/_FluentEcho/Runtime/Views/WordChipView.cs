using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluentEcho.Views
{
    public sealed class WordChipView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private LayoutElement layoutElement;

        [Header("Visual States")]
        [SerializeField] private Color pendingBackground = new(0.09f, 0.22f, 0.25f, 1f);
        [SerializeField] private Color matchedBackground = new(0.27f, 0.87f, 0.68f, 1f);
        [SerializeField] private Color pendingText = new(0.72f, 0.78f, 0.80f, 1f);
        [SerializeField] private Color matchedText = new(0.06f, 0.13f, 0.15f, 1f);

        private RectTransform RectTransform => (RectTransform) transform;

        private void Awake()
        {
            if (layoutElement == null)
                layoutElement = GetComponent<LayoutElement>();
        }

        public void Configure(string word)
        {
            label.text = word;
            SetMatched(false);
        }

        public float GetPreferredWidth(float minWidth, float horizontalPadding, float maxWidth)
        {
            if (label == null)
                return minWidth;

            label.ForceMeshUpdate();
            float preferred = label.GetPreferredValues(label.text).x + horizontalPadding;
            return Mathf.Clamp(preferred, minWidth, maxWidth);
        }

        public void SetSize(float width, float height)
        {
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = width;
                layoutElement.preferredHeight = height;
            }

            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public void SetMatched(bool matched)
        {
            background.color = matched ? matchedBackground : pendingBackground;
            label.color = matched ? matchedText : pendingText;
        }
    }
}
