using System;
using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Markdown.Code
{
    /// <summary>What the highlighter needs to know about a language: its keywords, how it writes comments, and which extras it has.</summary>
    public sealed class CodeLanguage
    {
        public CodeLanguage(string keywords, string lineComment, bool blockComments)
        {
            Keywords = new HashSet<string>(keywords.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            LineComment = lineComment;
            BlockComments = blockComments;
        }

        public HashSet<string> Keywords { get; }

        /// <summary>What starts a comment running to the end of the line: <c>//</c>, <c>#</c>, or null for none.</summary>
        public string LineComment { get; }

        /// <summary>Whether <c>/* … */</c> is a comment.</summary>
        public bool BlockComments { get; }

        /// <summary>Whether a name starting with a capital is taken for a type, as in C# and Unity code.</summary>
        public bool TypesByCase { get; set; } = true;

        /// <summary>C#'s <c>[Attribute]</c> at the start of a line.</summary>
        public bool Attributes { get; set; }

        /// <summary>A line starting with <c>#</c> is a directive: <c>#if</c>, <c>#pragma</c>.</summary>
        public bool Directives { get; set; }

        /// <summary>C#'s <c>@"verbatim"</c> and <c>$"interpolated"</c> strings.</summary>
        public bool PrefixedStrings { get; set; }

        /// <summary>JSON: a string followed by a colon is a key.</summary>
        public bool Keys { get; set; }
    }
}
