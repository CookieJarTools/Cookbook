using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Actions;
using CookieJarTools.Cookbook.Editor.Linking;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Index
{
    /// <summary>
    /// Every link and picture in every page, and where each goes: which pages link to a page, which
    /// mention an asset, and which links lead nowhere. Worked out again whenever the pages change.
    /// </summary>
    public sealed class DocLinkIndex
    {
        private static DocLinkIndex current;

        private readonly DocLibrary library;
        private readonly int version;

        private DocLinkIndex(DocLibrary library)
        {
            this.library = library;
            version = library.Version;
            foreach (var page in library.Everything.Prepend(library.Root).Where(p => !p.IsCode))
            {
                Read(page);
            }
        }

        public List<PageLink> Links { get; } = new List<PageLink>();

        /// <summary>The index for the library as it is now; made again only when the pages have changed.</summary>
        public static DocLinkIndex For(DocLibrary library)
        {
            if (current == null || current.library != library || current.version != library.Version)
            {
                current = new DocLinkIndex(library);
            }

            return current;
        }

        /// <summary>The other pages that link to a page.</summary>
        public IEnumerable<DocPage> To(DocPage page)
        {
            return Links.Where(l => l.To == page && l.From != page).Select(l => l.From).Distinct();
        }

        /// <summary>The pages that link to or show an asset.</summary>
        public IEnumerable<DocPage> ToAsset(string path)
        {
            return Links.Where(l => l.Asset == path).Select(l => l.From).Distinct();
        }

        private void Read(DocPage page)
        {
            var document = CookbookMarkdown.Parse(page.Body);
            foreach (var link in document.Descendants<LinkInline>())
            {
                Links.Add(Resolve(page, link.Url ?? string.Empty, link.IsImage));
            }

            foreach (var autolink in document.Descendants<AutolinkInline>())
            {
                Links.Add(new PageLink(page, autolink.Url, false));
            }

            if (page.IsFolder && !string.IsNullOrEmpty(page.Header.Banner))
            {
                Links.Add(Resolve(page, page.Header.Banner, true));
            }
        }

        private PageLink Resolve(DocPage from, string url, bool isImage)
        {
            var link = new PageLink(from, url, isImage);
            if (url.Length == 0 || url.StartsWith("#") || DocPaths.IsWeb(url))
            {
                return link;
            }

            if (GuideSteps.IsStep(url))
            {
                link.IsStep = true;
                link.Works = DocActions.IsAction(url) ? DocActions.Find(url) != null : DocChecks.Find(url) != null;
                return link;
            }

            if (DocPaths.IsProjectAsset(url))
            {
                link.Asset = DocPaths.Unescape(DocPaths.SplitAnchor(url, out _));
                link.Works = AssetDatabase.LoadMainAssetAtPath(link.Asset) != null;
                return link;
            }

            return isImage ? ResolvePicture(link, from) : ResolvePage(link, from);
        }

        private PageLink ResolvePicture(PageLink link, DocPage from)
        {
            link.Picture = DocPaths.Combine(from.Directory, link.Url);
            link.Works = link.Picture != null && from.Source.Exists(link.Picture);
            return link;
        }

        private PageLink ResolvePage(PageLink link, DocPage from)
        {
            link.To = DocLinks.Page(link.Url, library, from, out _);
            link.Works = link.To != null;
            return link;
        }
    }
}
