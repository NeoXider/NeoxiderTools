using System;

namespace Neo.HtmlToUitk.Navigation
{
    public readonly struct TransitionFrame
    {
        public TransitionFrame(float opacity, float translateXPercent, float translateYPercent, float scale)
        {
            Opacity = opacity;
            TranslateXPercent = translateXPercent;
            TranslateYPercent = translateYPercent;
            Scale = scale;
        }

        public float Opacity { get; }

        public float TranslateXPercent { get; }

        public float TranslateYPercent { get; }

        public float Scale { get; }

        public bool IsIdentity => Opacity >= 1f && Math.Abs(TranslateXPercent) < 1e-4f && Math.Abs(TranslateYPercent) < 1e-4f && Math.Abs(Scale - 1f) < 1e-4f;
    }

    public static class TransitionMath
    {
        public const float ScaleFrom = 0.92f;

        public static TransitionFrame Evaluate(TransitionKind kind, bool entering, TransitionDirection direction, float progress)
        {
            float p = progress < 0f ? 0f : progress > 1f ? 1f : progress;
            float visible = entering ? p : 1f - p;
            float away = 1f - visible;
            float sign = direction == TransitionDirection.Forward ? 1f : -1f;
            switch (kind)
            {
                case TransitionKind.Fade:
                    return new TransitionFrame(visible, 0f, 0f, 1f);
                case TransitionKind.Scale:
                    return new TransitionFrame(visible, 0f, 0f, ScaleFrom + (1f - ScaleFrom) * visible);
                case TransitionKind.SlideLeft:
                    return new TransitionFrame(1f, Slide(entering, away, sign), 0f, 1f);
                case TransitionKind.SlideRight:
                    return new TransitionFrame(1f, Slide(entering, away, -sign), 0f, 1f);
                case TransitionKind.SlideUp:
                    return new TransitionFrame(1f, 0f, Slide(entering, away, sign), 1f);
                default:
                    return new TransitionFrame(entering ? 1f : 0f, 0f, 0f, 1f);
            }
        }

        private static float Slide(bool entering, float away, float sign)
        {
            float distance = away * 100f;
            return entering ? sign * distance : -sign * distance;
        }
    }
}
