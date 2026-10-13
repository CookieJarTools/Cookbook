using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CookieJarTools.Cookbook.Editor.Window;
using CookieJarTools.Frosting.Editor;
using CookieJarTools.Frosting.Editor.Inspector.Chrome;
using CookieJarTools.Frosting.Editor.Inspector.Picker;
using CookieJarTools.Frosting.Editor.Layout;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Linking
{
    /// <summary>
    /// Puts a book button beside everything marked [Documentation], opening its page:
    /// <list type="bullet">
    /// <item>in Frosting windows, on the component's title bar and after the field;</item>
    /// <item>in Unity's own inspector, in the header at the top — a Docs button listing the
    /// documented components of the GameObject shown, or the asset's own page.</item>
    /// </list>
    /// Fields in Unity's own inspector get a link above them from <see cref="DocumentationDecorator"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class DocumentationButtons
    {
        private const string Fallback = "?";

        static DocumentationButtons()
        {
            FrostingHooks.TitlebarShown += TitlebarShown;
            FrostingHooks.MemberDecorated += MemberDecorated;
            FrostingHooks.ComponentBadges += ComponentBadges;
            UnityEditor.Editor.finishedDefaultHeaderGUI += HeaderGUI;
        }

        public static Texture Icon => EditorGUIUtility.IconContent("TextAsset Icon").image;

        public static DocumentationAttribute For(Type type)
        {
            return type == null ? null : type.GetCustomAttribute<DocumentationAttribute>(true);
        }

        private static void TitlebarShown(Object target, VisualElement buttons)
        {
            var documentation = For(target != null ? target.GetType() : null);
            if (documentation != null)
            {
                buttons.Add(ComponentTitlebar.Button(Icon, Fallback, "Open its documentation", () => CookbookWindow.OpenPage(documentation.Page)));
            }
        }

        private static void ComponentBadges(Object target, VisualElement badges)
        {
            if (For(target != null ? target.GetType() : null) != null)
            {
                badges.Add(ComponentPicker.IconBadge(Icon, "Has a page in the CookBook"));
            }
        }

        private static void MemberDecorated(MemberElement member)
        {
            var documentation = member.Attribute<DocumentationAttribute>();
            if (documentation != null && member.Field != null)
            {
                member.Field.AddIconButton(Icon, Fallback, "Open its documentation", () => CookbookWindow.OpenPage(documentation.Page));
            }
        }

        /// <summary>Unity's inspector header: a Docs button when anything shown has a page.</summary>
        private static void HeaderGUI(UnityEditor.Editor editor)
        {
            var documented = Documented(editor.target).ToList();
            if (documented.Count == 0)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var content = new GUIContent(" Docs", Icon, "Open the documentation for what is shown");
            if (GUILayout.Button(content, EditorStyles.miniButton, GUILayout.Height(18)))
            {
                ShowDocs(documented);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void ShowDocs(List<(string name, string page)> documented)
        {
            if (documented.Count == 1)
            {
                CookbookWindow.OpenPage(documented[0].page);
                return;
            }

            var menu = new GenericMenu();
            foreach (var (name, page) in documented)
            {
                menu.AddItem(new GUIContent(name), false, () => CookbookWindow.OpenPage(page));
            }

            menu.ShowAsContext();
        }

        /// <summary>The object shown and, for a GameObject, its components — each with a page.</summary>
        private static IEnumerable<(string name, string page)> Documented(Object target)
        {
            var shown = target is GameObject gameObject ? gameObject.GetComponents<Component>().Where(c => c != null).Cast<Object>() : new[] { target };
            foreach (var item in shown.Where(o => o != null))
            {
                var documentation = For(item.GetType());
                if (documentation != null)
                {
                    yield return (ObjectNames.NicifyVariableName(item.GetType().Name), documentation.Page);
                }
            }
        }
    }
}
