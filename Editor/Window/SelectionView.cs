using System;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Index;
using CookieJarTools.Cookbook.Editor.Markdown;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// The pages about what is selected in the editor, kept up to date as the selection changes
    /// while the Cookbook follows it. With nothing written yet, a button to start the page.
    /// </summary>
    public sealed class SelectionView : VisualElement
    {
        private const string Prefix = "cookbook-selection";

        private readonly IDocNavigator navigator;

        public SelectionView(IDocNavigator navigator)
        {
            this.navigator = navigator;
            AddToClassList(Prefix);
        }

        /// <summary>"Write a page about it" clicked: the name to give it, and the asset path to link, if any.</summary>
        public event Action<string, string> WritePage;

        public void Show(Object target)
        {
            Clear();
            if (target == null)
            {
                Add(Classed(new Label("About your selection"), Prefix + "__title"));
                Add(Classed(new Label("Select something in the Hierarchy or Project window to see the pages about it."), Prefix + "__empty"));
                return;
            }

            var head = Classed(new VisualElement(), Prefix + "__head");
            head.Add(Classed(new Image { image = AssetPreview.GetMiniThumbnail(target) }, Prefix + "__icon"));
            head.Add(Classed(new Label(target.name) { enableRichText = false }, Prefix + "__title"));
            head.Add(Classed(new Label(ObjectNames.NicifyVariableName(target.GetType().Name)), Prefix + "__type"));
            Add(head);

            var related = DocRelated.For(navigator.Library, target);
            if (related.Count == 0)
            {
                Nothing(target);
                return;
            }

            var list = ListRow.List();
            foreach (var (page, why) in related)
            {
                list.Add(new ListRow(ListRow.Icon(page), page.Title, why, null, () => navigator.Open(page, null)));
            }

            Add(list);
        }

        private void Nothing(Object target)
        {
            Add(Classed(new Label("Nothing in the Cookbook about " + target.name + " yet."), Prefix + "__empty"));
            var path = AssetDatabase.GetAssetPath(target);
            var write = new Button(() => WritePage?.Invoke(target.name, path)) { text = "Write a page about it" };
            write.AddToClassList(Prefix + "__write");
            Add(write);
        }

        private static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
