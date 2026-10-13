using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// Where the Code Reference's pages come from: made from the code, never written. A page's file
    /// is the script its type is written in, so showing the file, its date and who last changed it
    /// all go to the script.
    /// <para>
    /// Pictures put in a note are kept with the notes, in the CookBook's own folder: anything under
    /// <c>.code-notes/</c> is read and written there.
    /// </para>
    /// <para>
    /// Its paths start with <c>code:</c> — <c>"code:/CookbookEditor/Pages/DocLibrary.md"</c> — so
    /// they can never be taken for a path in another source.
    /// </para>
    /// </summary>
    public sealed class CodeSource : IDocSource, IDisposable
    {
        public const string PathPrefix = "code:";

        private readonly Dictionary<string, string> scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly IDocSource notes;

        /// <param name="notes">The CookBook's own folder, where notes and their pictures are kept.</param>
        public CodeSource(IDocSource notes)
        {
            this.notes = notes;
            CookbookSettings.CodeSourcesChanged += SourcesChanged;
        }

        public event Action Changed;

        public string Name => "Code Reference";

        /// <summary>Notes the script a page is made from.</summary>
        public void Remember(string path, string script)
        {
            scripts[path] = script;
        }

        public void Forget()
        {
            scripts.Clear();
        }

        public string ScriptOf(string path)
        {
            return scripts.TryGetValue(path, out var script) ? script : null;
        }

        public IEnumerable<string> Folders()
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> Files()
        {
            return Enumerable.Empty<string>();
        }

        public bool Exists(string path)
        {
            return IsNote(path) ? notes.Exists(path) : scripts.ContainsKey(path);
        }

        public string ReadText(string path)
        {
            return string.Empty;
        }

        public void WriteText(string path, string text)
        {
            throw new InvalidOperationException("The Code Reference is made from the code and cannot be written; write a note instead.");
        }

        public byte[] ReadBytes(string path)
        {
            return IsNote(path) ? notes.ReadBytes(path) : null;
        }

        public void WriteBytes(string path, byte[] bytes)
        {
            if (!IsNote(path))
            {
                throw new InvalidOperationException("Pictures for the Code Reference are kept with its notes.");
            }

            notes.WriteBytes(path, bytes);
        }

        public void CreateFolder(string path)
        {
        }

        public void Move(string from, string to)
        {
        }

        public void Remove(string path)
        {
        }

        public DateTime Modified(string path)
        {
            if (IsNote(path))
            {
                return notes.Modified(path);
            }

            var script = ScriptOf(path);
            return script != null && File.Exists(script) ? File.GetLastWriteTime(script) : DateTime.MinValue;
        }

        public string LocalPath(string path)
        {
            if (IsNote(path))
            {
                return notes.LocalPath(path);
            }

            var script = ScriptOf(path);
            return script == null ? null : Path.GetFullPath(script);
        }

        public void Dispose()
        {
            CookbookSettings.CodeSourcesChanged -= SourcesChanged;
        }

        private static bool IsNote(string path)
        {
            return path != null && path.StartsWith(CodeNotes.Folder + "/", StringComparison.Ordinal);
        }

        private void SourcesChanged()
        {
            CodeModel.Forget();
            Changed?.Invoke();
        }
    }
}
