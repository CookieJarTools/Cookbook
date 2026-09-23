using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Index
{
    /// <summary>
    /// The pages about something selected in the editor, each with why:
    /// <list type="bullet">
    /// <item>a GameObject — the pages its components' code points at, and those mentioning its prefab;</item>
    /// <item>a script — the pages its class points at;</item>
    /// <item>any other asset — the pages its type points at, and those that link to it or show it.</item>
    /// </list>
    /// </summary>
    public static class DocRelated
    {
        public static List<(DocPage page, string why)> For(DocLibrary library, Object target)
        {
            var found = new List<(DocPage page, string why)>();
            if (target == null)
            {
                return found;
            }

            if (target is GameObject gameObject)
            {
                AddComponents(library, gameObject, found);
                AddAsset(library, PrefabPath(gameObject), found);
            }
            else
            {
                AddType(library, ClassOf(target), found);
                AddAsset(library, AssetDatabase.GetAssetPath(target), found);
            }

            return found.Where(f => library.Shown(f.page))
                .GroupBy(f => f.page)
                .Select(g => (g.Key, string.Join(" · ", g.Select(f => f.why).Distinct())))
                .ToList();
        }

        private static void AddComponents(DocLibrary library, GameObject gameObject, List<(DocPage, string)> found)
        {
            foreach (var component in gameObject.GetComponents<Component>().Where(c => c != null))
            {
                AddType(library, component.GetType(), found);
            }
        }

        private static void AddType(DocLibrary library, Type type, List<(DocPage, string)> found)
        {
            if (type == null)
            {
                return;
            }

            foreach (var link in DocCodeIndex.ForType(type))
            {
                var page = library.Find(link.Page);
                if (page != null)
                {
                    found.Add((page, "About " + link.Name));
                }
            }
        }

        private static void AddAsset(DocLibrary library, string path, List<(DocPage, string)> found)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var name = System.IO.Path.GetFileName(path);
            found.AddRange(DocLinkIndex.For(library).ToAsset(path).Select(page => (page, "Mentions " + name)));
        }

        /// <summary>A script's class; a ScriptableObject's or other asset's own type.</summary>
        private static Type ClassOf(Object target)
        {
            return target is MonoScript script ? script.GetClass() : target.GetType();
        }

        private static string PrefabPath(GameObject gameObject)
        {
            var path = AssetDatabase.GetAssetPath(gameObject);
            return string.IsNullOrEmpty(path) ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject) : path;
        }
    }
}
