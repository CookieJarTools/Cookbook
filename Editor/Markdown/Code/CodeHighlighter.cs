using System.Collections.Generic;
using System.Text;

namespace CookieJarTools.Cookbook.Editor.Markdown.Code
{
    /// <summary>
    /// Colours code for a code block, as rich text: keywords, types, method calls, strings,
    /// numbers, comments, attributes and directives, in quiet colours that suit the dark theme.
    /// <para>
    /// One pass along the code, a piece at a time — not a full parser, so an odd construct may be
    /// coloured plainly, but nothing is ever lost or moved: every character comes out as written.
    /// </para>
    /// </summary>
    public sealed class CodeHighlighter
    {
        private const string Keyword = "#B4A7F5";
        private const string Type = "#7DD3C8";
        private const string Method = "#8DB6FF";
        private const string Text = "#A8D5A2";
        private const string Number = "#F2B47A";
        private const string Comment = "#5F6570";
        private const string Attribute = "#E5C07B";
        private const string Directive = "#C98FD6";
        private const string Key = "#8DB6FF";

        private readonly string code;
        private readonly CodeLanguage language;
        private readonly StringBuilder output = new StringBuilder();
        private readonly StringBuilder plain = new StringBuilder();
        private readonly List<string> parts = new List<string>();

        private int at;
        private int marks;
        private bool lineStart = true;
        private bool inAttribute;

        private CodeHighlighter(string code, CodeLanguage language)
        {
            this.code = code;
            this.language = language;
        }

        /// <summary>
        /// The code as rich text, in parts of whole lines that each fit in one label (see
        /// <see cref="InlineText.MaxTagMarks"/>); plain text (made safe) when the language is none.
        /// </summary>
        public static List<string> Highlight(string code, string languageName)
        {
            var language = CodeLanguages.For(languageName);
            if (language == null)
            {
                return new List<string> { InlineText.Plain(code) };
            }

            return new CodeHighlighter(code ?? string.Empty, language).Run();
        }

        private List<string> Run()
        {
            while (at < code.Length)
            {
                if (!Token())
                {
                    Pass(code[at]);
                }
            }

            FlushPlain();
            parts.Add(output.ToString());
            return parts;
        }

        /// <summary>The next piece, if it is one that is coloured; false leaves the character to pass through.</summary>
        private bool Token()
        {
            return DirectiveLine() || LineComment() || BlockComment() || StringLiteral() || NumberLiteral() || AttributeOpen() || Word();
        }

        private void Pass(char character)
        {
            plain.Append(character);
            at++;
            if (character == '\n')
            {
                lineStart = true;
                inAttribute = false;
                return;
            }

            lineStart &= char.IsWhiteSpace(character);
            inAttribute &= character != ']';
        }

        private bool DirectiveLine()
        {
            if (!language.Directives || !lineStart || code[at] != '#')
            {
                return false;
            }

            Emit(Directive, ToLineEnd(at), false);
            return true;
        }

        private bool LineComment()
        {
            var marker = language.LineComment;
            if (marker == null || string.CompareOrdinal(code, at, marker, 0, marker.Length) != 0)
            {
                return false;
            }

            Emit(Comment, ToLineEnd(at), true);
            return true;
        }

        private bool BlockComment()
        {
            if (!language.BlockComments || !StartsWith("/*"))
            {
                return false;
            }

            var end = code.IndexOf("*/", at + 2, System.StringComparison.Ordinal);
            var length = (end < 0 ? code.Length : end + 2) - at;
            Emit(Comment, code.Substring(at, length), true);
            return true;
        }

        /// <summary>A quoted string, escapes and all; in C#, verbatim and interpolated ones too. A JSON string before a colon is a key.</summary>
        private bool StringLiteral()
        {
            var prefix = language.PrefixedStrings && (code[at] == '@' || code[at] == '$') ? PrefixLength() : 0;
            var quote = at + prefix < code.Length ? code[at + prefix] : '\0';
            if (quote != '"' && quote != '\'' && quote != '`')
            {
                return false;
            }

            var verbatim = code.Substring(at, prefix).Contains("@");
            var end = StringEnd(at + prefix + 1, quote, verbatim);
            var literal = code.Substring(at, end - at);
            Emit(language.Keys && FollowedBy(end, ':') ? Key : Text, literal, false);
            return true;
        }

