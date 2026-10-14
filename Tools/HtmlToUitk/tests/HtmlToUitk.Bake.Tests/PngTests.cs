using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace HtmlToUitk.Bake.Tests
{
    public class PngTests
    {
        private static RgbaImage Pattern(int w, int h, int seed)
        {
            var image = new RgbaImage(w, h);
            uint state = (uint)seed * 2654435761u + 12345u;
            for (int i = 0; i < image.Pixels.Length; i++)
            {
                state = state * 1664525u + 1013904223u;
                image.Pixels[i] = (byte)(state >> 24);
            }

            return image;
        }

        private static RgbaImage Flat(int w, int h, Rgba8 color)
        {
            var image = new RgbaImage(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    image.Set(x, y, color);
                }
            }

            return image;
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 5)]
        [TestCase(16, 16)]
        [TestCase(64, 1)]
        [TestCase(1, 64)]
        [TestCase(100, 37)]
        [TestCase(257, 3)]
        public void RoundTripsRandomPixels(int w, int h)
        {
            RgbaImage source = Pattern(w, h, w * 31 + h);
            RgbaImage decoded = PngDecoder.Decode(PngEncoder.Encode(source));
            Assert.That(decoded.SameAs(source), Is.True);
        }

        [TestCase(1, 1, 0, 0, 0, 0)]
        [TestCase(8, 8, 255, 255, 255, 255)]
        [TestCase(33, 17, 12, 200, 99, 128)]
        [TestCase(300, 300, 1, 2, 3, 4)]
        [TestCase(1, 1000, 255, 0, 0, 255)]
        [TestCase(1000, 1, 0, 255, 0, 255)]
        public void RoundTripsFlatPixels(int w, int h, int r, int g, int b, int a)
        {
            RgbaImage source = Flat(w, h, new Rgba8((byte)r, (byte)g, (byte)b, (byte)a));
            RgbaImage decoded = PngDecoder.Decode(PngEncoder.Encode(source));
            Assert.That(decoded.SameAs(source), Is.True);
        }

        [Test]
        public void FlatImagesCompressWell()
        {
            byte[] png = PngEncoder.Encode(Flat(256, 256, new Rgba8(10, 20, 30, 255)));
            Assert.That(png.Length, Is.LessThan(2500));
        }

        [Test]
        public void EncodingIsDeterministic()
        {
            RgbaImage image = Pattern(40, 40, 7);
            Assert.That(PngEncoder.Encode(image), Is.EqualTo(PngEncoder.Encode(image)));
        }

        [Test]
        public void SignatureAndChunksAreFixed()
        {
            byte[] png = PngEncoder.Encode(Flat(4, 4, new Rgba8(1, 2, 3, 4)));
            Assert.That(png[0], Is.EqualTo(0x89));
            Assert.That(System.Text.Encoding.ASCII.GetString(png, 12, 4), Is.EqualTo("IHDR"));
            Assert.That(System.Text.Encoding.ASCII.GetString(png, 37, 4), Is.EqualTo("IDAT"));
            Assert.That(System.Text.Encoding.ASCII.GetString(png, png.Length - 8, 4), Is.EqualTo("IEND"));
        }

        [Test]
        public void HeaderDeclaresEightBitRgba()
        {
            byte[] png = PngEncoder.Encode(Flat(5, 7, new Rgba8(0, 0, 0, 255)));
            Assert.That(png[16 + 3], Is.EqualTo(5));
            Assert.That(png[20 + 3], Is.EqualTo(7));
            Assert.That(png[24], Is.EqualTo(8));
            Assert.That(png[25], Is.EqualTo(6));
        }

        [Test]
        public void StockInflateReadsTheStream()
        {
            RgbaImage image = Pattern(20, 20, 3);
            byte[] png = PngEncoder.Encode(image);
            int idatLength = (png[33] << 24) | (png[34] << 16) | (png[35] << 8) | png[36];
            using (var input = new MemoryStream(png, 43, idatLength - 2 - 4))
            using (var inflate = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                inflate.CopyTo(output);
                Assert.That(output.Length, Is.EqualTo(20 * (20 * 4 + 1)));
            }
        }

        [Test]
        public void ChunkChecksumsAreValid()
        {
            byte[] png = PngEncoder.Encode(Pattern(9, 9, 1));
            int pos = 8;
            while (pos < png.Length)
            {
                int length = (png[pos] << 24) | (png[pos + 1] << 16) | (png[pos + 2] << 8) | png[pos + 3];
                uint crc = Checksums.Crc32(png, pos + 4, length + 4);
                uint stored = ((uint)png[pos + 8 + length] << 24) | ((uint)png[pos + 9 + length] << 16) | ((uint)png[pos + 10 + length] << 8) | png[pos + 11 + length];
                Assert.That(crc, Is.EqualTo(stored));
                pos += 12 + length;
            }
        }

        [Test]
        public void CrcOfKnownStringMatches()
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes("123456789");
            Assert.That(Checksums.Crc32(data, 0, data.Length), Is.EqualTo(0xCBF43926u));
        }

        [Test]
        public void AdlerOfKnownStringMatches()
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes("Wikipedia");
            Assert.That(Checksums.Adler32(data, 0, data.Length), Is.EqualTo(0x11E60398u));
        }

        [Test]
        public void LongRunsUseMaxLengthMatches()
        {
            RgbaImage image = Flat(500, 4, new Rgba8(7, 7, 7, 7));
            Assert.That(PngDecoder.Decode(PngEncoder.Encode(image)).SameAs(image), Is.True);
        }

        [Test]
        public void RepeatedRowsCompress()
        {
            var image = new RgbaImage(64, 64);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    image.Set(x, y, new Rgba8((byte)(x * 4), (byte)(x * 2), (byte)x, 255));
                }
            }

            byte[] png = PngEncoder.Encode(image);
            Assert.That(png.Length, Is.LessThan(64 * 64 * 4 / 3));
            Assert.That(PngDecoder.Decode(png).SameAs(image), Is.True);
        }

        [Test]
        public void EncoderRejectsBadSizes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PngEncoder.Encode(0, 1, new byte[0]));
            Assert.Throws<ArgumentException>(() => PngEncoder.Encode(2, 2, new byte[3]));
            Assert.Throws<ArgumentNullException>(() => PngEncoder.Encode(null));
        }

        [Test]
        public void DecoderRejectsGarbage()
        {
            Assert.Throws<InvalidDataException>(() => PngDecoder.Decode(new byte[50]));
            Assert.Throws<InvalidDataException>(() => PngDecoder.Decode(null));
        }

        [Test]
        public void ImageSetAndGetAgree()
        {
            var image = new RgbaImage(3, 3);
            image.Set(2, 1, new Rgba8(9, 8, 7, 6));
            Assert.That(image.Get(2, 1), Is.EqualTo(new Rgba8(9, 8, 7, 6)));
            Assert.That(image.Get(0, 0), Is.EqualTo(new Rgba8(0, 0, 0, 0)));
        }

        [Test]
        public void SameAsDetectsDifferences()
        {
            RgbaImage a = Flat(3, 3, new Rgba8(1, 1, 1, 1));
            RgbaImage b = Flat(3, 3, new Rgba8(1, 1, 1, 1));
            Assert.That(a.SameAs(b), Is.True);
            b.Set(1, 1, new Rgba8(1, 1, 2, 1));
            Assert.That(a.SameAs(b), Is.False);
            Assert.That(a.SameAs(null), Is.False);
            Assert.That(a.SameAs(Flat(2, 3, new Rgba8(1, 1, 1, 1))), Is.False);
        }
    }
}
