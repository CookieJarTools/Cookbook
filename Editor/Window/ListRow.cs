using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// One line of a list, the way the CookBook shows most things: something small at the front,
    /// the name, a quieter detail after it, and a note at the end. The whole line is clickable.
    /// Lines sit in a <see cref="List"/>, which draws the frame around them.
    /// </summary>
    public sealed class ListRow : VisualElement
    {
        private const string Prefix = "cookbook-row";

        public ListRow(VisualElement leading, string title, string detail, string trailing, Action clicked)
        {
            AddToClassList(Prefix);
            if (leading != null)
            {
                leading.AddToClassList(Prefix + "__leading");
                Add(leading);
            }

            Add(Text(title, Prefix + "__title"));
            Add(Text(detail, Prefix + "__detail"));
            if (!string.IsNullOrEmpty(trailing))
            {
                Add(Text(trailing, Prefix + "__trailing"));
            }

            if (clicked != null)
            {
                RegisterCallback<ClickEvent>(_ => clicked());
            }
        }

        /// <summary>A frame for rows.</summary>
        public static VisualElement List()
        {
            var list = new VisualElement();
            list.AddToClassList("cookbook-list-frame");
            return list;
        }

        /// <summary>A page or folder icon, for the front of a row.</summary>
        public static VisualElement Icon(bool folder)
        {
            return new Image { image = EditorGUIUtility.IconContent(folder ? "Folder Icon" : "TextAsset Icon").image, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
        }

        /// <summary>The icon a page is shown with: a folder, a page, a script for the Code Reference.</summary>
        public static VisualElement Icon(Pages.DocPage page)
        {
            return new Image { image = PageIcons.For(page), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
        }

        /// <summary>A small square with a letter in it, tinted — for a section with no picture of its own.</summary>
        public static VisualElement Badge(string name, string tint)
        {
            var badge = new Label(string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            badge.AddToClassList(Prefix + "__badge");
            badge.style.color = TagColors.For(tint);
            badge.style.backgroundColor = TagColors.Soft(tint, 0.16f);
            return badge;
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text ?? string.Empty) { pickingMode = PickingMode.Ignore, enableRichText = false };
            label.AddToClassList(className);
            return label;
        }
    }
}
