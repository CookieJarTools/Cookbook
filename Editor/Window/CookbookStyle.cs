using CookieJarTools.Frosting.Editor.Inspector;
using UnityEditor;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>Frosting's look, and the CookBook's own sheet on top — found by name, so the package can live anywhere.</summary>
    public static class CookbookStyle
    {
        private const string StyleSheetName = "Cookbook";
        private const string RootClass = "cookbook-root";

        public static void Apply(VisualElement root)
        {
            FrostingStyle.Apply(root);
            root.AddToClassList(RootClass);

            var found = AssetDatabase.FindAssets($"t:StyleSheet {StyleSheetName}");
            foreach (var guid in found)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/" + StyleSheetName + ".uss"))
                {
                    root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(path));
                    return;
                }
            }
        }
    }
}
