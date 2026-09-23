using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Search
{
    /// <summary>
    /// Finds pages by their words. Every word searched for must be somewhere in the page — its
    /// title, tags or text — and pages rank higher the more of it is in the title and tags.
    /// <c>tag:combat</c> keeps only pages with that tag; words in quotes are found together.
    /// <para>
    /// Each page's text, with the Markdown taken out, is worked out once per library load.
    /// </para>
    /// </summary>
    public sealed class DocSearch
    {
        private const string TagPrefix = "tag:";
        private const int TitleScore = 10;
        private const int TagScore = 6;
        private const int PathScore = 2;
        private const int TextScore = 1;
        private const int MaxTextScore = 5;
        private const int SnippetBefore = 50;
        private const int SnippetLength = 180;
        private const string MarkColor = "#5E6AD266";

        private readonly Dictionary<DocPage, string> plain = new Dictionary<DocPage, string>();

        public DocSearch(DocLibrary library)
        {
            foreach (var page in library.Everything)
            {
                var words = System.Text.RegularExpressions.Regex.Replace(CookbookMarkdown.PlainText(page.Body), "<!--.*?-->", " ", System.Text.RegularExpressions.RegexOptions.Singleline);
                plain[page] = words.Replace('\n', ' ').Replace('\r', ' ');
            }
        }

        /// <param name="picked">Tags picked as filters, besides any typed as <c>tag:</c>; a page needs every one — its own or its folders'.</param>
        public List<SearchHit> Find(string query, IEnumerable<string> picked = null)
        {
            var words = Words(query, out var tags);
            tags.AddRange(picked ?? Enumerable.Empty<string>());
            if (words.Count == 0 && tags.Count == 0)
            {
                return new List<SearchHit>();
            }

            return plain.Keys
                .Where(page => tags.All(page.HasTag))
                .Select(page => Hit(page, words))
                .Where(hit => hit != null)
                .OrderByDescending(hit => hit.Score)
                .ThenBy(hit => hit.Page.Title)
                .ToList();
        }

        /// <summary>The words to find, quoted phrases kept whole, and the tags asked for.</summary>
        private static List<string> Words(string query, out List<string> tags)
        {
            tags = new List<string>();
            var words = new List<string>();
            var parts = (query ?? string.Empty).Split('"');
            for (var i = 0; i < parts.Length; i++)
            {
                var inQuotes = i % 2 == 1;
                words.AddRange(inQuotes ? new[] { parts[i].Trim() } : parts[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }

            tags.AddRange(words.Where(w => w.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)).Select(w => w.Substring(TagPrefix.Length)).Where(t => t.Length > 0));
            return words.Where(w => w.Length > 0 && !w.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private SearchHit Hit(DocPage page, List<string> words)
        {
            var text = plain[page];
            var score = 0;
            foreach (var word in words)
            {
                var found = Score(page, text, word);
                if (found == 0)
                {
                    return null;
                }

                score += found;
            }

            return new SearchHit(page, Math.Max(score, 1), Snippet(text, words));
        }

        private static int Score(DocPage page, string text, string word)
        {
            var score = Contains(page.Title, word) || Contains(page.Name, word) ? TitleScore : 0;
            score += Contains(page.Reference, word) ? PathScore : 0;
            score += page.Header.Tags.Any(t => Contains(t, word)) ? TagScore : 0;
            score += Math.Min(Count(text, word), MaxTextScore) * TextScore;
            return score;
        }

        /// <summary>The text around the first word found, each word searched for highlighted.</summary>
        private static string Snippet(string text, List<string> words)
        {
            var first = words.Select(w => text.IndexOf(w, StringComparison.OrdinalIgnoreCase)).Where(i => i >= 0).DefaultIfEmpty(0).Min();
            var start = Math.Max(0, first - SnippetBefore);
            var length = Math.Min(SnippetLength, text.Length - start);
            var piece = text.Substring(start, length).Trim();
            var marked = Highlight(piece, words);
            return (start > 0 ? "…" : string.Empty) + marked + (start + length < text.Length ? "…" : string.Empty);
        }

        private static string Highlight(string piece, List<string> words)
        {
            var result = new StringBuilder();
            var at = 0;
            while (at < piece.Length)
            {
                var (index, length) = NextMatch(piece, words, at);
                if (index < 0)
                {
                    result.Append(InlineText.Plain(piece.Substring(at)));
                    break;
                }

                result.Append(InlineText.Plain(piece.Substring(at, index - at)));
                result.Append("<mark=").Append(MarkColor).Append('>').Append(InlineText.Plain(piece.Substring(index, length))).Append("</mark>");
                at = index + length;
            }

            return result.ToString();
        }

        private static (int index, int length) NextMatch(string piece, List<string> words, int from)
        {
            var best = (index: -1, length: 0);
            foreach (var word in words)
            {
                var index = piece.IndexOf(word, from, StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && (best.index < 0 || index < best.index))
                {
                    best = (index, word.Length);
                }
            }

            return best;
        }

        private static bool Contains(string text, string word)
        {
            return text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int Count(string text, string word)
        {
            var count = 0;
            for (var at = text.IndexOf(word, StringComparison.OrdinalIgnoreCase); at >= 0; at = text.IndexOf(word, at + word.Length, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }

            return count;
        }
    }
}
