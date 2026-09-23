using System;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// A tag as a small neutral pill with a dot in the tag's own colour. It can be picked (as a
    /// filter), show a count, or have a cross to take it off a page.
    /// </summary>
    public sealed class TagChip : VisualElement
    {
        private const string Prefix = "cookbook-chip";

        private bool picked;

        public TagChip(string tag, Action<TagChip> clicked, int count = -1, Action removed = null)
        {
            Tag = tag;
            AddToClassList(Prefix);
            tooltip = removed != null ? "Tagged “" + tag + "”" : "Show pages tagged “" + tag + "”";

            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList(Prefix + "__dot");
            dot.style.backgroundColor = TagColors.For(tag);
            Add(dot);

            var name = new Label(tag) { pickingMode = PickingMode.Ignore, enableRichText = false };
            name.AddToClassList(Prefix + "__name");
            Add(name);

            if (count >= 0)
            {
                var number = new Label(count.ToString()) { pickingMode = PickingMode.Ignore };
                number.AddToClassList(Prefix + "__count");
                Add(number);
            }

            if (removed != null)
            {
                var cross = new Label("×") { tooltip = "Take this tag off" };
                cross.AddToClassList(Prefix + "__remove");
                cross.RegisterCallback<ClickEvent>(evt =>
                {
                    removed();
                    evt.StopPropagation();
                });
                Add(cross);
            }

            if (clicked == null)
            {
                return;
            }

            AddToClassList(Prefix + "--clickable");
            RegisterCallback<ClickEvent>(evt =>
            {
                clicked(this);
                evt.StopPropagation();
            });
        }

        public string Tag { get; }

        public bool Picked
        {
            get => picked;
            set
            {
                picked = value;
                EnableInClassList(Prefix + "--picked", value);
            }
        }
    }
}
