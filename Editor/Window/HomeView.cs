using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// Where the CookBook opens: its name and what it is for, a search box, the tags to browse by,
    /// then lists — the sections (top folders, and what was found in the project), the guides and
    /// how far through each you are, what changed lately, the loose pages, and how healthy the
    /// docs are. The top folder's own text, if it has any, is the line under the name.
    /// </summary>
    public sealed class HomeView : VisualElement
    {
        private const string Prefix = "cookbook-home";
        private const int RecentCount = 6;
        private const string Welcome = "Guides, notes and how-tos for the project.";

        private readonly IDocNavigator navigator;

        public HomeView(IDocNavigator navigator)
        {
            this.navigator = navigator;
            AddToClassList(Prefix);
        }

        public event Action<string> TagChosen;

        public event Action<string> Searched;

        /// <summary>The docs health row clicked.</summary>
        public event Action HealthChosen;

        public void Show(DocLibrary library)
        {
            Clear();
            Add(Header(library));
            Add(SearchBox());

            var tags = library.Tags();
            if (tags.Count > 0)
            {
                Add(Section("Tags", tags.Count, TagCloud(tags)));
            }

            var folders = library.Root.Children.Where(c => c.IsFolder).ToList();
            if (library.ShowProject && library.ProjectRoot.Children.Count > 0)
            {
                folders.Add(library.ProjectRoot);
            }

            if (library.ShowCode)
            {
                folders.Add(library.CodeRoot);
            }

            if (folders.Count > 0)
            {
                Add(Section("Sections", folders.Count, Rows(folders.Select(SectionRow))));
            }

            var guides = library.Pages.Select(p => (page: p, progress: GuideSteps.Progress(p))).Where(g => g.progress.total > 0).ToList();
            if (guides.Count > 0)
            {
                Add(Section("Guides", guides.Count, Rows(guides.Select(g => GuideRow(g.page, g.progress)))));
            }

            var recent = library.Recent(RecentCount);
            if (recent.Count > 0)
            {
                Add(Section("Recently updated", -1, Rows(recent.Select(p => RecentRow(library, p)))));
            }

            var pages = library.Root.Children.Where(c => !c.IsFolder).ToList();
            if (pages.Count > 0)
            {
                Add(Section("Pages", pages.Count, Rows(pages.Select(PageRow))));
            }

            Add(Section("Docs health", -1, Rows(new[] { HealthRow(library) })));
        }

        /// <summary>A guide, and how far through it you are.</summary>
        private ListRow GuideRow(DocPage page, (int done, int total) progress)
        {
            var trailing = progress.done == progress.total ? "Done" : progress.done + " of " + progress.total + " done";
            var row = new ListRow(ListRow.Icon(false), page.Title, FirstLine(page.Body), trailing, () => navigator.Open(page, null));
            row.EnableInClassList("cookbook-row--complete", progress.done == progress.total);
            return row;
        }

        private ListRow HealthRow(DocLibrary library)
        {
            var problems = new Health.HealthReport(library).Problems;
            var detail = problems == 0 ? "Every link goes somewhere and no page is behind its code" : problems + (problems == 1 ? " thing" : " things") + " to look at";
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("cookbook-health__dot");
            dot.AddToClassList(problems == 0 ? "cookbook-health__kind--good" : "cookbook-health__kind--stale");
            return new ListRow(dot, problems == 0 ? "All good" : "Needs a look", detail, "Open", () => HealthChosen?.Invoke());
        }

        private VisualElement Header(DocLibrary library)
        {
            var header = Classed(new VisualElement(), Prefix + "__header");
            header.Add(Classed(new Label("CookBook"), Prefix + "__title"));

            var intro = library.Root.Body.Trim();
            if (intro.Length == 0)
            {
                header.Add(Classed(new Label(Welcome), Prefix + "__intro"));
            }
            else
            {
                var markdown = new MarkdownView(navigator);
                markdown.AddToClassList(Prefix + "__intro-markdown");
                markdown.Show(library.Root, intro);
                header.Add(markdown);
            }

            var pages = library.Pages.Count();
            var folders = DocLibrary.Within(library.Root).Count(p => p.IsFolder);
            var facts = PlayerSettings.productName + "  ·  " + Count(pages, "page") + "  ·  " + Count(folders, "folder") + "  ·  " + Count(library.Tags().Count, "tag");
            header.Add(Classed(new Label(facts), Prefix + "__facts"));
            return header;
        }

        private VisualElement SearchBox()
        {
            var box = Classed(new VisualElement(), Prefix + "__search");
            var icon = new Image { image = EditorGUIUtility.IconContent("Search Icon").image, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            box.Add(Classed(icon, Prefix + "__search-icon"));

            var field = new TextField { tooltip = "Search page and folder names, tags and text" };
            field.textEdition.placeholder = "Search pages, folders and tags…";
            field.AddToClassList(Prefix + "__search-field");
            field.RegisterCallback<KeyUpEvent>(_ => Typed(field));
            box.Add(field);
            return box;
        }

        /// <summary>Hands what was typed to the window's search, which takes over from there.</summary>
        private void Typed(TextField field)
        {
            if (field.value.Trim().Length > 0)
            {
                Searched?.Invoke(field.value);
            }
        }

        private VisualElement Section(string title, int count, VisualElement content)
        {
            var section = Classed(new VisualElement(), Prefix + "__section");
            var head = Classed(new VisualElement(), Prefix + "__section-head");
            head.Add(Classed(new Label(title), Prefix + "__heading"));
            if (count >= 0)
            {
                head.Add(Classed(new Label(count.ToString()), Prefix + "__heading-count"));
            }

            section.Add(head);
            section.Add(content);
            return section;
        }

        private VisualElement TagCloud(List<(string tag, int count)> tags)
        {
            var cloud = Classed(new VisualElement(), Prefix + "__tags");
            foreach (var (tag, count) in tags)
            {
                cloud.Add(new TagChip(tag, chip => TagChosen?.Invoke(chip.Tag), count));
            }

            return cloud;
        }

        private static VisualElement Rows(IEnumerable<ListRow> rows)
        {
            var list = ListRow.List();
            foreach (var row in rows)
            {
                list.Add(row);
            }

            return list;
        }

        /// <summary>A section: a tinted letter, its name, the opening words of its intro, how many pages.</summary>
        private ListRow SectionRow(DocPage folder)
        {
            var tint = folder.Header.Tags.Count > 0 ? folder.Header.Tags[0] : folder.Title;
            var count = DocLibrary.Within(folder).Count(p => !p.IsFolder);
            var about = folder.IsProjectRoot ? "Markdown kept among the project's assets" : folder.IsCodeRoot ? CodeAbout(count) : FirstLine(folder.Body);
            var trailing = folder.IsCodeRoot ? Count(count, "type") : Count(count, "page");
            return new ListRow(ListRow.Badge(folder.Title, tint), folder.Title, about, trailing, () => navigator.Open(folder, null));
        }

        /// <summary>Until some code is chosen for it, the Code Reference's row says how to start.</summary>
        private static string CodeAbout(int types)
        {
            return types == 0 ? "Made from your code's comments — choose which code to document" : "Made from your code's comments, kept up to date with it";
        }

        private ListRow RecentRow(DocLibrary library, DocPage page)
        {
            var trail = string.Join(" › ", page.Trail().Select(p => p.Title));
            return new ListRow(ListRow.Icon(false), page.Title, trail, When(page.Source.Modified(page.FilePath)), () => navigator.Open(page, null));
        }

        private ListRow PageRow(DocPage page)
        {
            return new ListRow(ListRow.Icon(false), page.Title, FirstLine(page.Body), null, () => navigator.Open(page, null));
        }

        /// <summary>The first line of words, with the Markdown taken out and HTML — comments and tags — skipped.</summary>
        public static string FirstLine(string markdown)
        {
            var text = CookbookMarkdown.PlainText(markdown).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0 && !l.StartsWith("<")) ?? string.Empty;
            return text.Length <= 110 ? text : text.Substring(0, 110).TrimEnd() + "…";
        }

        private static string When(DateTime time)
        {
            var days = (DateTime.Now.Date - time.Date).Days;
            if (days <= 0)
            {
                return "Today";
            }

            return days == 1 ? "Yesterday" : days < 14 ? days + " days ago" : time.ToString("d MMM yyyy");
        }

        private static string Count(int count, string what)
        {
            return count + " " + what + (count == 1 ? string.Empty : "s");
        }

        private static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
