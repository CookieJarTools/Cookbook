using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEditor.Compilation;

namespace CookieJarTools.Cookbook.Editor.Index
{
    /// <summary>
    /// Every [Documentation] in the project's code — on classes, fields and methods — and the
    /// script each is written in. Found once per script reload, since only a reload can change them.
    /// </summary>
    public static class DocCodeIndex
    {
        private const string ScriptExtension = ".cs";

        private static readonly Dictionary<Type, string> Scripts = new Dictionary<Type, string>();

        private static List<CodeLink> all;
        private static Dictionary<Type, string> projectScripts;
        private static HashSet<string> projectAssemblies;

        public static IReadOnlyList<CodeLink> All => all ??= Gather();

        /// <summary>The code that names this page.</summary>
        public static IEnumerable<CodeLink> For(DocLibrary library, DocPage page)
        {
            return All.Where(link => library.Find(link.Page) == page);
        }

        /// <summary>The [Documentation] on a type and on its fields and methods, base types included.</summary>
        public static IEnumerable<CodeLink> ForType(Type type)
        {
            return All.Where(link => link.Type.IsAssignableFrom(type));
        }

        /// <summary>The script a type is written in, relative to the project; null when it cannot be found.</summary>
        public static string ScriptOf(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (ProjectScripts().TryGetValue(type, out var known))
            {
                return known;
            }

            if (!Scripts.TryGetValue(type, out var path))
            {
                Scripts[type] = path = FindScript(type);
            }

            return path;
        }

        /// <summary>Whether a type is the project's own: built from scripts under <c>Assets</c>, not a package's or Unity's.</summary>
        public static bool IsProjectType(Type type)
        {
            if (projectAssemblies == null)
            {
                projectAssemblies = new HashSet<string>(CompilationPipeline.GetAssemblies()
                    .Where(a => a.sourceFiles.Any(f => f.StartsWith("Assets/", StringComparison.Ordinal)))
                    .Select(a => a.name));
            }

            return type != null && projectAssemblies.Contains(type.Assembly.GetName().Name);
        }

        /// <summary>
        /// Every script under <c>Assets</c> and the class it holds, found in one go — much quicker
        /// than asking about each type in turn.
        /// </summary>
        private static Dictionary<Type, string> ProjectScripts()
        {
            if (projectScripts != null)
            {
                return projectScripts;
            }

            projectScripts = new Dictionary<Type, string>();
            foreach (var path in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var type = AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass();
                if (type != null)
                {
                    projectScripts[type] = path;
                }
            }

            return projectScripts;
        }

        private static List<CodeLink> Gather()
        {
            var links = new List<CodeLink>();
            links.AddRange(TypeCache.GetTypesWithAttribute<DocumentationAttribute>().Select(t => new CodeLink(t.GetCustomAttribute<DocumentationAttribute>().Page, t, null)));
            links.AddRange(TypeCache.GetFieldsWithAttribute<DocumentationAttribute>().Select(f => new CodeLink(f.GetCustomAttribute<DocumentationAttribute>().Page, f.DeclaringType, f.Name)));
            links.AddRange(TypeCache.GetMethodsWithAttribute<DocumentationAttribute>().Select(m => new CodeLink(m.GetCustomAttribute<DocumentationAttribute>().Page, m.DeclaringType, m.Name)));
            return links;
        }

        /// <summary>By Unity's own record of which script holds which class; failing that, a file named after the type.</summary>
        private static string FindScript(Type type)
        {
            var named = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript " + type.Name))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    return path;
                }

                if (Path.GetFileNameWithoutExtension(path) == type.Name && path.EndsWith(ScriptExtension))
                {
                    named.Add(path);
                }
            }

            return named.FirstOrDefault();
        }
    }
}
