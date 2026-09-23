using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Search
{
    /// <summary>A page that matched a search: how well, and the words around the first match, highlighted as rich text.</summary>
    public sealed class SearchHit
    {
        public SearchHit(DocPage page, int score, string snippet)
        {
            Page = page;
            Score = score;
            Snippet = snippet;
        }

        public DocPage Page { get; }

        public int Score { get; }

        public string Snippet { get; }
    }
}
