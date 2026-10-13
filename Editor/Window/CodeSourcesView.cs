using System.Linq;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using CookieJarTools.Cookbook.Editor.Reference;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// Choosing what the Code Reference covers, on its own page: each source with how many types
    /// it gives, a switch to leave it out for now, and a way to take it away; and a place to drop
    /// an assembly definition, folder or script to add one. The choice is the project's, shared by
    /// everyone working on it.
    /// </summary>
    public sealed class CodeSourcesView : VisualElement
    {
        private const string Prefix = "cookbook-sources";

        private readonly Label message;

        public CodeSourcesView()
        {
            AddToClassList(Prefix);
            var head = BlockBuilder.Classed(new VisualElement(), Prefix + "__head");
            head.Add(BlockBuilder.Classed(new Label("What's documented"), Prefix + "__heading"));
            head.Add(BlockBuilder.Classed(new Label("An assembly definition covers everything compiled into it, a folder every script under it, a script just itself. Shared with the whole team."), Prefix + "__about"));
            Add(head);

            var settings = CookbookSettings.instance;
            var model = CodeModel.Current;
            var list = ListRow.List();
            foreach (var entry in settings.CodeSources)
            {
                var count = model.Sources.Where(s => s.source == entry.Path).Select(s => s.types.Count).FirstOrDefault();
                list.Add(Row(entry, count));
            }

            if (settings.CodeSources.Count > 0)
            {
                Add(list);
            }

            Add(Adder());
            message = BlockBuilder.Classed(new Label(), Prefix + "__message");
            message.style.display = DisplayStyle.None;
            Add(message);
        }

        private VisualElement Row(CodeSourceEntry entry, int count)
        {
            var row = BlockBuilder.Classed(new VisualElement(), Prefix + "__row");
            row.EnableInClassList(Prefix + "__row--off", !entry.Enabled);
            row.Add(BlockBuilder.Classed(Icon(entry.Path), Prefix + "__icon"));
            var words = BlockBuilder.Classed(new VisualElement(), Prefix + "__words");
            words.Add(BlockBuilder.Classed(new Label(CodeScope.Title(entry.Path)) { enableRichText = false }, Prefix + "__title"));
            words.Add(BlockBuilder.Classed(new Label(CodeScope.Describe(entry.Path) + " · " + entry.Path) { enableRichText = false }, Prefix + "__path"));
            row.Add(words);

            var types = entry.Enabled ? (count == 1 ? "1 type" : count + " types") : "Off";
            row.Add(BlockBuilder.Classed(new Label(types), Prefix + "__count"));
            var toggle = BlockBuilder.Classed(new Button(() => CookbookSettings.instance.SetCodeSourceEnabled(entry, !entry.Enabled)) { text = entry.Enabled ? "Turn off" : "Turn on", tooltip = entry.Enabled ? "Leave it out of the reference for now" : "Put it back in the reference" }, Prefix + "__button");
            row.Add(toggle);
            row.Add(BlockBuilder.Classed(new Button(() => Remove(entry)) { text = "Remove", tooltip = "Take it out of the reference — notes written for its types are kept" }, Prefix + "__button"));
            return row;
        }

        /// <summary>Drop or pick something to add: whatever it is, it is checked before it goes in.</summary>
        private VisualElement Adder()
        {
            var adder = BlockBuilder.Classed(new VisualElement(), Prefix + "__add");
            adder.Add(BlockBuilder.Classed(new Label("Add an assembly definition, folder or script"), Prefix + "__add-label"));
            var field = new ObjectField { objectType = typeof(Object), allowSceneObjects = false };
            field.AddToClassList(Prefix + "__field");
            field.RegisterValueChangedCallback(evt => Add(evt.newValue, field));
            adder.Add(field);
            return adder;
        }

        private void Add(Object asset, ObjectField field)
        {
            if (asset == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            field.SetValueWithoutNotify(null);
            if (!CodeScope.CanUse(path))
            {
                Say("“" + asset.name + "” isn't an assembly definition, folder or script.");
                return;
            }

            if (CookbookSettings.instance.HasCodeSource(path))
            {
                Say("“" + CodeScope.Title(path) + "” is already in the reference.");
                return;
            }

            CookbookSettings.instance.AddCodeSource(path);
        }

        private static void Remove(CodeSourceEntry entry)
        {
            if (EditorUtility.DisplayDialog("CookBook", "Take “" + CodeScope.Title(entry.Path) + "” out of the Code Reference?\n\nNotes written for its types are kept, and come back if it is added again.", "Remove", "Cancel"))
            {
                CookbookSettings.instance.RemoveCodeSource(entry);
            }
        }

        private void Say(string text)
        {
            message.text = text;
            message.style.display = DisplayStyle.Flex;
        }

        public static Image Icon(string path)
        {
            var icon = CodeScope.IsAssembly(path) ? "AssemblyDefinitionAsset Icon" : CodeScope.IsScript(path) ? "cs Script Icon" : "Folder Icon";
            return new Image { image = EditorGUIUtility.IconContent(icon).image, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
        }
    }
}
