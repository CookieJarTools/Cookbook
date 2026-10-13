using System;
using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Linking
{
    /// <summary>Paths inside the CookBook: a relative path from a page's folder made into one from the root.</summary>
    public static class DocPaths
    {
        /// <summary>"Combat" + "../Art/hit.png" is "Art/hit.png". Null when it climbs out of the root.</summary>
        public static string Combine(string folder, string relative)
        {
            var parts = new List<string>();
            var joined = string.IsNullOrEmpty(folder) ? relative : folder + "/" + relative;
            foreach (var part in Uri.UnescapeDataString(joined).Replace('\\', '/').Split('/'))
            {
                if (!Step(parts, part))
                {
                    return null;
                }
            }

            return string.Join("/", parts);
        }

        /// <summary>A path as written in a link, with %20 and the like turned back into what they stand for.</summary>
        public static string Unescape(string path)
        {
            return Uri.UnescapeDataString(path);
        }

        public static bool IsWeb(string url)
        {
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A path into the Unity project rather than the CookBook.</summary>
        public static bool IsProjectAsset(string url)
        {
            return url.StartsWith("Assets/", StringComparison.Ordinal) || url.StartsWith("Packages/", StringComparison.Ordinal);
        }

        /// <summary>Splits "Page.md#setup" into "Page.md" and "setup".</summary>
        public static string SplitAnchor(string url, out string anchor)
        {
            var hash = url.IndexOf('#');
            anchor = hash < 0 ? null : url.Substring(hash + 1);
            return hash < 0 ? url : url.Substring(0, hash);
        }

        private static bool Step(List<string> parts, string part)
        {
            if (part.Length == 0 || part == ".")
            {
                return true;
            }

            if (part != "..")
            {
                parts.Add(part);
                return true;
            }

            if (parts.Count == 0)
            {
                return false;
            }

            parts.RemoveAt(parts.Count - 1);
            return true;
        }
    }
}
