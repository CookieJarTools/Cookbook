using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CookieJarTools.Cookbook.Editor.Reference.Scanning
{
    /// <summary>
    /// Works out what a declaration is from its words alone — the text before the <c>{</c>,
    /// <c>;</c> or <c>=&gt;</c> that ends it, strings already blanked out. Not a parser: enough to
    /// know a type from a method from a field, and to find their names and parameter names.
    /// </summary>
    public static class DeclarationReader
    {
        private static readonly Regex Namespace = new Regex(@"^namespace\s+([\w.]+)$");
        private static readonly Regex TypeWord = new Regex(@"\b(class|struct|interface|enum|record)\s+(?:(?:struct|class)\s+)?@?(\w+)\s*(<[^()]*?>)?");
        private static readonly Regex DelegateWord = new Regex(@"\bdelegate\b");
        private static readonly Regex EventWord = new Regex(@"\bevent\b");
        private static readonly Regex Identifier = new Regex(@"@?[A-Za-z_]\w*");
        private static readonly HashSet<string> Modifiers = new HashSet<string> { "public", "private", "protected", "internal", "static", "async", "virtual", "override", "abstract", "sealed", "new", "unsafe", "extern", "readonly", "partial", "ref" };

        /// <param name="header">The declaration's words, whitespace squeezed to single spaces.</param>
        /// <param name="typeName">The type it is inside, to know its constructors; null outside a type.</param>
        /// <param name="ending">What ended it: <c>'{'</c>, <c>';'</c> or <c>'='</c> for <c>=&gt;</c>.</param>
        public static Declaration Read(string header, string typeName, char ending)
        {
            var text = WithoutAttributes(header.Trim());
            if (text.Length == 0 || text.StartsWith("using ") || text.StartsWith("~") || text.Contains("operator ") || text.Contains("this["))
            {
                return Declaration.None;
            }

            var space = Namespace.Match(text);
            if (space.Success)
            {
                return new Declaration(DeclarationKind.Namespace, space.Groups[1].Value);
            }

            var paren = ParameterList(text);
            return TypeDeclaration(text, paren) ?? DelegateDeclaration(text, paren) ?? Member(text, paren, typeName, ending);
        }

        /// <summary>Whether the words give something a value — <c>x = 1</c> — outside any brackets.</summary>
        public static bool HasInitializer(string header)
        {
            return TopLevelEquals(header) >= 0;
        }

        /// <summary>The names of a method's parameters, from the text between its brackets.</summary>
        public static List<string> ParameterNames(string text, int open)
        {
            var close = Matching(text, open);
            var inside = close < 0 ? string.Empty : text.Substring(open + 1, close - open - 1);
            return Split(inside).Select(p => LastIdentifier(BeforeTopLevelEquals(WithoutAttributes(p.Trim())))).Where(n => n != null).ToList();
        }

        private static Declaration TypeDeclaration(string text, int paren)
        {
            var match = TypeWord.Match(text);
            if (!match.Success || (paren >= 0 && match.Index > paren))
            {
                return null;
            }

            var kind = match.Groups[1].Value == "enum" ? DeclarationKind.Enum : DeclarationKind.Type;
            return new Declaration(kind, match.Groups[2].Value) { Arity = Arity(match.Groups[3].Value) };
        }

        private static Declaration DelegateDeclaration(string text, int paren)
        {
            var match = DelegateWord.Match(text);
            if (!match.Success || paren < 0 || match.Index > paren)
            {
                return null;
            }

            var name = NameBefore(text, paren, out var generic);
            return name == null ? null : new Declaration(DeclarationKind.Delegate, name) { Arity = Arity(generic) };
        }

        private static Declaration Member(string text, int paren, string typeName, char ending)
        {
            var equals = TopLevelEquals(text);
            if (paren >= 0 && (equals < 0 || paren < equals))
            {
                return Method(text, paren, typeName);
            }

            var kind = EventWord.IsMatch(text) ? DeclarationKind.Event : ending == ';' || equals >= 0 ? DeclarationKind.Field : DeclarationKind.Property;
            var declared = new Declaration(kind, null) { HasInitializer = equals >= 0 };
            var names = Split(equals < 0 ? text : text.Substring(0, equals)).Select(d => LastIdentifier(BeforeTopLevelEquals(d))).Where(n => n != null);
            declared.Names.AddRange(names);
            return declared.Names.Count == 0 ? Declaration.None : declared;
        }

        private static Declaration Method(string text, int paren, string typeName)
        {
            var name = NameBefore(text, paren, out _);
            if (name == null || name == "if" || name == "while" || name == "for" || name == "foreach" || name == "switch")
            {
                return Declaration.None;
            }

            var constructor = name == typeName;
            var method = new Declaration(constructor ? DeclarationKind.Constructor : DeclarationKind.Method, name);
            method.Parameters.AddRange(ParameterNames(text, paren));
            return method;
        }

        /// <summary>The name just before an opening bracket, stepping back over any generic parameters: <c>Move&lt;T&gt;(</c> is "Move".</summary>
        private static string NameBefore(string text, int paren, out string generic)
        {
            generic = string.Empty;
            var end = paren;
            while (end > 0 && text[end - 1] == ' ')
            {
                end--;
            }

            if (end > 0 && text[end - 1] == '>')
            {
                var open = OpeningAngle(text, end - 1);
                generic = open < 0 ? string.Empty : text.Substring(open, end - open);
                end = open < 0 ? end : open;
            }

            var start = end;
            while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_' || text[start - 1] == '@'))
            {
                start--;
            }

            return start == end ? null : text.Substring(start, end - start).TrimStart('@');
        }

        private static int OpeningAngle(string text, int close)
        {
            var depth = 0;
            for (var i = close; i >= 0; i--)
            {
                depth += text[i] == '>' ? 1 : text[i] == '<' ? -1 : 0;
                if (depth == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int Arity(string generic)
        {
            return string.IsNullOrEmpty(generic) ? 0 : Split(generic.Trim('<', '>')).Count;
        }

        private static string LastIdentifier(string text)
        {
            var matches = Identifier.Matches(text ?? string.Empty);
            return matches.Count == 0 ? null : matches[matches.Count - 1].Value.TrimStart('@');
        }

        private static string BeforeTopLevelEquals(string text)
        {
            var equals = TopLevelEquals(text);
            return equals < 0 ? text : text.Substring(0, equals);
        }

        /// <summary>Leading <c>[Attributes]</c> taken off, however many there are.</summary>
        private static string WithoutAttributes(string text)
        {
            while (text.StartsWith("["))
            {
                var close = Matching(text, 0);
                text = close < 0 ? string.Empty : text.Substring(close + 1).TrimStart();
            }

            return text;
        }

        /// <summary>Where the bracket opened at <paramref name="open"/> closes, counting any inside it.</summary>
        private static int Matching(string text, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                depth += Opens(text[i]) ? 1 : Closes(text[i]) ? -1 : 0;
                if (depth == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Where the parameter list opens: the first bracket outside any other that follows a name —
        /// not a tuple being returned, <c>static (int a, int b) Pair(</c>, which follows a modifier or nothing.
        /// </summary>
        private static int ParameterList(string text)
        {
            var paren = TopLevel(text, '(');
            while (paren >= 0 && IsModifier(NameBefore(text, paren, out _)))
            {
                var close = Matching(text, paren);
                var next = close < 0 ? -1 : TopLevel(text.Substring(close + 1), '(');
                paren = next < 0 ? -1 : close + 1 + next;
            }

            return paren;
        }

        private static bool IsModifier(string word)
        {
            return word == null || Modifiers.Contains(word);
        }

        /// <summary>The first <paramref name="character"/> not inside brackets of any kind.</summary>
        private static int TopLevel(string text, char character)
        {
            var depth = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (depth == 0 && text[i] == character)
                {
                    return i;
                }

                depth += Opens(text[i]) ? 1 : Closes(text[i]) ? -1 : 0;
            }

            return -1;
        }

        /// <summary>The first <c>=</c> that gives a value — not part of <c>==</c>, <c>=&gt;</c>, <c>&lt;=</c> and the like — outside brackets.</summary>
        private static int TopLevelEquals(string text)
        {
            var depth = 0;
            for (var i = 0; i < text.Length; i++)
            {
                depth += Opens(text[i]) ? 1 : Closes(text[i]) ? -1 : 0;
                if (depth == 0 && text[i] == '=' && IsAssignment(text, i))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsAssignment(string text, int at)
        {
            var before = at > 0 ? text[at - 1] : ' ';
            var after = at + 1 < text.Length ? text[at + 1] : ' ';
            return after != '=' && after != '>' && before != '=' && before != '!' && before != '<' && before != '>';
        }

        /// <summary>Split at commas outside brackets: parameters, generic arguments, declarators.</summary>
        private static List<string> Split(string text)
        {
            var parts = new List<string>();
            var part = new StringBuilder();
            var depth = 0;
            foreach (var character in text)
            {
                depth += Opens(character) ? 1 : Closes(character) ? -1 : 0;
                if (depth == 0 && character == ',')
                {
                    parts.Add(part.ToString());
                    part.Clear();
                    continue;
                }

                part.Append(character);
            }

            if (part.ToString().Trim().Length > 0)
            {
                parts.Add(part.ToString());
            }

            return parts;
        }

        private static bool Opens(char character)
        {
            return character == '(' || character == '[' || character == '<' || character == '{';
        }

        private static bool Closes(char character)
        {
            return character == ')' || character == ']' || character == '>' || character == '}';
        }
    }
}
