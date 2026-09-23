using System;
using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// Where the pages are kept. Paths are relative to the source's root, with forward slashes:
    /// <c>"Combat/Damage.md"</c>, <c>"Combat/images/hit.png"</c>.
    /// <para>
    /// The Cookbook only ever reads and writes through this, so a page can come from somewhere else
    /// — a wiki, a shared drive, a server — by adding another source; nothing else needs to change.
    /// </para>
    /// </summary>
    public interface IDocSource
    {
        /// <summary>Something changed outside the Cookbook: pages edited, added or removed. Raised on the main thread.</summary>
        event Action Changed;

        string Name { get; }

        IEnumerable<string> Folders();

        IEnumerable<string> Files();

        bool Exists(string path);

        string ReadText(string path);

        void WriteText(string path, string text);

        byte[] ReadBytes(string path);

        void WriteBytes(string path, byte[] bytes);

        void CreateFolder(string path);

        void Move(string from, string to);

        /// <summary>Takes a page or folder out of the Cookbook, somewhere it can still be got back from.</summary>
        void Remove(string path);

        DateTime Modified(string path);

        /// <summary>Where the path is on this machine, to show it in the file browser; null when it is not a local file.</summary>
        string LocalPath(string path);
    }
}
