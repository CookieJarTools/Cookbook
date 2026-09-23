using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// The tags above the page tree, to filter by. Click tags to show only the pages that have all
    /// of them; click again to let go; Clear lets go of them all.
    /// <para>
    /// With many tags, only the most used are shown — and any picked, so a filter never hides —
    /// with a box to find a tag by name and a link to show them all. A long list scrolls rather
    /// than pushing the tree down.
    /// </para>
    /// </summary>
    public sealed class TagFilterBar : VisualElement
    {
        private const string Prefix = "cookbook-tags";
        private const int ShownAtFirst = 8;

        private readonly ScrollView chips;
        private readonly Button clear;
        private readonly TextField find;
        private readonly Button more;
        private readonly HashSet<string> picked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private List<(string tag, int count)> all = new List<(string, int)>();
        private bool showingAll;

        public TagFilterBar()
        {
            AddToClassList(Prefix);
            var head = new VisualElement();
            head.AddToClassList(Prefix + "__head");
            head.Add(new Label("Filter by tag") { name = "title" });
            clear = new Button(Clear) { text = "Clear", tooltip = "Show every page again" };
            clear.AddToClassList(Prefix + "__clear");
            head.Add(clear);
            Add(head);

            find = new TextField { tooltip = "Type part of a tag's name" };
            find.textEdition.placeholder = "Find a tag…";
            find.AddToClassList(Prefix + "__find");
            find.RegisterValueChangedCallback(_ => Draw());
            Add(find);

            chips = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            chips.AddToClassList(Prefix + "__chips");
            chips.contentContainer.AddToClassList(Prefix + "__chips-content");
            Add(chips);

            more = new Button(ToggleAll);
            more.AddToClassList(Prefix + "__more");
            Add(more);
        }

        /// <summary>The tags picked changed.</summary>
        public event Action<IReadOnlyCollection<string>> Changed;

        public IReadOnlyCollection<string> Picked => picked;

        public void Show(List<(string tag, int count)> tags)
        {
            all = tags;
            picked.RemoveWhere(t => tags.All(known => !known.tag.Equals(t, StringComparison.OrdinalIgnoreCase)));
            style.display = tags.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            find.style.display = tags.Count > ShownAtFirst ? DisplayStyle.Flex : DisplayStyle.None;
            Draw();
        }

        /// <summary>Picks a tag from elsewhere — a chip on a page — as if clicked here.</summary>
        public void Pick(string tag)
        {
            if (!picked.Add(tag))
            {
                return;
            }

            Refresh();
        }

        /// <summary>Puts back tags picked before — after a reload — without reporting a change.</summary>
        public void Set(IEnumerable<string> tags)
        {
            picked.Clear();
            picked.UnionWith(tags);
            Refresh(false);
        }

        /// <summary>
        /// The chips to show: those matching what is typed in the find box; otherwise the most used
        /// and every picked one, or all of them when asked.
        /// </summary>
        private void Draw()
        {
            var typed = (find.value ?? string.Empty).Trim();
            var matching = all.Where(t => typed.Length == 0 || t.tag.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var cut = typed.Length == 0 && !showingAll;
            var shown = cut ? matching.Where((t, index) => index < ShownAtFirst || picked.Contains(t.tag)).ToList() : matching;

            chips.Clear();
            foreach (var (tag, count) in shown)
            {
                chips.Add(new TagChip(tag, Toggle, count) { Picked = picked.Contains(tag) });
            }

            if (shown.Count == 0)
            {
                chips.Add(new Label("No tag matches “" + typed + "”") { name = "none" });
            }

            var hidden = all.Count - shown.Count;
            more.text = showingAll ? "Show fewer" : "Show all " + all.Count;
            more.style.display = typed.Length == 0 && (showingAll || hidden > 0) ? DisplayStyle.Flex : DisplayStyle.None;
            ShowClear();
        }

        private void ToggleAll()
        {
            showingAll = !showingAll;
            Draw();
        }

        private void Toggle(TagChip chip)
        {
            if (!picked.Remove(chip.Tag))
            {
                picked.Add(chip.Tag);
            }

            Refresh();
        }

        private void Clear()
        {
            picked.Clear();
            Refresh();
        }

        private void Refresh(bool report = true)
        {
            chips.Query<TagChip>().ForEach(c => c.Picked = picked.Contains(c.Tag));
            ShowClear();
            if (report)
            {
                Changed?.Invoke(picked);
            }
        }

        private void ShowClear()
        {
            clear.style.visibility = picked.Count > 0 ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
