using System;
using System.Collections.Generic;
using System.Linq;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// Arranges the Markdown found in the project as the project's own folders are — but only as
    /// deep as helps:
    /// <list type="bullet">
    /// <item><c>Assets</c> is not a level of its own: its folders sit at the top.</item>
    /// <item>A folder holding nothing but one other folder is merged with it:
    /// <c>TerrainMolder/Editor/Documentation</c>.</item>
    /// <item>A folder holding just one page is replaced by the page, so a tool's README sits
    /// straight under <c>CookieJarTools</c>.</item>
    /// </list>
    /// Installed packages go in a folder of their own, apart from anything under Assets.
    /// </summary>
    public sealed class ProjectTree
    {
        private const string AssetsRoot = "Assets";
        private const string PackagesRoot = "Packages";
        private const string PackagesTitle = "Installed packages";

        private static readonly string[] PlainNames = { "index", "readme", "package" };

        private readonly Dictionary<string, List<DocPage>> pages = new Dictionary<string, List<DocPage>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SortedSet<string>> folders = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Func<string, string, DocPage> makeFolder;

        /// <param name="makeFolder">Makes the folder page for a path, with the title to show.</param>
        public ProjectTree(IEnumerable<DocPage> found, Func<string, string, DocPage> makeFolder)
        {
            this.makeFolder = makeFolder;
            foreach (var page in found)
            {
                var folder = ProjectSource.Parent(page.Path);
                Of(pages, folder).Add(page);
                Record(folder);
            }
        }

        public void Build(DocPage root)
        {
            foreach (var page in PagesIn(AssetsRoot))
            {
                Attach(page, root);
            }

            foreach (var folder in FoldersIn(AssetsRoot))
            {
                AddFolder(folder, Last(folder), root);
            }

            if (folders.ContainsKey(string.Empty) && folders[string.Empty].Contains(PackagesRoot))
            {
                AddFolder(PackagesRoot, PackagesTitle, root);
            }
        }

        /// <summary>Notes each folder's place in the one above it, all the way up.</summary>
        private void Record(string folder)
        {
            for (var child = folder; child.Length > 0; child = ProjectSource.Parent(child))
            {
                Of(folders, ProjectSource.Parent(child)).Add(child);
            }
        }

        private void AddFolder(string path, string title, DocPage parent)
        {
            while (PagesIn(path).Count == 0 && FoldersIn(path).Count == 1)
            {
                path = FoldersIn(path).First();
                title += "/" + Last(path);
            }

            var own = PagesIn(path);
            var inner = FoldersIn(path);
            if (own.Count == 1 && inner.Count == 0)
            {
                Lift(own[0], title, parent);
                return;
            }

            var folder = makeFolder(path, title);
            Attach(folder, parent);
            foreach (var page in own)
            {
                Attach(page, folder);
            }

            foreach (var child in inner)
            {
                AddFolder(child, Last(child), folder);
            }
        }

        /// <summary>A lone page in place of its folder — named after the folder when the page itself is only "index", "README" or "PACKAGE".</summary>
        private static void Lift(DocPage page, string folderTitle, DocPage parent)
        {
            if (PlainNames.Contains(page.Title.ToLowerInvariant()))
            {
                page.DisplayTitle = folderTitle;
            }

            Attach(page, parent);
        }

        private static void Attach(DocPage page, DocPage parent)
        {
            page.Parent = parent;
            parent.Children.Add(page);
        }

        private List<DocPage> PagesIn(string folder)
        {
            return pages.TryGetValue(folder, out var found) ? found : new List<DocPage>();
        }

        private List<string> FoldersIn(string folder)
        {
            return folders.TryGetValue(folder, out var found) ? found.ToList() : new List<string>();
        }

        private static string Last(string path)
        {
            return path.Substring(path.LastIndexOf('/') + 1);
        }

        private static T Of<T>(Dictionary<string, T> map, string key) where T : new()
        {
            if (!map.TryGetValue(key, out var value))
            {
                map[key] = value = new T();
            }

            return value;
        }
    }
}
