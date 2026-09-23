using System.Collections.Generic;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Images.Gif
{
    /// <summary>A GIF read into textures: each frame whole, as it looks on screen, and how long it shows.</summary>
    public sealed class GifClip
    {
        public GifClip(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }

        public List<Texture2D> Frames { get; } = new List<Texture2D>();

        /// <summary>Seconds each frame shows for.</summary>
        public List<float> Delays { get; } = new List<float>();

        public void Destroy()
        {
            foreach (var frame in Frames)
            {
                Object.DestroyImmediate(frame);
            }

            Frames.Clear();
        }
    }
}
