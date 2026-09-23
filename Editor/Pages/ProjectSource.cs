using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// Markdown files kept in the project itself — READMEs in <c>Assets</c>, docs that came with a
    /// plugin or package — so the Cookbook can show them next to its own pages. Paths are the
    /// project's own: <c>"Assets/Plugins/Tool/README.md"</c>.
    /// <para>
    /// They are assets, so they are changed through the AssetDatabase — moved and renamed with
    /// their .meta files, and removed to the system's recycle bin — and changes are heard of from
    /// Unity's own import rather than a file watcher over the whole project.
    /// </para>
    /// </summary>
    public sealed class ProjectSource : IDocSource, IDisposable
    {
        private const string Markdown = "*.md";

        private static readonly string[] Roots = { "Assets", "Packages" };

        public ProjectSource()
        {
            ProjectMarkdownWatcher.Changed += Touched;
        }

        public event Action Changed;

        public string Name => "Project";

        public IEnumerable<string> Files()
        {
            return Roots.Where(Directory.Exists)
                .SelectMany(root => Directory.EnumerateFiles(root, Markdown, SearchOption.AllDirectories))
                .Select(p => p.Replace('\\', '/'))
                .Where(Visible);
        }

        public IEnumerable<string> Folders()
        {
            return Files().Select(Parent).Distinct();
        }

        public bool Exists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public string ReadText(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        public void WriteText(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
        }

        public byte[] ReadBytes(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public void WriteBytes(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
        }

        public void CreateFolder(string path)
        {
            Directory.CreateDirectory(path);
            AssetDatabase.ImportAsset(path);
        }

        public void Move(string from, string to)
        {
            var error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error))
            {
                throw new IOException(error);
            }
        }

        /// <summary>To the system's recycle bin, .meta and all.</summary>
        public void Remove(string path)
        {
            AssetDatabase.MoveAssetToTrash(path);
        }

        public DateTime Modified(string path)
        {
            return Directory.Exists(path) ? Directory.GetLastWriteTime(path) : File.GetLastWriteTime(path);
        }

        public string LocalPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public static string Parent(string path)
        {
            var slash = path.LastIndexOf('/');
            return slash < 0 ? string.Empty : path.Substring(0, slash);
        }

        /// <summary>Not in folders Unity itself skips: those starting with a dot or ending with a tilde.</summary>
        private static bool Visible(string path)
        {
            return !path.Split('/').Any(part => part.StartsWith(".") || part.EndsWith("~"));
        }

        public void Dispose()
        {
            ProjectMarkdownWatcher.Changed -= Touched;
        }

        private void Touched()
        {
            Changed?.Invoke();
        }
    }
}
