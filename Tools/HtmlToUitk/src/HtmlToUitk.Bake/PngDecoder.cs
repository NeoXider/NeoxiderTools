using System;
using System.IO;
using System.IO.Compression;

namespace HtmlToUitk.Bake
{
    public static class PngDecoder
    {
        public static RgbaImage Decode(byte[] png)
        {
            if (png == null || png.Length < 33)
            {
                throw new InvalidDataException("not a PNG file");
            }

            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < 8; i++)
            {
                if (png[i] != signature[i])
                {
                    throw new InvalidDataException("bad PNG signature");
                }
            }

            int pos = 8;
            int width = 0;
            int height = 0;
            int depth = 0;
            int colorType = 0;
            int interlace = 0;
            byte[] palette = null;
            byte[] transparency = null;
            var idat = new MemoryStream();
            while (pos + 8 <= png.Length)
            {
                int length = (int)ReadBigEndian(png, pos);
                string type = new string(new[] { (char)png[pos + 4], (char)png[pos + 5], (char)png[pos + 6], (char)png[pos + 7] });
                int dataStart = pos + 8;
                if (dataStart + length + 4 > png.Length)
                {
                    throw new InvalidDataException("truncated PNG chunk " + type);
                }

                if (type == "IHDR")
                {
                    width = (int)ReadBigEndian(png, dataStart);
                    height = (int)ReadBigEndian(png, dataStart + 4);
                    depth = png[dataStart + 8];
                    colorType = png[dataStart + 9];
                    interlace = png[dataStart + 12];
                }
                else if (type == "PLTE")
                {
                    palette = new byte[length];
                    Buffer.BlockCopy(png, dataStart, palette, 0, length);
                }
                else if (type == "tRNS")
                {
                    transparency = new byte[length];
                    Buffer.BlockCopy(png, dataStart, transparency, 0, length);
                }
                else if (type == "IDAT")
                {
                    idat.Write(png, dataStart, length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                pos = dataStart + length + 4;
            }

            if (width <= 0 || height <= 0)
            {
                throw new InvalidDataException("PNG has no IHDR");
            }

            if (depth != 8 || interlace != 0)
            {
                throw new NotSupportedException("only 8-bit non-interlaced PNG is supported");
            }

            int channels;
            switch (colorType)
            {
                case 0: channels = 1; break;
                case 2: channels = 3; break;
                case 3: channels = 1; break;
                case 4: channels = 2; break;
                case 6: channels = 4; break;
                default: throw new NotSupportedException("unsupported PNG colour type " + colorType);
            }

            byte[] zlib = idat.ToArray();
            if (zlib.Length < 6)
            {
                throw new InvalidDataException("PNG has no image data");
            }

            int stride = width * channels;
            var raw = new byte[height * (stride + 1)];
            using (var input = new MemoryStream(zlib, 2, zlib.Length - 2))
            using (var inflate = new DeflateStream(input, CompressionMode.Decompress))
            {
                int read = 0;
                while (read < raw.Length)
                {
                    int got = inflate.Read(raw, read, raw.Length - read);
                    if (got <= 0)
                    {
                        throw new InvalidDataException("PNG image data is too short");
                    }

                    read += got;
                }
            }

            var pixels = new byte[height * stride];
            for (int y = 0; y < height; y++)
            {
                int filter = raw[y * (stride + 1)];
                int src = y * (stride + 1) + 1;
                int dst = y * stride;
                for (int x = 0; x < stride; x++)
                {
                    int left = x >= channels ? pixels[dst + x - channels] : 0;
                    int up = y > 0 ? pixels[dst - stride + x] : 0;
                    int upLeft = x >= channels && y > 0 ? pixels[dst - stride + x - channels] : 0;
                    int value = raw[src + x];
                    switch (filter)
                    {
                        case 0: break;
                        case 1: value += left; break;
                        case 2: value += up; break;
                        case 3: value += (left + up) / 2; break;
                        case 4: value += Paeth(left, up, upLeft); break;
                        default: throw new InvalidDataException("bad PNG filter " + filter);
                    }

                    pixels[dst + x] = (byte)value;
                }
            }

            var image = new RgbaImage(width, height);
            for (int i = 0; i < width * height; i++)
            {
                int o = i * 4;
                switch (colorType)
                {
                    case 0:
                        image.Pixels[o] = image.Pixels[o + 1] = image.Pixels[o + 2] = pixels[i];
                        image.Pixels[o + 3] = 255;
                        break;
                    case 2:
                        image.Pixels[o] = pixels[i * 3];
                        image.Pixels[o + 1] = pixels[i * 3 + 1];
                        image.Pixels[o + 2] = pixels[i * 3 + 2];
                        image.Pixels[o + 3] = 255;
                        break;
                    case 3:
                        int index = pixels[i];
                        if (palette == null || index * 3 + 2 >= palette.Length)
                        {
                            throw new InvalidDataException("PNG palette index out of range");
                        }

                        image.Pixels[o] = palette[index * 3];
                        image.Pixels[o + 1] = palette[index * 3 + 1];
                        image.Pixels[o + 2] = palette[index * 3 + 2];
                        image.Pixels[o + 3] = transparency != null && index < transparency.Length ? transparency[index] : (byte)255;
                        break;
                    case 4:
                        image.Pixels[o] = image.Pixels[o + 1] = image.Pixels[o + 2] = pixels[i * 2];
                        image.Pixels[o + 3] = pixels[i * 2 + 1];
                        break;
                    default:
                        image.Pixels[o] = pixels[i * 4];
                        image.Pixels[o + 1] = pixels[i * 4 + 1];
                        image.Pixels[o + 2] = pixels[i * 4 + 2];
                        image.Pixels[o + 3] = pixels[i * 4 + 3];
                        break;
                }
            }

            return image;
        }

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc)
            {
                return a;
            }

            return pb <= pc ? b : c;
        }

        private static uint ReadBigEndian(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24) | ((uint)buffer[offset + 1] << 16) | ((uint)buffer[offset + 2] << 8) | buffer[offset + 3];
        }
    }
}
