using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    ///     Pins a RectTransform to one normalized anchor point (<see cref="x" />, <see cref="y" />) with a zero
    ///     anchored position. The Inspector applies it on edit; code and UnityEvents re-anchor through
    ///     <see cref="Apply" />, <see cref="SetX" />, <see cref="SetY" /> and <see cref="SetAnchor" />.
    /// </summary>
    [NeoDoc("UI/AnchorMove.md")]
    [CreateFromMenu("Neoxider/UI/AnchorMove")]
    [AddComponentMenu("Neoxider/" + "UI/" + nameof(AnchorMove))]
    public class AnchorMove : MonoBehaviour
    {
        [Header("Settings")] [Range(0, 1)] public float x = 0.5f;

        [Range(0, 1)] public float y = 0.5f;

        private RectTransform rect;

        private void OnValidate()
        {
            Apply();
        }

        /// <summary>Writes the current <see cref="x" />/<see cref="y" /> anchor and zeroes the anchored position.</summary>
        public void Apply()
        {
            if (rect == null)
            {
                rect = transform as RectTransform;
            }

            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);

            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Sets the horizontal anchor (clamped to 0-1) and applies it.</summary>
        public void SetX(float value)
        {
            x = Mathf.Clamp01(value);
            Apply();
        }

        /// <summary>Sets the vertical anchor (clamped to 0-1) and applies it.</summary>
        public void SetY(float value)
        {
            y = Mathf.Clamp01(value);
            Apply();
        }

        /// <summary>Sets both anchors (each clamped to 0-1) and applies them.</summary>
        public void SetAnchor(Vector2 anchor)
        {
            x = Mathf.Clamp01(anchor.x);
            y = Mathf.Clamp01(anchor.y);
            Apply();
        }
    }
}
