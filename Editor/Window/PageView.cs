using System;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Images;
using CookieJarTools.Cookbook.Editor.Index;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// A page for reading: its title, tags and when it was last updated, then the page itself. A
    /// folder shows its banner, its intro, and a card for each page and folder inside it.
    /// <para>
    /// A Code Reference page shows what its type is in place of where its file is kept; the Code
    /// Reference's own page is where its sources are chosen.
    /// </para>
    /// </summary>
    public sealed class PageView : VisualElement
    {
        private const string Prefix = "cookbook-page";
        private const string HomeTitle = "CookBook";

        private readonly IDocNavigator navigator;

        private MarkdownView markdown;

        public PageView(IDocNavigator navigator)
        {
            this.navigator = navigator;
            AddToClassList(Prefix);
            markdown = new MarkdownView(navigator);
        }

        /// <summary>A tag clicked: search for it.</summary>
        public event Action<string> TagChosen;

        /// <summary>"It's still right" clicked on an out-of-date page: mark it up to date.</summary>
        public event Action<DocPage> StillRight;

        public DocPage Page { get; private set; }

        public void Show(DocPage page)
        {
            Page = page;
            Clear();
            if (page.IsFolder)
            {
                AddBanner(page);
            }

            Add(BlockBuilder.Classed(new Label(page.IsRoot ? HomeTitle : page.Title) { enableRichText = false }, Prefix + "__title"));
            AddDetails(page);
            AddNotices(page);
            // A new one for each page: one emptied and filled again can be left redrawing everything, every frame.
            markdown = new MarkdownView(navigator);
            markdown.Show(page, page.Body);
            Add(markdown);
            if (page.IsCodeRoot)
            {
                Add(new CodeSourcesView());
            }

            if (page.IsFolder)
            {
                Add(Cards(page));
            }

            Add(new PageConnections(navigator, page));
        }

        /// <summary>The headings to list beside the page: the second and third levels, in order.</summary>
        public System.Collections.Generic.List<(string text, string anchor, int level)> Headings()
        {
            return markdown.Query<Label>().Where(l => l.name != null && l.name.StartsWith(BlockBuilder.AnchorPrefix) && (l.ClassListContains("cookbook-h2") || l.ClassListContains("cookbook-h3")))
                .ToList()
                .Select(l => (Plain(l.text), l.name.Substring(BlockBuilder.AnchorPrefix.Length), l.ClassListContains("cookbook-h2") ? 2 : 3))
                .ToList();
        }

        private static string Plain(string rich)
        {
            return System.Text.RegularExpressions.Regex.Replace(rich ?? string.Empty, "<[^>]*>", string.Empty);
        }

        /// <summary>Under the title: a note when the code has changed since the page was written, and a guide's progress.</summary>
        private void AddNotices(DocPage page)
        {
            if (page.IsFolder || page.IsCode)
            {
                return;
            }

            var changed = DocStaleness.ChangedSince(navigator.Library, page);
            if (changed.Count > 0)
            {
                Add(new StaleBanner(changed, () => StillRight?.Invoke(page)));
            }

            if (GuideSteps.Of(page).Count > 0)
            {
                Add(new GuideProgressBar(page));
            }
        }

        public VisualElement Anchor(string anchor)
        {
            return markdown.Anchor(anchor);
        }

        private void AddBanner(DocPage folder)
        {
            var texture = DocImages.Load(navigator.Library, folder, folder.Header.Banner);
            if (texture == null)
            {
                return;
            }

            var banner = BlockBuilder.Classed(new VisualElement(), Prefix + "__banner");
            banner.style.backgroundImage = texture;
            Add(banner);
        }

        /// <summary>Tags as chips, and the date it was last updated.</summary>
        private void AddDetails(DocPage page)
        {
            var details = BlockBuilder.Classed(new VisualElement(), Prefix + "__details");
            foreach (var tag in page.Header.Tags)
            {
                details.Add(new TagChip(tag, chip => TagChosen?.Invoke(chip.Tag)));
            }

            if (!string.IsNullOrEmpty(page.Header.Updated))
            {
                details.Add(BlockBuilder.Classed(new Label("Updated " + page.Header.Updated), Prefix + "__updated"));
            }

            if (page.Code != null)
            {
                details.Add(new CodeFacts(navigator, page.Code));
                AddLastChange(page, details);
            }
            else if (!page.IsRoot && !page.IsProjectRoot && !page.IsCode)
            {
                details.Add(Origin(page));
                AddLastChange(page, details);
            }

            if (details.childCount > 0)
            {
                Add(details);
            }
        }

        /// <summary>Where the page is kept: the CookBook's folder, or its place among the project's assets. Click to show the file.</summary>
        private static Label Origin(DocPage page)
        {
            var where = page.IsProject ? page.FilePath : CookbookSettings.instance.Root + "/" + page.FilePath;
            var origin = BlockBuilder.Classed(new Label((page.IsProject ? "In the project · " : string.Empty) + where), Prefix + "__origin");
            origin.EnableInClassList(Prefix + "__origin--project", page.IsProject);
            origin.tooltip = page.IsProject ? "Found among the project's assets — click to show it" : "Kept in the CookBook's folder — click to show the file";
            origin.RegisterCallback<ClickEvent>(_ => ShowFile(page));
            return origin;
        }

        private static void ShowFile(DocPage page)
        {
            var asset = page.IsProject ? AssetDatabase.LoadMainAssetAtPath(page.FilePath) : null;
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                return;
            }

            EditorUtility.RevealInFinder(page.Source.LocalPath(page.FilePath));
        }

        /// <summary>Who last changed the file and when, from git, added once git answers — nothing when it is not in git.</summary>
        private void AddLastChange(DocPage page, VisualElement details)
        {
            var label = BlockBuilder.Classed(new Label(), Prefix + "__updated");
            label.style.display = DisplayStyle.None;
            details.Add(label);
            GitInfo.LastChange(page.Source.LocalPath(page.FilePath), (author, date) =>
            {
                label.text = "Changed by " + author + " · " + date;
                label.style.display = DisplayStyle.Flex;
            });
        }

        private VisualElement Cards(DocPage folder)
        {
            if (folder.Children.Count == 0)
            {
                var empty = folder.IsCode ? "No code chosen yet. Add an assembly definition, folder or script above." : "Nothing in here yet. Right-click the folder in the tree to add a page.";
                return BlockBuilder.Classed(new Label(empty), Prefix + "__empty");
            }

            var list = ListRow.List();
            list.AddToClassList(Prefix + "__children");
            var what = folder.IsCode ? "type" : "page";
            foreach (var child in folder.Children)
            {
                var count = child.IsFolder ? DocLibrary.Within(child).Count(p => !p.IsFolder) : -1;
                var trailing = count < 0 ? null : count == 1 ? "1 " + what : count + " " + what + "s";
                list.Add(new ListRow(ListRow.Icon(child), child.Title, HomeView.FirstLine(child.Body), trailing, () => navigator.Open(child, null)));
            }

            return list;
        }
    }
}
