using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax.Inlines;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// Turns a paragraph's inline Markdown — bold, italics, code, links — into the rich text UI
    /// Toolkit draws, split around any images. What the writer typed is wrapped in
    /// <c>&lt;noparse&gt;</c>, so a <c>&lt;</c> in a page shows as itself rather than starting a tag.
    /// </summary>
    public sealed class InlineText
    {
        public const string LinkColor = "#9AA3F7";
        public const string MissingColor = "#EB8A8A";

        private const string CodeColor = "#E6E8EB";
        private const string CodeMark = "#FFFFFF10";
        private const string HighlightMark = "#F2C94C38";

        /// <summary>
        /// Tag marks one label may hold. Unity's rich text reader throws on every repaint past about
        /// 300 tags — a paragraph of 60 links — so a paragraph or code block longer than this is drawn
        /// as several labels.
        /// </summary>
        public const int MaxTagMarks = 400;

        private readonly List<InlineRun> runs = new List<InlineRun>();
        private readonly System.Func<string, bool> linkWorks;

        private StringBuilder text = new StringBuilder();
        private List<string> links = new List<string>();
        private int taskPosition = -1;
        private bool taskChecked;

        /// <param name="linkWorks">Whether a link goes anywhere; one that does not is shown in red, like a page still to write.</param>
        private InlineText(System.Func<string, bool> linkWorks)
        {
            this.linkWorks = linkWorks;
        }

        public static List<InlineRun> Runs(ContainerInline inline, System.Func<string, bool> linkWorks)
        {
            var builder = new InlineText(linkWorks);
            foreach (var child in inline ?? Enumerable.Empty<Inline>())
            {
                builder.Add(child);
                builder.SplitIfFull();
            }

            builder.Flush();
            return builder.runs;
        }

        /// <summary>The whole of it as one rich text string, images left out: for headings and table cells.</summary>
        public static string Rich(ContainerInline inline, System.Func<string, bool> linkWorks, out List<string> links)
        {
            var textRuns = Runs(inline, linkWorks).Where(r => r.Image == null).ToList();
            links = textRuns.SelectMany(r => r.Links).ToList();
            return textRuns.Count == 1 ? textRuns[0].Text : Renumbered(textRuns);
        }

        public static string Plain(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : "<noparse>" + value.Replace("</noparse>", "</ noparse>") + "</noparse>";
        }

        /// <summary>Several runs' text as one, their link ids counted on from each other.</summary>
        private static string Renumbered(List<InlineRun> textRuns)
        {
            var joined = new StringBuilder();
            var offset = 0;
            foreach (var run in textRuns)
            {
                var runText = run.Text;
                for (var i = run.Links.Count - 1; i >= 0; i--)
                {
                    runText = runText.Replace("<link=\"" + i + "\">", "<link=\"" + (i + offset) + "\">");
                }

                joined.Append(runText);
                offset += run.Links.Count;
            }

            return joined.ToString();
        }

        private void AddChildren(ContainerInline container)
        {
            foreach (var child in container)
            {
                Add(child);
            }
        }

        private void Add(Inline inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(Plain(literal.Content.ToString()));
                    break;
                case CodeInline code:
                    text.Append("<mark=").Append(CodeMark).Append("><color=").Append(CodeColor).Append('>').Append(Plain(" " + code.Content + " ")).Append("</color></mark>");
                    break;
                case LineBreakInline lineBreak:
                    text.Append(lineBreak.IsHard ? "\n" : " ");
                    break;
                case HtmlEntityInline entity:
                    text.Append(Plain(entity.Transcoded.ToString()));
                    break;
                case HtmlInline html:
                    AddHtml(html.Tag);
                    break;
                case TaskList task:
                    taskPosition = task.Span.Start;
                    taskChecked = task.Checked;
                    break;
                case AutolinkInline autolink:
                    AddLink((autolink.IsEmail ? "mailto:" : string.Empty) + autolink.Url, () => text.Append(Plain(autolink.Url)));
                    break;
                case LinkInline link:
                    AddLink(link);
                    break;
                case EmphasisInline emphasis:
                    AddEmphasis(emphasis);
                    break;
                case ContainerInline container:
                    AddChildren(container);
                    break;
            }
        }

        private void AddLink(LinkInline link)
        {
            if (link.IsImage)
            {
                Flush();
                runs.Add(new InlineRun(link));
                return;
            }

            AddLink(link.Url ?? string.Empty, () => AddChildren(link));
        }

        private void AddLink(string url, System.Action label)
        {
            var color = linkWorks(url) ? LinkColor : MissingColor;
            text.Append("<link=\"").Append(links.Count).Append("\"><color=").Append(color).Append("><u>");
            links.Add(url);
            label();
            text.Append("</u></color></link>");
        }

        private void AddEmphasis(EmphasisInline emphasis)
        {
            var (open, close) = Tags(emphasis.DelimiterChar, emphasis.DelimiterCount);
            text.Append(open);
            AddChildren(emphasis);
            text.Append(close);
        }

        private static (string open, string close) Tags(char delimiter, int count)
        {
            switch (delimiter)
            {
                case '~':
                    return count >= 2 ? ("<s>", "</s>") : ("<sub>", "</sub>");
                case '^':
                    return ("<sup>", "</sup>");
                case '+':
                    return ("<u>", "</u>");
                case '=':
                    return ("<mark=" + HighlightMark + ">", "</mark>");
                default:
                    return count >= 2 ? ("<b>", "</b>") : ("<i>", "</i>");
            }
        }

        /// <summary>Of HTML written into a page, only line breaks mean anything here.</summary>
        private void AddHtml(string tag)
        {
            if (tag.StartsWith("<br", System.StringComparison.OrdinalIgnoreCase))
            {
                text.Append('\n');
            }
        }

        /// <summary>Ends the label here when it holds as many tags as it safely can; the paragraph goes on in the next.</summary>
        private void SplitIfFull()
        {
            var marks = 0;
            for (var i = 0; i < text.Length; i++)
            {
                marks += text[i] == '<' ? 1 : 0;
            }

            if (marks < MaxTagMarks)
            {
                return;
            }

            Flush();
            if (runs.Count > 0)
            {
                runs[runs.Count - 1].GoesOn = true;
            }
        }

        private void Flush()
        {
            var written = text.ToString();
            if (written.Trim().Length > 0 || taskPosition >= 0)
            {
                runs.Add(new InlineRun(written.Trim('\n', ' '), links) { TaskPosition = taskPosition, TaskChecked = taskChecked });
            }

            text = new StringBuilder();
            links = new List<string>();
            taskPosition = -1;
        }
    }
}
