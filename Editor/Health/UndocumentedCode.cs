using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Index;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Health
{
    /// <summary>
    /// The project's own components and ScriptableObjects — scripts under <c>Assets</c> — that no
    /// page is about: nothing marked [Documentation] on the class. How much each is used can be
    /// counted too: the prefabs, scenes and assets that use it. That means opening every one, so it
    /// is only done when asked, and remembered until the next script reload.
    /// </summary>
    public static class UndocumentedCode
    {
        private const string AssetsPrefix = "Assets/";

        private static Dictionary<string, int> uses;

        /// <summary>Whether uses have been counted this session.</summary>
        public static bool Counted => uses != null;

        public static List<(Type type, string script, int uses)> Find()
        {
            var documented = new HashSet<Type>(DocCodeIndex.All.Where(l => l.Member == null).Select(l => l.Type));
            var found = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Concat(TypeCache.GetTypesDerivedFrom<ScriptableObject>())
                .Where(t => DocCodeIndex.IsProjectType(t) && !t.IsAbstract && !t.IsGenericType && !documented.Contains(t) && !IsEditorType(t))
                .Select(t => (type: t, script: DocCodeIndex.ScriptOf(t)))
                .Where(t => t.script != null && t.script.StartsWith(AssetsPrefix, StringComparison.Ordinal))
                .Select(t => (t.type, t.script, uses: Uses(t.script)))
                .ToList();

            return Counted ? found.OrderByDescending(t => t.uses).ThenBy(t => t.script).ToList() : found.OrderBy(t => t.script).ToList();
        }

        /// <summary>
        /// Goes through every prefab, scene and asset, noting which scripts each uses. Shows its
        /// progress, and can be cancelled — then nothing is kept.
        /// </summary>
        public static void CountUses()
        {
            var paths = AssetDatabase.FindAssets("t:Prefab").Concat(AssetDatabase.FindAssets("t:Scene")).Concat(AssetDatabase.FindAssets("t:ScriptableObject"))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.StartsWith(AssetsPrefix, StringComparison.Ordinal))
                .Distinct()
                .ToList();
            var counted = new Dictionary<string, int>();
            try
            {
                for (var i = 0; i < paths.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("CookBook", "Counting where scripts are used… " + paths[i], i / (float)paths.Count))
                    {
                        return;
                    }

                    Tally(paths[i], counted);
                }

                uses = counted;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void Tally(string path, Dictionary<string, int> counted)
        {
            foreach (var script in AssetDatabase.GetDependencies(path, true).Where(d => d.EndsWith(".cs", StringComparison.Ordinal)))
            {
                counted[script] = counted.TryGetValue(script, out var count) ? count + 1 : 1;
            }
        }

        private static int Uses(string script)
        {
            return uses != null && uses.TryGetValue(script, out var count) ? count : -1;
        }

        private static bool IsEditorType(Type type)
        {
            return typeof(EditorWindow).IsAssignableFrom(type) || typeof(UnityEditor.Editor).IsAssignableFrom(type) || type.Assembly.GetName().Name.EndsWith("Editor", StringComparison.Ordinal);
        }
    }
}
