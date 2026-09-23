using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Images.Gif
{
    /// <summary>Packs a frame's colour indices with GIF's LZW, for 8-bit indices.</summary>
    public sealed class LzwEncoder
    {
        private const int MinCodeSize = 8;
        private const int Clear = 1 << MinCodeSize;
        private const int End = Clear + 1;
        private const int MaxCodes = 4096;
        private const int MaxCodeSize = 12;

        private readonly Dictionary<int, int> table = new Dictionary<int, int>();
        private readonly List<byte> output = new List<byte>();

        private int codeSize;
        private int next;
        private int bitBuffer;
        private int bitCount;

        public static int CodeSize => MinCodeSize;

        public static byte[] Encode(byte[] indices)
        {
            return new LzwEncoder().Run(indices);
        }

        private byte[] Run(byte[] indices)
        {
            Restart();
            if (indices.Length == 0)
            {
                Emit(End);
                return Flush();
            }

            int prefix = indices[0];
            for (var i = 1; i < indices.Length; i++)
            {
                prefix = Step(prefix, indices[i]);
            }

            Emit(prefix);
            Emit(End);
            return Flush();
        }

        /// <summary>Carries on the current string with the next index, or writes it out and starts a new one.</summary>
        private int Step(int prefix, byte index)
        {
            var key = (prefix << 8) | index;
            if (table.TryGetValue(key, out var code))
            {
                return code;
            }

            Emit(prefix);
            if (next < MaxCodes)
            {
                table[key] = next++;
                return index;
            }

            Emit(Clear);
            Restart();
            return index;
        }

        /// <summary>A clear code, and back to the starting size and an empty table.</summary>
        private void Restart()
        {
            if (next == 0)
            {
                codeSize = MinCodeSize + 1;
                Emit(Clear);
            }

            table.Clear();
            codeSize = MinCodeSize + 1;
            next = End + 1;
        }

        /// <summary>
        /// A code, least significant bits first. Once the table holds more codes than fit the size,
        /// later codes are a bit longer — as the decoder expects.
        /// </summary>
        private void Emit(int code)
        {
            bitBuffer |= code << bitCount;
            bitCount += codeSize;
            while (bitCount >= 8)
            {
                output.Add((byte)(bitBuffer & 0xFF));
                bitBuffer >>= 8;
                bitCount -= 8;
            }

            if (next > (1 << codeSize) - 1 && codeSize < MaxCodeSize && code != Clear)
            {
                codeSize++;
            }
        }

        private byte[] Flush()
        {
            if (bitCount > 0)
            {
                output.Add((byte)(bitBuffer & 0xFF));
            }

            return output.ToArray();
        }
    }
}
