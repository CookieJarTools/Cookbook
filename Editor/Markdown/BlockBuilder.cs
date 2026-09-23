using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Images;
using CookieJarTools.Cookbook.Editor.Linking;
using CookieJarTools.Cookbook.Editor.Pages;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.Tables;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// Draws a page's Markdown as UI Toolkit elements, block by block: headings, paragraphs, lists
    /// and checklists, quotes, callouts, code, tables, rules and pictures. Links and checkboxes
    /// report to the navigator.
    /// </summary>
    public sealed class BlockBuilder
    {
        public const string AnchorPrefix = "anchor-";

        private const string Prefix = "cookbook-";
        private const string LinkHoverClass = "cookbook-text--link-hover";

        private readonly IDocNavigator navigator;
        private readonly DocPage page;

        public BlockBuilder(IDocNavigator navigator, DocPage page)
        {
            this.navigator = navigator;
            this.page = page;
        }

        public void AddBlocks(VisualElement parent, ContainerBlock container)
        {
            foreach (var block in container)
            {
                AddBlock(parent, block, 0);
            }
        }

        /// <summary>A label of rich text whose links open when clicked.</summary>
        public Label RichLabel(string rich, List<string> links, string className)
        {
            var label = new Label(rich) { enableRichText = true };
            label.AddToClassList(Prefix + "text");
            label.AddToClassList(className);
            label.selection.isSelectable = true;
            if (links.Count == 0)
            {
                return label;
            }

            label.RegisterCallback<PointerUpLinkTagEvent>(evt => OpenLink(links, evt.linkID));
            label.RegisterCallback<PointerOverLinkTagEvent>(_ => label.AddToClassList(LinkHoverClass));
            label.RegisterCallback<PointerOutLinkTagEvent>(_ => label.RemoveFromClassList(LinkHoverClass));
            return label;
        }

        private bool LinkWorks(string url)
        {
            return DocLinks.Works(url, navigator.Library, page);
        }

        private void OpenLink(List<string> links, string id)
        {
            if (int.TryParse(id, out var index) && index >= 0 && index < links.Count)
            {
                DocLinks.Open(links[index], navigator, page);
            }
        }

        private void AddBlock(VisualElement parent, Block block, int depth)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    parent.Add(Heading(heading));
                    break;
                case ParagraphBlock paragraph:
                    AddParagraph(parent, paragraph);
                    break;
                case AlertBlock alert:
                    parent.Add(Callout(alert));
                    break;
                case QuoteBlock quote:
                    parent.Add(Boxed(quote, Prefix + "quote"));
                    break;
                case ListBlock list:
                    parent.Add(List(list, depth));
                    break;
                case Table table:
                    parent.Add(new TableBuilder(this).Build(table));
                    break;
                case CodeBlock code:
                    parent.Add(new CodeBlockElement(code));
                    break;
                case ThematicBreakBlock _:
                    parent.Add(Classed(new VisualElement(), Prefix + "rule"));
                    break;
                case HtmlBlock html:
                    AddHtml(parent, html);
                    break;
                case ContainerBlock container:
                    AddBlocks(parent, container);
                    break;
            }
        }

        private VisualElement Heading(HeadingBlock heading)
        {
            var rich = InlineText.Rich(heading.Inline, LinkWorks, out var links);
            var label = RichLabel(rich, links, Prefix + "h" + heading.Level);
            var id = heading.TryGetAttributes()?.Id;
            if (!string.IsNullOrEmpty(id))
            {
                label.name = AnchorPrefix + id;
            }

            return label;
        }

        private void AddParagraph(VisualElement parent, ParagraphBlock paragraph)
        {
            var step = GuideSteps.Lone(paragraph);
            if (step != null)
            {
                parent.Add(Step(step));
                return;
            }

            foreach (var run in InlineText.Runs(paragraph.Inline, LinkWorks))
            {
                parent.Add(run.Image != null ? Picture(run.Image) : Text(run));
            }
        }

        /// <summary>An action or check link on a line of its own is a guide step; one in a sentence stays a link.</summary>
        private VisualElement Step(LinkInline link)
        {
            var label = string.Concat(link.Descendants<LiteralInline>().Select(l => l.Content.ToString()));
            var key = GuideSteps.PageKey(page);
            return Actions.DocChecks.IsCheck(link.Url) ? new CheckStep(link.Url, label, key) : new ActionButton(link.Url, label, key);
        }

        private VisualElement Text(InlineRun run)
        {
            var label = RichLabel(run.Text, run.Links, Prefix + "paragraph");
            label.EnableInClassList(Prefix + "paragraph--goes-on", run.GoesOn);
            if (run.TaskPosition < 0)
            {
                return label;
            }

            var row = Classed(new VisualElement(), Prefix + "task");
            var box = Classed(new Label(run.TaskChecked ? "✓" : string.Empty) { tooltip = run.TaskChecked ? "Done — click to untick" : "Click to tick" }, Prefix + "task__box");
            box.EnableInClassList(Prefix + "task__box--checked", run.TaskChecked);
            box.RegisterCallback<ClickEvent>(_ => navigator.ToggleTask(run.TaskPosition));
            label.EnableInClassList(Prefix + "task__text--checked", run.TaskChecked);
            row.Add(box);
            row.Add(label);
            return row;
        }

        private VisualElement Picture(LinkInline image)
        {
            var alt = string.Concat(image.Descendants<LiteralInline>().Select(l => l.Content.ToString()));
            var assetPath = DocPaths.IsProjectAsset(image.Url ?? string.Empty) ? DocPaths.Unescape(image.Url) : null;
            if (assetPath != null && LiveAssetElement.Shows(assetPath))
            {
                return new LiveAssetElement(assetPath);
            }

            if (DocImages.IsAnimated(image.Url))
            {
                return new AnimatedPicture(DocImages.LoadClip(navigator.Library, page, image.Url), alt, image.Title);
            }

            return new PictureElement(DocImages.Load(navigator.Library, page, image.Url), alt, image.Title);
        }

        private VisualElement Boxed(ContainerBlock container, string className)
        {
            var box = Classed(new VisualElement(), className);
            AddBlocks(box, container);
            return box;
        }

        /// <summary>A GitHub-style callout: <c>&gt; [!NOTE]</c>, TIP, IMPORTANT, WARNING or CAUTION.</summary>
        private VisualElement Callout(AlertBlock alert)
        {
            var kind = alert.Kind.ToString().ToLowerInvariant();
            var box = Classed(new VisualElement(), Prefix + "callout");
            box.AddToClassList(Prefix + "callout--" + kind);

            var title = Classed(new VisualElement(), Prefix + "callout__title");
            title.Add(Classed(new VisualElement(), Prefix + "callout__icon"));
            title.Add(Classed(new Label(char.ToUpperInvariant(kind[0]) + kind.Substring(1)), Prefix + "callout__name"));
            box.Add(title);
            AddBlocks(box, alert);
            return box;
        }

        private VisualElement List(ListBlock list, int depth)
        {
            var element = Classed(new VisualElement(), Prefix + "list");
            var number = int.TryParse(list.OrderedStart, out var start) ? start : 1;
            foreach (var item in list.OfType<ListItemBlock>())
            {
                element.Add(ListItem(item, list.IsOrdered ? number++ + "." : Bullet(depth), depth));
            }

            return element;
        }

        private VisualElement ListItem(ListItemBlock item, string marker, int depth)
        {
            var row = Classed(new VisualElement(), Prefix + "list__item");
            var isTask = item.FirstOrDefault() is ParagraphBlock first && first.Inline?.FirstChild is Markdig.Extensions.TaskLists.TaskList;
            row.Add(Classed(new Label(isTask ? string.Empty : marker), isTask ? Prefix + "list__marker--task" : Prefix + "list__marker"));

            var content = Classed(new VisualElement(), Prefix + "list__content");
            foreach (var block in item)
            {
                AddBlock(content, block, depth + 1);
            }

            row.Add(content);
            return row;
        }

        private static string Bullet(int depth)
        {
            switch (depth % 3)
            {
                case 0:
                    return "•";
                case 1:
                    return "◦";
                default:
                    return "▪";
            }
        }

        /// <summary>Raw HTML is not drawn; any words in it are, so nothing written disappears. Comments are left out.</summary>
        private static void AddHtml(VisualElement parent, HtmlBlock html)
        {
            var text = html.Lines.ToString();
            if (html.Type == HtmlBlockType.Comment || text.Trim().Length == 0)
            {
                return;
            }

            parent.Add(Classed(new Label(text) { enableRichText = false }, Prefix + "paragraph"));
        }

        public static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
