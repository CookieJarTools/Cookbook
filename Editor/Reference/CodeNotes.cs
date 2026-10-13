using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// Notes written for types in the Code Reference, kept in the CookBook's own folder under
    /// <c>.code-notes</c> — hidden from its tree — one Markdown file per type, named by its key:
    /// <c>.code-notes/Ns.DocLibrary.md</c>. The page is made again from the code each time; the
    /// note is what lasts. Its header holds the page's tags.
    /// </summary>
    public static class CodeNotes
    {
        public const string Folder = ".code-notes";

        public static string PathOf(string key)
        {
            return Folder + "/" + key + DocPage.Extension;
        }

        /// <summary>The note's header and words, or an empty header and no words when none has been written.</summary>
        public static DocHeader Read(IDocSource source, string key, out string body)
        {
            var path = PathOf(key);
            if (!source.Exists(path))
            {
                body = string.Empty;
                return new DocHeader();
            }

            return DocHeader.Split(source.ReadText(path), out body);
        }

        /// <summary>Writes the note, or takes it away — to the trash — when there is nothing left in it.</summary>
        public static void Write(IDocSource source, string key, IEnumerable<string> tags, string body)
        {
            var path = PathOf(key);
            var header = Read(source, key, out _);
            header.Id = null;
            header.Title = null;
            header.Tags = tags.ToList();
            header.Updated = DateTime.Now.ToString("yyyy-MM-dd");
            if (string.IsNullOrWhiteSpace(body) && header.Tags.Count == 0)
            {
                Remove(source, path);
                return;
            }

            source.WriteText(path, header.Join(body.Trim() + "\n"));
        }

        /// <summary>The key of every type a note has been written for.</summary>
        public static List<string> Keys(IDocSource source)
        {
            var folder = source.LocalPath(Folder);
            if (folder == null || !Directory.Exists(folder))
            {
                return new List<string>();
            }

            return Directory.EnumerateFiles(folder, "*" + DocPage.Extension).Select(Path.GetFileNameWithoutExtension).ToList();
        }

        private static void Remove(IDocSource source, string path)
        {
            if (source.Exists(path))
            {
                source.Remove(path);
            }
        }
    }
}
