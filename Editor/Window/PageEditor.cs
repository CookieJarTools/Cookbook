using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Images;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using CookieJarTools.Frosting.Editor.Fields.Attributed;
using CookieJarTools.Frosting.Editor.Inspector.Chrome;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// Writing a page: its title and tags, the Markdown on the left and how it will look on the
    /// right, drawn again a moment after each change. Ctrl+S saves.
    /// <para>
    /// Pictures go in by dragging image files onto the text, by picking a file, or by capturing the
    /// Scene or Game view; each is copied next to the page and put in the text where the cursor
    /// is. Dragging a project asset in links to it. A folder also has a banner image.
    /// </para>
    /// </summary>
    public sealed class PageEditor : VisualElement
    {
        private const string Prefix = "cookbook-editor";
        private const long PreviewDelay = 250;

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif" };

        private readonly IDocNavigator navigator;
        private readonly TextField title;
        private readonly TagEditor tags;
        private readonly TextField banner;
        private readonly VisualElement bannerRow;
        private readonly TextField body;
        private readonly MarkdownView preview;
        private readonly Label status;

        private IVisualElementScheduledItem pendingPreview;
        private int cursor;
        private ClipRecorder recorder;
        private Button recordScene;
        private Button recordGame;

        public PageEditor(IDocNavigator navigator)
        {
            this.navigator = navigator;
            AddToClassList(Prefix);

            title = Classed(new TextField("Title"), Prefix + "__title");
            tags = new TagEditor();
            tags.Changed += MarkDirty;
            banner = new TextField("Banner") { tooltip = "An image shown across the top of the folder" };
            bannerRow = Classed(new VisualElement(), Prefix + "__banner");
            bannerRow.Add(banner);
            bannerRow.Add(Classed(new Button(PickBanner) { text = "Choose…" }, Prefix + "__button"));
            Add(title);
            Add(tags);
            Add(bannerRow);
            Add(Toolbar(out status));

            var split = new TwoPaneSplitView(0, 420, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "cookbook-editor-split" };
            split.AddToClassList(Prefix + "__split");
            body = Classed(new TextField { multiline = true, verticalScrollerVisibility = ScrollerVisibility.Auto }, Prefix + "__body");
            body.RegisterValueChangedCallback(_ => Changed());
            body.RegisterCallback<FocusOutEvent>(_ => cursor = body.cursorIndex);
            body.RegisterCallback<DragUpdatedEvent>(DragUpdated);
            body.RegisterCallback<DragPerformEvent>(DragPerformed);
            title.RegisterValueChangedCallback(_ => MarkDirty());
            banner.RegisterValueChangedCallback(_ => MarkDirty());

            var previewScroll = Classed(new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden }, Prefix + "__preview");
            previewScroll.contentContainer.AddToClassList(Prefix + "__preview-content");
            preview = new MarkdownView(navigator);
            previewScroll.Add(preview);
            split.Add(body);
            split.Add(previewScroll);
            Add(split);

            RegisterCallback<KeyDownEvent>(ShortcutKey, TrickleDown.TrickleDown);
        }

        /// <summary>Saved, with the page as it now is — renamed pages have a new path.</summary>
        public event Action<DocPage> Saved;

        public DocPage Page { get; private set; }

        public bool Dirty { get; private set; }

        public void Edit(DocPage page)
        {
            Page = page;
            title.SetValueWithoutNotify(page.IsRoot ? string.Empty : page.Title);
            title.SetEnabled(!page.IsRoot && !page.IsProject && !page.IsCode);
            title.tooltip = page.IsProject ? "Found in the project: it keeps its own file name" : page.IsCode ? "Named after the type: this is a note shown on its page" : string.Empty;
            tags.Show(page.Header.Tags, navigator.Library.Tags().Select(t => t.tag));
            banner.SetValueWithoutNotify(page.Header.Banner ?? string.Empty);
            bannerRow.style.display = page.IsFolder ? DisplayStyle.Flex : DisplayStyle.None;
            body.SetValueWithoutNotify(page.EditableBody);
            cursor = page.EditableBody.Length;
            Dirty = false;
            ShowStatus();
            preview.Show(page, page.EditableBody);
        }

        /// <summary>Writes the page if anything changed. Returns the page as saved, or as it was.</summary>
        public DocPage Save()
        {
            if (Page == null || !Dirty)
            {
                return Page;
            }

            Page.Header.Banner = string.IsNullOrWhiteSpace(banner.value) ? null : banner.value.Trim();
            var name = Page.IsRoot || string.IsNullOrWhiteSpace(title.value) ? Page.Title : title.value.Trim();
            var saved = navigator.Library.Save(Page, name, tags.Tags, body.value) ?? Page;
            Page = saved;
            Dirty = false;
            ShowStatus();
            Saved?.Invoke(saved);
            return saved;
        }

        /// <summary>What has been written and not saved, to keep through a script reload.</summary>
        public PageDraft Draft()
        {
            return new PageDraft { title = title.value, tags = string.Join(",", tags.Tags), banner = banner.value, body = body.value };
        }

        /// <summary>Puts back writing kept through a script reload, still unsaved.</summary>
        public void Restore(PageDraft draft)
        {
            title.SetValueWithoutNotify(draft.title);
            tags.Show((draft.tags ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), navigator.Library.Tags().Select(t => t.tag));
            banner.SetValueWithoutNotify(draft.banner);
            body.SetValueWithoutNotify(draft.body);
            preview.Show(Page, draft.body);
            MarkDirty();
        }

        /// <summary>Ready to type: in the text with the cursor at the end, or in the title.</summary>
        public void Focus(bool onText)
        {
            if (!onText)
            {
                title.Focus();
                return;
            }

            body.Focus();
            cursor = body.value.Length;
            body.SelectRange(cursor, cursor);
        }

        private VisualElement Toolbar(out Label saved)
        {
            var bar = Classed(new VisualElement(), Prefix + "__toolbar");
            bar.Add(Button("Image…", "Pick an image file to put in the page", PickImage));
            bar.Add(Button("Capture Scene", "A picture of the Scene view, put in the page", () => AddPicture("scene", ViewCapture.Scene())));
            bar.Add(Button("Capture Game", "A picture of what the game camera sees, put in the page", () => AddPicture("game", ViewCapture.Game())));
            recordScene = Button("Record Scene", "Record a few seconds of the Scene view as a GIF, put in the page", () => Record(true));
            recordGame = Button("Record Game", "Record a few seconds of what the game camera sees as a GIF, put in the page", () => Record(false));
            bar.Add(recordScene);
            bar.Add(recordGame);
            bar.Add(Picker("Link…", "Link to another page — pick it from the list", () => PagePicker.Items(navigator.Library), Insert));
            bar.Add(Picker("Action…", "Add a button that does something for the reader — makes an asset, opens a scene, runs a tool", ActionPicker.Items, Insert));
            bar.Add(Classed(new VisualElement(), Prefix + "__spacer"));
            saved = Classed(new Label(), Prefix + "__status");
            bar.Add(saved);
            bar.Add(Classed(Button("Save", "Save the page (Ctrl+S)", () => Save()), Prefix + "__button--primary"));
            return bar;
        }

        /// <summary>
        /// A button opening a searchable list; what is picked is put in the text. The moment that
        /// opens the list lies over the button, beside it rather than in it — a button with anything
        /// inside is sized to that, not to its words.
        /// </summary>
        private VisualElement Picker(string text, string tip, Func<List<(string name, object value)>> items, Action<string> picked)
        {
            var holder = Classed(new VisualElement(), Prefix + "__picker");
            var moment = Classed(new GuiMoment(), Prefix + "__button-moment");
            holder.Add(Button(text, tip, () => moment.Run(rect => new ItemPicker(text.TrimEnd('…'), items(), value => picked(value as string)).Show(rect))));
            holder.Add(moment);
            return holder;
        }

        private Button Button(string text, string tip, Action clicked)
        {
            return Classed(new Button(clicked) { text = text, tooltip = tip }, Prefix + "__button");
        }

        private void Changed()
        {
            MarkDirty();
            pendingPreview?.Pause();
            pendingPreview = schedule.Execute(() => preview.Show(Page, body.value)).StartingIn(PreviewDelay);
        }

        private void MarkDirty()
        {
            Dirty = true;
            ShowStatus();
        }

        private void ShowStatus()
        {
            status.text = Dirty ? "Unsaved changes" : "Saved";
            status.EnableInClassList(Prefix + "__status--dirty", Dirty);
        }

        private void ShortcutKey(KeyDownEvent evt)
        {
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.S)
            {
                Save();
                evt.StopImmediatePropagation();
            }
        }

        /// <summary>Puts text where the cursor was, and leaves the cursor after it.</summary>
        private void Insert(string text)
        {
            var at = Mathf.Clamp(body.focusController?.focusedElement == body ? body.cursorIndex : cursor, 0, body.value.Length);
            var before = at > 0 && body.value[at - 1] != '\n' && text.StartsWith("!") ? "\n" : string.Empty;
            body.value = body.value.Insert(at, before + text);
            cursor = at + before.Length + text.Length;
            body.Focus();
            body.SelectRange(cursor, cursor);
        }

        private void PickImage()
        {
            var file = EditorUtility.OpenFilePanel("Put an image in the page", string.Empty, "png,jpg,jpeg,gif");
            if (!string.IsNullOrEmpty(file))
            {
                AddPicture(Path.GetFileName(file), File.ReadAllBytes(file));
            }
        }

        private void PickBanner()
        {
            var file = EditorUtility.OpenFilePanel("Banner for the folder", string.Empty, "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(file) && Page != null)
            {
                banner.value = navigator.Library.AddImage(Page, Path.GetFileName(file), File.ReadAllBytes(file));
            }
        }

        /// <summary>Saves the picture beside the page and puts it in the text. A name without an extension is a capture, saved as PNG.</summary>
        private void AddPicture(string name, byte[] bytes)
        {
            if (bytes == null || Page == null)
            {
                EditorUtility.DisplayDialog("Cookbook", "There was nothing to capture — open the Scene view, or add a camera.", "OK");
                return;
            }

            var file = Path.HasExtension(name) ? name : name + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
            var path = navigator.Library.AddImage(Page, file, bytes);
            Insert("![" + Path.GetFileNameWithoutExtension(file) + "](" + path.Replace(" ", "%20") + ")\n");
        }

        private void DragUpdated(DragUpdatedEvent evt)
        {
            if (DragAndDrop.paths.Any(IsImage) || DragAndDrop.objectReferences.Any(AssetDatabase.Contains))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        }

        /// <summary>Image files are copied next to the page; project assets are linked to where they are.</summary>
        private void DragPerformed(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();
            var assets = DragAndDrop.objectReferences.Where(AssetDatabase.Contains).ToList();
            foreach (var asset in assets)
            {
                Insert(AssetLink(asset));
            }

            foreach (var file in DragAndDrop.paths.Where(p => IsImage(p) && !p.StartsWith("Assets/")))
            {
                AddPicture(Path.GetFileName(file), File.ReadAllBytes(file));
            }

            evt.StopPropagation();
        }

        /// <summary>A picture or ScriptableObject is shown in the page — the object alive, to change there; anything else is linked to.</summary>
        private static string AssetLink(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset).Replace(" ", "%20");
            var shown = asset is Texture2D || asset is ScriptableObject;
            return shown ? "![" + asset.name + "](" + path + ")\n" : "[" + asset.name + "](" + path + ")";
        }

        /// <summary>
        /// Starts recording the Scene view or game camera, or stops the one running. The button
        /// shows it is recording and for how long; the GIF goes into the page when it stops.
        /// </summary>
        private void Record(bool scene)
        {
            if (recorder != null && recorder.Recording)
            {
                recorder.Stop();
                return;
            }

            recorder = new ClipRecorder(scene);
            var button = scene ? recordScene : recordGame;
            recorder.Ticked += seconds => button.text = "■ Stop  " + seconds.ToString("0.0") + "s";
            recorder.Finished += bytes => Recorded(scene, bytes);
            if (!recorder.Start())
            {
                EditorUtility.DisplayDialog("Cookbook", "There is nothing to record — open the Scene view, or add a camera.", "OK");
                return;
            }

            button.text = "■ Stop";
            button.AddToClassList(Prefix + "__button--recording");
        }

        private void Recorded(bool scene, byte[] bytes)
        {
            foreach (var button in new[] { recordScene, recordGame })
            {
                button.text = button == recordScene ? "Record Scene" : "Record Game";
                button.RemoveFromClassList(Prefix + "__button--recording");
            }

            if (bytes != null)
            {
                AddPicture((scene ? "scene-clip-" : "game-clip-") + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".gif", bytes);
            }
        }

        private static bool IsImage(string path)
        {
            return ImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
        }

        private static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
