using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// The tags on a page being written: chips with a cross to take one off, and a box to add one.
    /// While typing, tags already used elsewhere that match are offered underneath — click one, or
    /// press Enter to add what was typed.
    /// </summary>
    public sealed class TagEditor : VisualElement
    {
        private const string Prefix = "cookbook-tag-editor";
        private const int MaxSuggestions = 8;

        private readonly List<string> tags = new List<string>();
        private readonly VisualElement chips;
        private readonly TextField entry;
        private readonly VisualElement suggestions;

        private IEnumerable<string> known = Enumerable.Empty<string>();

        public TagEditor()
        {
            AddToClassList(Prefix);
            var label = new Label("Tags");
            label.AddToClassList(Prefix + "__label");
            Add(label);

            var box = new VisualElement();
            box.AddToClassList(Prefix + "__box");
            chips = new VisualElement();
            chips.AddToClassList(Prefix + "__chips");
            entry = new TextField { tooltip = "Type a tag and press Enter" };
            entry.textEdition.placeholder = "Add a tag…";
            entry.AddToClassList(Prefix + "__entry");
            entry.RegisterValueChangedCallback(evt => Suggest(evt.newValue));
            entry.RegisterCallback<KeyDownEvent>(Key, TrickleDown.TrickleDown);
            chips.Add(entry);
            box.Add(chips);

            suggestions = new VisualElement();
            suggestions.AddToClassList(Prefix + "__suggestions");
            box.Add(suggestions);
            Add(box);
        }

        public event Action Changed;

        public IReadOnlyList<string> Tags => tags;

        /// <param name="used">Every tag in the Cookbook, to offer while typing.</param>
        public void Show(IEnumerable<string> pageTags, IEnumerable<string> used)
        {
            known = used.ToList();
            tags.Clear();
            tags.AddRange(pageTags);
            entry.SetValueWithoutNotify(string.Empty);
            Draw();
            Suggest(string.Empty);
        }

        private void Key(KeyDownEvent evt)
        {
            var add = evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == ',';
            if (add)
            {
                AddTag(entry.value);
                evt.StopImmediatePropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Backspace && entry.value.Length == 0 && tags.Count > 0)
            {
                RemoveTag(tags[tags.Count - 1]);
            }
        }

        private void AddTag(string tag)
        {
            var clean = (tag ?? string.Empty).Trim().Trim(',').Trim();
            entry.SetValueWithoutNotify(string.Empty);
            Suggest(string.Empty);
            if (clean.Length == 0 || tags.Any(t => t.Equals(clean, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            tags.Add(clean);
            Draw();
            Changed?.Invoke();
            entry.Focus();
        }

        private void RemoveTag(string tag)
        {
            tags.Remove(tag);
            Draw();
            Changed?.Invoke();
        }

        private void Draw()
        {
            chips.Query<TagChip>().ForEach(chip => chip.RemoveFromHierarchy());
            for (var i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                chips.Insert(i, new TagChip(tag, null, -1, () => RemoveTag(tag)));
            }
        }

        private void Suggest(string typed)
        {
            suggestions.Clear();
            var text = (typed ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                suggestions.style.display = DisplayStyle.None;
                return;
            }

            var matches = known.Where(t => t.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 && !tags.Contains(t, StringComparer.OrdinalIgnoreCase)).Take(MaxSuggestions).ToList();
            foreach (var tag in matches)
            {
                suggestions.Add(new TagChip(tag, chip => AddTag(chip.Tag)));
            }

            suggestions.style.display = matches.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
