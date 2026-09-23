using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;
using CookieJarTools.Cookbook.Editor.Window;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Search
{
    /// <summary>
    /// What a search found: each page with the folders it is in and the words around the match.
    /// Click one to open it. The Code Reference's pages can be left out of the results — each
    /// person's own choice, remembered.
    /// </summary>
    public sealed class SearchResultsView : VisualElement
    {
        private const string Prefix = "cookbook-results";
        private const string IncludeCodeKey = "CookieJar.Cookbook.SearchCode";

        private string lastQuery;
        private IReadOnlyCollection<string> lastTags;
        private List<SearchHit> lastHits;

        public SearchResultsView()
        {
            AddToClassList(Prefix);
        }

        public event Action<DocPage> Chosen;

        /// <summary>A tag on a result clicked: filter by it.</summary>
        public event Action<string> TagChosen;

        private static bool IncludeCode
        {
            get => EditorPrefs.GetBool(IncludeCodeKey, true);
            set => EditorPrefs.SetBool(IncludeCodeKey, value);
        }

        public void Show(string query, IReadOnlyCollection<string> tags, List<SearchHit> hits)
        {
            lastQuery = query;
            lastTags = tags;
            lastHits = hits;
            Clear();
            var code = hits.Count(h => h.Page.IsCode);
            var shown = IncludeCode ? hits : hits.Where(h => !h.Page.IsCode).ToList();
            var head = Classed(new VisualElement(), Prefix + "__head-row");
            head.Add(Classed(new Label(Heading(query, tags, shown.Count)) { enableRichText = false }, Prefix + "__heading"));
            if (code > 0)
            {
                head.Add(CodeToggle(code));
            }

            Add(head);
            foreach (var hit in shown)
            {
                Add(Result(hit));
            }
        }

        /// <summary>Whether the Code Reference's pages are among the results, with how many there are.</summary>
        private Button CodeToggle(int count)
        {
            var toggle = Classed(new Button(ToggleCode) { text = "Code Reference · " + count }, Prefix + "__code");
            toggle.EnableInClassList(Prefix + "__code--off", !IncludeCode);
            toggle.tooltip = IncludeCode ? "Pages made from the code are in the results. Click to leave them out." : "Pages made from the code are left out. Click to put them in.";
            return toggle;
        }

        private void ToggleCode()
        {
            IncludeCode = !IncludeCode;
            Show(lastQuery, lastTags, lastHits);
        }

        private VisualElement Result(SearchHit hit)
        {
            var result = Classed(new VisualElement(), Prefix + "__item");
            result.RegisterCallback<ClickEvent>(_ => Chosen?.Invoke(hit.Page));

            var head = Classed(new VisualElement(), Prefix + "__head");
            var icon = new Image { image = PageIcons.For(hit.Page), scaleMode = ScaleMode.ScaleToFit };
            head.Add(Classed(icon, Prefix + "__icon"));
            head.Add(Classed(new Label(hit.Page.Title) { enableRichText = false }, Prefix + "__title"));
            var trail = string.Join(" › ", hit.Page.Trail().Select(p => p.Title));
            head.Add(Classed(new Label(trail) { enableRichText = false }, Prefix + "__trail"));
            result.Add(head);

            if (hit.Page.Header.Tags.Count > 0)
            {
                var chips = Classed(new VisualElement(), Prefix + "__tags");
                foreach (var tag in hit.Page.Header.Tags)
                {
                    chips.Add(new TagChip(tag, chip => TagChosen?.Invoke(chip.Tag)));
                }

                result.Add(chips);
            }

            if (hit.Snippet.Length > 0)
            {
                result.Add(Classed(new Label(hit.Snippet) { enableRichText = true }, Prefix + "__snippet"));
            }

            return result;
        }

        private static string Heading(string query, IReadOnlyCollection<string> tags, int count)
        {
            var what = count == 0 ? "Nothing found" : count + (count == 1 ? " page" : " pages");
            var words = string.IsNullOrWhiteSpace(query) ? string.Empty : " for “" + query + "”";
            var tagged = tags.Count == 0 ? string.Empty : " tagged " + string.Join(" + ", tags);
            return what + words + tagged;
        }

        private static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
