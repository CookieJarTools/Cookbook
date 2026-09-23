using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>What a drawn page asks of the window showing it, when a link or a checkbox is clicked.</summary>
    public interface IDocNavigator
    {
        DocLibrary Library { get; }

        void Open(DocPage page, string anchor);

        void ScrollTo(string anchor);

        /// <summary>A link to a page that does not exist yet, by the name given.</summary>
        void Missing(string reference);

        /// <summary>Ticks or unticks the checklist box at a place in the page's Markdown.</summary>
        void ToggleTask(int position);
    }
}
