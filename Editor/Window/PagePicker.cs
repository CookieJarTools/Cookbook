using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>Every page and folder, filed by folder, each with the link that goes to it — for the editor's Link… button.</summary>
    public static class PagePicker
    {
        public static List<(string name, object value)> Items(DocLibrary library)
        {
            return DocLibrary.Within(library.Root)
                .Select(page => (Place(page), (object)Link(page, library)))
                .ToList();
        }

        /// <summary>Folders and all, so the list files it in submenus: "Combat/Damage".</summary>
        private static string Place(DocPage page)
        {
            var trail = page.Trail().Select(p => p.Title).ToList();
            trail.Add(page.IsFolder ? page.Title + " (folder)" : page.Title);
            return string.Join("/", trail);
        }

        /// <summary>By title when only one page has it — easiest to read — and by path when not.</summary>
        private static string Link(DocPage page, DocLibrary library)
        {
            var name = library.Find(page.Title) == page ? page.Title : page.Reference;
            return name == page.Title ? "[[" + name + "]]" : "[[" + name + "|" + page.Title + "]]";
        }
    }
}
