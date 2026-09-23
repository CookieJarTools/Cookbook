using System.Linq;
using CookieJarTools.Cookbook.Editor.Index;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// The bottom of a page: the other pages that link to it, and the code whose [Documentation]
    /// points at it — each a row to go to. Nothing is shown when there is neither.
    /// </summary>
    public sealed class PageConnections : VisualElement
    {
        private const string Prefix = "cookbook-connections";

        public PageConnections(IDocNavigator navigator, DocPage page)
        {
            AddToClassList(Prefix);
            var library = navigator.Library;
            var linking = DocLinkIndex.For(library).To(page).Where(library.Shown).ToList();
            var code = DocCodeIndex.For(library, page).ToList();
            style.display = linking.Count + code.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            if (linking.Count > 0)
            {
                var list = Section("Linked from", linking.Count);
                foreach (var from in linking)
                {
                    list.Add(new ListRow(ListRow.Icon(from), from.Title, string.Join(" › ", from.Trail().Select(p => p.Title)), null, () => navigator.Open(from, null)));
                }
            }

            if (code.Count > 0)
            {
                var list = Section("In the code", code.Count);
                foreach (var link in code)
                {
                    list.Add(new ListRow(ScriptIcon(), link.Name, link.Script ?? "Script not found", link.Member == null ? "class" : "member", () => ShowScript(link)));
                }
            }
        }

        private VisualElement Section(string title, int count)
        {
            var head = new VisualElement();
            head.AddToClassList(Prefix + "__head");
            head.Add(new Label(title) { name = "title" });
            head.Add(new Label(count.ToString()) { name = "count" });
            Add(head);
            var list = ListRow.List();
            Add(list);
            return list;
        }

        private static VisualElement ScriptIcon()
        {
            return new Image { image = EditorGUIUtility.IconContent("cs Script Icon").image, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
        }

        private static void ShowScript(CodeLink link)
        {
            var script = link.Script == null ? null : AssetDatabase.LoadAssetAtPath<MonoScript>(link.Script);
            if (script == null)
            {
                return;
            }

            Selection.activeObject = script;
            EditorGUIUtility.PingObject(script);
        }
    }
}
