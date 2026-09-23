using CookieJarTools.Cookbook.Editor.Images.Gif;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Images
{
    /// <summary>A GIF in a page, playing: each frame for as long as the clip says, round and round. Sized like any picture.</summary>
    public sealed class AnimatedPicture : VisualElement
    {
        private const string RootClass = "cookbook-picture";

        private readonly GifClip clip;
        private readonly Image image;

        private int frame;
        private IVisualElementScheduledItem playing;

        public AnimatedPicture(GifClip clip, string alt, string caption)
        {
            this.clip = clip;
            AddToClassList(RootClass);
            if (clip == null || clip.Frames.Count == 0)
            {
                var missing = new Label("Clip not found" + (string.IsNullOrEmpty(alt) ? string.Empty : ": " + alt));
                missing.AddToClassList(RootClass + "__missing");
                Add(missing);
                return;
            }

            image = new Image { image = clip.Frames[0], scaleMode = ScaleMode.ScaleToFit, tooltip = alt };
            image.AddToClassList(RootClass + "__image");
            image.style.maxWidth = clip.Width;
            image.RegisterCallback<GeometryChangedEvent>(_ => Fit());
            Add(image);
            if (!string.IsNullOrEmpty(caption))
            {
                var label = new Label(caption);
                label.AddToClassList(RootClass + "__caption");
                Add(label);
            }

            RegisterCallback<AttachToPanelEvent>(_ => Play());
            RegisterCallback<DetachFromPanelEvent>(_ => playing?.Pause());
        }

        private void Play()
        {
            playing?.Pause();
            playing = schedule.Execute(Next).StartingIn(Delay());
        }

        private void Next()
        {
            frame = (frame + 1) % clip.Frames.Count;
            image.image = clip.Frames[frame];
            playing = schedule.Execute(Next).StartingIn(Delay());
        }

        private long Delay()
        {
            return (long)(clip.Delays[frame] * 1000f);
        }

        private void Fit()
        {
            var height = image.layout.width * clip.Height / Mathf.Max(1f, clip.Width);
            if (Mathf.Abs(image.resolvedStyle.height - height) > 0.5f)
            {
                image.style.height = height;
            }
        }
    }
}
