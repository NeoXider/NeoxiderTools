using System;

namespace HtmlToUitk.Bake
{
    internal static class BoxBlur
    {
        public static int[] BoxSizes(double sigma, int passes)
        {
            double ideal = Math.Sqrt(12.0 * sigma * sigma / passes + 1.0);
            int lower = (int)Math.Floor(ideal);
            if (lower % 2 == 0)
            {
                lower--;
            }

            if (lower < 1)
            {
                lower = 1;
            }

            int upper = lower + 2;
            double mIdeal = (12.0 * sigma * sigma - passes * lower * lower - 4.0 * passes * lower - 3.0 * passes) / (-4.0 * lower - 4.0);
            int m = DMath.RoundToInt(mIdeal);
            var sizes = new int[passes];
            for (int i = 0; i < passes; i++)
            {
                sizes[i] = i < m ? lower : upper;
            }

            return sizes;
        }

        public static void Gaussian(int[] data, int width, int height, double sigma)
        {
            if (sigma <= 0)
            {
                return;
            }

            int[] sizes = BoxSizes(sigma, 3);
            var scratch = new int[Math.Max(width, height)];
            for (int pass = 0; pass < sizes.Length; pass++)
            {
                for (int y = 0; y < height; y++)
                {
                    Line(data, y * width, 1, width, sizes[pass], scratch);
                }
            }

            for (int pass = 0; pass < sizes.Length; pass++)
            {
                for (int x = 0; x < width; x++)
                {
                    Line(data, x, width, height, sizes[pass], scratch);
                }
            }
        }

        private static void Line(int[] data, int start, int step, int length, int size, int[] scratch)
        {
            int radius = (size - 1) / 2;
            long sum = 0;
            for (int i = 0; i <= radius && i < length; i++)
            {
                sum += data[start + i * step];
            }

            for (int i = 0; i < length; i++)
            {
                scratch[i] = (int)((sum + size / 2) / size);
                int add = i + radius + 1;
                int remove = i - radius;
                if (add < length)
                {
                    sum += data[start + add * step];
                }

                if (remove >= 0)
                {
                    sum -= data[start + remove * step];
                }
            }

            for (int i = 0; i < length; i++)
            {
                data[start + i * step] = scratch[i];
            }
        }
    }
}
