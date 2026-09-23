using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Reference;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// Where the Cookbook's pages are kept, relative to the project folder, and the code chosen for
    /// its Code Reference. Saved with the project, so the team shares it.
    /// </summary>
    [FilePath("ProjectSettings/CookbookSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class CookbookSettings : ScriptableSingleton<CookbookSettings>
    {
        private const string DefaultRoot = "Docs";

        [SerializeField] private string root = DefaultRoot;
        [SerializeField] private List<CodeSourceEntry> codeSources = new List<CodeSourceEntry>();

        /// <summary>The Code Reference's sources were added, removed, or switched on or off.</summary>
        public static event Action CodeSourcesChanged;

        public string Root
        {
            get => string.IsNullOrWhiteSpace(root) ? DefaultRoot : root;
            set
            {
                root = value;
                Save(true);
            }
        }

        public IReadOnlyList<CodeSourceEntry> CodeSources => codeSources;

        public bool HasCodeSource(string path)
        {
            return codeSources.Any(s => string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase));
        }

        public void AddCodeSource(string path)
        {
            if (string.IsNullOrEmpty(path) || HasCodeSource(path))
            {
                return;
            }

            codeSources.Add(new CodeSourceEntry(path));
            CodeChanged();
        }

        public void RemoveCodeSource(CodeSourceEntry entry)
        {
            if (codeSources.Remove(entry))
            {
                CodeChanged();
            }
        }

        public void SetCodeSourceEnabled(CodeSourceEntry entry, bool enabled)
        {
            if (entry.Enabled == enabled)
            {
                return;
            }

            entry.Enabled = enabled;
            CodeChanged();
        }

        private void CodeChanged()
        {
            Save(true);
            CodeSourcesChanged?.Invoke();
        }
    }
}
