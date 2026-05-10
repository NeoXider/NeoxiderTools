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
            /// <summary>Все выигрышные линии подряд на первом не-null LineRenderer.</summary>
            SequentialSingle,

            /// <summary>
            ///     Если выигрышных линий не больше числа назначенных LineRenderer — рисуем их параллельно;
            ///     иначе откат к последовательному режиму на первом рендерере.
            /// </summary>
            ParallelWhenPossible,
        }

        [Tooltip("Включить анимацию линий выплат после выигрыша.")]
        public bool enabled;

        [Tooltip("Один или несколько LineRenderer на сцене / префабе.")]
        public LineRenderer[] renderers;

        [Tooltip("SequentialSingle — всегда один рендерер по очереди; ParallelWhenPossible — несколько линий одновременно при достаточном числе рендереров.")]
        public LayoutMode layout = LayoutMode.ParallelWhenPossible;

        public enum WinLineColorStyle
        {
            /// <summary>Центр — поле color; края темнее по RGB; альфа не ослабляется кодом.</summary>
            AccentGlow,

            /// <summary>Один цвет color по всей линии (RGBA из инспектора).</summary>
            SolidFlat,

            /// <summary>Линейный градиент по длине линии: начало → конец.</summary>
            LinearGradient,

            /// <summary>Свой градиент Unity в инспекторе (по нормализованной длине 0…1).</summary>
            CustomGradient,
        }

        [Space]
        [Header("Colors")]
        [Tooltip("AccentGlow — края темнее по RGB, альфа как у color; SolidFlat — один color; LinearGradient — от colorLineStart к colorLineEnd; CustomGradient — поле customLineGradient (альфа только из инспектора).")]
        public WinLineColorStyle colorStyle = WinLineColorStyle.AccentGlow;

        [Tooltip("Базовый цвет (RGBA из инспектора): AccentGlow / SolidFlat / блик при travel; LinearGradient в статике не использует это поле.")]
        public Color color = new(1f, 0.88f, 0.18f, 1f);

        [Tooltip("Цвет у первой точки линии (режим LinearGradient).")]
        public Color colorLineStart = new(1f, 0.92f, 0.2f, 1f);

        [Tooltip("Цвет у последней точки линии (режим LinearGradient).")]
        public Color colorLineEnd = new(1f, 0.35f, 0.08f, 1f);

        [Tooltip("Пользовательский градиент по длине линии (только при colorStyle = CustomGradient).")]
        public Gradient customLineGradient = new();

        [Min(0.001f)]
        public float width = 0.055f;

        [Tooltip("Пульсация толщины (мультипликатор к базовой ширине).")]
        [Min(0f)]
        public float widthPulseSpeed = 2.8f;

        [Tooltip("Минимальный множитель ширины при пульсации.")]
        [Range(0.35f, 2f)]
        public float widthPulseMinScale = 0.72f;

        [Tooltip("Максимальный множитель ширины при пульсации.")]
        [Range(0.35f, 2f)]
        public float widthPulseMaxScale = 1.28f;

        [Tooltip("Скорость движения яркого участка вдоль линии. 0 — без «бегущего» эффекта (только пульсация ширины и статичный режим цвета).")]
        [Min(0f)]
        public float travelSpeed;

        [Space]
        [Header("Timing")]
        [Tooltip("Длительность показа каждой выигрышной линии (сек).")]
        [Min(0.05f)]
        public float holdSeconds = 1.05f;

        [Tooltip("Пауза между линиями в последовательном режиме.")]
        [Min(0f)]
        public float stepGapSeconds = 0.12f;

        [Tooltip("Пауза между полными циклами (если зацикливание включено).")]
        [Min(0f)]
        public float cycleGapSeconds = 0.4f;

        [Tooltip("Повторять до следующего спина (останавливается при новом спине).")]
        public bool loopUntilNextSpin = true;

        [Tooltip("Дополнительный сдвиг каждой точки линии в мировых координатах (чуть «перед» символами).")]
        public Vector3 worldOffset = new(0f, 0f, -0.025f);

        [Tooltip("Сглаживание изгибов полилинии.")]
        [Min(0)]
        public int cornerVertices = 4;

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
                    if (IsCustomGradientUsable())
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
                    if (!IsCustomGradientUsable())
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

        private static void ApplyTravelSpotlightGradient(LineRenderer lr, float phase, Color dim, Color bright,
            float band = 0.14f)
        {
            Gradient travelGrad = new Gradient();
            travelGrad.SetKeys(
                new[]
                {
                    new GradientColorKey(dim, 0f),
                    new GradientColorKey(dim, Mathf.Clamp01(phase - band)),
                    new GradientColorKey(bright, Mathf.Clamp01(phase)),
                    new GradientColorKey(dim, Mathf.Clamp01(phase + band)),
                    new GradientColorKey(dim, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(dim.a, 0f),
                    new GradientAlphaKey(dim.a, Mathf.Clamp01(phase - band)),
                    new GradientAlphaKey(bright.a, Mathf.Clamp01(phase)),
                    new GradientAlphaKey(dim.a, Mathf.Clamp01(phase + band)),
                    new GradientAlphaKey(dim.a, 1f),
                });
            lr.colorGradient = travelGrad;
        }

        private static void ApplyUniformColorGradient(LineRenderer lr, Color c)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
            lr.colorGradient = g;
        }

        private void ApplyAccentGlowStatic(LineRenderer lr)
        {
            Color edge = DarkenRgbKeepAlpha(color, 0.52f);
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(edge, 0f),
                    new GradientColorKey(color, 0.5f),
                    new GradientColorKey(edge, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(edge.a, 0f),
                    new GradientAlphaKey(color.a, 0.5f),
                    new GradientAlphaKey(edge.a, 1f),
                });
            lr.colorGradient = g;
        }

        /// <summary>Только затемнение RGB; альфа как у исходного цвета (код не ослабляет прозрачность).</summary>
        private static Color DarkenRgbKeepAlpha(Color c, float rgbMultiplier)
        {
            Color d = c;
            d.r = Mathf.Clamp01(c.r * rgbMultiplier);
            d.g = Mathf.Clamp01(c.g * rgbMultiplier);
            d.b = Mathf.Clamp01(c.b * rgbMultiplier);
            return d;
        }

        private static void ApplyLinearColorGradient(LineRenderer lr, Color lineStart, Color lineEnd)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(lineStart, 0f), new GradientColorKey(lineEnd, 1f) },
                new[] { new GradientAlphaKey(lineStart.a, 0f), new GradientAlphaKey(lineEnd.a, 1f) });
            lr.colorGradient = g;
        }

        private bool IsCustomGradientUsable()
        {
            GradientColorKey[] keys = customLineGradient.colorKeys;
            return keys != null && keys.Length >= 2;
        }
    }
}
