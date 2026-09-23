using System;
using System.Collections.Generic;
using CookieJarTools.Cookbook.Editor.Images.Gif;
using CookieJarTools.Cookbook.Editor.Linking;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Images
{
    /// <summary>
    /// Pictures for pages: files kept with the pages, read from the source and remembered until the
    /// file changes, or textures in the project named by their asset path. Web images are not
    /// fetched — a page is drawn from what is in the project.
    /// </summary>
    public static class DocImages
    {
        private static readonly Dictionary<string, (DateTime modified, Texture2D texture)> Loaded = new Dictionary<string, (DateTime, Texture2D)>();
        private static readonly Dictionary<string, (DateTime modified, GifClip clip)> Clips = new Dictionary<string, (DateTime, GifClip)>();

        /// <param name="url">As written in the page: relative to the page's folder, or an asset path.</param>
        public static Texture2D Load(DocLibrary library, DocPage page, string url)
        {
            if (string.IsNullOrEmpty(url) || DocPaths.IsWeb(url))
            {
                return null;
            }

            if (DocPaths.IsProjectAsset(url))
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(DocPaths.Unescape(url));
            }

            var path = DocPaths.Combine(page?.Directory ?? string.Empty, url);
            return path == null ? null : FromSource(page?.Source ?? library.Source, path);
        }

        /// <summary>Whether the picture is a GIF, which plays rather than sits still.</summary>
        public static bool IsAnimated(string url)
        {
            var path = DocPaths.SplitAnchor(url ?? string.Empty, out _);
            return path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A GIF, read into frames and remembered until the file changes; null when it cannot be found or read.</summary>
        public static GifClip LoadClip(DocLibrary library, DocPage page, string url)
        {
            if (string.IsNullOrEmpty(url) || DocPaths.IsWeb(url))
            {
                return null;
            }

            var asset = DocPaths.IsProjectAsset(url);
            var path = asset ? DocPaths.Unescape(url) : DocPaths.Combine(page?.Directory ?? string.Empty, url);
            var source = asset ? library.Project : page?.Source ?? library.Source;
            if (path == null || !source.Exists(path))
            {
                return null;
            }

            var modified = source.Modified(path);
            if (Clips.TryGetValue(path, out var known) && known.modified == modified)
            {
                return known.clip;
            }

            known.clip?.Destroy();
            var clip = GifReader.Read(source.ReadBytes(path));
            Clips[path] = (modified, clip);
            return clip;
        }

        private static Texture2D FromSource(IDocSource source, string path)
        {
            if (!source.Exists(path))
            {
                return null;
            }

            var modified = source.Modified(path);
            if (Loaded.TryGetValue(path, out var known) && known.modified == modified && known.texture != null)
            {
                return known.texture;
            }

            var texture = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave, name = path };
            if (!texture.LoadImage(source.ReadBytes(path)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            if (known.texture != null)
            {
                UnityEngine.Object.DestroyImmediate(known.texture);
            }

            Loaded[path] = (modified, texture);
            return texture;
        }
    }
}
