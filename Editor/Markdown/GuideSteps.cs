using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Actions;
using CookieJarTools.Cookbook.Editor.Pages;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// A guide's steps: its action and check links that stand on a line of their own. Also how far
    /// the reader has got through them, from <see cref="StepMemory"/>.
    /// </summary>
    public static class GuideSteps
    {
        /// <summary>How a page is known in step memory: its id, or its path when it has none.</summary>
        public static string PageKey(DocPage page)
        {
            return page == null ? string.Empty : string.IsNullOrEmpty(page.Header.Id) ? page.Reference : page.Header.Id;
        }

        /// <summary>The step link a paragraph is, when it is nothing but an action or check link; null otherwise.</summary>
        public static LinkInline Lone(ParagraphBlock paragraph)
        {
            var parts = paragraph.Inline?.Where(i => !(i is LiteralInline literal && literal.Content.IsEmptyOrWhitespace()) && !(i is LineBreakInline)).ToList();
            var link = parts != null && parts.Count == 1 ? parts[0] as LinkInline : null;
            return link != null && !link.IsImage && IsStep(link.Url) ? link : null;
        }

        public static bool IsStep(string url)
        {
            return DocActions.IsAction(url) || DocChecks.IsCheck(url);
        }

        /// <summary>The links of a page's steps, in order.</summary>
        public static List<string> Of(DocPage page)
        {
            return CookbookMarkdown.Parse(page.Body).Descendants<ParagraphBlock>().Select(Lone).Where(l => l != null).Select(l => l.Url).ToList();
        }

        public static (int done, int total) Progress(DocPage page)
        {
            var steps = Of(page);
            var key = PageKey(page);
            return (steps.Count(s => StepMemory.IsDone(key, s)), steps.Count);
        }

        /// <summary>Forgets which of the page's steps were done.</summary>
        public static void Reset(DocPage page)
        {
            var key = PageKey(page);
            foreach (var step in Of(page))
            {
                StepMemory.Set(key, step, false);
            }
        }
    }
}
