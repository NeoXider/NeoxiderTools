using System;

namespace HtmlToUitk.Bake
{
    public static class BlurBaker
    {
        public static RgbaImage Gaussian(RgbaImage source, double sigma)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int w = source.Width;
            int h = source.Height;
            int n = w * h;
            var r = new int[n];
            var g = new int[n];
            var b = new int[n];
            var a = new int[n];
            for (int i = 0; i < n; i++)
            {
                int alpha = source.Pixels[i * 4 + 3];
                r[i] = source.Pixels[i * 4] * alpha;
                g[i] = source.Pixels[i * 4 + 1] * alpha;
                b[i] = source.Pixels[i * 4 + 2] * alpha;
                a[i] = alpha * 255;
            }

            BoxBlur.Gaussian(r, w, h, sigma);
            BoxBlur.Gaussian(g, w, h, sigma);
            BoxBlur.Gaussian(b, w, h, sigma);
            BoxBlur.Gaussian(a, w, h, sigma);
            var result = new RgbaImage(w, h);
            for (int i = 0; i < n; i++)
            {
                int pa = a[i];
                if (pa <= 0)
                {
                    continue;
                }

                result.Pixels[i * 4] = Clamp((r[i] * 255 + pa / 2) / pa);
                result.Pixels[i * 4 + 1] = Clamp((g[i] * 255 + pa / 2) / pa);
                result.Pixels[i * 4 + 2] = Clamp((b[i] * 255 + pa / 2) / pa);
                result.Pixels[i * 4 + 3] = Clamp((pa + 127) / 255);
            }

            return result;
        }

        private static byte Clamp(int value)
        {
            return (byte)(value < 0 ? 0 : value > 255 ? 255 : value);
        }
    }
}
