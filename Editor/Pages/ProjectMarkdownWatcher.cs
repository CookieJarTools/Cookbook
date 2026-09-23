using System;
using System.Linq;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>Hears from Unity's import when Markdown files in the project are added, changed, moved or deleted.</summary>
    public sealed class ProjectMarkdownWatcher : AssetPostprocessor
    {
        public static event Action Changed;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(p => p.EndsWith(DocPage.Extension, StringComparison.OrdinalIgnoreCase)))
            {
                EditorApplication.delayCall += () => Changed?.Invoke();
            }
        }
    }
}
