using System;
using System.Collections.Generic;

namespace HtmlToUitk.Bake
{
    internal sealed class BitWriter
    {
        private readonly List<byte> bytes = new List<byte>();
        private uint buffer;
        private int count;

        public void WriteBits(uint value, int bits)
        {
            buffer |= value << count;
            count += bits;
            while (count >= 8)
            {
                bytes.Add((byte)(buffer & 0xFF));
                buffer >>= 8;
                count -= 8;
            }
        }

        public void WriteHuffman(uint code, int bits)
        {
            uint reversed = 0;
            for (int i = 0; i < bits; i++)
            {
                reversed = (reversed << 1) | ((code >> i) & 1);
            }

            WriteBits(reversed, bits);
        }

        public byte[] Finish()
        {
            if (count > 0)
            {
                bytes.Add((byte)(buffer & 0xFF));
                buffer = 0;
                count = 0;
            }

            return bytes.ToArray();
        }
    }

    internal static class Deflate
    {
        private const int WindowSize = 32768;
        private const int MinMatch = 3;
        private const int MaxMatch = 258;
        private const int HashBits = 15;
        private const int MaxChain = 48;

        private static readonly int[] LengthBase = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
        private static readonly int[] LengthExtra = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
        private static readonly int[] DistBase = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
        private static readonly int[] DistExtra = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

        public static byte[] Compress(byte[] data)
        {
            var writer = new BitWriter();
            writer.WriteBits(1, 1);
            writer.WriteBits(1, 2);
            int n = data.Length;
            var head = new int[1 << HashBits];
            for (int i = 0; i < head.Length; i++)
            {
                head[i] = -1;
            }

            var prev = new int[n > 0 ? n : 1];
            int pos = 0;
            while (pos < n)
            {
                int bestLen = 0;
                int bestDist = 0;
                if (pos + MinMatch <= n)
                {
                    int h = Hash(data, pos);
                    int candidate = head[h];
                    int chain = 0;
                    int limit = Math.Min(MaxMatch, n - pos);
                    while (candidate >= 0 && pos - candidate <= WindowSize && chain < MaxChain)
                    {
                        int len = 0;
                        while (len < limit && data[candidate + len] == data[pos + len])
                        {
                            len++;
                        }

                        if (len > bestLen)
                        {
                            bestLen = len;
                            bestDist = pos - candidate;
                            if (len == limit)
                            {
                                break;
                            }
                        }

                        candidate = prev[candidate];
                        chain++;
                    }
                }

                if (bestLen >= MinMatch)
                {
                    WriteLength(writer, bestLen);
                    WriteDistance(writer, bestDist);
                    for (int k = 0; k < bestLen; k++)
                    {
                        Insert(data, prev, head, pos + k, n);
                    }

                    pos += bestLen;
                }
                else
                {
                    WriteSymbol(writer, data[pos]);
                    Insert(data, prev, head, pos, n);
                    pos++;
                }
            }

            WriteSymbol(writer, 256);
            return writer.Finish();
        }

        private static int Hash(byte[] data, int pos)
        {
            uint v = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16));
            return (int)((v * 2654435761u) >> (32 - HashBits));
        }

        private static void Insert(byte[] data, int[] prev, int[] head, int pos, int n)
        {
            if (pos + MinMatch > n)
            {
                return;
            }

            int h = Hash(data, pos);
            prev[pos] = head[h];
            head[h] = pos;
        }

        private static void WriteSymbol(BitWriter writer, int symbol)
        {
            if (symbol < 144)
            {
                writer.WriteHuffman((uint)(0x30 + symbol), 8);
            }
            else if (symbol < 256)
            {
                writer.WriteHuffman((uint)(0x190 + symbol - 144), 9);
            }
            else if (symbol < 280)
            {
                writer.WriteHuffman((uint)(symbol - 256), 7);
            }
            else
            {
                writer.WriteHuffman((uint)(0xC0 + symbol - 280), 8);
            }
        }

        private static void WriteLength(BitWriter writer, int length)
        {
            int index = LengthBase.Length - 1;
            while (LengthBase[index] > length)
            {
                index--;
            }

            WriteSymbol(writer, 257 + index);
            if (LengthExtra[index] > 0)
            {
                writer.WriteBits((uint)(length - LengthBase[index]), LengthExtra[index]);
            }
        }

        private static void WriteDistance(BitWriter writer, int distance)
        {
            int index = DistBase.Length - 1;
            while (DistBase[index] > distance)
            {
                index--;
            }

            writer.WriteHuffman((uint)index, 5);
            if (DistExtra[index] > 0)
            {
                writer.WriteBits((uint)(distance - DistBase[index]), DistExtra[index]);
            }
        }
    }
}
