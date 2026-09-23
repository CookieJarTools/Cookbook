using CookieJarTools.Cookbook.Editor.Actions;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// A step a page does for the reader: a mark showing whether it is done, what it does, and a
    /// Run button. Underneath goes what happened — done, with a link to what it made, or what went
    /// wrong. Once done it stays ticked — on this machine, through restarts — and can be run again.
    /// An action the project does not have says so, with no button.
    /// </summary>
    public sealed class ActionButton : VisualElement
    {
        private const string Prefix = "cookbook-action";

        private readonly DocAction action;
        private readonly string page;
        private readonly string url;
        private readonly VisualElement mark;
        private readonly Label status;
        private readonly Button run;

        private Object made;

        /// <param name="place">Which page this is on, so being done is remembered for this step alone.</param>
        public ActionButton(string url, string label, string place)
        {
            action = DocActions.Find(url);
            page = place;
            this.url = url;
            AddToClassList(Prefix);

            mark = BlockBuilder.Classed(new Label(), Prefix + "__mark");
            var words = BlockBuilder.Classed(new VisualElement(), Prefix + "__words");
            words.Add(BlockBuilder.Classed(new Label(string.IsNullOrWhiteSpace(label) ? action?.Name ?? url : label), Prefix + "__name"));
            var description = action == null ? "This project has no action called “" + url.Replace(DocActions.Scheme, string.Empty) + "”." : action.Description;
            if (!string.IsNullOrEmpty(description))
            {
                words.Add(BlockBuilder.Classed(new Label(description), Prefix + "__description"));
            }

            status = BlockBuilder.Classed(new Label(), Prefix + "__status");
            status.RegisterCallback<ClickEvent>(_ => DocActions.Show(made));
            words.Add(status);

            run = BlockBuilder.Classed(new Button(Run) { text = "Run", tooltip = "Do this step" }, Prefix + "__run");
            run.style.display = action == null ? DisplayStyle.None : DisplayStyle.Flex;

            Add(mark);
            Add(words);
            Add(run);
            EnableInClassList(Prefix + "--missing", action == null);
            ShowDone(StepMemory.IsDone(page, url), null);
        }

        private void Run()
        {
            var outcome = action.Run();
            if (outcome.Cancelled)
            {
                return;
            }

            made = outcome.Made;
            if (!outcome.Done)
            {
                ShowFailed(outcome.Message);
                return;
            }

            StepMemory.Set(page, url, true);
            ShowDone(true, outcome.Message);
        }

        private void ShowDone(bool done, string message)
        {
            EnableInClassList(Prefix + "--done", done);
            RemoveFromClassList(Prefix + "--failed");
            ((Label)mark).text = done ? "✓" : string.Empty;
            run.text = done ? "Run again" : "Run";
            status.text = done ? (message ?? "Done") + (made != null ? " — show it" : string.Empty) : string.Empty;
            status.EnableInClassList(Prefix + "__status--link", made != null);
            status.style.display = done ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowFailed(string message)
        {
            AddToClassList(Prefix + "--failed");
            ((Label)mark).text = "!";
            status.text = "Didn't work: " + message;
            status.style.display = DisplayStyle.Flex;
        }
    }
}
