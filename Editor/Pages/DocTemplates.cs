using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CookieJarTools.Cookbook.Editor.Pages
{
    /// <summary>
    /// Pages to start from: How-to, Feature spec, Onboarding guide and Postmortem to begin with,
    /// kept as Markdown in a hidden <c>.templates</c> folder inside the Cookbook's folder — change
    /// them, or add your own, and they are offered too. <c>{{title}}</c>, <c>{{date}}</c> and
    /// <c>{{author}}</c> are filled in when a page is made; tags in a template's header are given
    /// to the page.
    /// </summary>
    public static class DocTemplates
    {
        public const string Folder = ".templates";

        private static readonly (string name, string text)[] Starters =
        {
            ("How-to", "---\ntags: [how-to]\n---\n\nWhat this helps you do, in a sentence.\n\n## Before you start\n\n- [ ] What you need first\n\n## Steps\n\n1. The first thing to do.\n2. The next.\n\n## Check it worked\n\nWhat you should see when it's done.\n"),
            ("Feature spec", "---\ntags: [spec]\n---\n\n> [!NOTE]\n> Status: Draft · Owner: {{author}} · Started: {{date}}\n\n## The problem\n\nWhat's wrong or missing, and who it's for.\n\n## The idea\n\nHow it should work, from the player's side.\n\n## How it works\n\nThe systems involved, and how they fit together.\n\n## Out of scope\n\n- What this won't do\n\n## Open questions\n\n- [ ] Still to decide\n"),
            ("Onboarding guide", "---\ntags: [guide, onboarding]\n---\n\nWelcome! This takes you from a fresh checkout to working on the game.\n\n## Set up\n\n1. Open the project in Unity.\n\n   [Main Camera is in the scene](check:component/Camera)\n\n2. Open the main scene.\n\n## Your first change\n\n1. Something small to try.\n\n## Where to go next\n\n- Links to the pages to read next\n"),
            ("Postmortem", "---\ntags: [postmortem]\n---\n\n> [!NOTE]\n> {{date}} · Written by {{author}}\n\n## What happened\n\nA short account, in order.\n\n## What went well\n\n- \n\n## What went wrong\n\n- \n\n## What we'll change\n\n- [ ] \n"),
        };

        /// <summary>The templates there are, by name — the starters are written out the first time.</summary>
        public static List<string> Names(IDocSource source)
        {
            var folder = source.LocalPath(Folder);
            if (folder == null)
            {
                return Starters.Select(s => s.name).ToList();
            }

            WriteStarters(source, folder);
            return Directory.GetFiles(folder, "*" + DocPage.Extension).Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToList();
        }

        /// <summary>A template's header tags, and its words with the blanks filled in.</summary>
        public static (List<string> tags, string body) Fill(IDocSource source, string name, string title)
        {
            var text = source.ReadText(Folder + "/" + name + DocPage.Extension);
            if (string.IsNullOrEmpty(text))
            {
                text = Starters.FirstOrDefault(s => s.name == name).text ?? string.Empty;
            }

            var header = DocHeader.Split(text, out var body);
            body = "# " + title + "\n\n" + body
                .Replace("{{title}}", title)
                .Replace("{{date}}", DateTime.Now.ToString("yyyy-MM-dd"))
                .Replace("{{author}}", Environment.UserName);
            return (header.Tags, body);
        }

        private static void WriteStarters(IDocSource source, string folder)
        {
            if (Directory.Exists(folder))
            {
                return;
            }

            foreach (var (name, text) in Starters)
            {
                source.WriteText(Folder + "/" + name + DocPage.Extension, text);
            }
        }
    }
}
