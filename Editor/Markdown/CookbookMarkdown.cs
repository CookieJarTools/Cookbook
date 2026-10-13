using Markdig;
using Markdig.Syntax;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// The Markdown the CookBook reads: CommonMark, plus tables, checklists, bare links,
    /// strikethrough and highlights, GitHub-style callouts (<c>&gt; [!NOTE]</c>), heading anchors,
    /// and <c>[[Page]]</c> links.
    /// </summary>
    public static class CookbookMarkdown
    {
        private static MarkdownPipeline pipeline;

        public static MarkdownPipeline Pipeline => pipeline ??= Build();

        public static MarkdownDocument Parse(string markdown)
        {
            return Markdig.Markdown.Parse(markdown ?? string.Empty, Pipeline);
        }

        /// <summary>The words of a page with the Markdown taken out, for searching.</summary>
        public static string PlainText(string markdown)
        {
            return Markdig.Markdown.ToPlainText(markdown ?? string.Empty, Pipeline);
        }

        private static MarkdownPipeline Build()
        {
            var builder = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .UseGridTables()
                .UseTaskLists()
                .UseAutoLinks()
                .UseEmphasisExtras()
                .UseAlertBlocks()
                .UseAutoIdentifiers();
            builder.Extensions.AddIfNotAlready<PageLinkExtension>();
            return builder.Build();
        }
    }
}
