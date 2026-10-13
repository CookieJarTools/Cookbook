using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// Jump to a page by typing a few letters of its name — in order, not necessarily together:
    /// "stgd" finds "Step-by-Step Guides". Names that start with what was typed, or have it all
    /// together, come first, and the CookBook's own pages before those found in the project.
    /// Arrow keys and Enter, or click; Escape to close.
    /// </summary>
    public sealed class QuickOpen : EditorWindow
    {
        private const string Prefix = "cookbook-quick";
        private const float Width = 520f;
        private const float Height = 360f;
        private const int MaxRows = 40;

        private readonly List<DocPage> shown = new List<DocPage>();

        private DocLibrary library;
        private Action<DocPage> chosen;
        private TextField field;
        private ScrollView list;
        private int highlighted;

        /// <param name="under">A place on screen to drop down from, centred under it.</param>
        public static void Show(Rect under, DocLibrary library, Action<DocPage> chosen)
        {
            var quick = CreateInstance<QuickOpen>();
            quick.library = library;
            quick.chosen = chosen;
            var anchor = new Rect(under.center.x - Width / 2f, under.y, Width, under.height);
            quick.ShowAsDropDown(anchor, new Vector2(Width, Height));
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            CookbookStyle.Apply(root);
            root.AddToClassList(Prefix);
            root.RegisterCallback<KeyDownEvent>(Key, TrickleDown.TrickleDown);

            field = new TextField();
            field.textEdition.placeholder = "Go to a page…";
            field.AddToClassList(Prefix + "__field");
            field.RegisterValueChangedCallback(_ => Fill());
            root.Add(field);

            list = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            list.AddToClassList(Prefix + "__list");
            root.Add(list);

            Fill();
            field.schedule.Execute(() => field.Focus());
        }

        private void Fill()
        {
            var typed = (field.value ?? string.Empty).Trim();
            shown.Clear();
            shown.AddRange(library.Everything.Where(p => !p.IsProjectRoot)
                .Select(p => (page: p, score: Score(p.Title, typed)))
                .Where(p => p.score > 0)
                .OrderByDescending(p => p.score)
                .ThenBy(p => p.page.IsProject)
                .ThenBy(p => p.page.Title)
                .Take(MaxRows)
                .Select(p => p.page));
            highlighted = 0;

            list.Clear();
            for (var i = 0; i < shown.Count; i++)
            {
                list.Add(Row(i));
            }

            if (shown.Count == 0)
            {
                list.Add(new Label("No page matches “" + typed + "”") { name = "none" });
            }
        }

        private VisualElement Row(int index)
        {
            var page = shown[index];
            var row = new ListRow(ListRow.Icon(page), page.Title, string.Join(" › ", page.Trail().Select(p => p.Title)), page.IsProject ? "Project" : page.IsCode ? "Code" : null, () => Pick(index));
            row.userData = index;
            row.AddToClassList(Prefix + "__row");
            row.EnableInClassList(Prefix + "__row--highlighted", index == highlighted);
            row.RegisterCallback<PointerEnterEvent>(_ => Highlight(index));
            return row;
        }

        /// <summary>Nought when the letters are not all in the name in order; more when it starts with them, most when they sit together.</summary>
        private static int Score(string title, string typed)
        {
            if (typed.Length == 0)
            {
                return 1;
            }

            var lower = title.ToLowerInvariant();
            var wanted = typed.ToLowerInvariant();
            if (lower.StartsWith(wanted))
            {
                return 300;
            }

            if (lower.Contains(wanted))
            {
                return 200;
            }

            var at = 0;
            foreach (var letter in wanted)
            {
                at = lower.IndexOf(letter, at);
                if (at < 0)
                {
                    return 0;
                }

                at++;
            }

            return 100 - Mathf.Min(99, at - wanted.Length);
        }

        private void Key(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    Highlight(Mathf.Min(shown.Count - 1, highlighted + 1));
                    break;
                case KeyCode.UpArrow:
                    Highlight(Mathf.Max(0, highlighted - 1));
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Pick(highlighted);
                    break;
                case KeyCode.Escape:
                    Close();
                    break;
                default:
                    return;
            }

            evt.StopImmediatePropagation();
        }

        private void Highlight(int index)
        {
            highlighted = index;
            foreach (var row in list.Children())
            {
                row.EnableInClassList(Prefix + "__row--highlighted", row.userData is int i && i == index);
                if (row.userData is int at && at == index)
                {
                    list.ScrollTo(row);
                }
            }
        }

        private void Pick(int index)
        {
            if (index < 0 || index >= shown.Count)
            {
                return;
            }

            var page = shown[index];
            var picked = chosen;
            Close();
            picked?.Invoke(page);
        }
    }
}
