using System.Collections.Generic;
using Markdig.Syntax.Inlines;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// A stretch of a paragraph as the drawing needs it: rich text with the links it holds (each
    /// <c>&lt;link&gt;</c> tag's id is its place in <see cref="Links"/>), or an image standing on
    /// its own. A paragraph with an image in it becomes text, image, text.
    /// </summary>
    public sealed class InlineRun
    {
        public InlineRun(string text, List<string> links)
        {
            Text = text;
            Links = links;
        }

        public InlineRun(LinkInline image)
        {
            Image = image;
        }

        public string Text { get; }

        public List<string> Links { get; }

        public LinkInline Image { get; }

        /// <summary>Where a checklist box is in the Markdown, when the paragraph starts with one; -1 when not.</summary>
        public int TaskPosition { get; set; } = -1;

        public bool TaskChecked { get; set; }

        /// <summary>Whether the paragraph carries on in the next run, split only because it was too long for one label.</summary>
        public bool GoesOn { get; set; }
    }
}
