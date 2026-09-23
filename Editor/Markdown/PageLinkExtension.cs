using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>Adds <see cref="PageLinkParser"/> ahead of Markdig's own links, which would otherwise take the first bracket.</summary>
    public sealed class PageLinkExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<PageLinkParser>())
            {
                pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new PageLinkParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }
    }
}
