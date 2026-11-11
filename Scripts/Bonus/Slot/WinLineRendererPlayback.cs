using System;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     Optional runtime playback of winning paylines on one or more <see cref="LineRenderer"/>s.
    /// </summary>
    [Serializable]
    public class WinLineRendererPlayback
    {
        public enum LayoutMode
        {
            /// <summary>All winning lines in sequence on the first non-null LineRenderer.</summary>
            SequentialSingle,

            /// <summary>
            ///     If the number of winning lines does not exceed the number of assigned LineRenderers — draw them in parallel;
            ///     otherwise fall back to sequential mode on the first renderer.
            /// </summary>
            ParallelWhenPossible
        }

        [Tooltip("Enable payline animation after a win.")]
        public bool enabled;

        [Tooltip("One or more LineRenderers on the scene / prefab.")]
        public LineRenderer[] renderers;

        [Tooltip(
            "SequentialSingle — always one renderer in turn; ParallelWhenPossible — multiple lines simultaneously when enough renderers are assigned.")]
        public LayoutMode layout = LayoutMode.ParallelWhenPossible;

        public enum WinLineColorStyle
        {
            /// <summary>Center — the color field; edges darker by RGB; alpha not reduced by code.</summary>
            AccentGlow,

            /// <summary>A single color along the whole line (RGBA from the inspector).</summary>
            SolidFlat,

            /// <summary>Linear gradient along the line length: start → end.</summary>
            LinearGradient,

            /// <summary>Custom Unity gradient in the inspector (by normalized length 0…1).</summary>
            CustomGradient
        }

        [Space]
        [Header("Colors")]
        [Tooltip(
            "AccentGlow — edges darker by RGB, alpha same as color; SolidFlat — a single color; LinearGradient — from colorLineStart to colorLineEnd; CustomGradient — the customLineGradient field (alpha only from the inspector).")]
        public WinLineColorStyle colorStyle = WinLineColorStyle.AccentGlow;

        [Tooltip(
            "Base color (RGBA from the inspector): AccentGlow / SolidFlat / highlight during travel; LinearGradient does not use this field in static mode.")]
        public Color color = new(1f, 0.88f, 0.18f, 1f);

        [Tooltip("Color at the first line point (LinearGradient mode).")]
        public Color colorLineStart = new(1f, 0.92f, 0.2f, 1f);

        [Tooltip("Color at the last line point (LinearGradient mode).")]
        public Color colorLineEnd = new(1f, 0.35f, 0.08f, 1f);

        [Tooltip("Custom gradient along the line length (only when colorStyle = CustomGradient).")]
        public Gradient customLineGradient = new();

        [Min(0.001f)] public float width = 0.055f;

        [Tooltip("Width pulsation (multiplier applied to the base width).")] [Min(0f)]
        public float widthPulseSpeed = 2.8f;

        [Tooltip("Minimum width multiplier during pulsation.")] [Range(0.35f, 2f)]
        public float widthPulseMinScale = 0.72f;

        [Tooltip("Maximum width multiplier during pulsation.")] [Range(0.35f, 2f)]
        public float widthPulseMaxScale = 1.28f;

        [Tooltip(
            "Speed of the bright segment moving along the line. 0 — no \"running\" effect (only width pulsation and static color mode).")]
        [Min(0f)]
        public float travelSpeed;

        [Space] [Header("Timing")] [Tooltip("Display duration for each winning line (sec).")] [Min(0.05f)]
        public float holdSeconds = 1.05f;

        [Tooltip("Pause between lines in sequential mode.")] [Min(0f)]
        public float stepGapSeconds = 0.12f;

        [Tooltip("Pause between full cycles (if looping is enabled).")] [Min(0f)]
        public float cycleGapSeconds = 0.4f;

        [Tooltip("Repeat until the next spin (stops on a new spin).")]
        public bool loopUntilNextSpin = true;

        [Tooltip("Additional offset of each line point in world coordinates (slightly \"in front of\" the symbols).")]
        public Vector3 worldOffset = new(0f, 0f, -0.025f);

        [Tooltip("Smoothing of the polyline curves.")] [Min(0)]
        public int cornerVertices = 4;

        // WHY: LineRenderer.colorGradient copies the gradient, so one scratch gradient and fixed key arrays serve
        // every renderer and frame; the travel animation used to allocate a Gradient plus two arrays per frame.
        [NonSerialized] private Gradient _scratchGradient;
        [NonSerialized] private GradientColorKey[] _travelColorKeys;
        [NonSerialized] private GradientAlphaKey[] _travelAlphaKeys;
        [NonSerialized] private GradientColorKey[] _twoColorKeys;
        [NonSerialized] private GradientAlphaKey[] _twoAlphaKeys;
        [NonSerialized] private GradientColorKey[] _threeColorKeys;
        [NonSerialized] private GradientAlphaKey[] _threeAlphaKeys;
        [NonSerialized] private bool _customGradientUsable;

        public bool IsActive
        {
            get
            {
                if (!enabled || renderers == null)
                {
                    return false;
                }

                foreach (LineRenderer r in renderers)
                {
                    if (r != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void ClearAll()
        {
            if (renderers == null)
            {
                return;
            }

            foreach (LineRenderer lr in renderers)
            {
                if (lr == null)
                {
                    continue;
                }

                lr.enabled = false;
                lr.positionCount = 0;
            }
        }

        internal LineRenderer FirstRenderer()
        {
            if (renderers == null)
            {
                return null;
            }

            foreach (LineRenderer lr in renderers)
            {
                if (lr != null)
                {
                    return lr;
                }
            }

            return null;
        }

        internal int CountAssignedRenderers()
        {
            if (renderers == null)
            {
                return 0;
            }

            int n = 0;
            foreach (LineRenderer lr in renderers)
            {
                if (lr != null)
                {
                    n++;
                }
            }

            return n;
        }

        internal LineRenderer RendererAt(int visualIndex)
        {
            if (renderers == null || visualIndex < 0)
            {
                return null;
            }

            int idx = -1;
            foreach (LineRenderer lr in renderers)
            {
                if (lr == null)
                {
                    continue;
                }

                idx++;
                if (idx == visualIndex)
                {
                    return lr;
                }
            }

            return null;
        }

        internal void ConfigurePolyline(LineRenderer lr, Vector3[] points)
        {
            if (lr == null || points == null || points.Length < 2)
            {
                return;
            }

            lr.loop = false;
            lr.useWorldSpace = true;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.numCornerVertices = Mathf.Max(0, cornerVertices);
            lr.numCapVertices = Mathf.Clamp(cornerVertices, 0, 8);
            lr.widthMultiplier = 1f;
            lr.startWidth = width * 0.85f;
            lr.endWidth = width * 1.05f;
            lr.enabled = true;

            // WHY: Gradient.colorKeys allocates, so the custom-gradient check runs once per configured line, not per frame.
            _customGradientUsable = IsCustomGradientUsable();

            if (travelSpeed <= 0f)
            {
                ApplyStaticColors(lr);
            }
        }

        internal void ApplyVisualFrame(LineRenderer lr, float animTime)
        {
            if (lr == null || !lr.enabled)
            {
                return;
            }

            float pulse =
                widthPulseSpeed <= 0f
                    ? 1f
                    : Mathf.Lerp(widthPulseMinScale, widthPulseMaxScale,
                        Mathf.Sin(animTime * widthPulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f);
            lr.widthMultiplier = pulse;

            if (travelSpeed <= 0f)
            {
                return;
            }

            float phase = Mathf.Repeat(animTime * travelSpeed, 1f);
            GetTravelSpotColors(phase, out Color bright, out Color dim);
            ApplyTravelSpotlightGradient(lr, phase, dim, bright);
        }

        private void ApplyStaticColors(LineRenderer lr)
        {
            switch (colorStyle)
            {
                case WinLineColorStyle.SolidFlat:
                    ApplyUniformColorGradient(lr, color);
                    break;

                case WinLineColorStyle.AccentGlow:
                    ApplyAccentGlowStatic(lr);
                    break;

                case WinLineColorStyle.LinearGradient:
                    ApplyLinearColorGradient(lr, colorLineStart, colorLineEnd);
                    break;

                case WinLineColorStyle.CustomGradient:
                    if (_customGradientUsable)
                    {
                        lr.colorGradient = customLineGradient;
                    }
                    else
                    {
                        ApplyAccentGlowStatic(lr);
                    }

                    break;
            }
        }

        private void GetTravelSpotColors(float phase, out Color bright, out Color dim)
        {
            switch (colorStyle)
            {
                case WinLineColorStyle.SolidFlat:
                case WinLineColorStyle.AccentGlow:
                    bright = color;
                    dim = DarkenRgbKeepAlpha(color, 0.48f);
                    break;

                case WinLineColorStyle.LinearGradient:
                    bright = Color.Lerp(colorLineStart, colorLineEnd, phase);
                    Color baseDim = Color.Lerp(colorLineStart, colorLineEnd, Mathf.Repeat(phase + 0.5f, 1f));
                    dim = DarkenRgbKeepAlpha(baseDim, 0.55f);
                    break;

                case WinLineColorStyle.CustomGradient:
                    if (!_customGradientUsable)
                    {
                        bright = color;
                        dim = DarkenRgbKeepAlpha(color, 0.48f);
                        break;
                    }

                    bright = customLineGradient.Evaluate(phase);
                    dim = customLineGradient.Evaluate(Mathf.Repeat(phase + 0.42f, 1f));
                    break;

                default:
                    bright = color;
                    dim = DarkenRgbKeepAlpha(color, 0.48f);
                    break;
            }
        }

        private void ApplyTravelSpotlightGradient(LineRenderer lr, float phase, Color dim, Color bright,
            float band = 0.14f)
        {
            _travelColorKeys ??= new GradientColorKey[5];
            _travelAlphaKeys ??= new GradientAlphaKey[5];

            float before = Mathf.Clamp01(phase - band);
            float center = Mathf.Clamp01(phase);
            float after = Mathf.Clamp01(phase + band);

            _travelColorKeys[0] = new GradientColorKey(dim, 0f);
            _travelColorKeys[1] = new GradientColorKey(dim, before);
            _travelColorKeys[2] = new GradientColorKey(bright, center);
            _travelColorKeys[3] = new GradientColorKey(dim, after);
            _travelColorKeys[4] = new GradientColorKey(dim, 1f);

            _travelAlphaKeys[0] = new GradientAlphaKey(dim.a, 0f);
            _travelAlphaKeys[1] = new GradientAlphaKey(dim.a, before);
            _travelAlphaKeys[2] = new GradientAlphaKey(bright.a, center);
            _travelAlphaKeys[3] = new GradientAlphaKey(dim.a, after);
            _travelAlphaKeys[4] = new GradientAlphaKey(dim.a, 1f);

            ApplyScratchGradient(lr, _travelColorKeys, _travelAlphaKeys);
        }

        private void ApplyUniformColorGradient(LineRenderer lr, Color c)
        {
            ApplyLinearColorGradient(lr, c, c);
        }

        private void ApplyAccentGlowStatic(LineRenderer lr)
        {
            Color edge = DarkenRgbKeepAlpha(color, 0.52f);
            _threeColorKeys ??= new GradientColorKey[3];
            _threeAlphaKeys ??= new GradientAlphaKey[3];
            _threeColorKeys[0] = new GradientColorKey(edge, 0f);
            _threeColorKeys[1] = new GradientColorKey(color, 0.5f);
            _threeColorKeys[2] = new GradientColorKey(edge, 1f);
            _threeAlphaKeys[0] = new GradientAlphaKey(edge.a, 0f);
            _threeAlphaKeys[1] = new GradientAlphaKey(color.a, 0.5f);
            _threeAlphaKeys[2] = new GradientAlphaKey(edge.a, 1f);
            ApplyScratchGradient(lr, _threeColorKeys, _threeAlphaKeys);
        }

        private void ApplyScratchGradient(LineRenderer lr, GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            _scratchGradient ??= new Gradient();
            _scratchGradient.SetKeys(colorKeys, alphaKeys);
            lr.colorGradient = _scratchGradient;
        }

        /// <summary>RGB darkening only; alpha same as the original color (code does not reduce opacity).</summary>
        private static Color DarkenRgbKeepAlpha(Color c, float rgbMultiplier)
        {
            Color d = c;
            d.r = Mathf.Clamp01(c.r * rgbMultiplier);
            d.g = Mathf.Clamp01(c.g * rgbMultiplier);
            d.b = Mathf.Clamp01(c.b * rgbMultiplier);
            return d;
        }

        private void ApplyLinearColorGradient(LineRenderer lr, Color lineStart, Color lineEnd)
        {
            _twoColorKeys ??= new GradientColorKey[2];
            _twoAlphaKeys ??= new GradientAlphaKey[2];
            _twoColorKeys[0] = new GradientColorKey(lineStart, 0f);
            _twoColorKeys[1] = new GradientColorKey(lineEnd, 1f);
            _twoAlphaKeys[0] = new GradientAlphaKey(lineStart.a, 0f);
            _twoAlphaKeys[1] = new GradientAlphaKey(lineEnd.a, 1f);
            ApplyScratchGradient(lr, _twoColorKeys, _twoAlphaKeys);
        }

        private bool IsCustomGradientUsable()
        {
            if (customLineGradient == null)
            {
                return false;
            }

            GradientColorKey[] keys = customLineGradient.colorKeys;
            return keys != null && keys.Length >= 2;
        }
    }
}
