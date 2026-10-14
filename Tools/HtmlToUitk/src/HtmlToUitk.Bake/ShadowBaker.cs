using System;

namespace HtmlToUitk.Bake
{
    public sealed class ShadowSpec
    {
        public double BlurRadius;
        public double Spread;
        public double CornerRadius;
        public Rgba8 Color = new Rgba8(0, 0, 0, 255);
    }

    public sealed class ShadowSprite
    {
        public RgbaImage Image;
        public int Slice;
        public int Margin;
        public int EffectiveRadius;

        public int Width => Image.Width;
        public int Height => Image.Height;
        public int MinBoxWidth => 2 * (Slice - Margin);
        public int MinBoxHeight => 2 * (Slice - Margin);
    }

    public static class ShadowBaker
    {
        public static int MarginFor(double blurRadius)
        {
            return blurRadius <= 0 ? 0 : (int)Math.Ceiling(1.5 * blurRadius);
        }

        public static double EffectiveCornerRadius(double cornerRadius, double spread)
        {
            if (cornerRadius <= 0)
            {
                return 0;
            }

            double r = cornerRadius + spread;
            return r < 0 ? 0 : r;
        }

        public static ShadowSprite Bake(ShadowSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            double blur = spec.BlurRadius < 0 ? 0 : spec.BlurRadius;
            int margin = MarginFor(blur);
            double radius = EffectiveCornerRadius(spec.CornerRadius, spec.Spread);
            int radiusCeil = (int)Math.Ceiling(radius);
            int slice = Math.Max(1, 2 * margin + radiusCeil);
            int size = 2 * slice + 2;
            var coverage = new int[size * size];
            Rasterise(coverage, size, margin, radius);
            BoxBlur.Gaussian(coverage, size, size, blur / 2.0);
            var image = new RgbaImage(size, size);
            Rgba8 c = spec.Color;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cov = coverage[y * size + x];
                    if (cov < 0)
                    {
                        cov = 0;
                    }

                    if (cov > 65536)
                    {
                        cov = 65536;
                    }

                    int a = (cov * c.A + 32768) >> 16;
                    image.Set(x, y, new Rgba8(c.R, c.G, c.B, (byte)a));
                }
            }

            return new ShadowSprite { Image = image, Slice = slice, Margin = margin, EffectiveRadius = radiusCeil };
        }

        private static void Rasterise(int[] coverage, int size, int margin, double radius)
        {
            long x0 = 8L * margin;
            long x1 = 8L * (size - margin);
            long half = (x1 - x0) / 2;
            long r8 = (long)DMath.RoundToInt(radius * 8.0);
            if (r8 > half)
            {
                r8 = half;
            }

            long r2 = r8 * r8;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int count = 0;
                    for (int sy = 0; sy < 4; sy++)
                    {
                        long py = 8L * y + 2 * sy + 1;
                        for (int sx = 0; sx < 4; sx++)
                        {
                            long px = 8L * x + 2 * sx + 1;
                            if (Inside(px, py, x0, x1, r8, r2))
                            {
                                count++;
                            }
                        }
                    }

                    coverage[y * size + x] = count * 4096;
                }
            }
        }

        private static bool Inside(long px, long py, long lo, long hi, long r8, long r2)
        {
            if (px < lo || px > hi || py < lo || py > hi)
            {
                return false;
            }

            long cx;
            long cy;
            if (px < lo + r8)
            {
                cx = lo + r8;
            }
            else if (px > hi - r8)
            {
                cx = hi - r8;
            }
            else
            {
                return true;
            }

            if (py < lo + r8)
            {
                cy = lo + r8;
            }
            else if (py > hi - r8)
            {
                cy = hi - r8;
            }
            else
            {
                return true;
            }

            long dx = px - cx;
            long dy = py - cy;
            return dx * dx + dy * dy <= r2;
        }
    }
}
