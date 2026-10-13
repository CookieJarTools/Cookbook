using CookieJarTools.Cookbook.Editor.Window;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Linking
{
    /// <summary>
    /// In Unity's own inspector, a small "Documentation" link above a field marked [Documentation].
    /// Frosting windows put a book button after the field instead.
    /// </summary>
    [CustomPropertyDrawer(typeof(DocumentationAttribute))]
    public sealed class DocumentationDecorator : DecoratorDrawer
    {
        private const string Text = "Documentation ›";
        private const float Height = 16f;

        public override VisualElement CreatePropertyGUI()
        {
            var page = ((DocumentationAttribute)attribute).Page;
            var link = new Button(() => CookbookWindow.OpenPage(page)) { text = Text, tooltip = "Open " + page + " in the CookBook" };
            link.style.alignSelf = Align.FlexEnd;
            link.style.backgroundColor = Color.clear;
            link.style.borderTopWidth = link.style.borderBottomWidth = link.style.borderLeftWidth = link.style.borderRightWidth = 0f;
            link.style.fontSize = 10f;
            link.style.color = new Color(0.48f, 0.64f, 1f);
            link.style.marginBottom = 0f;
            link.style.height = Height;
            return link;
        }

        public override float GetHeight()
        {
            return Height;
        }

        public override void OnGUI(Rect position)
        {
            var style = new GUIStyle(EditorStyles.linkLabel) { alignment = TextAnchor.MiddleRight, fontSize = 10 };
            if (GUI.Button(position, Text, style))
            {
                CookbookWindow.OpenPage(((DocumentationAttribute)attribute).Page);
            }
        }
    }
}
