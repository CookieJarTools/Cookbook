using System.Collections.Generic;
using CookieJarTools.Cookbook.Editor.Reference;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// A page or a folder in the Cookbook. A folder is a folder in the source; its banner and intro
    /// come from a <c>_folder.md</c> inside it. A page is a <c>.md</c> file, titled by its file name
    /// unless its header gives a title.
    /// <para>
    /// A page knows the source it came from: the Cookbook's own folder, the project, for
    /// Markdown found among the assets, or the code, for the Code Reference's pages.
    /// </para>
    /// </summary>
    public sealed class DocPage
    {
        public const string Extension = ".md";
        public const string FolderFile = "_folder.md";

        public DocPage(string path, bool isFolder, DocHeader header, string body, IDocSource source)
        {
            Path = path;
            IsFolder = isFolder;
            Header = header;
            Body = body;
            Source = source;
        }

        /// <summary>Where it is in the source: <c>"Combat/Damage.md"</c> for a page, <c>"Combat"</c> for a folder, <c>""</c> for the root.</summary>
        public string Path { get; }

        public bool IsFolder { get; }

        public DocHeader Header { get; }

        public string Body { get; }

        public IDocSource Source { get; }

        /// <summary>Found among the project's assets rather than in the Cookbook's own folder.</summary>
        public bool IsProject => Source is ProjectSource;

        /// <summary>The folder holding everything found in the project.</summary>
        public bool IsProjectRoot => IsProject && IsFolder && Path.Length == 0;

        /// <summary>Made from the code, for the Code Reference.</summary>
        public bool IsCode => Source is CodeSource;

        /// <summary>The Code Reference's own folder.</summary>
        public bool IsCodeRoot => IsCode && IsFolder && Path == CodeSource.PathPrefix;

        /// <summary>The type a Code Reference page is about; null for any other page.</summary>
        public CodeType Code { get; set; }

        /// <summary>What someone wrote for a Code Reference page, without its header: what editing it changes.</summary>
        public string Note { get; set; }

        /// <summary>The words that editing the page changes: the page's own, or, for the Code Reference, its note.</summary>
        public string EditableBody => Code != null ? Note ?? string.Empty : Body;

        /// <summary>What to call it when the header gives no title: a project page's first heading, a project folder's path.</summary>
        public string DisplayTitle { get; set; }

        public DocPage Parent { get; set; }

        public List<DocPage> Children { get; } = new List<DocPage>();

        /// <summary>The Cookbook's own top folder — its home page.</summary>
        public bool IsRoot => IsFolder && Path.Length == 0 && !IsProject && !IsCode;

        /// <summary>The name of the file or folder, without the extension.</summary>
        public string Name
        {
            get
            {
                var slash = Path.LastIndexOf('/');
                var name = slash < 0 ? Path : Path.Substring(slash + 1);
                return IsFolder ? name : name.Substring(0, name.Length - Extension.Length);
            }
        }

        public string Title => !string.IsNullOrWhiteSpace(Header.Title) ? Header.Title : !string.IsNullOrWhiteSpace(DisplayTitle) ? DisplayTitle : Name;

        /// <summary>How [Documentation] and links can name it: folders and name, no extension — <c>"Combat/Damage"</c>.</summary>
        public string Reference => IsFolder ? Path : Path.Substring(0, Path.Length - Extension.Length);

        /// <summary>The file its header and Markdown are kept in.</summary>
        public string FilePath => IsFolder ? Join(Path, FolderFile) : Path;

        /// <summary>Whether its words can be written in the Cookbook: not a project or code folder, which have none of their own. A Code Reference page's note can be.</summary>
        public bool IsEditable => !((IsProject || IsCode) && IsFolder);

        /// <summary>The folder its images and relative links are found from — for the Code Reference, the notes' folder.</summary>
        public string Directory
        {
            get
            {
                if (Code != null)
                {
                    return CodeNotes.Folder;
                }

                if (IsFolder)
                {
                    return Path;
                }

                var slash = Path.LastIndexOf('/');
                return slash < 0 ? string.Empty : Path.Substring(0, slash);
            }
        }

        public static string Join(string folder, string name)
        {
            return string.IsNullOrEmpty(folder) ? name : folder + "/" + name;
        }

        /// <summary>Its own tags and those of every folder it is in: a folder's tags cover everything inside it.</summary>
        public IEnumerable<string> AllTags()
        {
            var tags = new List<string>();
            for (var page = this; page != null; page = page.Parent)
            {
                tags.AddRange(page.Header.Tags);
            }

            return tags;
        }

        public bool HasTag(string tag)
        {
            foreach (var own in AllTags())
            {
                if (string.Equals(own, tag, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The folders above it, outermost first, not counting the root.</summary>
        public IEnumerable<DocPage> Trail()
        {
            var trail = new List<DocPage>();
            for (var page = Parent; page != null && !page.IsRoot && !page.IsProjectRoot && !page.IsCodeRoot; page = page.Parent)
            {
                trail.Insert(0, page);
            }

            return trail;
        }

        public override string ToString()
        {
            return Reference;
        }
    }
}