        private int PrefixLength()
        {
            var length = 0;
            while (at + length < code.Length && length < 2 && (code[at + length] == '@' || code[at + length] == '$'))
            {
                length++;
            }

            return length;
        }

        /// <summary>Just past the closing quote — or the end of the line, for one never closed.</summary>
        private int StringEnd(int from, char quote, bool verbatim)
        {
            for (var i = from; i < code.Length; i++)
            {
                var character = code[i];
                if (character == '\\' && !verbatim)
                {
                    i++;
                    continue;
                }

                if (character == quote || (character == '\n' && !verbatim && quote != '`'))
                {
                    return i + 1;
                }
            }

            return code.Length;
        }

        private bool NumberLiteral()
        {
            var starts = char.IsDigit(code[at]) || (code[at] == '.' && at + 1 < code.Length && char.IsDigit(code[at + 1]));
            if (!starts || (at > 0 && IsWordCharacter(code[at - 1])))
            {
                return false;
            }

            var end = at;
            while (end < code.Length && (char.IsLetterOrDigit(code[end]) || code[end] == '.' || code[end] == '_'))
            {
                end++;
            }

            Emit(Number, code.Substring(at, end - at), false);
            return true;
        }

        /// <summary>C#'s <c>[</c> at the start of a line: the names up to <c>]</c> are an attribute.</summary>
        private bool AttributeOpen()
        {
            if (!language.Attributes || !lineStart || code[at] != '[')
            {
                return false;
            }

            inAttribute = true;
            Pass('[');
            return true;
        }

        /// <summary>A name: a keyword, an attribute, a method being called, a type, or plain.</summary>
        private bool Word()
        {
            if (!char.IsLetter(code[at]) && code[at] != '_')
            {
                return false;
            }

            var end = at;
            while (end < code.Length && IsWordCharacter(code[end]))
            {
                end++;
            }

            var word = code.Substring(at, end - at);
            var color = WordColor(word, end);
            if (color == null)
            {
                plain.Append(word);
                lineStart = false;
                at = end;
                return true;
            }

            Emit(color, word, false);
            return true;
        }

        private string WordColor(string word, int end)
        {
            if (language.Keywords.Contains(word))
            {
                return Keyword;
            }

            if (inAttribute)
            {
                return Attribute;
            }

            if (FollowedBy(end, '('))
            {
                return Method;
            }

            return language.TypesByCase && char.IsUpper(word[0]) ? Type : null;
        }

        /// <summary>Whether the next thing after the spaces is the character.</summary>
        private bool FollowedBy(int from, char character)
        {
            var i = from;
            while (i < code.Length && (code[i] == ' ' || code[i] == '\t'))
            {
                i++;
            }

            return i < code.Length && code[i] == character;
        }

        private string ToLineEnd(int from)
        {
            var end = code.IndexOf('\n', from);
            return code.Substring(from, (end < 0 ? code.Length : end) - from);
        }

        private bool StartsWith(string text)
        {
            return string.CompareOrdinal(code, at, text, 0, text.Length) == 0;
        }

        private static bool IsWordCharacter(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_';
        }

        /// <summary>A coloured piece. Pieces can span lines (block comments, verbatim strings), so the line start is worked out from what they end with.</summary>
        private void Emit(string color, string text, bool italic)
        {
            FlushPlain();
            output.Append("<color=").Append(color).Append('>');
            output.Append(italic ? "<i>" : string.Empty).Append(InlineText.Plain(text)).Append(italic ? "</i>" : string.Empty);
            output.Append("</color>");
            marks += italic ? 6 : 4;
            at += text.Length;
            lineStart = text.EndsWith("\n");
        }

        private void FlushPlain()
        {
            if (plain.Length == 0)
            {
                return;
            }

            var text = plain.ToString();
            plain.Clear();
            marks += 2;
            var cut = marks >= InlineText.MaxTagMarks ? text.IndexOf('\n') : -1;
            if (cut < 0)
            {
                output.Append(InlineText.Plain(text));
                return;
            }

            output.Append(InlineText.Plain(text.Substring(0, cut)));
            parts.Add(output.ToString());
            output.Clear();
            marks = 2;
            output.Append(InlineText.Plain(text.Substring(cut + 1)));
        }
    }
}
