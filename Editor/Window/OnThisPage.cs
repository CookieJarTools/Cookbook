using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// "On this page": the headings of a long page, down the right of the window, the one being
    /// read picked out as the page scrolls. Shown only when the window is wide enough to have room
    /// beside the page, and the page has at least three headings to list.
    /// </summary>
    public sealed class OnThisPage : VisualElement
    {
        private const string Prefix = "cookbook-toc";
        private const int Fewest = 3;
        private const float Room = 1200f;
        private const float ReadingLine = 60f;

        private readonly ScrollView reading;
        private readonly List<(Label link, VisualElement heading)> entries = new List<(Label, VisualElement)>();
        private readonly Action<string> go;

        private float width;

        public OnThisPage(ScrollView reading, Action<string> go)
        {
            this.reading = reading;
            this.go = go;
            AddToClassList(Prefix);
            reading.verticalScroller.valueChanged += _ => Follow();
        }

        /// <param name="headings">Text, anchor and level of each; the elements are found in the page by anchor.</param>
        public void Show(List<(string text, string anchor, int level)> headings, Func<string, VisualElement> find)
        {
            Clear();
            entries.Clear();
            if (headings.Count >= Fewest)
            {
                Add(new Label("On this page") { name = "title" });
            }

            foreach (var (text, anchor, level) in headings)
            {
                var link = new Label(text) { tooltip = text, enableRichText = false };
                link.AddToClassList(Prefix + "__link");
                link.AddToClassList(Prefix + "__link--" + level);
                link.RegisterCallback<ClickEvent>(_ => go(anchor));
                Add(link);
                entries.Add((link, find(anchor)));
            }

            Fit(width);
            schedule.Execute(Follow);
        }

        public void Hide()
        {
            Clear();
            entries.Clear();
            style.display = DisplayStyle.None;
        }

        /// <summary>The window's width changed: shown only when there is room beside the page.</summary>
        public void Fit(float windowWidth)
        {
            width = windowWidth;
            style.display = entries.Count >= Fewest && width >= Room ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Picks out the last heading scrolled past the top of the reading area.</summary>
        private void Follow()
        {
            var current = -1;
            for (var i = 0; i < entries.Count; i++)
            {
                var heading = entries[i].heading;
                if (heading != null && heading.worldBound.y - reading.worldBound.y <= ReadingLine)
                {
                    current = i;
                }
            }

            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].link.EnableInClassList(Prefix + "__link--current", i == Math.Max(0, current));
            }
        }
    }
}
