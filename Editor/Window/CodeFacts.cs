using System;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Reference;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// What a Code Reference page is about, under its title: the kind of thing it is, its
    /// namespace and assembly, what it inherits — click to go there — and the script, which opens
    /// in the code editor.
    /// </summary>
    public sealed class CodeFacts : VisualElement
    {
        private const string Prefix = "cookbook-facts";

        private readonly IDocNavigator navigator;

        public CodeFacts(IDocNavigator navigator, CodeType type)
        {
            this.navigator = navigator;
            AddToClassList(Prefix);
            Add(Fact(type.Kind, Prefix + "__kind", null, null));
            if (type.Namespace.Length > 0)
            {
                Add(Fact(type.Namespace, Prefix + "__namespace", "Namespace", null));
            }

            var parent = type.Type.BaseType;
            if (parent != null && parent != typeof(object) && parent != typeof(ValueType) && parent != typeof(Enum) && parent != typeof(MulticastDelegate))
            {
                Add(Fact("Inherits " + TypeNames.Of(parent), Prefix + "__link", "Go to " + TypeNames.Of(parent), () => GoTo(parent)));
            }

            var assembly = CodeScope.AssemblyOf(type.Script);
            if (assembly != null)
            {
                Add(Fact(assembly, Prefix + "__assembly", "Compiled into this assembly", null));
            }

            Add(Fact(type.Script, Prefix + "__script", "Open the script", () => OpenScript(type.Script, type.Type)));
        }

        private static Label Fact(string text, string className, string tip, Action clicked)
        {
            var fact = BlockBuilder.Classed(new Label(text) { enableRichText = false, tooltip = tip }, className);
            fact.AddToClassList(Prefix + "__fact");
            if (clicked != null)
            {
                fact.AddToClassList(Prefix + "__fact--link");
                fact.RegisterCallback<ClickEvent>(_ => clicked());
            }

            return fact;
        }

        /// <summary>Its page in the reference if it has one; Unity's or Microsoft's otherwise.</summary>
        private void GoTo(Type type)
        {
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            var page = navigator.Library.Find(CodeLinks.Id(CodeKeys.Of(definition)));
            if (page != null)
            {
                navigator.Open(page, null);
                return;
            }

            var url = CodeLinks.Url(definition);
            if (url != null)
            {
                Application.OpenURL(url);
            }
        }

        /// <summary>Opens the script in the code editor, at the line the type is declared on when it can be found.</summary>
        private static void OpenScript(string script, Type type)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(script);
            if (asset == null)
            {
                return;
            }

            var lines = asset.text.Split('\n');
            var name = " " + TypeNames.Stem(type.Name);
            var line = Array.FindIndex(lines, l => l.Contains(name) && new[] { "class", "struct", "interface", "enum", "record", "delegate" }.Any(l.Contains));
            AssetDatabase.OpenAsset(asset, line < 0 ? 0 : line + 1);
        }
    }
}
