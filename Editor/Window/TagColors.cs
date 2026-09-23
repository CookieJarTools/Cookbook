using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// A colour for each tag, the same every time and on every machine: worked out from the tag's
    /// letters, so "combat" is always the same colour without anyone choosing it. The colours are
    /// kept quiet — they show as a dot, not a fill.
    /// </summary>
    public static class TagColors
    {
        private static readonly Color[] Palette =
        {
            new Color32(94, 106, 210, 255),
            new Color32(38, 181, 206, 255),
            new Color32(76, 183, 130, 255),
            new Color32(242, 201, 76, 255),
            new Color32(242, 153, 74, 255),
            new Color32(235, 87, 87, 255),
            new Color32(187, 135, 252, 255),
            new Color32(149, 162, 179, 255),
        };

        public static Color For(string tag)
        {
            var hash = 17;
            foreach (var letter in (tag ?? string.Empty).ToLowerInvariant())
            {
                hash = unchecked(hash * 31 + letter);
            }

            return Palette[(hash & int.MaxValue) % Palette.Length];
        }

        /// <summary>The colour, faded, for backgrounds.</summary>
        public static Color Soft(string tag, float alpha)
        {
            var color = For(tag);
            color.a = alpha;
            return color;
        }
    }
}
