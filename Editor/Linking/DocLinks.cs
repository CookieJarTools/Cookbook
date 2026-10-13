using System;
using CookieJarTools.Cookbook.Editor.Actions;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Linking
{
    /// <summary>
    /// Where a link in a page goes:
    /// <list type="bullet">
    /// <item><c>[[Page]]</c>, or a page's path relative to this one (<c>../Art/Style.md</c>) — that page;</item>
    /// <item><c>#heading</c> — that heading on this page, and <c>Page.md#heading</c> on another;</item>
    /// <item><c>Assets/…</c> — that asset, selected and shown in the Project window;</item>
    /// <item>a web address — the browser.</item>
    /// </list>
    /// </summary>
    public static class DocLinks
    {
        /// <summary>Whether the link goes somewhere: false for a page that does not exist or an asset that is not there.</summary>
        public static bool Works(string url, DocLibrary library, DocPage from)
        {
            if (string.IsNullOrEmpty(url) || url.StartsWith("#") || DocPaths.IsWeb(url))
            {
                return true;
            }

            if (DocActions.IsAction(url))
            {
                return DocActions.Find(url) != null;
            }

            if (DocChecks.IsCheck(url))
            {
                return DocChecks.Find(url) != null;
            }

            if (DocPaths.IsProjectAsset(url))
            {
                return AssetDatabase.LoadMainAssetAtPath(DocPaths.Unescape(DocPaths.SplitAnchor(url, out _))) != null;
            }

            return Page(url, library, from, out _) != null;
        }

        public static void Open(string url, IDocNavigator navigator, DocPage from)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            if (url.StartsWith("#"))
            {
                navigator.ScrollTo(url.Substring(1));
                return;
            }

            if (DocPaths.IsWeb(url))
            {
                Application.OpenURL(url);
                return;
            }

            if (DocActions.IsAction(url))
            {
                RunAction(url);
                return;
            }

            if (DocChecks.IsCheck(url))
            {
                RunCheck(url);
                return;
            }

            if (DocPaths.IsProjectAsset(url))
            {
                ShowAsset(DocPaths.Unescape(DocPaths.SplitAnchor(url, out _)));
                return;
            }

            OpenPage(url, navigator, from);
        }

        /// <summary>The page a link names, or null; with the heading it points at, if any.</summary>
        public static DocPage Page(string url, DocLibrary library, DocPage from, out string anchor)
        {
            var target = DocPaths.SplitAnchor(url, out anchor);
            if (target.StartsWith(PageLinkParser.Scheme, StringComparison.Ordinal))
            {
                return library.Find(target.Substring(PageLinkParser.Scheme.Length));
            }

            var relative = DocPaths.Combine(from?.Directory ?? string.Empty, target);
            return (relative == null ? null : library.Find(relative)) ?? library.Find(target);
        }

        private static void OpenPage(string url, IDocNavigator navigator, DocPage from)
        {
            var page = Page(url, navigator.Library, from, out var anchor);
            if (page != null)
            {
                navigator.Open(page, anchor);
                return;
            }

            var name = DocPaths.SplitAnchor(url, out _);
            navigator.Missing(name.StartsWith(PageLinkParser.Scheme, StringComparison.Ordinal) ? name.Substring(PageLinkParser.Scheme.Length) : name);
        }

        /// <summary>An action linked from inside a sentence: run, with a note in the console if it did not work.</summary>
        private static void RunAction(string url)
        {
            var action = DocActions.Find(url);
            var outcome = action?.Run();
            if (action == null || (outcome != null && !outcome.Done && !outcome.Cancelled))
            {
                Debug.LogWarning("CookBook: " + (action == null ? "no action called " + url : outcome.Message));
            }
        }

        /// <summary>A check linked from inside a sentence: what it found goes to the console.</summary>
        private static void RunCheck(string url)
        {
            var check = DocChecks.Find(url);
            var outcome = check?.Run();
            Debug.Log("CookBook: " + (check == null ? "no check called " + url : check.Name + " — " + (outcome.Passed ? "✓ " : "✗ ") + outcome.Message));
        }

        private static void ShowAsset(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                Debug.LogWarning("CookBook: nothing at " + path);
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
