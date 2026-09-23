using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// Writes a part of a <c>///</c> comment as Markdown: <c>&lt;c&gt;</c> as code, <c>&lt;code&gt;</c>
    /// as a code block, <c>&lt;para&gt;</c> as a paragraph, lists as lists, and each
    /// <c>&lt;see cref&gt;</c> as whatever link the caller finds for it. The comment's own words are
    /// escaped, so a <c>List&lt;int&gt;</c> or a <c>*</c> in them shows as written.
    /// </summary>
    public static class XmlMarkdown
    {
        private const string Escaped = "\\`*_[]<>|#";

        private static readonly Regex Spaces = new Regex(@"[ \t\r\n]+");
        private static readonly Regex BlankLines = new Regex(@"\n{3,}");

        /// <param name="link">The Markdown for a <c>cref</c> and the words to show for it (empty when the comment gave none).</param>
        public static string Write(XElement element, Func<string, string, string> link)
        {
            if (element == null)
            {
                return string.Empty;
            }

            var markdown = new StringBuilder();
            Nodes(element, markdown, link);
            return BlankLines.Replace(TrimLines(markdown.ToString()), "\n\n").Trim();
        }

        /// <summary>The same, on one line: for a table cell or a line in a list.</summary>
        public static string WriteLine(XElement element, Func<string, string, string> link)
        {
            return Spaces.Replace(Write(element, link), " ").Replace("|", "\\|");
        }

        public static string Escape(string text)
        {
            var escaped = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                escaped.Append(Escaped.IndexOf(character) >= 0 ? "\\" : string.Empty).Append(character);
            }

            return escaped.ToString();
        }

        /// <summary>Text as inline code, however many backticks it holds.</summary>
        public static string Code(string text)
        {
            var trimmed = Spaces.Replace(text ?? string.Empty, " ").Trim();
            var fence = trimmed.Contains("`") ? "``" : "`";
            return trimmed.Length == 0 ? string.Empty : fence + (fence.Length > 1 ? " " + trimmed + " " : trimmed) + fence;
        }

        private static void Nodes(XElement element, StringBuilder markdown, Func<string, string, string> link)
        {
            foreach (var node in element.Nodes())
            {
                Node(node, markdown, link);
            }
        }

        private static void Node(XNode node, StringBuilder markdown, Func<string, string, string> link)
        {
            if (node is XText text)
            {
                markdown.Append(Escape(Spaces.Replace(text.Value, " ")));
                return;
            }

            if (node is XElement element)
            {
                Element(element, markdown, link);
            }
        }

        private static void Element(XElement element, StringBuilder markdown, Func<string, string, string> link)
        {
            switch (element.Name.LocalName)
            {
                case "c":
                    markdown.Append(Code(element.Value));
                    break;
                case "code":
                    markdown.Append("\n\n```csharp\n").Append(Dedent(element.Value)).Append("\n```\n\n");
                    break;
                case "para":
                    markdown.Append("\n\n");
                    Nodes(element, markdown, link);
                    markdown.Append("\n\n");
                    break;
                case "see":
                case "seealso":
                    See(element, markdown, link);
                    break;
                case "paramref":
                case "typeparamref":
                    markdown.Append(Code((string)element.Attribute("name")));
                    break;
                case "list":
                    List(element, markdown, link);
                    break;
                case "br":
                    markdown.Append("<br>");
                    break;
                case "b":
                case "strong":
                    Wrapped(element, "**", markdown, link);
                    break;
                case "i":
                case "em":
                    Wrapped(element, "*", markdown, link);
                    break;
                case "inheritdoc":
                    break;
                default:
                    Nodes(element, markdown, link);
                    break;
            }
        }

        private static void See(XElement element, StringBuilder markdown, Func<string, string, string> link)
        {
            var cref = (string)element.Attribute("cref");
            var href = (string)element.Attribute("href");
            var langword = (string)element.Attribute("langword");
            var words = Spaces.Replace(element.Value, " ").Trim();
            if (cref != null)
            {
                markdown.Append(link(cref, words));
            }
            else if (href != null)
            {
                markdown.Append('[').Append(Escape(words.Length > 0 ? words : href)).Append("](").Append(href.Replace(" ", "%20")).Append(')');
            }
            else if (langword != null)
            {
                markdown.Append(Code(langword));
            }
        }

        private static void List(XElement list, StringBuilder markdown, Func<string, string, string> link)
        {
            var numbered = (string)list.Attribute("type") == "number";
            var number = 1;
            markdown.Append("\n\n");
            foreach (var item in list.Elements("item"))
            {
                markdown.Append(numbered ? number++ + ". " : "- ");
                var term = item.Element("term");
                var description = item.Element("description");
                if (term != null)
                {
                    markdown.Append("**").Append(WriteLine(term, link)).Append("**").Append(description != null ? " — " : string.Empty);
                }

                markdown.Append(WriteLine(description ?? (term == null ? item : null), link)).Append('\n');
            }

            markdown.Append('\n');
        }

        private static void Wrapped(XElement element, string mark, StringBuilder markdown, Func<string, string, string> link)
        {
            markdown.Append(mark);
            Nodes(element, markdown, link);
            markdown.Append(mark);
        }

        /// <summary>Each line's edges trimmed — but not inside a code block, whose indents matter.</summary>
        private static string TrimLines(string markdown)
        {
            var lines = markdown.Split('\n');
            var inCode = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var fence = lines[i].Trim().StartsWith("```");
                lines[i] = inCode && !fence ? lines[i] : lines[i].Trim(' ');
                inCode ^= fence;
            }

            return string.Join("\n", lines);
        }

        /// <summary>A code sample's lines, the indent they all share taken off.</summary>
        private static string Dedent(string code)
        {
            var lines = code.Replace("\r", string.Empty).Split('\n').SkipWhile(l => l.Trim().Length == 0).Reverse().SkipWhile(l => l.Trim().Length == 0).Reverse().ToList();
            var indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
            return string.Join("\n", lines.Select(l => l.Length >= indent ? l.Substring(indent) : l.TrimStart()));
        }
    }
}
