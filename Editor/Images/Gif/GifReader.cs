using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Images.Gif
{
    /// <summary>
    /// Reads a GIF into whole frames: each image drawn over what was there, as a browser would,
    /// honouring transparency and how each frame is cleared before the next.
    /// </summary>
    public sealed class GifReader
    {
        private const byte ExtensionBlock = 0x21;
        private const byte ImageBlock = 0x2C;
        private const byte Trailer = 0x3B;
        private const byte GraphicControl = 0xF9;
        private const int RestoreBackground = 2;
        private const int RestorePrevious = 3;
        private const float DefaultDelay = 0.1f;

        private static readonly int[] InterlaceStart = { 0, 4, 2, 1 };
        private static readonly int[] InterlaceStep = { 8, 8, 4, 2 };

        private readonly byte[] data;

        private int position;
        private int width;
        private int height;
        private Color32[] global;
        private Color32[] canvas;
        private GifClip clip;
        private int disposal;
        private int delay;
        private int transparent = -1;

        private GifReader(byte[] data)
        {
            this.data = data;
        }

        /// <summary>The clip, or null when the bytes are not a GIF that can be read.</summary>
        public static GifClip Read(byte[] data)
        {
            try
            {
                return data != null && data.Length > 13 && data[0] == 'G' && data[1] == 'I' && data[2] == 'F' ? new GifReader(data).Run() : null;
            }
            catch (Exception exception) when (exception is IndexOutOfRangeException || exception is InvalidDataException)
            {
                return null;
            }
        }

        private GifClip Run()
        {
            position = 6;
            width = Short();
            height = Short();
            var packed = data[position++];
            position += 2;
            global = (packed & 0x80) != 0 ? Palette(1 << ((packed & 7) + 1)) : null;
            canvas = new Color32[width * height];
            clip = new GifClip(width, height);

            while (position < data.Length && Block(data[position++]))
            {
            }

            return clip.Frames.Count > 0 ? clip : null;
        }

        /// <summary>One block; false at the end of the file, or at anything not understood.</summary>
        private bool Block(byte kind)
        {
            switch (kind)
            {
                case ExtensionBlock:
                    Extension();
                    return true;
                case ImageBlock:
                    Image();
                    return true;
                default:
                    return false;
            }
        }

        private void Extension()
        {
            var label = data[position++];
            if (label != GraphicControl)
            {
                SubBlocks();
                return;
            }

            position++;
            var packed = data[position++];
            disposal = (packed >> 2) & 7;
            delay = Short();
            var index = data[position++];
            transparent = (packed & 1) != 0 ? index : -1;
            position++;
        }

        private void Image()
        {
            var left = Short();
            var top = Short();
            var w = Short();
            var h = Short();
            var packed = data[position++];
            var palette = (packed & 0x80) != 0 ? Palette(1 << ((packed & 7) + 1)) : global;
            var interlaced = (packed & 0x40) != 0;
            var minCodeSize = data[position++];
            var indices = LzwDecoder.Decode(SubBlocks(), minCodeSize, w * h);
            if (interlaced)
            {
                indices = Deinterlace(indices, w, h);
            }

            var before = disposal == RestorePrevious ? (Color32[])canvas.Clone() : null;
            for (var y = 0; y < h; y++)
            {
                DrawRow(indices, palette, y, left, top, w);
            }

            clip.Frames.Add(Snapshot());
            clip.Delays.Add(delay <= 1 ? DefaultDelay : delay / 100f);
            Clear(before, left, top, w, h);
            disposal = 0;
            delay = 0;
            transparent = -1;
        }

        private void DrawRow(byte[] indices, Color32[] palette, int y, int left, int top, int w)
        {
            var row = top + y;
            if (row < 0 || row >= height || palette == null)
            {
                return;
            }

            for (var x = 0; x < w; x++)
            {
                var index = indices[y * w + x];
                var column = left + x;
                if (index != transparent && column >= 0 && column < width && index < palette.Length)
                {
                    canvas[row * width + column] = palette[index];
                }
            }
        }

        /// <summary>After a frame: its area left, cleared to nothing, or put back as it was before, as the frame asked.</summary>
        private void Clear(Color32[] before, int left, int top, int w, int h)
        {
            if (disposal == RestorePrevious && before != null)
            {
                canvas = before;
                return;
            }

            if (disposal != RestoreBackground)
            {
                return;
            }

            for (var y = Math.Max(0, top); y < Math.Min(height, top + h); y++)
            {
                Array.Clear(canvas, y * width + Math.Max(0, left), Math.Max(0, Math.Min(width, left + w) - Math.Max(0, left)));
            }
        }

        /// <summary>The canvas as a texture — turned over, since a texture's rows run bottom to top.</summary>
        private Texture2D Snapshot()
        {
            var flipped = new Color32[canvas.Length];
            for (var y = 0; y < height; y++)
            {
                Array.Copy(canvas, y * width, flipped, (height - 1 - y) * width, width);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(flipped);
            texture.Apply();
            return texture;
        }

        private static byte[] Deinterlace(byte[] indices, int w, int h)
        {
            var result = new byte[indices.Length];
            var source = 0;
            for (var pass = 0; pass < InterlaceStart.Length; pass++)
            {
                source = CopyPass(indices, result, w, h, pass, source);
            }

            return result;
        }

        private static int CopyPass(byte[] indices, byte[] result, int w, int h, int pass, int source)
        {
            for (var y = InterlaceStart[pass]; y < h; y += InterlaceStep[pass])
            {
                Array.Copy(indices, source, result, y * w, w);
                source += w;
            }

            return source;
        }

        private Color32[] Palette(int size)
        {
            var palette = new Color32[size];
            for (var i = 0; i < size; i++)
            {
                palette[i] = new Color32(data[position], data[position + 1], data[position + 2], 255);
                position += 3;
            }

            return palette;
        }

        /// <summary>Data blocks strung together: a length byte, that many bytes, until a length of nought.</summary>
        private byte[] SubBlocks()
        {
            var bytes = new List<byte>();
            for (var length = data[position++]; length > 0; length = data[position++])
            {
                bytes.AddRange(new ArraySegment<byte>(data, position, length));
                position += length;
            }

            return bytes.ToArray();
        }

        private int Short()
        {
            var value = data[position] | (data[position + 1] << 8);
            position += 2;
            return value;
        }
    }
}
