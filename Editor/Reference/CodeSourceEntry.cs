using System;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// Code chosen for the Code Reference: an assembly definition (everything compiled into that
    /// assembly), a folder (every script under it) or a single script. Kept by its asset path.
    /// </summary>
    [Serializable]
    public sealed class CodeSourceEntry
    {
        [SerializeField] private string path;
        [SerializeField] private bool enabled = true;

        public CodeSourceEntry(string path)
        {
            this.path = path;
        }

        public string Path => path;

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }
    }
}
