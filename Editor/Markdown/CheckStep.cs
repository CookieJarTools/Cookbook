using CookieJarTools.Cookbook.Editor.Actions;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// A step a guide checks for itself: what it looks for, whether it passes, and a button to look
    /// again. It looks as soon as it is shown, so a guide opens already knowing what is done. A pass
    /// counts toward the guide's progress.
    /// </summary>
    public sealed class CheckStep : VisualElement
    {
        private const string Prefix = "cookbook-action";

        private readonly DocCheck check;
        private readonly string page;
        private readonly string url;
        private readonly Label mark;
        private readonly Label status;

        public CheckStep(string url, string label, string page)
        {
            check = DocChecks.Find(url);
            this.page = page;
            this.url = url;
            AddToClassList(Prefix);
            AddToClassList(Prefix + "--check");

            mark = BlockBuilder.Classed(new Label(), Prefix + "__mark");
            var words = BlockBuilder.Classed(new VisualElement(), Prefix + "__words");
            words.Add(BlockBuilder.Classed(new Label(string.IsNullOrWhiteSpace(label) ? check?.Name ?? url : label), Prefix + "__name"));
            var description = check == null ? "This project has no check called “" + url.Replace(DocChecks.Scheme, string.Empty) + "”." : check.Description;
            if (!string.IsNullOrEmpty(description))
            {
                words.Add(BlockBuilder.Classed(new Label(description), Prefix + "__description"));
            }

            status = BlockBuilder.Classed(new Label(), Prefix + "__status");
            words.Add(status);

            var again = BlockBuilder.Classed(new Button(Look) { text = "Check again", tooltip = "Look again" }, Prefix + "__run");
            again.style.display = check == null ? DisplayStyle.None : DisplayStyle.Flex;

            Add(mark);
            Add(words);
            Add(again);
            EnableInClassList(Prefix + "--missing", check == null);
            if (check != null)
            {
                RegisterCallback<AttachToPanelEvent>(_ => Look());
            }
        }

        private void Look()
        {
            var outcome = check.Run();
            StepMemory.Set(page, url, outcome.Passed);
            EnableInClassList(Prefix + "--done", outcome.Passed);
            EnableInClassList(Prefix + "--failed", !outcome.Passed);
            mark.text = outcome.Passed ? "✓" : "–";
            status.text = outcome.Message;
            status.style.display = DisplayStyle.Flex;
        }
    }
}
