using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Index;
using CookieJarTools.Cookbook.Editor.Pages;
using CookieJarTools.Cookbook.Editor.Reference;

namespace CookieJarTools.Cookbook.Editor.Health
{
    /// <summary>
    /// What needs looking after in the CookBook: pages whose code has changed since, links that go
    /// nowhere, pictures that are missing, guide steps the project no longer has, and pictures kept
    /// with the pages that no page shows any more — and notes kept for types the code no longer has.
    /// <para>
    /// Also, not counted as problems: the Code Reference's types with members nothing is written about.
    /// </para>
    /// </summary>
    public sealed class HealthReport
    {
        private const string ImagesFolder = "images/";

        private static readonly string[] PictureExtensions = { ".png", ".jpg", ".jpeg", ".gif" };

        public HealthReport(DocLibrary library)
        {
            var links = DocLinkIndex.For(library).Links.Where(l => library.Shown(l.From)).ToList();
            Stale = DocStaleness.All(library);
            BrokenLinks = links.Where(l => !l.Works && !l.IsImage && !l.IsStep).ToList();
            MissingPictures = links.Where(l => !l.Works && l.IsImage).ToList();
            MissingSteps = links.Where(l => !l.Works && l.IsStep).ToList();
            UnusedPictures = Unused(library, links);
            LostNotes = Lost(library);
            CodeGaps = library.ShowCode ? CodeModel.Current.Types.Select(t => (type: t, missing: t.Undocumented)).Where(g => g.missing > 0).OrderByDescending(g => g.missing).ToList() : new List<(CodeType, int)>();
        }

        public List<(DocPage page, List<(string script, DateTime changed)> scripts)> Stale { get; }

        public List<PageLink> BrokenLinks { get; }

        public List<PageLink> MissingPictures { get; }

        public List<PageLink> MissingSteps { get; }

        /// <summary>Pictures in the CookBook's own images folders that no page or banner shows.</summary>
        public List<string> UnusedPictures { get; }

        /// <summary>Notes written for types that no longer exist — renamed, moved to another namespace, or removed.</summary>
        public List<string> LostNotes { get; }

        /// <summary>The Code Reference's types, with how many of their parts — the type itself and its members — have no description.</summary>
        public List<(CodeType type, int missing)> CodeGaps { get; }

        public int Problems => Stale.Count + BrokenLinks.Count + MissingPictures.Count + MissingSteps.Count + UnusedPictures.Count + LostNotes.Count;

        /// <summary>Notes whose type no loaded assembly has — looked for in every assembly, so a note for code that is only left out of the reference for now is not lost.</summary>
        private static List<string> Lost(DocLibrary library)
        {
            var keys = CodeNotes.Keys(library.Source);
            if (keys.Count == 0)
            {
                return keys;
            }

            var known = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(Types).Select(CodeKeys.Of));
            return keys.Where(k => !known.Contains(k)).OrderBy(k => k).ToList();
        }

        private static IEnumerable<Type> Types(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException partly)
            {
                return partly.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// Only the CookBook's own folder is looked through: pictures among the project's assets
        /// are likely used by the game, not the docs.
        /// </summary>
        private static List<string> Unused(DocLibrary library, List<PageLink> links)
        {
            var shown = new HashSet<string>(links.Where(l => l.Picture != null).Select(l => l.Picture), StringComparer.OrdinalIgnoreCase);
            return library.Source.Files()
                .Where(p => p.StartsWith(ImagesFolder, StringComparison.OrdinalIgnoreCase) || p.IndexOf("/" + ImagesFolder, StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(p => PictureExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .Where(p => !shown.Contains(p))
                .OrderBy(p => p)
                .ToList();
        }
    }
}
