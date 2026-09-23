using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Images.Gif
{
    /// <summary>
    /// The 256 colours a GIF can have, chosen for a whole clip by median cut: the clip's colours are
    /// split again and again along their widest spread, weighted by how much each is used, until
    /// there are 256 groups, and each group's average becomes a colour. Every pixel is then given
    /// the nearest of them.
    /// </summary>
    public sealed class GifPalette
    {
        public const int Size = 256;

        private const int Bits = 5;
        private const int Levels = 1 << Bits;
        private const int Keys = Levels * Levels * Levels;

        private readonly int[] lookup = new int[Keys];

        public GifPalette(IEnumerable<Color32[]> frames)
        {
            var counts = new int[Keys];
            foreach (var frame in frames)
            {
                Count(frame, counts);
            }

            var used = Enumerable.Range(0, Keys).Where(k => counts[k] > 0).ToList();
            var boxes = Cut(used, counts);
            Colors = boxes.Select(box => Average(box, counts)).ToArray();
            for (var i = 0; i < boxes.Count; i++)
            {
                Assign(boxes[i], i);
            }
        }

        public Color32[] Colors { get; }

        /// <summary>A frame as palette indices, in its own pixel order.</summary>
        public byte[] Map(Color32[] frame)
        {
            var indices = new byte[frame.Length];
            for (var i = 0; i < frame.Length; i++)
            {
                indices[i] = (byte)lookup[Key(frame[i])];
            }

            return indices;
        }

        private static void Count(Color32[] frame, int[] counts)
        {
            foreach (var pixel in frame)
            {
                counts[Key(pixel)]++;
            }
        }

        private static int Key(Color32 color)
        {
            return ((color.r >> 3) << 10) | ((color.g >> 3) << 5) | (color.b >> 3);
        }

        private static int Channel(int key, int channel)
        {
            return (key >> (10 - channel * 5)) & (Levels - 1);
        }

        /// <summary>Splits the colours into at most 256 groups, always splitting the group with the most spread times use.</summary>
        private static List<List<int>> Cut(List<int> used, int[] counts)
        {
            var boxes = new List<List<int>> { used };
            while (boxes.Count < Size)
            {
                var widest = boxes.Where(b => b.Count > 1).OrderByDescending(b => Spread(b, out _) * (long)b.Sum(k => counts[k])).FirstOrDefault();
                if (widest == null)
                {
                    break;
                }

                boxes.Remove(widest);
                boxes.AddRange(Split(widest, counts));
            }

            return boxes;
        }

        private static int Spread(List<int> box, out int channel)
        {
            var best = -1;
            channel = 0;
            for (var c = 0; c < 3; c++)
            {
                var spread = box.Max(k => Channel(k, c)) - box.Min(k => Channel(k, c));
                if (spread > best)
                {
                    best = spread;
                    channel = c;
                }
            }

            return best;
        }

        /// <summary>In two along the widest channel, where half the pixels fall either side.</summary>
        private static IEnumerable<List<int>> Split(List<int> box, int[] counts)
        {
            Spread(box, out var channel);
            var sorted = box.OrderBy(k => Channel(k, channel)).ToList();
            var half = sorted.Sum(k => (long)counts[k]) / 2;
            var running = 0L;
            var cut = 1;
            for (; cut < sorted.Count - 1 && running + counts[sorted[cut - 1]] < half; cut++)
            {
                running += counts[sorted[cut - 1]];
            }

            return new[] { sorted.GetRange(0, cut), sorted.GetRange(cut, sorted.Count - cut) };
        }

        private static Color32 Average(List<int> box, int[] counts)
        {
            if (box.Count == 0)
            {
                return new Color32(0, 0, 0, 255);
            }

            long r = 0, g = 0, b = 0, total = 0;
            foreach (var key in box)
            {
                long weight = counts[key];
                r += Expand(Channel(key, 0)) * weight;
                g += Expand(Channel(key, 1)) * weight;
                b += Expand(Channel(key, 2)) * weight;
                total += weight;
            }

            total = System.Math.Max(1, total);
            return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), 255);
        }

        private static int Expand(int level)
        {
            return (level << 3) | (level >> 2);
        }

        private void Assign(List<int> box, int index)
        {
            foreach (var key in box)
            {
                lookup[key] = index;
            }
        }
    }
}
