using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;
using CookieJarTools.Cookbook.Editor.Window;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>Right-click an assembly definition, folder or script in the Project window to put it in the Code Reference, or take it out.</summary>
    public static class CodeReferenceMenu
    {
        private const string AddItem = "Assets/CookBook/Add to Code Reference";
        private const string RemoveItem = "Assets/CookBook/Remove from Code Reference";

        [MenuItem(AddItem, false, 2000)]
        private static void Add()
        {
            foreach (var path in Chosen().Where(p => !CookbookSettings.instance.HasCodeSource(p)))
            {
                CookbookSettings.instance.AddCodeSource(path);
            }

            var window = CookbookWindow.ShowWindow();
            window.Open(window.Library.CodeRoot, null);
        }

        [MenuItem(AddItem, true)]
        private static bool CanAdd()
        {
            return Chosen().Any(p => !CookbookSettings.instance.HasCodeSource(p));
        }

        [MenuItem(RemoveItem, false, 2001)]
        private static void Remove()
        {
            foreach (var entry in CookbookSettings.instance.CodeSources.Where(e => Chosen().Contains(e.Path)).ToList())
            {
                CookbookSettings.instance.RemoveCodeSource(entry);
            }
        }

        [MenuItem(RemoveItem, true)]
        private static bool CanRemove()
        {
            return Chosen().Any(CookbookSettings.instance.HasCodeSource);
        }

        private static string[] Chosen()
        {
            return Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(CodeScope.CanUse).ToArray();
        }
    }
}
