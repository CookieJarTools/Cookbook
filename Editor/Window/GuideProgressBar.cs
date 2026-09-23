using CookieJarTools.Cookbook.Editor.Actions;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// How far through a guide the reader is: "3 of 7 steps done", with a bar, kept up to date as
    /// steps are done and checks pass. Reset forgets this guide's progress, to go through it again.
    /// </summary>
    public sealed class GuideProgressBar : VisualElement
    {
        private const string Prefix = "cookbook-progress";

        private readonly DocPage page;
        private readonly Label words;
        private readonly VisualElement fill;

        public GuideProgressBar(DocPage page)
        {
            this.page = page;
            AddToClassList(Prefix);

            var head = new VisualElement();
            head.AddToClassList(Prefix + "__head");
            words = new Label();
            words.AddToClassList(Prefix + "__words");
            var reset = new Button(() => GuideSteps.Reset(page)) { text = "Reset", tooltip = "Forget which steps are done, to go through the guide again" };
            reset.AddToClassList(Prefix + "__reset");
            head.Add(words);
            head.Add(reset);
            Add(head);

            var track = new VisualElement();
            track.AddToClassList(Prefix + "__track");
            fill = new VisualElement();
            fill.AddToClassList(Prefix + "__fill");
            track.Add(fill);
            Add(track);

            RegisterCallback<AttachToPanelEvent>(_ => StepMemory.Changed += Refresh);
            RegisterCallback<DetachFromPanelEvent>(_ => StepMemory.Changed -= Refresh);
            Refresh();
        }

        private void Refresh()
        {
            var (done, total) = GuideSteps.Progress(page);
            words.text = done == total ? "All " + total + " steps done" : done + " of " + total + " steps done";
            fill.style.width = Length.Percent(total == 0 ? 0f : 100f * done / total);
            EnableInClassList(Prefix + "--complete", done == total && total > 0);
        }
    }
}
