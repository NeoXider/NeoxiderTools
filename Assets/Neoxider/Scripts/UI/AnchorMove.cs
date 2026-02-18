using UnityEngine;

namespace Neo.UI
{
    [NeoDoc("UI/AnchorMove.md")]
    [AddComponentMenu("Neo/" + "UI/" + nameof(AnchorMove))]
    public class AnchorMove : MonoBehaviour
    {
        [Header("Settings")] [Range(0, 1)] public float x = 0.5f;

        [Range(0, 1)] public float y = 0.5f;

        private RectTransform rect;

        private void OnValidate()
        {
            rect ??= transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);

            rect.anchoredPosition = Vector2.zero;
        }
    }
}