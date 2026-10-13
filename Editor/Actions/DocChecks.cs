using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Actions
{
    /// <summary>
    /// Every check a guide can show, found by the name after <c>check:</c> in its link.
    /// <list type="bullet">
    /// <item>A method marked [CookbookCheck], by its id: <c>check:scene-lit</c>.</item>
    /// <item><c>check:asset/Assets/Data/Enemy.asset</c> — that asset exists.</item>
    /// <item><c>check:component/Camera</c> — an open scene has one.</item>
    /// <item><c>check:tag/Player</c> — something in an open scene is tagged with it.</item>
    /// <item><c>check:layer/Enemies</c> — the layer has been made.</item>
    /// <item><c>check:package/com.unity.inputsystem</c> — the package is installed.</item>
    /// </list>
    /// </summary>
    public static class DocChecks
    {
        public const string Scheme = "check:";
        public const string AssetPrefix = "asset/";
        public const string ComponentPrefix = "component/";
        public const string TagPrefix = "tag/";
        public const string LayerPrefix = "layer/";
        public const string PackagePrefix = "package/";

        private const string Manifest = "Packages/manifest.json";

        private static Dictionary<string, (MethodInfo method, CookbookCheckAttribute attribute)> marked;

        public static IEnumerable<DocCheck> Marked => Methods().Select(pair => ForMethod(pair.Key, pair.Value.method, pair.Value.attribute)).OrderBy(c => c.Name);

        public static bool IsCheck(string url)
        {
            return url != null && url.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The check a link names — with or without <c>check:</c> — or null when there is none.</summary>
        public static DocCheck Find(string url)
        {
            var id = Uri.UnescapeDataString((IsCheck(url) ? url.Substring(Scheme.Length) : url ?? string.Empty).Trim());
            var builtIn = BuiltIn(id);
            if (builtIn != null)
            {
                return builtIn;
            }

            return Methods().TryGetValue(id, out var found) ? ForMethod(id, found.method, found.attribute) : null;
        }

        private static DocCheck BuiltIn(string id)
        {
            if (Starts(id, AssetPrefix, out var path))
            {
                return new DocCheck(id, Path.GetFileName(path) + " exists", "Looks for " + path + ".", () => AssetExists(path));
            }

            if (Starts(id, ComponentPrefix, out var component))
            {
                return new DocCheck(id, "The scene has a " + ObjectNames.NicifyVariableName(component), "Looks in the open scenes for a " + component + ".", () => ComponentInScene(component));
            }

            if (Starts(id, TagPrefix, out var tag))
            {
                return new DocCheck(id, "Something is tagged " + tag, "Looks in the open scenes for a GameObject tagged " + tag + ".", () => Tagged(tag));
            }

            if (Starts(id, LayerPrefix, out var layer))
            {
                return new DocCheck(id, "The " + layer + " layer exists", "Looks for a layer called " + layer + " in the project's layers.", () => LayerExists(layer));
            }

            if (Starts(id, PackagePrefix, out var package))
            {
                return new DocCheck(id, package + " is installed", "Looks for " + package + " in the project's packages.", () => PackageInstalled(package));
            }

            return null;
        }

        private static bool Starts(string id, string prefix, out string rest)
        {
            var starts = id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            rest = starts ? id.Substring(prefix.Length) : null;
            return starts;
        }

        private static CheckOutcome AssetExists(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            return asset != null ? new CheckOutcome(true, "Found " + asset.name) : new CheckOutcome(false, "There's nothing at " + path + " yet");
        }

        private static CheckOutcome ComponentInScene(string typeName)
        {
            var type = TypeCache.GetTypesDerivedFrom<Component>().FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) || t.FullName == typeName);
            if (type == null)
            {
                return new CheckOutcome(false, "This project has no component called " + typeName);
            }

            var found = Object.FindFirstObjectByType(type, FindObjectsInactive.Include) as Component;
            return found != null ? new CheckOutcome(true, "Found on " + found.gameObject.name) : new CheckOutcome(false, "No " + type.Name + " in the open scenes yet");
        }

        private static CheckOutcome Tagged(string tag)
        {
            if (!InternalEditorUtility.tags.Contains(tag))
            {
                return new CheckOutcome(false, "There's no " + tag + " tag yet");
            }

            var found = GameObject.FindGameObjectsWithTag(tag);
            return found.Length > 0 ? new CheckOutcome(true, "Found " + found[0].name) : new CheckOutcome(false, "Nothing in the open scenes is tagged " + tag + " yet");
        }

        private static CheckOutcome LayerExists(string layer)
        {
            return LayerMask.NameToLayer(layer) >= 0 ? new CheckOutcome(true, "The layer is there") : new CheckOutcome(false, "There's no " + layer + " layer yet");
        }

        private static CheckOutcome PackageInstalled(string package)
        {
            var manifest = File.Exists(Manifest) ? File.ReadAllText(Manifest) : string.Empty;
            return manifest.Contains("\"" + package + "\"") ? new CheckOutcome(true, "Installed") : new CheckOutcome(false, package + " isn't installed yet");
        }

        private static Dictionary<string, (MethodInfo method, CookbookCheckAttribute attribute)> Methods()
        {
            if (marked != null)
            {
                return marked;
            }

            marked = new Dictionary<string, (MethodInfo method, CookbookCheckAttribute attribute)>(StringComparer.OrdinalIgnoreCase);
            foreach (var method in TypeCache.GetMethodsWithAttribute<CookbookCheckAttribute>().Where(Usable))
            {
                var attribute = method.GetCustomAttribute<CookbookCheckAttribute>();
                marked[string.IsNullOrWhiteSpace(attribute.Id) ? method.DeclaringType?.Name + "." + method.Name : attribute.Id] = (method, attribute);
            }

            return marked;
        }

        private static bool Usable(MethodInfo method)
        {
            var usable = method.IsStatic && method.GetParameters().Length == 0 && (method.ReturnType == typeof(bool) || method.ReturnType == typeof(string));
            if (!usable)
            {
                Debug.LogWarning($"CookBook: [CookbookCheck] on {method.DeclaringType?.Name}.{method.Name} is ignored — it must be static, take nothing, and return bool or string.");
            }

            return usable;
        }

        private static DocCheck ForMethod(string id, MethodInfo method, CookbookCheckAttribute attribute)
        {
            return new DocCheck(id, attribute.Name, attribute.Description, () => RunMethod(method));
        }

        /// <summary>True, or no words back, is a pass; false, or words about what is wrong, is not.</summary>
        private static CheckOutcome RunMethod(MethodInfo method)
        {
            var result = method.Invoke(null, null);
            if (result is bool passed)
            {
                return new CheckOutcome(passed, passed ? "Passed" : "Not yet");
            }

            var problem = result as string;
            return string.IsNullOrEmpty(problem) ? new CheckOutcome(true, "Passed") : new CheckOutcome(false, problem);
        }
    }
}
