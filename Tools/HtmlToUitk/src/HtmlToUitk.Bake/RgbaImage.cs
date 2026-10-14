using System;

namespace HtmlToUitk.Bake
{
    public readonly struct Rgba8 : IEquatable<Rgba8>
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public Rgba8(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public bool Equals(Rgba8 other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object obj) => obj is Rgba8 other && Equals(other);
        public override int GetHashCode() => (R << 24) ^ (G << 16) ^ (B << 8) ^ A;
        public override string ToString() => "rgba(" + R + "," + G + "," + B + "," + A + ")";
    }

    public sealed class RgbaImage
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; }

        public RgbaImage(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "image size must be positive");
            }

            Width = width;
            Height = height;
            Pixels = new byte[checked(width * height * 4)];
        }

        public RgbaImage(int width, int height, byte[] pixels)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "image size must be positive");
            }

            if (pixels == null || pixels.Length != width * height * 4)
            {
                throw new ArgumentException("pixel buffer does not match the image size", nameof(pixels));
            }

            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public Rgba8 Get(int x, int y)
        {
            int i = (y * Width + x) * 4;
            return new Rgba8(Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
        }

        public void Set(int x, int y, Rgba8 color)
        {
            int i = (y * Width + x) * 4;
            Pixels[i] = color.R;
            Pixels[i + 1] = color.G;
            Pixels[i + 2] = color.B;
            Pixels[i + 3] = color.A;
        }

        public bool SameAs(RgbaImage other)
        {
            if (other == null || other.Width != Width || other.Height != Height)
            {
                return false;
            }

            for (int i = 0; i < Pixels.Length; i++)
            {
                if (Pixels[i] != other.Pixels[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
