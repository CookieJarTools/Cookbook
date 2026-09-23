using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Images.Gif
{
    /// <summary>Writes frames as a looping GIF: one palette for the whole clip, every frame whole.</summary>
    public static class GifWriter
    {
        private const int SubBlock = 255;

        /// <param name="frames">Each frame's pixels, top row first.</param>
        /// <param name="delay">Hundredths of a second per frame.</param>
        public static byte[] Write(IReadOnlyList<Color32[]> frames, int width, int height, int delay)
        {
            var palette = new GifPalette(frames);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Header(writer, width, height, palette);
                foreach (var frame in frames)
                {
                    Frame(writer, palette.Map(frame), width, height, delay);
                }

                writer.Write((byte)0x3B);
                return stream.ToArray();
            }
        }

        /// <summary>The signature, the screen, the palette, and the note that makes it loop forever.</summary>
        private static void Header(BinaryWriter writer, int width, int height, GifPalette palette)
        {
            writer.Write(Encoding.ASCII.GetBytes("GIF89a"));
            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((byte)0xF7);
            writer.Write((byte)0);
            writer.Write((byte)0);
            for (var i = 0; i < GifPalette.Size; i++)
            {
                var color = i < palette.Colors.Length ? palette.Colors[i] : new Color32(0, 0, 0, 255);
                writer.Write(color.r);
                writer.Write(color.g);
                writer.Write(color.b);
            }

            writer.Write(new byte[] { 0x21, 0xFF, 0x0B });
            writer.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));
            writer.Write(new byte[] { 0x03, 0x01, 0x00, 0x00, 0x00 });
        }

        private static void Frame(BinaryWriter writer, byte[] indices, int width, int height, int delay)
        {
            writer.Write(new byte[] { 0x21, 0xF9, 0x04, 0x04 });
            writer.Write((ushort)delay);
            writer.Write(new byte[] { 0x00, 0x00 });

            writer.Write((byte)0x2C);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((byte)0);

            writer.Write((byte)LzwEncoder.CodeSize);
            var packed = LzwEncoder.Encode(indices);
            for (var at = 0; at < packed.Length; at += SubBlock)
            {
                var length = Mathf.Min(SubBlock, packed.Length - at);
                writer.Write((byte)length);
                writer.Write(packed, at, length);
            }

            writer.Write((byte)0);
        }
    }
}
