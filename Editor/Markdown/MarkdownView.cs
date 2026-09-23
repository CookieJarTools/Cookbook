using CookieJarTools.Cookbook.Editor.Pages;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>A page's Markdown, drawn. Drawn again from scratch each time it is shown.</summary>
    public sealed class MarkdownView : VisualElement
    {
        private readonly IDocNavigator navigator;

        public MarkdownView(IDocNavigator navigator)
        {
            this.navigator = navigator;
            AddToClassList("cookbook-markdown");
        }

        public void Show(DocPage page, string markdown)
        {
            Clear();
            new BlockBuilder(navigator, page).AddBlocks(this, CookbookMarkdown.Parse(markdown));
        }

        /// <summary>The heading a <c>#anchor</c> link points at, or null.</summary>
        public VisualElement Anchor(string anchor)
        {
            return string.IsNullOrEmpty(anchor) ? null : this.Q(BlockBuilder.AnchorPrefix + anchor);
        }
    }
}
