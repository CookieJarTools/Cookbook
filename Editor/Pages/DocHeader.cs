using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// The few lines between <c>---</c> marks at the top of a page file: its id, tags, when it was
    /// last updated, a banner image and where it sorts among its neighbours.
    /// <code>
    /// ---
    /// id: 3f9a1c2b7d4e
    /// tags: [combat, ai]
    /// updated: 2026-09-19
    /// ---
    /// </code>
    /// A page without a header is fine; it gets one the first time the Cookbook saves it. Lines the
    /// Cookbook does not know are kept as they were.
    /// </summary>
    public sealed class DocHeader
    {
        private const string Fence = "---";

        private readonly List<string> other = new List<string>();

        public string Id { get; set; }

        public string Title { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        public string Updated { get; set; }

        public string Banner { get; set; }

        public int Order { get; set; }

        /// <summary>Nothing in it: no id, tags, date, banner, order or other lines. A file whose header is empty is written without one.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(Title) && Tags.Count == 0 && string.IsNullOrEmpty(Updated) && string.IsNullOrEmpty(Banner) && Order == 0 && other.Count == 0;

        /// <summary>Splits a page file into its header and the Markdown after it.</summary>
        public static DocHeader Split(string text, out string body)
        {
            var header = new DocHeader();
            body = text ?? string.Empty;
            var normalised = body.Replace("\r\n", "\n");
            if (!normalised.StartsWith(Fence + "\n"))
            {
                return header;
            }

            var end = normalised.IndexOf("\n" + Fence, Fence.Length, StringComparison.Ordinal);
            if (end < 0)
            {
                return header;
            }

            foreach (var line in normalised.Substring(Fence.Length + 1, end - Fence.Length - 1).Split('\n'))
            {
                header.Read(line);
            }

            var after = normalised.IndexOf('\n', end + 1);
            body = after < 0 ? string.Empty : normalised.Substring(after + 1).TrimStart('\n');
            return header;
        }

        /// <summary>The header, then the Markdown: the whole page file.</summary>
        public string Join(string body)
        {
            var text = new StringBuilder();
            text.Append(Fence).Append('\n');
            Line(text, "id", Id);
            Line(text, "title", Title);
            Line(text, "tags", Tags.Count > 0 ? "[" + string.Join(", ", Tags) + "]" : null);
            Line(text, "updated", Updated);
            Line(text, "banner", Banner);
            Line(text, "order", Order != 0 ? Order.ToString() : null);
            foreach (var line in other)
            {
                text.Append(line).Append('\n');
            }

            text.Append(Fence).Append("\n\n");
            text.Append(body ?? string.Empty);
            return text.ToString();
        }

        private static void Line(StringBuilder text, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                text.Append(key).Append(": ").Append(value).Append('\n');
            }
        }

        private void Read(string line)
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                KeepUnknown(line);
                return;
            }

            var key = line.Substring(0, colon).Trim().ToLowerInvariant();
            var value = line.Substring(colon + 1).Trim().Trim('"', '\'');
            if (!Set(key, value))
            {
                KeepUnknown(line);
            }
        }

        private bool Set(string key, string value)
        {
            switch (key)
            {
                case "id":
                    Id = value;
                    return true;
                case "title":
                    Title = value;
                    return true;
                case "tags":
                    Tags = value.Trim('[', ']').Split(',').Select(t => t.Trim().Trim('"', '\'')).Where(t => t.Length > 0).ToList();
                    return true;
                case "updated":
                    Updated = value;
                    return true;
                case "banner":
                    Banner = value;
                    return true;
                case "order":
                    Order = int.TryParse(value, out var order) ? order : 0;
                    return true;
                default:
                    return false;
            }
        }

        private void KeepUnknown(string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                other.Add(line);
            }
        }
    }
}
