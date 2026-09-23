using System.Linq;
using System.Reflection;
using System.Text;
using CookieJarTools.Cookbook.Editor.Index;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// A type's page, written as Markdown so it reads, searches and links like any other page: its
    /// summary and declaration, the note written for it, then each member under its heading —
    /// declaration, words, parameters, what it returns — and the types inside it and made from it.
    /// </summary>
    public sealed class TypeMarkdown
    {
        public const string Signature = Markdown.CodeBlockElement.SignatureLanguage;
        public const string NotesHeading = "Notes";
        public const string NoWords = "*No description yet.*";

        private readonly CodeModel model;
        private readonly CodeType type;
        private readonly StringBuilder page = new StringBuilder();

        private TypeMarkdown(CodeModel model, CodeType type)
        {
            this.model = model;
            this.type = type;
        }

        /// <param name="note">What someone wrote for the page, without its header; empty when nothing was.</param>
        public static string Write(CodeModel model, CodeType type, string note)
        {
            var writer = new TypeMarkdown(model, type);
            writer.WriteAll(note);
            return writer.page.ToString();
        }

        private void WriteAll(string note)
        {
            var comment = type.Comment;
            Paragraph(comment != null && comment.HasWords ? Words(comment.Summary) : NoWords);
            Block(Signature, Signatures.Of(type.Type));
            Paragraph(Words(comment?.Remarks));
            TypeParameters(comment);
            if (comment?.Example != null)
            {
                Heading(2, "Example");
                Paragraph(Words(comment.Example));
            }

            if (!string.IsNullOrWhiteSpace(note))
            {
                Heading(2, NotesHeading);
                Paragraph(note.Trim());
            }

            Guides();
            if (type.Type.IsEnum)
            {
                Values();
            }
            else
            {
                Members();
            }

            Nested();
            Derived();
        }

        private void TypeParameters(DocComment comment)
        {
            if (comment == null || comment.TypeParameters.Count == 0)
            {
                return;
            }

            Paragraph("**Type parameters**");
            List(comment.TypeParameters.Select(p => XmlMarkdown.Code(p.Key) + " — " + Line(p.Value)));
        }

        /// <summary>The pages that name this type with [Documentation]: written guides, above the generated detail.</summary>
        private void Guides()
        {
            var pages = DocCodeIndex.All.Where(l => l.Type == type.Type).Select(l => l.Page).Distinct().ToList();
            if (pages.Count == 0)
            {
                return;
            }

            Heading(2, "Guides");
            List(pages.Select(p => "[[" + p + "]]"));
        }

        private void Values()
        {
            if (type.Members.Count == 0)
            {
                return;
            }

            Heading(2, CodeType.Values);
            page.Append("| Name | Value | Description |\n| --- | --- | --- |\n");
            foreach (var value in type.Members)
            {
                var number = System.Convert.ToInt64(((FieldInfo)value.Info).GetRawConstantValue());
                var words = value.IsDocumented ? XmlMarkdown.WriteLine(value.Comment.Summary, Link) : string.Empty;
                page.Append("| ").Append(XmlMarkdown.Code(value.Name)).Append(" | ").Append(number).Append(" | ").Append(words).Append(" |\n");
            }

            page.Append('\n');
        }

        private void Members()
        {
            foreach (var group in type.Members.GroupBy(m => m.Group))
            {
                Heading(2, group.Key);
                foreach (var member in group)
                {
                    Member(member);
                }
            }
        }

        private void Member(CodeMember member)
        {
            Heading(3, member.Name);
            Block(Signature, Signatures.Of(member.Info));
            var obsolete = member.Info.GetCustomAttribute<System.ObsoleteAttribute>();
            if (obsolete != null)
            {
                Paragraph("> [!WARNING]\n> Obsolete" + (string.IsNullOrEmpty(obsolete.Message) ? "." : ": " + XmlMarkdown.Escape(obsolete.Message)));
            }

            var comment = member.Comment;
            Paragraph(member.IsDocumented ? Words(comment.Summary) : NoWords);
            Parameters(member, comment);
            if (comment?.Returns != null || comment?.Value != null)
            {
                Paragraph("**" + (comment.Returns != null ? "Returns" : "Value") + "** " + Words(comment.Returns ?? comment.Value));
            }

            Paragraph(Words(comment?.Remarks));
        }

        private void Parameters(CodeMember member, DocComment comment)
        {
            var method = member.Info as MethodBase;
            var parameters = method?.GetParameters();
            if (parameters == null || comment == null || !parameters.Any(p => comment.Parameters.ContainsKey(p.Name)))
            {
                return;
            }

            var lines = parameters.Select(p => XmlMarkdown.Code(p.Name) + (comment.Parameters.TryGetValue(p.Name, out var words) ? " — " + Line(words) : string.Empty));
            Paragraph("**Parameters**");
            List(lines);
        }

        private void Nested()
        {
            var nested = model.Types.Where(t => t.Type.DeclaringType == type.Type).ToList();
            if (nested.Count == 0)
            {
                return;
            }

            Heading(2, "Nested types");
            List(nested.Select(Summary));
        }

        private void Derived()
        {
            var derived = model.Derived(type).OrderBy(t => t.Title).ToList();
            if (derived.Count == 0)
            {
                return;
            }

            Heading(2, type.Type.IsInterface ? "Implemented by" : "Derived types");
            List(derived.Select(Summary));
        }

        /// <summary>A link to a type's page and the start of what it is about, for a list.</summary>
        private string Summary(CodeType other)
        {
            var words = other.Comment != null && other.Comment.HasWords ? " — " + Line(other.Comment.Summary) : string.Empty;
            return CodeLinks.Page(other, null, other.Title) + words;
        }

        private string Words(System.Xml.Linq.XElement element)
        {
            return XmlMarkdown.Write(element, Link);
        }

        private string Line(System.Xml.Linq.XElement element)
        {
            return XmlMarkdown.WriteLine(element, Link);
        }

        private string Link(string cref, string words)
        {
            return CodeLinks.Cref(model, type, cref, words);
        }

        private void Heading(int level, string text)
        {
            page.Append(new string('#', level)).Append(' ').Append(text).Append("\n\n");
        }

        private void Paragraph(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                page.Append(text).Append("\n\n");
            }
        }

        private void Block(string language, string code)
        {
            page.Append("```").Append(language).Append('\n').Append(code).Append("\n```\n\n");
        }

        private void List(System.Collections.Generic.IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                page.Append("- ").Append(line).Append('\n');
            }

            page.Append('\n');
        }
    }
}
