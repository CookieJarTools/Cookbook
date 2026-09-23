using System;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>One line of the page tree: an icon for a folder or page, and its title. Right-click for what can be done with it.</summary>
    public sealed class DocTreeRow : VisualElement
    {
        private readonly Image icon;
        private readonly Label title;

        public DocTreeRow(Action<DocPage, ContextualMenuPopulateEvent> menu)
        {
            AddToClassList("cookbook-tree__row");
            icon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("cookbook-tree__icon");
            title = new Label { pickingMode = PickingMode.Ignore, enableRichText = false };
            title.AddToClassList("cookbook-tree__title");
            Add(icon);
            Add(title);
            this.AddManipulator(new ContextualMenuManipulator(evt => menu(Page, evt)));
        }

        public DocPage Page { get; private set; }

        public void Show(DocPage page)
        {
            Page = page;
            title.text = page.Title;
            icon.image = PageIcons.For(page);
            EnableInClassList("cookbook-tree__row--project", page.IsProject);
            EnableInClassList("cookbook-tree__row--code", page.IsCode);
        }
    }
}
