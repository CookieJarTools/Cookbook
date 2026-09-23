using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Images
{
    /// <summary>
    /// An image in a page: never wider than its own pixels or the page, as tall as its shape needs
    /// at that width, with its title as a caption underneath. A picture that cannot be found says so,
    /// with its alt text.
    /// </summary>
    public sealed class PictureElement : VisualElement
    {
        private const string RootClass = "cookbook-picture";

        private readonly Image image;
        private readonly Texture2D texture;

        public PictureElement(Texture2D texture, string alt, string caption)
        {
            this.texture = texture;
            AddToClassList(RootClass);
            if (texture == null)
            {
                var missing = new Label("Image not found" + (string.IsNullOrEmpty(alt) ? string.Empty : ": " + alt));
                missing.AddToClassList(RootClass + "__missing");
                Add(missing);
                return;
            }

            image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, tooltip = alt };
            image.AddToClassList(RootClass + "__image");
            image.style.maxWidth = texture.width;
            image.RegisterCallback<GeometryChangedEvent>(_ => Fit());
            Add(image);

            if (!string.IsNullOrEmpty(caption))
            {
                var label = new Label(caption);
                label.AddToClassList(RootClass + "__caption");
                Add(label);
            }
        }

        private void Fit()
        {
            var height = image.layout.width * texture.height / Mathf.Max(1f, texture.width);
            if (Mathf.Abs(image.resolvedStyle.height - height) > 0.5f)
            {
                image.style.height = height;
            }
        }
    }
}
