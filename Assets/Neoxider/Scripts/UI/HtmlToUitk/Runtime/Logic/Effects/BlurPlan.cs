using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Effects
{
    public readonly struct BlurLevel
    {
        public BlurLevel(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }
    }

    public static class BlurPlan
    {
        public const int MinSize = 4;
        public const int MaxIterations = 6;

        public static int IterationsForRadius(float radiusPx, int baseDownsample)
        {
            if (radiusPx <= 0f)
                return 0;
            float effective = radiusPx / Math.Max(1, baseDownsample);
            int iterations = 1 + (int)Math.Floor(Math.Log(Math.Max(1.0, effective), 2.0));
            return Math.Min(MaxIterations, Math.Max(1, iterations));
        }

        public static IReadOnlyList<BlurLevel> Levels(int width, int height, int baseDownsample, int iterations)
        {
            List<BlurLevel> levels = new List<BlurLevel>();
            int w = Math.Max(MinSize, width / Math.Max(1, baseDownsample));
            int h = Math.Max(MinSize, height / Math.Max(1, baseDownsample));
            levels.Add(new BlurLevel(w, h));
            for (int i = 0; i < iterations; i++)
            {
                w = Math.Max(MinSize, w / 2);
                h = Math.Max(MinSize, h / 2);
                levels.Add(new BlurLevel(w, h));
                if (w == MinSize && h == MinSize)
                    break;
            }

            return levels;
        }
    }
}
