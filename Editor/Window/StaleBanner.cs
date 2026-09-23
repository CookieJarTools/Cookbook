using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// A quiet note at the top of a page whose code has changed since it was written: which
    /// scripts, and when. Open a script to see what changed; "It's still right" marks the page up
    /// to date again.
    /// </summary>
    public sealed class StaleBanner : VisualElement
    {
        private const string Prefix = "cookbook-stale";
        private const int NamedScripts = 3;

        public StaleBanner(List<(string script, DateTime changed)> changed, Action stillRight)
        {
            AddToClassList(Prefix);
            var dot = new VisualElement();
            dot.AddToClassList(Prefix + "__dot");
            Add(dot);

            var words = new VisualElement();
            words.AddToClassList(Prefix + "__words");
            var names = changed.Take(NamedScripts).Select(c => Path.GetFileName(c.script)).ToList();
            var more = changed.Count > NamedScripts ? " and " + (changed.Count - NamedScripts) + " more" : string.Empty;
            var title = new Label("This page might be out of date");
            title.AddToClassList(Prefix + "__title");
            var detail = new Label(string.Join(", ", names) + more + (changed.Count == 1 ? " has" : " have") + " changed since it was written — last on " + changed[0].changed.ToString("d MMM") + ".");
            detail.AddToClassList(Prefix + "__detail");
            words.Add(title);
            words.Add(detail);
            Add(words);

            Add(Button("Open " + Path.GetFileName(changed[0].script), () => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(changed[0].script))));
            Add(Button("It's still right", stillRight));
        }

        private static Button Button(string text, Action clicked)
        {
            var button = new Button(clicked) { text = text };
            button.AddToClassList(Prefix + "__button");
            return button;
        }
    }
}
