using System;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Index;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Window;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Health
{
    /// <summary>
    /// Docs health: everything in <see cref="HealthReport"/>, a list for each kind of problem, each
    /// row going to where it can be fixed — and, to finish, the project's code no page is about yet.
    /// </summary>
    public sealed class HealthView : VisualElement
    {
        private const string Prefix = "cookbook-health";
        private const int GapRows = 40;

        private readonly IDocNavigator navigator;

        public HealthView(IDocNavigator navigator)
        {
            this.navigator = navigator;
            AddToClassList(Prefix);
        }

        public void Show()
        {
            Clear();
            var report = new HealthReport(navigator.Library);
            Add(Classed(new Label("Docs health"), Prefix + "__title"));
            Add(Classed(new Label(report.Problems == 0 ? "Everything looks good — every link goes somewhere and no page is behind its code." : report.Problems + (report.Problems == 1 ? " thing" : " things") + " to look at."), Prefix + "__summary"));

            AddSection("Might be out of date", "Pages whose code has changed since they were written", report.Stale.Select(StaleRow));
            AddSection("Links that go nowhere", "A page, asset or heading that isn't there", report.BrokenLinks.Select(l => LinkRow(l, "cookbook-health__kind--link")));
            AddSection("Missing pictures", "Pictures a page shows that can't be found", report.MissingPictures.Select(l => LinkRow(l, "cookbook-health__kind--picture")));
            AddSection("Steps the project doesn't have", "Guide actions and checks with no code behind them", report.MissingSteps.Select(l => LinkRow(l, "cookbook-health__kind--step")));
            AddSection("Pictures nothing shows", "In the CookBook's images folders, but not in any page", report.UnusedPictures.Select(PictureRow));
            AddSection("Notes for code that's gone", "Written for a type that was renamed, moved or removed — copy what's worth keeping to its new page", report.LostNotes.Select(NoteRow));
            Add(Undocumented());
            AddCodeGaps(report);
        }

        private void AddSection(string title, string about, System.Collections.Generic.IEnumerable<ListRow> rows)
        {
            var list = rows.ToList();
            if (list.Count == 0)
            {
                return;
            }

            Add(Head(title, list.Count, about));
            var frame = ListRow.List();
            list.ForEach(frame.Add);
            Add(frame);
        }

        private VisualElement Head(string title, int count, string about)
        {
            var head = Classed(new VisualElement(), Prefix + "__head");
            var words = Classed(new VisualElement(), Prefix + "__head-words");
            var line = Classed(new VisualElement(), Prefix + "__head-line");
            line.Add(Classed(new Label(title), Prefix + "__heading"));
            line.Add(Classed(new Label(count < 0 ? string.Empty : count.ToString()), Prefix + "__count"));
            words.Add(line);
            words.Add(Classed(new Label(about), Prefix + "__about"));
            head.Add(words);
            return head;
        }

        private ListRow StaleRow((Pages.DocPage page, System.Collections.Generic.List<(string script, DateTime changed)> scripts) stale)
        {
            var detail = string.Join(", ", stale.scripts.Select(s => Path.GetFileName(s.script))) + " changed";
            return new ListRow(Dot(Prefix + "__kind--stale"), stale.page.Title, detail, stale.scripts[0].changed.ToString("d MMM"), () => navigator.Open(stale.page, null));
        }

        private ListRow LinkRow(PageLink link, string kind)
        {
            return new ListRow(Dot(kind), link.From.Title, link.Url, null, () => navigator.Open(link.From, null));
        }

        private ListRow PictureRow(string path)
        {
            var local = navigator.Library.Source.LocalPath(path);
            return new ListRow(Dot(Prefix + "__kind--unused"), Path.GetFileName(path), Path.GetDirectoryName(path)?.Replace('\\', '/'), "Show", () => EditorUtility.RevealInFinder(local));
        }

        /// <summary>The project's code with no page: shown whether or not anything else needs doing, since it is a to-do list rather than a fault.</summary>
        private VisualElement Undocumented()
        {
            var section = new VisualElement();
            var code = UndocumentedCode.Find();
            var head = Head("Code with no page", code.Count, UndocumentedCode.Counted ? "Your components and ScriptableObjects no page is about — most used first" : "Your components and ScriptableObjects no page is about. Count uses to put the most used first.");
            var count = Classed(new Button(() => { UndocumentedCode.CountUses(); Show(); }) { text = UndocumentedCode.Counted ? "Count again" : "Count uses", tooltip = "Go through every prefab, scene and asset to see where each script is used" }, Prefix + "__button");
            head.Add(count);
            section.Add(head);
            if (code.Count == 0)
            {
                section.Add(Classed(new Label("Every component and ScriptableObject has a page."), Prefix + "__empty"));
                return section;
            }

            var frame = ListRow.List();
            foreach (var (type, script, uses) in code)
            {
                var trailing = uses < 0 ? null : uses == 0 ? "Unused" : "Used in " + uses;
                frame.Add(new ListRow(ScriptIcon(), ObjectNames.NicifyVariableName(type.Name), script, trailing, () => Ping(script)));
            }

            section.Add(frame);
            return section;
        }

        private ListRow NoteRow(string key)
        {
            var local = navigator.Library.Source.LocalPath(Reference.CodeNotes.PathOf(key));
            return new ListRow(Dot(Prefix + "__kind--note"), key, Reference.CodeNotes.PathOf(key), "Show", () => EditorUtility.RevealInFinder(local));
        }

        /// <summary>The Code Reference's types with parts nothing is written about, most first: a to-do list, not faults.</summary>
        private void AddCodeGaps(HealthReport report)
        {
            if (report.CodeGaps.Count == 0)
            {
                return;
            }

            Add(Head("Code Reference gaps", report.CodeGaps.Count, "Types in the Code Reference with parts that have no /// description — most missing first"));
            var frame = ListRow.List();
            foreach (var (type, missing) in report.CodeGaps.Take(GapRows))
            {
                var page = navigator.Library.Find(Reference.CodeLinks.Id(type.Key));
                frame.Add(new ListRow(ScriptIcon(), type.Title, type.Namespace, missing == 1 ? "1 missing" : missing + " missing", () => navigator.Open(page, null)));
            }

            Add(frame);
            if (report.CodeGaps.Count > GapRows)
            {
                Add(Classed(new Label("…and " + (report.CodeGaps.Count - GapRows) + " more."), Prefix + "__empty"));
            }
        }

        private static VisualElement Dot(string kind)
        {
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList(Prefix + "__dot");
            dot.AddToClassList(kind);
            return dot;
        }

        private static VisualElement ScriptIcon()
        {
            return new Image { image = EditorGUIUtility.IconContent("cs Script Icon").image, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
        }

        private static void Ping(string script)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(script);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
