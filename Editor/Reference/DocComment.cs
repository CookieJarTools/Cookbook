using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// A <c>///</c> comment, taken apart: its summary, remarks, what it returns, its parameters.
    /// Each part is still XML, to be written out as Markdown by <see cref="XmlMarkdown"/> once
    /// it is known what its <c>cref</c>s link to.
    /// <para>
    /// A comment that is not well-formed XML keeps its words as the summary, tags and all taken out.
    /// </para>
    /// </summary>
    public sealed class DocComment
    {
        private DocComment()
        {
        }

        public XElement Summary { get; private set; }

        public XElement Remarks { get; private set; }

        public XElement Returns { get; private set; }

        public XElement Value { get; private set; }

        public XElement Example { get; private set; }

        public Dictionary<string, XElement> Parameters { get; } = new Dictionary<string, XElement>();

        public Dictionary<string, XElement> TypeParameters { get; } = new Dictionary<string, XElement>();

        /// <summary><c>&lt;inheritdoc/&gt;</c>: the words are the base member's.</summary>
        public bool Inherits { get; private set; }

        public bool HasWords => Summary != null && Summary.Value.Trim().Length > 0;

        public static DocComment Parse(string xml)
        {
            var comment = new DocComment();
            XElement root;
            try
            {
                root = XElement.Parse("<doc>" + xml + "</doc>", LoadOptions.PreserveWhitespace);
            }
            catch (XmlException)
            {
                comment.Summary = new XElement("summary", System.Text.RegularExpressions.Regex.Replace(xml, "<[^>]*>", " "));
                return comment;
            }

            comment.Fill(root);
            return comment;
        }

        /// <summary>A comment of the tooltip's words: what a serialized field says of itself in the Inspector.</summary>
        public static DocComment FromText(string text)
        {
            return new DocComment { Summary = new XElement("summary", text) };
        }

        private void Fill(XElement root)
        {
            Summary = root.Element("summary");
            Remarks = root.Element("remarks");
            Returns = root.Element("returns");
            Value = root.Element("value");
            Example = root.Element("example");
            Inherits = root.Element("inheritdoc") != null;
            foreach (var parameter in root.Elements("param").Where(p => p.Attribute("name") != null))
            {
                Parameters[parameter.Attribute("name").Value] = parameter;
            }

            foreach (var parameter in root.Elements("typeparam").Where(p => p.Attribute("name") != null))
            {
                TypeParameters[parameter.Attribute("name").Value] = parameter;
            }

            // Words with no <summary> around them are its summary.
            if (Summary == null && root.Elements().All(e => e.Name == "inheritdoc") && root.Value.Trim().Length > 0)
            {
                Summary = new XElement("summary", root.Nodes());
            }
        }
    }
}
