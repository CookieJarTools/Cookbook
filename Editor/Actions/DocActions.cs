using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Actions
{
    /// <summary>
    /// Every action a page can run, found by the name after <c>action:</c> in its link.
    /// <list type="bullet">
    /// <item>A method marked [CookbookAction], by its id: <c>action:enemy-data</c>.</item>
    /// <item><c>action:create/EnemyData</c> — a new asset of that ScriptableObject type; the reader
    /// chooses where, or add <c>?at=Assets/Data/Enemy.asset</c> to decide for them.</item>
    /// <item><c>action:menu/Window/General/Console</c> — runs that menu item.</item>
    /// <item><c>action:open/Assets/Scenes/Main.unity</c> — opens an asset or scene.</item>
    /// <item><c>action:select/Assets/Data</c> — selects an asset and shows it in the Project window.</item>
    /// </list>
    /// Only methods someone marked, and these, can be run — a page cannot run anything else.
    /// </summary>
    public static class DocActions
    {
        public const string Scheme = "action:";
        public const string CreatePrefix = "create/";
        public const string MenuPrefix = "menu/";
        public const string OpenPrefix = "open/";
        public const string SelectPrefix = "select/";

        private const string AtQuery = "?at=";

        private static Dictionary<string, (MethodInfo method, CookbookActionAttribute attribute)> marked;

        /// <summary>The methods marked [CookbookAction], by id, in name order.</summary>
        public static IEnumerable<DocAction> Marked => Methods().Select(pair => ForMethod(pair.Key, pair.Value.method, pair.Value.attribute)).OrderBy(a => a.Name);

        /// <summary>ScriptableObject types from the project's own code, for "create" actions.</summary>
        public static IEnumerable<Type> Creatable => TypeCache.GetTypesDerivedFrom<ScriptableObject>().Where(IsCreatable).OrderBy(t => t.Name);

        public static bool IsAction(string url)
        {
            return url != null && url.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The action a link names — with or without <c>action:</c> — or null when there is none.</summary>
        public static DocAction Find(string url)
        {
            var id = IsAction(url) ? url.Substring(Scheme.Length) : url ?? string.Empty;
            id = Uri.UnescapeDataString(id.Trim());
            if (id.StartsWith(CreatePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Create(id.Substring(CreatePrefix.Length));
            }

            if (id.StartsWith(MenuPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Menu(id.Substring(MenuPrefix.Length));
            }

            if (id.StartsWith(OpenPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Open(id.Substring(OpenPrefix.Length));
            }

            if (id.StartsWith(SelectPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Select(id.Substring(SelectPrefix.Length));
            }

            return Methods().TryGetValue(id, out var found) ? ForMethod(id, found.method, found.attribute) : null;
        }

        /// <summary>Selects what an action made and shows it in the Project window.</summary>
        public static void Show(Object made)
        {
            if (made == null)
            {
                return;
            }

            Selection.activeObject = made;
            EditorGUIUtility.PingObject(made);
        }

        private static Dictionary<string, (MethodInfo method, CookbookActionAttribute attribute)> Methods()
        {
            if (marked != null)
            {
                return marked;
            }

            marked = new Dictionary<string, (MethodInfo method, CookbookActionAttribute attribute)>(StringComparer.OrdinalIgnoreCase);
            foreach (var method in TypeCache.GetMethodsWithAttribute<CookbookActionAttribute>().Where(Usable))
            {
                var attribute = method.GetCustomAttribute<CookbookActionAttribute>();
                marked[string.IsNullOrWhiteSpace(attribute.Id) ? method.DeclaringType?.Name + "." + method.Name : attribute.Id] = (method, attribute);
            }

            return marked;
        }

        /// <summary>Static, and taking nothing or just the path to save at. Anything else is pointed out once and left out.</summary>
        private static bool Usable(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var usable = method.IsStatic && (parameters.Length == 0 || (parameters.Length == 1 && parameters[0].ParameterType == typeof(string)));
            if (!usable)
            {
                Debug.LogWarning($"CookBook: [CookbookAction] on {method.DeclaringType?.Name}.{method.Name} is ignored — it must be static, and take nothing or one string path.");
            }

            return usable;
        }

        private static DocAction ForMethod(string id, MethodInfo method, CookbookActionAttribute attribute)
        {
            return new DocAction(id, attribute.Name, attribute.Description, () => RunMethod(method, attribute));
        }

        private static ActionOutcome RunMethod(MethodInfo method, CookbookActionAttribute attribute)
        {
            var asksWhere = method.GetParameters().Length == 1;
            var path = asksWhere ? AskWhere(attribute.DefaultName ?? attribute.Name, attribute.Extension, attribute.DefaultFolder) : null;
            if (asksWhere && path == null)
            {
                return ActionOutcome.Cancel();
            }

            var result = method.Invoke(null, asksWhere ? new object[] { path } : null);
            var made = result as Object;
            Show(made);
            return ActionOutcome.Finished(made);
        }

        /// <summary>The save dialog, inside the project. Null when the reader cancels.</summary>
        private static string AskWhere(string name, string extension, string folder)
        {
            var path = EditorUtility.SaveFilePanelInProject("Where should it go?", name, string.IsNullOrEmpty(extension) ? "asset" : extension, "Choose where to save it.", string.IsNullOrEmpty(folder) ? "Assets" : folder);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        private static DocAction Create(string spec)
        {
            var at = spec.IndexOf(AtQuery, StringComparison.OrdinalIgnoreCase);
            var typeName = at < 0 ? spec : spec.Substring(0, at);
            var fixedPath = at < 0 ? null : spec.Substring(at + AtQuery.Length);
            var type = Creatable.FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) || t.FullName == typeName);
            if (type == null)
            {
                return null;
            }

            var name = ObjectNames.NicifyVariableName(type.Name);
            var description = fixedPath == null ? "Makes a new " + name + " — you choose where it goes." : "Makes a new " + name + " at " + fixedPath + ".";
            return new DocAction(CreatePrefix + spec, "Create " + name, description, () => CreateAsset(type, fixedPath));
        }

        private static ActionOutcome CreateAsset(Type type, string fixedPath)
        {
            var path = fixedPath ?? AskWhere("New " + ObjectNames.NicifyVariableName(type.Name), "asset", null);
            if (path == null)
            {
                return ActionOutcome.Cancel();
            }

            var folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            Show(asset);
            return ActionOutcome.Finished(asset);
        }

        private static DocAction Menu(string item)
        {
            var name = item.Substring(item.LastIndexOf('/') + 1);
            return new DocAction(MenuPrefix + item, name, "Runs " + item.Replace("/", " › ") + " from the menu.", () => EditorApplication.ExecuteMenuItem(item) ? ActionOutcome.Finished(null) : ActionOutcome.Failed("There is no menu item " + item));
        }

        private static DocAction Open(string path)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            return new DocAction(OpenPrefix + path, "Open " + name, "Opens " + path + ".", () => OpenAsset(path));
        }

        private static ActionOutcome OpenAsset(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                return ActionOutcome.Failed("Nothing at " + path);
            }

            if (asset is SceneAsset && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return ActionOutcome.Cancel();
            }

            AssetDatabase.OpenAsset(asset);
            return ActionOutcome.Finished(asset, "Opened " + asset.name);
        }

        private static DocAction Select(string path)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            return new DocAction(SelectPrefix + path, "Show " + name, "Selects " + path + " in the Project window.", () =>
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                Show(asset);
                return asset != null ? ActionOutcome.Finished(asset, "Selected " + asset.name) : ActionOutcome.Failed("Nothing at " + path);
            });
        }

        /// <summary>The project's own ScriptableObjects: not Unity's, not editor windows or inspectors, not abstract.</summary>
        private static bool IsCreatable(Type type)
        {
            var assembly = type.Assembly.GetName().Name;
            return !type.IsAbstract && !type.IsGenericType && !assembly.StartsWith("Unity") && !assembly.StartsWith("com.unity")
                && !typeof(EditorWindow).IsAssignableFrom(type) && !typeof(UnityEditor.Editor).IsAssignableFrom(type) && !IsSingleton(type) && type.Namespace?.StartsWith("UnityEditor") != true;
        }

        /// <summary>Settings kept by <c>ScriptableSingleton</c> are made by their own code, never by hand.</summary>
        private static bool IsSingleton(Type type)
        {
            for (var at = type.BaseType; at != null; at = at.BaseType)
            {
                if (at.IsGenericType && at.GetGenericTypeDefinition() == typeof(ScriptableSingleton<>))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
