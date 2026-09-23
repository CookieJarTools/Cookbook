namespace CookieJarTools.Cookbook.Editor.Images.Gif
{
    /// <summary>Unpacks a GIF frame's LZW-compressed colour indices.</summary>
    public sealed class LzwDecoder
    {
        private const int MaxCodes = 4096;

        private readonly byte[] data;
        private readonly int minCodeSize;
        private readonly int clear;
        private readonly int end;
        private readonly short[] prefix = new short[MaxCodes];
        private readonly byte[] suffix = new byte[MaxCodes];
        private readonly byte[] stack = new byte[MaxCodes + 1];
        private readonly byte[] output;

        private int position;
        private int bitBuffer;
        private int bitCount;
        private int codeSize;
        private int mask;
        private int next;
        private int written;

        private LzwDecoder(byte[] data, int minCodeSize, int pixels)
        {
            this.data = data;
            this.minCodeSize = minCodeSize;
            clear = 1 << minCodeSize;
            end = clear + 1;
            output = new byte[pixels];
            for (var i = 0; i < clear; i++)
            {
                prefix[i] = -1;
                suffix[i] = (byte)i;
            }

            Reset();
        }

        public static byte[] Decode(byte[] data, int minCodeSize, int pixels)
        {
            return new LzwDecoder(data, minCodeSize, pixels).Run();
        }

        private byte[] Run()
        {
            var previous = -1;
            byte first = 0;
            while (written < output.Length)
            {
                var code = ReadCode();
                if (code < 0 || code == end)
                {
                    break;
                }

                if (code == clear)
                {
                    Reset();
                    previous = -1;
                    continue;
                }

                first = previous < 0 ? Start(code) : Continue(code, previous, first);
                previous = code;
            }

            return output;
        }

        private byte Start(int code)
        {
            output[written++] = suffix[code];
            return suffix[code];
        }

        /// <summary>A code after the first: its string written out, and the dictionary grown by the previous string plus this one's first byte.</summary>
        private byte Continue(int code, int previous, byte first)
        {
            var top = 0;
            var at = code;
            if (code >= next)
            {
                stack[top++] = first;
                at = previous;
            }

            while (at >= clear)
            {
                stack[top++] = suffix[at];
                at = prefix[at];
            }

            var head = suffix[at];
            stack[top++] = head;
            while (top > 0 && written < output.Length)
            {
                output[written++] = stack[--top];
            }

            Grow(previous, head);
            return head;
        }

        private void Grow(int previous, byte head)
        {
            if (next >= MaxCodes)
            {
                return;
            }

            prefix[next] = (short)previous;
            suffix[next] = head;
            next++;
            if ((next & mask) == 0 && next < MaxCodes)
            {
                codeSize++;
                mask = (1 << codeSize) - 1;
            }
        }

        private void Reset()
        {
            codeSize = minCodeSize + 1;
            mask = (1 << codeSize) - 1;
            next = clear + 2;
        }

        /// <summary>The next code, least significant bits first; -1 when the data has run out.</summary>
        private int ReadCode()
        {
            while (bitCount < codeSize)
            {
                if (position >= data.Length)
                {
                    return -1;
                }

                bitBuffer |= data[position++] << bitCount;
                bitCount += 8;
            }

            var code = bitBuffer & mask;
            bitBuffer >>= codeSize;
            bitCount -= codeSize;
            return code;
        }
    }
}
