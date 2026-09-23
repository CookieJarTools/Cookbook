using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using Assembly = UnityEditor.Compilation.Assembly;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// What a Code Reference source covers: the scripts in it, and the assembly each is compiled
    /// into. Worked out from Unity's own list of assemblies and their scripts, so a folder or
    /// assembly under <c>Packages</c> works as well as one under <c>Assets</c>.
    /// </summary>
    public static class CodeScope
    {
        public const string AssemblyExtension = ".asmdef";
        public const string ScriptExtension = ".cs";

        private static Dictionary<string, string> assemblyOfScript;
        private static Assembly[] assemblies;

        /// <summary>Whether an asset can be a source: an assembly definition, a folder, or a script.</summary>
        public static bool CanUse(string path)
        {
            return !string.IsNullOrEmpty(path) && (IsAssembly(path) || IsScript(path) || AssetDatabase.IsValidFolder(path));
        }

        public static bool IsAssembly(string path)
        {
            return path.EndsWith(AssemblyExtension, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsScript(string path)
        {
            return path.EndsWith(ScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>What to call the source: the assembly's name, the folder's or the script's.</summary>
        public static string Title(string path)
        {
            return IsAssembly(path) ? AssemblyName(path) ?? Path.GetFileNameWithoutExtension(path) : Path.GetFileNameWithoutExtension(path);
        }

        public static string Describe(string path)
        {
            return IsAssembly(path) ? "Assembly" : IsScript(path) ? "Script" : "Folder";
        }

        /// <summary>The scripts a source covers, as project paths.</summary>
        public static List<string> Scripts(string path)
        {
            if (IsScript(path))
            {
                return File.Exists(path) || AssemblyOf(path) != null ? new List<string> { path } : new List<string>();
            }

            if (IsAssembly(path))
            {
                var name = AssemblyName(path);
                return All().Where(a => a.name == name).SelectMany(a => a.sourceFiles).ToList();
            }

            var folder = path.TrimEnd('/') + "/";
            return All().SelectMany(a => a.sourceFiles).Where(f => f.StartsWith(folder, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>The name of the assembly a script is compiled into, or null.</summary>
        public static string AssemblyOf(string script)
        {
            if (assemblyOfScript == null)
            {
                assemblyOfScript = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var assembly in All())
                {
                    foreach (var file in assembly.sourceFiles)
                    {
                        assemblyOfScript[file] = assembly.name;
                    }
                }
            }

            return assemblyOfScript.TryGetValue(script, out var name) ? name : null;
        }

        /// <summary>The "name" an assembly definition gives, read from its file.</summary>
        private static string AssemblyName(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
            var text = asset != null ? asset.text : File.Exists(path) ? File.ReadAllText(path) : null;
            if (text == null)
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(text, "\"name\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static Assembly[] All()
        {
            return assemblies ??= CompilationPipeline.GetAssemblies();
        }
    }
}
