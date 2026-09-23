using CookieJarTools.Frosting.Editor.Fields;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// A project asset shown in a page, alive: <c>![](Assets/Data/Enemy.asset)</c>. A
    /// ScriptableObject shows its fields — Frosting's — and can be changed right there; a material,
    /// its inspector; a prefab, model or anything else with a picture, its preview; a scene, a
    /// button to open it. Each has its name, what it is, and buttons to find it or open it.
    /// </summary>
    public sealed class LiveAssetElement : VisualElement
    {
        private const string Prefix = "cookbook-asset";
        private const long PreviewPoll = 100;
        private const int PreviewTries = 40;

        private readonly Object asset;
        private int tries;

        public LiveAssetElement(string path)
        {
            AddToClassList(Prefix);
            asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                Add(BlockBuilder.Classed(new Label("Nothing at " + path), Prefix + "__missing"));
                return;
            }

            Add(Header(path));
            var body = Body();
            if (body != null)
            {
                Add(BlockBuilder.Classed(body, Prefix + "__body"));
            }
        }

        /// <summary>Whether an asset path is one to show alive: anything but a picture, which is shown as one.</summary>
        public static bool Shows(string path)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            return type != null && !typeof(Texture).IsAssignableFrom(type);
        }

        private VisualElement Header(string path)
        {
            var header = BlockBuilder.Classed(new VisualElement(), Prefix + "__header");
            var icon = new Image { image = AssetDatabase.GetCachedIcon(path), scaleMode = ScaleMode.ScaleToFit };
            header.Add(BlockBuilder.Classed(icon, Prefix + "__icon"));
            header.Add(BlockBuilder.Classed(new Label(asset.name), Prefix + "__name"));
            header.Add(BlockBuilder.Classed(new Label(ObjectNames.NicifyVariableName(asset.GetType().Name)), Prefix + "__type"));
            header.Add(BlockBuilder.Classed(new Button(Select) { text = "Show", tooltip = "Select it and show it in the Project window" }, Prefix + "__button"));
            header.Add(BlockBuilder.Classed(new Button(() => AssetDatabase.OpenAsset(asset)) { text = "Open", tooltip = "Open it" }, Prefix + "__button"));
            return header;
        }

        private VisualElement Body()
        {
            if (asset is ScriptableObject)
            {
                var serialized = new SerializedObject(asset);
                var fields = FrostingPropertyTree.Build(serialized);
                fields.Bind(serialized);
                return fields;
            }

            if (asset is Material)
            {
                return new InspectorElement(asset);
            }

            return asset is SceneAsset ? null : Preview();
        }

        /// <summary>The asset's picture, which Unity makes in the background: looked for until it is ready.</summary>
        private VisualElement Preview()
        {
            var image = new Image { scaleMode = ScaleMode.ScaleToFit };
            image.AddToClassList(Prefix + "__preview");
            image.schedule.Execute(() => image.image = AssetPreview.GetAssetPreview(asset)).Every(PreviewPoll).Until(() => image.image != null || ++tries > PreviewTries);
            return image;
        }

        private void Select()
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
