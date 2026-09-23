using System;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// <c>[[Page]]</c> and <c>[[Page|what to show]]</c>: a link to another page by its title, id or
    /// path, without writing out where the file is. Made into an ordinary link to
    /// <c>cookbook:Page</c>.
    /// </summary>
    public sealed class PageLinkParser : InlineParser
    {
        public const string Scheme = "cookbook:";

        public PageLinkParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            if (slice.PeekChar() != '[')
            {
                return false;
            }

            var start = slice.Start;
            var text = slice.Text;
            var from = start + 2;
            var close = from <= slice.End ? text.IndexOf("]]", from, slice.End - from + 1, StringComparison.Ordinal) : -1;
            var inner = close > from ? text.Substring(from, close - from) : string.Empty;
            if (inner.Trim().Length == 0 || inner.IndexOf('\n') >= 0)
            {
                return false;
            }

            var bar = inner.IndexOf('|');
            var target = (bar < 0 ? inner : inner.Substring(0, bar)).Trim();
            var label = (bar < 0 ? inner : inner.Substring(bar + 1)).Trim();

            var link = new LinkInline(Scheme + target, string.Empty) { Span = new SourceSpan(start, close + 1), IsClosed = true };
            link.AppendChild(new LiteralInline(label.Length > 0 ? label : target));
            processor.Inline = link;
            slice.Start = close + 2;
            return true;
        }
    }
}
