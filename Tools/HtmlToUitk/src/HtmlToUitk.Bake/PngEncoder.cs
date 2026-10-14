using System;
using System.IO;

namespace HtmlToUitk.Bake
{
    public static class PngEncoder
    {
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static byte[] Encode(RgbaImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            return Encode(image.Width, image.Height, image.Pixels);
        }

        public static byte[] Encode(int width, int height, byte[] rgba)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "image size must be positive");
            }

            if (rgba == null || rgba.Length != width * height * 4)
            {
                throw new ArgumentException("pixel buffer does not match the image size", nameof(rgba));
            }

            int stride = width * 4;
            var raw = new byte[height * (stride + 1)];
            for (int y = 0; y < height; y++)
            {
                raw[y * (stride + 1)] = 0;
                Buffer.BlockCopy(rgba, y * stride, raw, y * (stride + 1) + 1, stride);
            }

            byte[] deflated = Deflate.Compress(raw);
            uint adler = Checksums.Adler32(raw, 0, raw.Length);
            var zlib = new byte[2 + deflated.Length + 4];
            zlib[0] = 0x78;
            zlib[1] = 0x01;
            Buffer.BlockCopy(deflated, 0, zlib, 2, deflated.Length);
            WriteBigEndian(zlib, 2 + deflated.Length, adler);

            using (var stream = new MemoryStream())
            {
                stream.Write(Signature, 0, Signature.Length);
                var header = new byte[13];
                WriteBigEndian(header, 0, (uint)width);
                WriteBigEndian(header, 4, (uint)height);
                header[8] = 8;
                header[9] = 6;
                WriteChunk(stream, "IHDR", header);
                WriteChunk(stream, "IDAT", zlib);
                WriteChunk(stream, "IEND", new byte[0]);
                return stream.ToArray();
            }
        }

        private static void WriteChunk(Stream stream, string type, byte[] data)
        {
            var length = new byte[4];
            WriteBigEndian(length, 0, (uint)data.Length);
            stream.Write(length, 0, 4);
            var body = new byte[4 + data.Length];
            for (int i = 0; i < 4; i++)
            {
                body[i] = (byte)type[i];
            }

            Buffer.BlockCopy(data, 0, body, 4, data.Length);
            stream.Write(body, 0, body.Length);
            var crc = new byte[4];
            WriteBigEndian(crc, 0, Checksums.Crc32(body, 0, body.Length));
            stream.Write(crc, 0, 4);
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }
}
