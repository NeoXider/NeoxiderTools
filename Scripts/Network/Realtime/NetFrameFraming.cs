#if MIRROR
using System;
using Mirror;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     Versioned, length-prefixed framing for hand-written Mirror payloads:
    ///     <c>byte version, uint bodyLength, body...</c>.
    ///     <para>
    ///         Every game that flattens its own world state into one byte payload (the only way to get an
    ///         allocation-free, quantised snapshot through Mirror, whose weaver cannot serialise nested arrays of
    ///         structs) needs the same four things: a version byte so an old peer is refused loudly instead of
    ///         misread, a body length so a truncated or over-read frame is detected, bounded counts so a corrupt count
    ///         cannot allocate or loop for ever, and a clamp for narrowing ints. Writing them once means the format
    ///         code is only the fields.
    ///     </para>
    ///     <para>
    ///         The reader is total: a wrong version, a length that exceeds the buffer, a count above its ceiling or a
    ///         body that was not consumed exactly all raise <see cref="FormatException"/>, which Mirror turns into a
    ///         disconnect instead of a silent desync.
    ///     </para>
    /// </summary>
    /// <example>
    ///     <code>
    ///     int lengthAt = NetFrameFraming.WriteHeader(writer, version: 1);
    ///     writer.WriteVarUInt(NetFrameFraming.NonNegative(entities.Count));
    ///     for (int i = 0; i &lt; entities.Count; i++) WriteEntity(writer, entities[i]);
    ///     NetFrameFraming.PatchLength(writer, lengthAt);
    ///
    ///     // reader
    ///     NetFrameFraming.ReadHeader(reader, 1, "WorldFrame", out int bodyStart, out int bodyEnd);
    ///     int count = NetFrameFraming.ReadCount(reader, max: 384, "entity");
    ///     for (int i = 0; i &lt; count; i++) ReadEntity(reader);
    ///     NetFrameFraming.VerifyBodyLength(reader, bodyStart, bodyEnd);
    ///     </code>
    /// </example>
    public static class NetFrameFraming
    {
        /// <summary>Bytes taken by the body-length field.</summary>
        public const int LengthFieldBytes = sizeof(uint);

        /// <summary>
        ///     Writes the version and a length placeholder. Returns the position of the length field for
        ///     <see cref="PatchLength"/>.
        /// </summary>
        public static int WriteHeader(NetworkWriter writer, byte version)
        {
            writer.WriteByte(version);
            int lengthAt = writer.Position;
            writer.WriteUInt(0u);
            return lengthAt;
        }

        /// <summary>Back-patches the body length once the body is written; leaves the writer at its end.</summary>
        public static void PatchLength(NetworkWriter writer, int lengthFieldAt)
        {
            int end = writer.Position;
            int bodyStart = lengthFieldAt + LengthFieldBytes;
            uint length = (uint)(end - bodyStart);
            writer.Position = lengthFieldAt;
            writer.WriteUInt(length);
            writer.Position = end;
        }

        /// <summary>Reads and validates the header. <paramref name="what"/> names the frame in the error message.</summary>
        /// <exception cref="FormatException">Wrong version, or the declared body is longer than the remaining bytes.</exception>
        public static void ReadHeader(
            NetworkReader reader,
            byte expectedVersion,
            string what,
            out int bodyStart,
            out int bodyEnd)
        {
            byte version = reader.ReadByte();
            if (version != expectedVersion)
            {
                throw new FormatException(
                    $"{what} version mismatch: this peer speaks {expectedVersion}, the payload is {version}.");
            }

            uint bodyLength = reader.ReadUInt();
            bodyStart = reader.Position;
            long end = bodyStart + (long)bodyLength;
            if (end > reader.Remaining + (long)bodyStart)
            {
                throw new FormatException(
                    $"{what} declares {bodyLength} body bytes but only {reader.Remaining} remain.");
            }

            bodyEnd = (int)end;
        }

        /// <summary>Throws unless the reader consumed exactly the declared body.</summary>
        /// <exception cref="FormatException">The body was under- or over-read.</exception>
        public static void VerifyBodyLength(NetworkReader reader, int bodyStart, int bodyEnd)
        {
            if (reader.Position != bodyEnd)
            {
                throw new FormatException(
                    $"frame body length mismatch: header declared {bodyEnd - bodyStart} bytes, read {reader.Position - bodyStart}.");
            }
        }

        /// <summary>Reads a varuint count and refuses one above <paramref name="max"/>.</summary>
        /// <exception cref="FormatException">The count exceeds the ceiling.</exception>
        public static int ReadCount(NetworkReader reader, int max, string what)
        {
            uint raw = reader.ReadVarUInt();
            if (raw > (uint)max)
            {
                throw new FormatException($"{what} count {raw} exceeds the ceiling {max}.");
            }

            return (int)raw;
        }

        /// <summary>A varuint standing in for an int field (level, xp, kills): saturates at <see cref="int.MaxValue"/>.</summary>
        public static int ReadCount32(NetworkReader reader)
        {
            uint raw = reader.ReadVarUInt();
            return raw > int.MaxValue ? int.MaxValue : (int)raw;
        }

        /// <summary>Negative values write as 0, so an unsigned varint never receives a wrapped number.</summary>
        public static uint NonNegative(int value)
        {
            return value < 0 ? 0u : (uint)value;
        }

        /// <summary>Clamps an int into one byte.</summary>
        public static byte ClampByte(int value)
        {
            return value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)value;
        }
    }
}
#endif
