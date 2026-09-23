using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CookieJarTools.Cookbook.Editor.Reference.Scanning
{
    /// <summary>
    /// Reads a script for its <c>///</c> comments and what each is on. One pass along the text:
    /// strings, characters and ordinary comments are stepped over, braces are counted to know
    /// what is inside what, and the words before each <c>{</c>, <c>;</c> or <c>=&gt;</c> are given
    /// to <see cref="DeclarationReader"/>. Nothing inside a method or property body is looked at.
    /// <para>
    /// Comments are kept by key, the same as <see cref="CodeKeys"/> makes from reflection:
    /// <c>"Ns.Type"</c>, <c>"Ns.Type.field"</c>, <c>"Ns.Type.Move(target,speed)"</c>,
    /// <c>"Ns.Type.#ctor(value)"</c>.
    /// </para>
    /// </summary>
    public sealed class SourceScanner
    {
        private static readonly Regex Spaces = new Regex(@"\s+");

        private readonly string text;
        private readonly ScannedSource found = new ScannedSource();
        private readonly List<ScanScope> scopes = new List<ScanScope>();
        private readonly StringBuilder header = new StringBuilder();
        private readonly StringBuilder doc = new StringBuilder();

        private string fileNamespace = string.Empty;
        private int at;
        private int expressionDepth = -1;

        private SourceScanner(string text)
        {
            this.text = text ?? string.Empty;
        }

        public static ScannedSource Scan(string text)
        {
            var scanner = new SourceScanner(text);
            scanner.Run();
            return scanner.found;
        }

        private ScanScope Current => scopes.Count == 0 ? null : scopes[scopes.Count - 1];

        private bool InBody => Current != null && Current.Kind == ScopeKind.Other;

        private bool InExpression => expressionDepth >= 0;

        private void Run()
        {
            while (at < text.Length)
            {
                Step();
            }
        }

        private void Step()
        {
            var c = text[at];
            if (c == '/' && Next(1) == '/')
            {
                LineComment();
            }
            else if (c == '/' && Next(1) == '*')
            {
                at = Until("*/", at + 2);
            }
            else if (c == '#' && AtLineStart())
            {
                at = LineEnd(at);
            }
            else if (c == '"' || ((c == '@' || c == '$') && (Next(1) == '"' || Next(2) == '"')))
            {
                SkipString();
            }
            else
            {
                Code(c);
            }
        }

        private void Code(char c)
        {
            at++;
            switch (c)
            {
                case '\'':
                    SkipCharacter();
                    break;
                case '{':
                    Open();
                    break;
                case '}':
                    Close();
                    break;
                case ';':
                    Semicolon();
                    break;
                case ',' when Current?.Kind == ScopeKind.Enum:
                    EnumMember();
                    break;
                case '=' when text.Length > at && text[at] == '>':
                    at++;
                    Arrow();
                    break;
                default:
                    header.Append(c);
                    break;
            }
        }

        /// <summary><c>///</c> lines are kept for what comes next; any other comment is passed over.</summary>
        private void LineComment()
        {
            var end = LineEnd(at);
            if (Next(2) == '/' && Next(3) != '/')
            {
                var line = text.Substring(at + 3, end - at - 3);
                doc.Append(line.StartsWith(" ") ? line.Substring(1) : line).Append('\n');
            }

            at = end;
        }

        private void Open()
        {
            if (InBody || InExpression)
            {
                Push(ScopeKind.Other, null, null);
                return;
            }

            var declared = Read('{');
            switch (declared.Kind)
            {
                case DeclarationKind.Namespace:
                    Push(ScopeKind.Namespace, Join(Namespace(), declared.Name), null);
                    break;
                case DeclarationKind.Type:
                case DeclarationKind.Enum:
                    var key = TypeKey(declared);
                    Remember(key);
                    found.Types.Add(key);
                    Push(declared.Kind == DeclarationKind.Enum ? ScopeKind.Enum : ScopeKind.Type, key, declared.Name);
                    break;
                default:
                    Member(declared);
                    Push(ScopeKind.Other, null, null);
                    expressionDepth = declared.HasInitializer ? scopes.Count - 1 : expressionDepth;
                    break;
            }

            Clear();
        }

        private void Close()
        {
            if (Current?.Kind == ScopeKind.Enum && !InExpression)
            {
                EnumMember();
            }

            if (scopes.Count > 0)
            {
                scopes.RemoveAt(scopes.Count - 1);
            }

            header.Clear();
            if (!InBody)
            {
                doc.Clear();
            }
        }

        private void Semicolon()
        {
            if (InExpression && scopes.Count == expressionDepth)
            {
                expressionDepth = -1;
                Clear();
                return;
            }

            if (InBody || InExpression)
            {
                header.Clear();
                return;
            }

            var declared = Read(';');
            if (declared.Kind == DeclarationKind.Namespace)
            {
                fileNamespace = declared.Name;
            }
            else if (declared.Kind == DeclarationKind.Delegate)
            {
                var key = TypeKey(declared);
                Remember(key);
                found.Types.Add(key);
            }
            else
            {
                Member(declared);
            }

            Clear();
        }

        /// <summary>
        /// <c>=&gt;</c> at a type's level ends an expression-bodied method or property; the rest,
        /// up to its <c>;</c>, is passed over. Inside a field's value it is just part of a lambda.
        /// </summary>
        private void Arrow()
        {
            if (InBody || InExpression || DeclarationReader.HasInitializer(Squeezed()))
            {
                header.Append("=>");
                return;
            }

            Member(Read('='));
            expressionDepth = scopes.Count;
            Clear();
        }

        private void EnumMember()
        {
            var words = Squeezed();
            var name = Regex.Match(Regex.Replace(words, @"^\[[^\]]*\]\s*", string.Empty), @"@?[A-Za-z_]\w*");
            if (name.Success && Current != null)
            {
                Remember(Current.Key + "." + name.Value.TrimStart('@'));
            }

            Clear();
        }

        private void Member(Declaration declared)
        {
            var type = Current;
            if (type == null || (type.Kind != ScopeKind.Type && type.Kind != ScopeKind.Enum))
            {
                return;
            }

            if (declared.Kind == DeclarationKind.Method || declared.Kind == DeclarationKind.Constructor)
            {
                var name = declared.Kind == DeclarationKind.Constructor ? CodeKeys.Constructor : declared.Name;
                Remember(type.Key + "." + name + "(" + string.Join(",", declared.Parameters) + ")");
                return;
            }

            foreach (var name in declared.Names)
            {
                Remember(type.Key + "." + name);
            }
        }

        private Declaration Read(char ending)
        {
            var type = Current != null && Current.Kind == ScopeKind.Type ? Current.Name : null;
            return DeclarationReader.Read(Squeezed(), type, ending);
        }

        private void Remember(string key)
        {
            if (doc.Length > 0 && !found.Comments.ContainsKey(key))
            {
                found.Comments[key] = doc.ToString();
            }
        }

        private string TypeKey(Declaration declared)
        {
            var name = declared.Name + (declared.Arity > 0 ? "`" + declared.Arity : string.Empty);
            var outer = Current != null && Current.Kind == ScopeKind.Type ? Current.Key : Namespace();
            return Join(outer, name);
        }

        private string Namespace()
        {
            var inner = scopes.LastOrDefault(s => s.Kind == ScopeKind.Namespace);
            return inner?.Key ?? fileNamespace;
        }

        private void Push(ScopeKind kind, string key, string name)
        {
            scopes.Add(new ScanScope(kind, key, name));
        }

        private void Clear()
        {
            header.Clear();
            doc.Clear();
        }

        private string Squeezed()
        {
            return Spaces.Replace(header.ToString(), " ").Trim();
        }

        /// <summary>A string left out, "" put in its place so the words around it still read the same.</summary>
        private void SkipString()
        {
            var verbatim = false;
            while (text[at] != '"')
            {
                verbatim |= text[at] == '@';
                at++;
            }

            var raw = string.CompareOrdinal(text, at, "\"\"\"", 0, 3) == 0;
            at = raw ? Until("\"\"\"", at + 3) : verbatim ? VerbatimEnd(at + 1) : QuotedEnd(at + 1, '"');
            header.Append("\"\"");
        }

        private void SkipCharacter()
        {
            at = QuotedEnd(at, '\'');
            header.Append("' '");
        }

        private int QuotedEnd(int from, char quote)
        {
            for (var i = from; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (text[i] == quote || text[i] == '\n')
                {
                    return i + 1;
                }
            }

            return text.Length;
        }

        private int VerbatimEnd(int from)
        {
            for (var i = from; i < text.Length; i++)
            {
                if (text[i] != '"')
                {
                    continue;
                }

                if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                return i + 1;
            }

            return text.Length;
        }

        private int Until(string end, int from)
        {
            var index = text.IndexOf(end, from, System.StringComparison.Ordinal);
            return index < 0 ? text.Length : index + end.Length;
        }

        private int LineEnd(int from)
        {
            var end = text.IndexOf('\n', from);
            return end < 0 ? text.Length : end;
        }

        private bool AtLineStart()
        {
            for (var i = at - 1; i >= 0 && text[i] != '\n'; i--)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private char Next(int offset)
        {
            return at + offset < text.Length ? text[at + offset] : '\0';
        }

        private static string Join(string outer, string name)
        {
            return string.IsNullOrEmpty(outer) ? name : outer + "." + name;
        }
    }
}
