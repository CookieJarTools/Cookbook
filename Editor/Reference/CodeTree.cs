using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// Arranges the Code Reference: a folder for each source, the namespaces inside it, a page for
    /// each type — only as deep as helps:
    /// <list type="bullet">
    /// <item>A namespace holding nothing but one other is merged with it: <c>CookieJarTools.CookBook.Editor</c>.</item>
    /// <item>A namespace holding just one type is replaced by the type's page.</item>
    /// <item>A type declared inside another goes under the other's page.</item>
    /// </list>
    /// </summary>
    public sealed class CodeTree
    {
        public const string RootId = "code";
        public const string RootTitle = "Code Reference";

        private const string FolderIdPrefix = "code-folder:";
        private const string RootBody = "Made from the `///` comments in your code, and kept up to date with it: every public type, its members, and the fields Unity serializes.\n";

        private readonly CodeModel model;
        private readonly CodeSource source;
        private readonly IDocSource notes;
        private readonly Func<DocPage, DocPage> index;
        private readonly Dictionary<Type, DocPage> pages = new Dictionary<Type, DocPage>();

        /// <param name="notes">The CookBook's own folder, where notes are kept.</param>
        /// <param name="index">Makes each page and folder findable by its id.</param>
        public CodeTree(CodeModel model, CodeSource source, IDocSource notes, Func<DocPage, DocPage> index)
        {
            this.model = model;
            this.source = source;
            this.notes = notes;
            this.index = index;
        }

        public DocPage Build()
        {
            source.Forget();
            var root = index(new DocPage(CodeSource.PathPrefix, true, new DocHeader { Id = RootId }, RootBody, source) { DisplayTitle = RootTitle });
            foreach (var (path, types) in model.Sources)
            {
                AddSource(root, path, types);
            }

            return root;
        }

        private void AddSource(DocPage root, string path, List<CodeType> types)
        {
            var body = "Everything public in the " + CodeScope.Describe(path).ToLowerInvariant() + " `" + path + "`.\n";
            var folder = Folder(path, CodeScope.Title(path), body);
            Attach(folder, root);
            var outer = types.Where(t => !t.Type.IsNested || types.All(o => o.Type != t.Type.DeclaringType)).ToList();
            AddNamespace(folder, path, string.Empty, outer);
            foreach (var type in types.Where(t => !outer.Contains(t)).OrderBy(t => Depth(t.Type)))
            {
                Attach(TypePage(type), pages[type.Type.DeclaringType]);
            }
        }

        /// <summary>The types in a namespace, and each namespace below it as a folder — merged or lifted where that helps.</summary>
        private void AddNamespace(DocPage folder, string sourcePath, string space, List<CodeType> types)
        {
            foreach (var type in types.Where(t => t.Namespace == space).OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase))
            {
                Attach(TypePage(type), folder);
            }

            foreach (var child in ChildNamespaces(space, types))
            {
                var inner = Deepest(child, types);
                var within = types.Where(t => Within(t.Namespace, inner)).ToList();
                if (within.Count == 1)
                {
                    Attach(TypePage(within[0]), folder);
                    continue;
                }

                var title = space.Length == 0 ? inner : inner.Substring(space.Length + 1);
                var namespaceFolder = Folder(sourcePath + "/" + inner, title, string.Empty);
                Attach(namespaceFolder, folder);
                AddNamespace(namespaceFolder, sourcePath, inner, within);
            }
        }

        /// <summary>Follows a namespace down while it holds no types and just one namespace.</summary>
        private static string Deepest(string space, List<CodeType> types)
        {
            while (types.All(t => t.Namespace != space))
            {
                var below = ChildNamespaces(space, types);
                if (below.Count != 1)
                {
                    break;
                }

                space = below[0];
            }

            return space;
        }

        /// <summary>The namespaces one level below, that hold types somewhere inside them.</summary>
        private static List<string> ChildNamespaces(string space, List<CodeType> types)
        {
            var prefix = space.Length == 0 ? string.Empty : space + ".";
            return types.Select(t => t.Namespace)
                .Where(n => n.Length > prefix.Length && n.StartsWith(prefix, StringComparison.Ordinal))
                .Select(n => prefix + n.Substring(prefix.Length).Split('.')[0])
                .Distinct()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int Depth(Type type)
        {
            var depth = 0;
            for (var at = type; at.IsNested; at = at.DeclaringType)
            {
                depth++;
            }

            return depth;
        }

        private static bool Within(string space, string outer)
        {
            return space == outer || space.StartsWith(outer + ".", StringComparison.Ordinal);
        }

        private DocPage TypePage(CodeType type)
        {
            var header = CodeNotes.Read(notes, type.Key, out var note);
            header.Id = CodeLinks.Id(type.Key);
            var path = CodeSource.PathPrefix + "/" + type.Source + "/" + type.Key + DocPage.Extension;
            var page = new DocPage(path, false, header, model.Page(type, note), source) { DisplayTitle = type.Title, Code = type, Note = note };
            source.Remember(path, type.Script);
            pages[type.Type] = page;
            return index(page);
        }

        private DocPage Folder(string path, string title, string body)
        {
            var header = new DocHeader { Id = FolderIdPrefix + path };
            return index(new DocPage(CodeSource.PathPrefix + "/" + path, true, header, body, source) { DisplayTitle = title });
        }

        private static void Attach(DocPage page, DocPage parent)
        {
            page.Parent = parent;
            parent.Children.Add(page);
        }
    }
}
