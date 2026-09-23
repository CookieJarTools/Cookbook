using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Index
{
    /// <summary>One link or picture in a page, and where it turned out to go.</summary>
    public sealed class PageLink
    {
        public PageLink(DocPage from, string url, bool isImage)
        {
            From = from;
            Url = url;
            IsImage = isImage;
        }

        public DocPage From { get; }

        public string Url { get; }

        public bool IsImage { get; }

        /// <summary>The page it goes to, when it goes to one.</summary>
        public DocPage To { get; set; }

        /// <summary>The project asset it names — linked, or shown in the page — when it names one.</summary>
        public string Asset { get; set; }

        /// <summary>The picture file it shows, from the Cookbook's folder, when it is a picture kept with the pages.</summary>
        public string Picture { get; set; }

        /// <summary>A guide step: an action or a check.</summary>
        public bool IsStep { get; set; }

        /// <summary>Whether it goes anywhere: false for a page, asset, picture or step that is not there.</summary>
        public bool Works { get; set; } = true;
    }
}
