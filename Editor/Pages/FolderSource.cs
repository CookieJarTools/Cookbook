using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// Pages kept as files in a folder on this machine — plain Markdown, so they can be edited with
    /// anything and shared with git like any other file. Changes made outside the CookBook are
    /// noticed and reported.
    /// <para>
    /// Removing moves to a hidden <c>.trash</c> folder rather than deleting, so nothing removed by
    /// mistake is lost.
    /// </para>
    /// </summary>
    public sealed class FolderSource : IDocSource, IDisposable
    {
        private const string TrashFolder = ".trash";
        private const double SettleSeconds = 0.3;

        private readonly string root;
        private readonly FileSystemWatcher watcher;

        private volatile bool touched;
        private double changedAt;

        public FolderSource(string root)
        {
            this.root = Path.GetFullPath(root);
            Directory.CreateDirectory(this.root);

            watcher = new FileSystemWatcher(this.root) { IncludeSubdirectories = true, EnableRaisingEvents = true };
            watcher.Changed += Touched;
            watcher.Created += Touched;
            watcher.Deleted += Touched;
            watcher.Renamed += Touched;
            EditorApplication.update += Poll;
        }

        public event Action Changed;

        public string Name => Path.GetFileName(root);

        public IEnumerable<string> Folders()
        {
            return Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).Select(Relative).Where(p => !Hidden(p));
        }

        public IEnumerable<string> Files()
        {
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Select(Relative).Where(p => !Hidden(p));
        }

        public bool Exists(string path)
        {
            var full = Full(path);
            return File.Exists(full) || Directory.Exists(full);
        }

        public string ReadText(string path)
        {
            var full = Full(path);
            return File.Exists(full) ? File.ReadAllText(full) : string.Empty;
        }

        public void WriteText(string path, string text)
        {
            var full = Full(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, text);
        }

        public byte[] ReadBytes(string path)
        {
            var full = Full(path);
            return File.Exists(full) ? File.ReadAllBytes(full) : null;
        }

        public void WriteBytes(string path, byte[] bytes)
        {
            var full = Full(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, bytes);
        }

        public void CreateFolder(string path)
        {
            Directory.CreateDirectory(Full(path));
        }

        public void Move(string from, string to)
        {
            var source = Full(from);
            var target = Full(to);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (Directory.Exists(source))
            {
                Directory.Move(source, target);
                return;
            }

            File.Move(source, target);
        }

        public void Remove(string path)
        {
            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            Move(path, TrashFolder + "/" + stamp + "/" + path);
        }

        public DateTime Modified(string path)
        {
            var full = Full(path);
            return Directory.Exists(full) ? Directory.GetLastWriteTime(full) : File.GetLastWriteTime(full);
        }

        public string LocalPath(string path)
        {
            return Full(path);
        }

        public void Dispose()
        {
            EditorApplication.update -= Poll;
            watcher.Dispose();
        }

        /// <summary>Anything whose path has a part starting with a dot: the trash, and the likes of .git.</summary>
        private static bool Hidden(string path)
        {
            return path.Split('/').Any(part => part.StartsWith("."));
        }

        /// <summary>From the watcher's thread: only noted here, and reported on the main thread once things settle.</summary>
        private void Touched(object sender, FileSystemEventArgs args)
        {
            touched = true;
        }

        private void Poll()
        {
            if (touched)
            {
                touched = false;
                changedAt = EditorApplication.timeSinceStartup;
                return;
            }

            if (changedAt > 0 && EditorApplication.timeSinceStartup - changedAt > SettleSeconds)
            {
                changedAt = 0;
                Changed?.Invoke();
            }
        }

        private string Full(string path)
        {
            return Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
        }

        private string Relative(string full)
        {
            return full.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
        }
    }
}
