namespace HtmlToUitk.Bake
{
    internal static class Checksums
    {
        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }

        public static uint Crc32(byte[] data, int offset, int count, uint seed = 0)
        {
            uint c = seed ^ 0xFFFFFFFFu;
            for (int i = 0; i < count; i++)
            {
                c = CrcTable[(c ^ data[offset + i]) & 0xFF] ^ (c >> 8);
            }

            return c ^ 0xFFFFFFFFu;
        }

        public static uint Adler32(byte[] data, int offset, int count)
        {
            uint a = 1;
            uint b = 0;
            int i = 0;
            while (i < count)
            {
                int chunk = count - i < 5552 ? count - i : 5552;
                for (int k = 0; k < chunk; k++)
                {
                    a += data[offset + i + k];
                    b += a;
                }

                a %= 65521;
                b %= 65521;
                i += chunk;
            }

            return (b << 16) | a;
        }
    }
}
