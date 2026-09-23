using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Index
{
    /// <summary>
    /// Whether a page might be out of date: the scripts whose code names it with [Documentation]
    /// have been changed since the page was. Judged by when the files were last written — a
    /// fresh checkout makes everything look new at once, so it is a nudge, not a verdict.
    /// </summary>
    public static class DocStaleness
    {
        private static readonly TimeSpan Slack = TimeSpan.FromMinutes(1);

        /// <summary>The scripts changed since the page, with when; empty when the page is up to date or no code names it.</summary>
        public static List<(string script, DateTime changed)> ChangedSince(DocLibrary library, DocPage page)
        {
            var written = page.Source.Modified(page.FilePath);
            return DocCodeIndex.For(library, page)
                .Select(link => link.Script)
                .Where(script => script != null && File.Exists(script))
                .Distinct()
                .Select(script => (script, File.GetLastWriteTime(script)))
                .Where(pair => pair.Item2 > written + Slack)
                .OrderByDescending(pair => pair.Item2)
                .ToList();
        }

        /// <summary>Every page that might be out of date, with the scripts changed since.</summary>
        public static List<(DocPage page, List<(string script, DateTime changed)> scripts)> All(DocLibrary library)
        {
            return DocCodeIndex.All
                .Select(link => library.Find(link.Page))
                .Where(page => page != null && library.Shown(page))
                .Distinct()
                .Select(page => (page, ChangedSince(library, page)))
                .Where(pair => pair.Item2.Count > 0)
                .ToList();
        }
    }
}
