using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>The icon a page or folder is shown with, wherever it is listed: the tree, cards, search results, Go to Page.</summary>
    public static class PageIcons
    {
        public static Texture For(DocPage page)
        {
            return EditorGUIUtility.IconContent(Name(page)).image;
        }

        private static string Name(DocPage page)
        {
            if (page.IsProjectRoot)
            {
                return "Project";
            }

            if (!page.IsCode)
            {
                return page.IsFolder ? "Folder Icon" : "TextAsset Icon";
            }

            if (page.IsCodeRoot || !page.IsFolder)
            {
                return "cs Script Icon";
            }

            return page.Parent != null && page.Parent.IsCodeRoot ? "AssemblyDefinitionAsset Icon" : "Folder Icon";
        }
    }
}
