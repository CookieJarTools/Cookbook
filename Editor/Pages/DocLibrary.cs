using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Reference;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// Every page and folder, as a tree, and the ways to find and change them. Read in full when
    /// made and again whenever a source changes — pages are small, and it keeps search and links
    /// simple.
    /// <para>
    /// Three sources: the CookBook's own folder, whose tree is <see cref="Root"/>; Markdown found
    /// among the project's assets, gathered under <see cref="ProjectRoot"/> in the project's own
    /// folders (tidied by <see cref="ProjectTree"/>); and the Code Reference under
    /// <see cref="CodeRoot"/>, made from the code chosen for it (see <see cref="CodeTree"/>). The
    /// project's pages and the Code Reference can each be hidden; each person chooses for themselves.
    /// </para>
    /// <para>
    /// The Code Reference's pages are found only by id — <c>"code:Ns.Type"</c>, or
    /// <c>"code:Type"</c> when only one type has that name — so a type never takes a title or
    /// path a written page answers to.
    /// </para>
    /// <para>
    /// Pages are found by their id, by their reference (<c>"Combat/Damage"</c>,
    /// <c>"Assets/Plugins/Tool/README"</c>), or by a title when only one page has it.
    /// </para>
    /// </summary>
    public sealed class DocLibrary
    {
        public const string ProjectRootId = "project";

        private const string ImagesFolder = "images";
        private const int IdLength = 12;
        private const string ShowProjectKey = "CookieJar.Cookbook.ShowProjectDocs";
        private const string ShowCodeKey = "CookieJar.Cookbook.ShowCodeReference";
        private const string ProjectRootTitle = "Found in the project";

        private static DocLibrary current;

        private readonly Dictionary<string, DocPage> byId = new Dictionary<string, DocPage>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DocPage> byReference = new Dictionary<string, DocPage>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<DocPage>> byTitle = new Dictionary<string, List<DocPage>>(StringComparer.OrdinalIgnoreCase);

        private DocLibrary(IDocSource source, IDocSource project, CodeSource code)
        {
            Source = source;
            Project = project;
            Code = code;
            Source.Changed += Reload;
            Project.Changed += Reload;
            Code.Changed += Reload;
            Reload();
        }

        /// <summary>Anything in the library changed: re-read after an outside edit, or changed by the CookBook.</summary>
        public event Action Reloaded;

        /// <summary>The project's library, from the folder in the CookBook settings and the project's own Markdown.</summary>
        public static DocLibrary Current
        {
            get
            {
                var root = CookbookSettings.instance.Root;
                if (current == null || current.RootFolder != root)
                {
                    current?.Dispose();
                    var folder = new FolderSource(root);
                    current = new DocLibrary(folder, new ProjectSource(), new CodeSource(folder)) { RootFolder = root };
                }

                return current;
            }
        }

        /// <summary>The CookBook's own folder.</summary>
        public IDocSource Source { get; }

        /// <summary>Markdown among the project's assets.</summary>
        public IDocSource Project { get; }

        /// <summary>The Code Reference's pages, made from the code.</summary>
        public CodeSource Code { get; }

        public DocPage Root { get; private set; }

        /// <summary>The folder holding what was found in the project, arranged in the project's own folders.</summary>
        public DocPage ProjectRoot { get; private set; }

        /// <summary>The Code Reference: a folder for each source of code chosen, with a page for each type.</summary>
        public DocPage CodeRoot { get; private set; }

        /// <summary>Counts up every time the pages are read again, so anything worked out from them knows when to look again.</summary>
        public int Version { get; private set; }

        /// <summary>Whether pages found in the project are shown. Each person's own choice, kept on their machine.</summary>
        public bool ShowProject
        {
            get => EditorPrefs.GetBool(ShowProjectKey, true);
            set
            {
                EditorPrefs.SetBool(ShowProjectKey, value);
                Version++;
                Reloaded?.Invoke();
            }
        }

        /// <summary>Whether the Code Reference is shown. Each person's own choice, kept on their machine.</summary>
        public bool ShowCode
        {
            get => EditorPrefs.GetBool(ShowCodeKey, true);
            set
            {
                EditorPrefs.SetBool(ShowCodeKey, value);
                Version++;
                Reloaded?.Invoke();
            }
        }

        /// <summary>Every page — not folders — that is being shown; the Code Reference's are not counted.</summary>
        public IEnumerable<DocPage> Pages => byReference.Values.Where(p => !p.IsFolder && Shown(p));

        /// <summary>Everything shown, folders too: the CookBook's own tree, then what was found in the project when that is shown.</summary>
        public IEnumerable<DocPage> Everything
        {
            get
            {
                var project = ShowProject ? Within(ProjectRoot).Prepend(ProjectRoot) : Enumerable.Empty<DocPage>();
                var code = ShowCode ? Within(CodeRoot).Prepend(CodeRoot) : Enumerable.Empty<DocPage>();
                return Within(Root).Concat(project).Concat(code);
            }
        }

        private string RootFolder { get; set; }

        /// <summary>A page by id, by reference, or by a title only one page has; null when none matches.</summary>
        public DocPage Find(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            var key = reference.Trim().Trim('/');
            if (key.EndsWith(DocPage.Extension, StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(0, key.Length - DocPage.Extension.Length);
            }

            if (byId.TryGetValue(key, out var page) || byReference.TryGetValue(key, out page))
            {
                return page;
            }

            if (key.StartsWith(CodeLinks.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return FindCode(key.Substring(CodeLinks.Scheme.Length));
            }

            return byTitle.TryGetValue(key, out var titled) && titled.Count == 1 ? titled[0] : null;
        }

        public bool Shown(DocPage page)
        {
            return page != null && (!page.IsProject || ShowProject) && (!page.IsCode || ShowCode);
        }

        public void Reload()
        {
            byId.Clear();
            byReference.Clear();
            byTitle.Clear();
            ReadCookbook();
            ReadProject();
            ReadCode();
            Version++;
            Reloaded?.Invoke();
        }

        /// <summary>
        /// Writes a page: a new id if it has none, and today as updated. A new title renames the file
        /// or folder — or, for a page whose header gives its title, just changes that.
        /// <para>
        /// A page found in the project keeps its file name and title, and is only given a header
        /// when there is something to put in it — someone's README is not rewritten for nothing.
        /// </para>
        /// </summary>
        public DocPage Save(DocPage page, string title, IEnumerable<string> tags, string body)
        {
            if (page.IsProject)
            {
                return SaveProjectPage(page, tags, body);
            }

            if (page.IsCode)
            {
                return SaveNote(page, tags, body);
            }

            var header = page.Header;
            var titledInHeader = !string.IsNullOrWhiteSpace(header.Title);
            header.Id = string.IsNullOrEmpty(header.Id) ? NewId() : header.Id;
            header.Tags = CleanTags(tags);
            header.Updated = DateTime.Now.ToString("yyyy-MM-dd");
            header.Title = titledInHeader ? title : null;

            page.Source.WriteText(page.FilePath, header.Join(body));
            var saved = ReloadedById(header.Id);
            return titledInHeader || saved == null ? saved : Rename(saved, title);
        }

        public DocPage CreatePage(DocPage folder, string title, string body = null, IEnumerable<string> tags = null)
        {
            var path = Free(folder.Source, DocPage.Join(folder.Path, FileName(title)), DocPage.Extension);
            var header = new DocHeader { Id = NewId(), Updated = DateTime.Now.ToString("yyyy-MM-dd"), Tags = CleanTags(tags ?? Enumerable.Empty<string>()) };
            folder.Source.WriteText(path, header.Join(body ?? "# " + title + "\n\n"));
            return ReloadedById(header.Id);
        }

        public DocPage CreateFolder(DocPage parent, string title)
        {
            var path = Free(parent.Source, DocPage.Join(parent.Path, FileName(title)), string.Empty);
            var header = new DocHeader { Id = NewId() };
            parent.Source.CreateFolder(path);
            parent.Source.WriteText(DocPage.Join(path, DocPage.FolderFile), header.Join(string.Empty));
            return ReloadedById(header.Id);
        }

        /// <summary>A new name for a page or folder: its file or folder is renamed.</summary>
        public DocPage Rename(DocPage page, string title)
        {
            if (page.IsRoot || page.IsProjectRoot || page.IsCode || title == page.Name)
            {
                return page;
            }

            var path = Renamed(page, title);
            page.Source.Move(page.Path, path);
            return ReloadedAt(page, path);
        }

        /// <summary>Into another folder of the same source, keeping its name — numbered if the folder already has one by that name.</summary>
        public DocPage Move(DocPage page, DocPage folder)
        {
            if (page.IsRoot || page.IsProjectRoot || page.IsCode || folder.IsCode || page.Parent == folder || IsWithin(folder, page) || page.Source != folder.Source || folder.IsProjectRoot)
            {
                return page;
            }

            var extension = page.IsFolder ? string.Empty : DocPage.Extension;
            var path = Free(page.Source, DocPage.Join(folder.Path, page.Name), extension);
            page.Source.Move(page.Path, path);
            return ReloadedAt(page, path);
        }

        /// <summary>Out of the CookBook, into the source's trash — a folder with everything in it.</summary>
        public void Remove(DocPage page)
        {
            if (page.IsRoot || page.IsProjectRoot || page.IsCode)
            {
                return;
            }

            page.Source.Remove(page.Path);
            Reload();
        }

        /// <summary>Saves an image next to a page, under a name not already taken; the path back is relative to the page.</summary>
        public string AddImage(DocPage page, string name, byte[] bytes)
        {
            var stem = FileName(Path.GetFileNameWithoutExtension(name));
            var extension = Path.GetExtension(name).ToLowerInvariant();
            var path = Free(page.Source, DocPage.Join(DocPage.Join(page.Directory, ImagesFolder), stem), extension);
            page.Source.WriteBytes(path, bytes);
            return path.Substring(page.Directory.Length).TrimStart('/');
        }

        /// <summary>Every tag used, on pages or folders shown, with how many have it — most used first.</summary>
        public List<(string tag, int count)> Tags()
        {
            return Everything
                .SelectMany(p => p.Header.Tags)
                .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                .Select(g => (g.First(), g.Count()))
                .OrderByDescending(t => t.Item2)
                .ThenBy(t => t.Item1)
                .ToList();
        }

        /// <summary>The pages changed most recently, going by their files.</summary>
        public List<DocPage> Recent(int count)
        {
            return Pages.OrderByDescending(p => p.Source.Modified(p.FilePath)).Take(count).ToList();
        }

        /// <summary>A page or folder's pages and folders, and theirs, all the way down.</summary>
        public static IEnumerable<DocPage> Within(DocPage folder)
        {
            foreach (var child in folder.Children)
            {
                yield return child;
                foreach (var inner in Within(child))
                {
                    yield return inner;
                }
            }
        }

        private void Dispose()
        {
            (Source as IDisposable)?.Dispose();
            (Project as IDisposable)?.Dispose();
            Code.Dispose();
        }

        private void ReadCookbook()
        {
            Root = Folder(Source, string.Empty);
            var folders = new Dictionary<string, DocPage> { [string.Empty] = Root };
            foreach (var path in Source.Folders().Where(IsDocFolder).OrderBy(p => p.Count(c => c == '/')))
            {
                folders[path] = Folder(Source, path);
                Attach(folders, folders[path]);
            }

            foreach (var path in Source.Files().Where(IsPageFile))
            {
                Attach(folders, Page(Source, path));
            }

            Sort(Root);
        }

        /// <summary>The project's Markdown, in the project's own folders — see <see cref="ProjectTree"/> for how they are tidied.</summary>
        private void ReadProject()
        {
            ProjectRoot = new DocPage(string.Empty, true, new DocHeader { Id = ProjectRootId }, string.Empty, Project) { DisplayTitle = ProjectRootTitle };
            byId[ProjectRootId] = ProjectRoot;
            var found = Project.Files().Select(path => ProjectPage(path)).ToList();
            new ProjectTree(found, ProjectFolder).Build(ProjectRoot);
            Sort(ProjectRoot);
        }

        /// <summary>The Code Reference, made again from the code chosen for it — see <see cref="CodeTree"/>.</summary>
        private void ReadCode()
        {
            CodeRoot = new CodeTree(CodeModel.Current, Code, Source, IndexById).Build();
        }

        /// <summary>A Code Reference page by its type's full key, or by its name when only one type has it.</summary>
        private DocPage FindCode(string key)
        {
            var type = CodeModel.Current.Find(key.Trim()) ?? CodeModel.Current.FindByName(key.Trim());
            return type != null && byId.TryGetValue(CodeLinks.Id(type.Key), out var page) ? page : null;
        }

        /// <summary>A type's note written, then the Code Reference made again with it.</summary>
        private DocPage SaveNote(DocPage page, IEnumerable<string> tags, string body)
        {
            CodeNotes.Write(Source, page.Code.Key, CleanTags(tags), body);
            return ReloadedById(page.Header.Id);
        }

        private DocPage IndexById(DocPage page)
        {
            byId[page.Header.Id] = page;
            return page;
        }

        private DocPage ProjectPage(string path)
        {
            var page = Page(Project, path);
            page.DisplayTitle = FirstHeading(page.Body) ?? page.Name;
            return page;
        }

        private DocPage ProjectFolder(string path, string title)
        {
            return Index(new DocPage(path, true, new DocHeader(), string.Empty, Project) { DisplayTitle = title });
        }

        private DocPage SaveProjectPage(DocPage page, IEnumerable<string> tags, string body)
        {
            var header = page.Header;
            header.Tags = CleanTags(tags);
            header.Title = null;
            page.Source.WriteText(page.FilePath, header.IsEmpty ? body : header.Join(body));
            Reload();
            return Find(page.Reference);
        }

        /// <summary>The text of the first <c>#</c> heading, for a project page's title: a README is better known by what it is about.</summary>
        private static string FirstHeading(string body)
        {
            foreach (var line in body.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("# "))
                {
                    return trimmed.Substring(2).Trim();
                }
            }

            return null;
        }

        private static List<string> CleanTags(IEnumerable<string> tags)
        {
            return tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList();
        }

        private static bool IsDocFolder(string path)
        {
            return !path.Split('/').Any(part => part.Equals(ImagesFolder, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsPageFile(string path)
        {
            var name = path.Substring(path.LastIndexOf('/') + 1);
            return path.EndsWith(DocPage.Extension, StringComparison.OrdinalIgnoreCase) && name != DocPage.FolderFile && IsDocFolder(path);
        }

        private static bool IsWithin(DocPage page, DocPage folder)
        {
            for (var at = page; at != null; at = at.Parent)
            {
                if (at == folder)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Sort(DocPage folder)
        {
            folder.Children.Sort((a, b) => a.Header.Order != b.Header.Order ? a.Header.Order.CompareTo(b.Header.Order) : a.IsFolder != b.IsFolder ? (a.IsFolder ? -1 : 1) : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            foreach (var child in folder.Children.Where(c => c.IsFolder))
            {
                Sort(child);
            }
        }

        private static string NewId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, IdLength);
        }

        /// <summary>A title as a file name: what a file name cannot hold taken out.</summary>
        private static string FileName(string title)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var name = new string(title.Trim().Where(c => !invalid.Contains(c)).ToArray()).Trim('.', ' ');
            return name.Length == 0 ? "Untitled" : name;
        }

        private DocPage Folder(IDocSource source, string path)
        {
            var header = DocHeader.Split(source.ReadText(DocPage.Join(path, DocPage.FolderFile)), out var body);
            return Index(new DocPage(path, true, header, body, source));
        }

        private DocPage Page(IDocSource source, string path)
        {
            var header = DocHeader.Split(source.ReadText(path), out var body);
            return Index(new DocPage(path, false, header, body, source));
        }

        private DocPage Index(DocPage page)
        {
            if (!string.IsNullOrEmpty(page.Header.Id))
            {
                byId[page.Header.Id] = page;
            }

            byReference[page.Reference] = page;
            if (!byTitle.TryGetValue(page.Name, out var titled))
            {
                byTitle[page.Name] = titled = new List<DocPage>();
            }

            titled.Add(page);
            return page;
        }

        private static void Attach(Dictionary<string, DocPage> folders, DocPage page)
        {
            var slash = page.Path.LastIndexOf('/');
            var parentPath = slash < 0 ? string.Empty : page.Path.Substring(0, slash);
            if (folders.TryGetValue(parentPath, out var parent))
            {
                page.Parent = parent;
                parent.Children.Add(page);
            }
        }

        private string Renamed(DocPage page, string title)
        {
            var extension = page.IsFolder ? string.Empty : DocPage.Extension;
            return Free(page.Source, DocPage.Join(ProjectSource.Parent(page.Path), FileName(title)), extension);
        }

        /// <summary>The path with the extension, numbered if something is already there: "Page 2.md".</summary>
        private static string Free(IDocSource source, string stem, string extension)
        {
            var path = stem + extension;
            for (var number = 2; source.Exists(path); number++)
            {
                path = stem + " " + number + extension;
            }

            return path;
        }

        private DocPage ReloadedById(string id)
        {
            Reload();
            return Find(id);
        }

        /// <summary>After a move or rename: the page again, by id if it has one, by where it went if not.</summary>
        private DocPage ReloadedAt(DocPage page, string path)
        {
            Reload();
            var id = page.Header.Id;
            var reference = page.IsFolder ? path : path.Substring(0, path.Length - DocPage.Extension.Length);
            return (string.IsNullOrEmpty(id) ? null : Find(id)) ?? Find(reference);
        }
    }
}
