using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     Small uGUI construction helpers so the survivor HUD is built in code with no imported
    ///     prefabs — rounded panels, bars, labels and buttons in a consistent v10 dark style.
    /// </summary>
    public static class SurvivorUI
    {
        public static readonly Color Ink = new Color(0.07f, 0.07f, 0.10f, 0.86f);
        public static readonly Color Panel = new Color(0.12f, 0.12f, 0.17f, 0.96f);
        public static readonly Color Track = new Color(1f, 1f, 1f, 0.10f);
        public static readonly Color Accent = new Color(0.49f, 0.36f, 0.94f);
        public static readonly Color Cyan = new Color(0.25f, 0.79f, 0.94f);
        public static readonly Color Good = new Color(0.26f, 0.85f, 0.64f);
        public static readonly Color Danger = new Color(1f, 0.36f, 0.48f);
        public static readonly Color Text = new Color(0.93f, 0.94f, 0.98f);
        public static readonly Color Muted = new Color(0.62f, 0.64f, 0.74f);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, bool rounded = true)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = SurvivorArt.RoundedRect;
                img.type = UnityEngine.UI.Image.Type.Sliced;
            }

            return img;
        }

        public static TMP_Text Label(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal)
        {
            RectTransform rt = Rect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        /// <summary>Anchors a rect to a corner/edge with a pixel offset and size.</summary>
        public static RectTransform Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            if (anchorMin.x != anchorMax.x)
            {
                rt.sizeDelta = new Vector2(0f, size.y);
            }
            else
            {
                rt.sizeDelta = size;
            }

            return rt;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>A horizontal bar: dark rounded track + a left-anchored fill. Returns the fill image.</summary>
        public static Image Bar(string name, Transform parent, Color fillColor, out RectTransform track)
        {
            Image trackImg = Image(name, parent, Track);
            track = (RectTransform)trackImg.transform;
            Image fill = Image("Fill", track, fillColor);
            RectTransform fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            return fill;
        }

        /// <summary>Sets a fill image built by <see cref="Bar" /> to a normalized [0..1] amount.</summary>
        public static void SetFill(Image fill, float amount)
        {
            if (fill == null)
            {
                return;
            }

            var rt = (RectTransform)fill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        }

        public static Button Button(string name, Transform parent, Color color)
        {
            Image img = Image(name, parent, color);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            cb.normalColor = Color.white;
            cb.colorMultiplier = 1f;
            btn.colors = cb;
            return btn;
        }
    }
}
