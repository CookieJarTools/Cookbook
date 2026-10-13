using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Markdown;
using CookieJarTools.Cookbook.Editor.Pages;
using CookieJarTools.Cookbook.Editor.Health;
using CookieJarTools.Cookbook.Editor.Search;
using CookieJarTools.Frosting.Editor.Inspector.Chrome;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// The CookBook: the page tree down the side, the page to the right. Back and forward, the
    /// folders the page is in along the top, a search box, and Edit to write the page.
    /// <para>
    /// Leaving a page being written saves it. Writing not yet saved is kept through script reloads.
    /// The page shown is found again when the pages change on disk.
    /// </para>
    /// </summary>
    public sealed class CookbookWindow : EditorWindow, IDocNavigator
    {
        private const string Prefix = "cookbook";
        private const long ScrollDelay = 50;
        private const string HealthPanel = "health";
        private const string SelectionPanel = "selection";
        private const long SelectionCheck = 250;

        [SerializeField] private string current;
        [SerializeField] private bool editing;
        [SerializeField] private PageHistory history = new PageHistory();
        [SerializeField] private PageDraft draft;
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private bool showingResults;
        [SerializeField] private string panel;
        [SerializeField] private bool following;

        private DocSearch search;
        private DocTree tree;
        private TagFilterBar tagFilter;
        private HomeView home;
        private VisualElement trail;
        private ToolbarSearchField searchField;
        private Button backButton;
        private Button forwardButton;
        private Button editButton;
        private Button projectToggle;
        private ScrollView reading;
        private PageView pageView;
        private SearchResultsView results;
        private PageEditor editor;
        private HealthView health;
        private SelectionView selectionView;
        private OnThisPage onThisPage;
        private Button followButton;
        private GuiMoment quickMoment;
        private Object shownSelection;

        public DocLibrary Library => DocLibrary.Current;

        private DocPage Current
        {
            get
            {
                var page = Library.Find(current);
                return Library.Shown(page) ? page : Library.Root;
            }
        }

        [MenuItem("Window/CookieJarTools/CookBook")]
        public static CookbookWindow ShowWindow()
        {
            var window = GetWindow<CookbookWindow>();
            window.titleContent = new GUIContent("CookBook", EditorGUIUtility.IconContent("TextAsset Icon").image);
            window.minSize = new Vector2(420f, 260f);
            return window;
        }

        /// <summary>Opens the window at a page named by id, path or title — as [Documentation] does. Offers to write the page if there is none.</summary>
        public static void OpenPage(string reference)
        {
            var window = ShowWindow();
            var page = window.Library.Find(reference);
            if (page == null)
            {
                window.Missing(reference);
                return;
            }

            window.Open(page, null);
        }

        public void Open(DocPage page, string anchor)
        {
            if (page == null)
            {
                return;
            }

            var leaving = Key(Current);
            FinishEditing();
            LeaveResults();
            panel = null;
            if (leaving != Key(page))
            {
                history.Left(leaving);
            }

            Show(page, anchor);
        }

        public void ScrollTo(string anchor)
        {
            var target = pageView.Anchor(anchor);
            if (target != null)
            {
                reading.schedule.Execute(() => reading.ScrollTo(target)).StartingIn(ScrollDelay);
            }
        }

        public void Missing(string reference)
        {
            var folder = DocTree.FolderOf(Current, Library.Root);
            var name = reference.Contains("/") ? reference.Substring(reference.LastIndexOf('/') + 1) : reference;
            if (!EditorUtility.DisplayDialog("CookBook", "There is no page called “" + reference + "” yet.\n\nWrite it now, in " + (folder.IsRoot ? "the top folder" : folder.Title) + "?", "Write it", "Not now"))
            {
                return;
            }

            Open(Library.CreatePage(folder, name), null);
            StartEditing();
        }

        public void ToggleTask(int position)
        {
            var page = Current;
            var body = page.Body;
            if (position < 0 || position + 2 >= body.Length || body[position] != '[')
            {
                return;
            }

            var ticked = body[position + 1] == ' ' ? 'x' : ' ';
            var changed = body.Substring(0, position + 1) + ticked + body.Substring(position + 2);
            var scroll = reading.scrollOffset;
            Show(Library.Save(page, page.Title, page.Header.Tags, changed), null);
            reading.schedule.Execute(() => reading.scrollOffset = scroll);
        }

        private static string Key(DocPage page)
        {
            if (page == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(page.Header.Id) ? page.Reference : page.Header.Id;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            CookbookStyle.Apply(root);
            root.Add(Toolbar());

            var split = new TwoPaneSplitView(0, 230, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "cookbook-split" };
            split.AddToClassList(Prefix + "__split");
            split.Add(Sidebar());
            split.Add(Content());
            root.Add(split);

            Library.Reloaded += Reloaded;
            Selection.selectionChanged += SelectionChanged;
            root.schedule.Execute(CheckSelection).Every(SelectionCheck);
            Reloaded();
            RestoreEditing();
        }

        [Shortcut("CookBook/Go to Page", typeof(CookbookWindow), KeyCode.K, ShortcutModifiers.Action)]
        private static void GoToPageShortcut(ShortcutArguments arguments)
        {
            (arguments.context as CookbookWindow)?.OpenQuickOpen();
        }

        /// <summary>Jump to a page by name: a list dropped down over the top of the window.</summary>
        private void OpenQuickOpen()
        {
            quickMoment.Run(rect => QuickOpen.Show(GUIUtility.GUIToScreenRect(rect), Library, page => Open(page, null)));
        }

        private void OnDisable()
        {
            if (DocLibrary.Current != null)
            {
                Library.Reloaded -= Reloaded;
            }

            Selection.selectionChanged -= SelectionChanged;

            KeepDraft();
        }

        private VisualElement Toolbar()
        {
            var bar = new VisualElement();
            bar.AddToClassList(Prefix + "__toolbar");
            var homeButton = ToolbarButton("Home", "Back to the CookBook's home page", GoHome);
            homeButton.AddToClassList(Prefix + "__home");
            bar.Add(homeButton);
            backButton = ToolbarButton("‹", "Back", GoBack);
            forwardButton = ToolbarButton("›", "Forward", GoForward);
            bar.Add(backButton);
            bar.Add(forwardButton);

            trail = new VisualElement();
            trail.AddToClassList(Prefix + "__trail");
            bar.Add(trail);

            searchField = new ToolbarSearchField { placeholderText = "Search… (Ctrl+K to jump)" };
            searchField.AddToClassList(Prefix + "__search");

            // Typing carries on here from the home page's search box, so taking focus must not select what is already typed.
            searchField.Q<TextField>().textSelection.selectAllOnFocus = false;
            searchField.Q<TextField>().textSelection.selectAllOnMouseUp = false;
            searchField.RegisterValueChangedCallback(evt => Search(evt.newValue));
            bar.Add(searchField);

            followButton = ToolbarButton("Selection", "Follow the selection: show the pages about whatever is selected in the editor", ToggleFollowing);
            followButton.AddToClassList(Prefix + "__follow");
            bar.Add(followButton);
            bar.Add(ToolbarButton("+", "New page or folder", () => NewMenu(bar)));
            bar.Add(ToolbarButton("⋯", "More", MoreMenu));
            quickMoment = new GuiMoment();
            quickMoment.AddToClassList(Prefix + "__moment");
            bar.Add(quickMoment);
            editButton = ToolbarButton("Edit", "Write this page", ToggleEditing);
            editButton.AddToClassList(Prefix + "__edit");
            bar.Add(editButton);
            return bar;
        }

        private VisualElement Content()
        {
            var content = new VisualElement();
            content.AddToClassList(Prefix + "__content");

            reading = new ScrollView(ScrollViewMode.Vertical);
            reading.AddToClassList(Prefix + "__reading");
            reading.contentContainer.AddToClassList(Prefix + "__reading-content");
            reading.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            pageView = new PageView(this);
            pageView.TagChosen += PickTag;
            home = new HomeView(this);
            home.TagChosen += PickTag;
            home.Searched += SearchFromHome;
            home.HealthChosen += ShowHealth;
            results = new SearchResultsView();
            results.Chosen += page => Open(page, null);
            results.TagChosen += PickTag;
            pageView.StillRight += StillRight;
            health = new HealthView(this);
            selectionView = new SelectionView(this);
            selectionView.WritePage += WritePageAbout;
            reading.Add(home);
            reading.Add(pageView);
            reading.Add(results);
            reading.Add(health);
            reading.Add(selectionView);

            onThisPage = new OnThisPage(reading, ScrollTo);
            content.RegisterCallback<GeometryChangedEvent>(evt => onThisPage.Fit(evt.newRect.width));

            editor = new PageEditor(this);
            editor.Saved += Saved;
            content.Add(reading);
            content.Add(editor);
            content.Add(onThisPage);
            return content;
        }

        private Button ToolbarButton(string text, string tip, System.Action clicked)
        {
            var button = new Button(clicked) { text = text, tooltip = tip };
            button.AddToClassList(Prefix + "__tool");
            return button;
        }

        /// <summary>The pages changed, here or on disk: the tree drawn again, and the page shown again unless it is being written.</summary>
        private void Reloaded()
        {
            search = null;
            tagFilter.Show(Library.Tags());
            ShowTree();
            if (editing)
            {
                tree.Select(Current);
                return;
            }

            var scroll = reading.scrollOffset;
            Show(Current, null);
            RefreshResults();
            RefreshPanel();
            reading.schedule.Execute(() => reading.scrollOffset = scroll);
        }

        private void Show(DocPage page, string anchor)
        {
            page ??= Library.Root;
            current = Key(page);
            tree.Select(page);
            ShowTrail(page);
            if (page.IsRoot)
            {
                home.Show(Library);
            }
            else
            {
                pageView.Show(page);
            }

            ShowMode();
            ShowOnThisPage(page);
            if (!string.IsNullOrEmpty(anchor))
            {
                ScrollTo(anchor);
                return;
            }

            reading.scrollOffset = Vector2.zero;
        }

        /// <summary>Reading, writing, or search results: one shown at a time.</summary>
        private void ShowMode()
        {
            var inPanel = !editing && !string.IsNullOrEmpty(panel);
            var searching = !editing && !inPanel && showingResults;
            var atHome = !searching && !inPanel && Current.IsRoot;
            var onPage = !searching && !inPanel && !atHome;
            reading.style.display = editing ? DisplayStyle.None : DisplayStyle.Flex;
            editor.style.display = editing ? DisplayStyle.Flex : DisplayStyle.None;
            home.style.display = atHome ? DisplayStyle.Flex : DisplayStyle.None;
            pageView.style.display = onPage ? DisplayStyle.Flex : DisplayStyle.None;
            results.style.display = searching ? DisplayStyle.Flex : DisplayStyle.None;
            health.style.display = inPanel && panel == HealthPanel ? DisplayStyle.Flex : DisplayStyle.None;
            selectionView.style.display = inPanel && panel == SelectionPanel ? DisplayStyle.Flex : DisplayStyle.None;
            followButton.EnableInClassList(Prefix + "__follow--on", following);
            if (!onPage || editing)
            {
                onThisPage.Hide();
            }
            editButton.text = editing ? "Done" : "Edit";
            editButton.SetEnabled(editing || Current.IsEditable);
            editButton.EnableInClassList(Prefix + "__edit--on", editing);
            backButton.SetEnabled(history.CanGoBack);
            forwardButton.SetEnabled(history.CanGoForward);
        }

        private void ShowTrail(DocPage page)
        {
            trail.Clear();
            trail.Add(TrailButton(Library.Root, "CookBook"));
            foreach (var folder in page.Trail())
            {
                trail.Add(TrailSeparator());
                trail.Add(TrailButton(folder, folder.Title));
            }

            if (!page.IsRoot)
            {
                trail.Add(TrailSeparator());
                var here = TrailButton(page, page.Title);
                here.AddToClassList(Prefix + "__crumb--here");
                trail.Add(here);
            }
        }

        private Button TrailButton(DocPage page, string text)
        {
            var button = new Button(() => Open(page, null)) { text = text, enableRichText = false };
            button.AddToClassList(Prefix + "__crumb");
            return button;
        }

        private Label TrailSeparator()
        {
            var separator = new Label("›");
            separator.AddToClassList(Prefix + "__crumb-separator");
            return separator;
        }

        /// <summary>Typing in the search box, or picking tags: what matches is listed in place of the page.</summary>
        private void Search(string query)
        {
            showingResults = !string.IsNullOrWhiteSpace(query) || tags.Count > 0;
            if (showingResults)
            {
                FinishEditing();
                panel = null;
                RefreshResults();
            }

            ShowMode();
        }

        private void RefreshResults()
        {
            if (!showingResults || results == null)
            {
                return;
            }

            search ??= new DocSearch(Library);
            var query = searchField.value.Trim();
            results.Show(query, tags, search.Find(query, tags));
        }

        /// <summary>Opening a page puts the results away; the search box and tags stay as they were, to come back to.</summary>
        private void LeaveResults()
        {
            showingResults = false;
        }

        private VisualElement Sidebar()
        {
            var sidebar = new VisualElement();
            sidebar.AddToClassList(Prefix + "__sidebar");
            tagFilter = new TagFilterBar();
            tagFilter.Changed += TagsChanged;
            tree = new DocTree(TreeMenu);
            tree.Chosen += page => Open(page, null);
            tree.MovedInto += (page, folder) => Moved(page, folder);
            sidebar.Add(tagFilter);
            sidebar.Add(SidebarHeading(sidebar));
            sidebar.Add(tree);
            return sidebar;
        }

        /// <summary>"Pages", with a button to add one — the same as the toolbar's.</summary>
        private VisualElement SidebarHeading(VisualElement sidebar)
        {
            var heading = new VisualElement();
            heading.AddToClassList(Prefix + "__sidebar-heading");
            heading.Add(new Label("Pages"));
            projectToggle = new Button(ToggleProject);
            projectToggle.AddToClassList(Prefix + "__sidebar-toggle");
            ShowProjectToggle();
            heading.Add(projectToggle);
            var add = new Button(() => NewMenu(heading)) { text = "+", tooltip = "New page or folder" };
            add.AddToClassList(Prefix + "__sidebar-add");
            heading.Add(add);
            return heading;
        }

        /// <summary>Shows or hides the Markdown found among the project's assets — each person's own choice.</summary>
        private void ToggleProject()
        {
            Library.ShowProject = !Library.ShowProject;
            ShowProjectToggle();
            if (Current.IsProject && !Library.ShowProject)
            {
                Show(Library.Root, null);
            }
        }

        private void ShowProjectToggle()
        {
            var shown = Library.ShowProject;
            projectToggle.text = shown ? "Project docs: shown" : "Project docs: hidden";
            projectToggle.tooltip = shown ? "Markdown found in the project's assets is shown. Click to hide it." : "Markdown found in the project's assets is hidden. Click to show it.";
            projectToggle.EnableInClassList(Prefix + "__sidebar-toggle--off", !shown);
        }

        private void PickTag(string tag)
        {
            tagFilter.Pick(tag);
        }

        private void TagsChanged(IReadOnlyCollection<string> picked)
        {
            tags = picked.ToList();
            ShowTree();
            Search(searchField.value);
        }

        /// <summary>The tree, narrowed to pages with every tag picked — their own or their folders'.</summary>
        private void ShowTree()
        {
            var known = Library.Tags();
            tags = tags.Where(t => known.Any(k => k.tag.Equals(t, System.StringComparison.OrdinalIgnoreCase))).ToList();
            tagFilter.Set(tags);
            var top = Library.Root.Children.AsEnumerable();
            top = Library.ShowProject && Library.ProjectRoot.Children.Count > 0 ? top.Append(Library.ProjectRoot) : top;
            top = Library.ShowCode ? top.Append(Library.CodeRoot) : top;
            tree.Show(top, tags.Count == 0 ? null : page => tags.All(page.HasTag));
            tree.Select(Current);
        }

        /// <summary>Typing on the home screen moves into the toolbar's search box, so the typing carries on there.</summary>
        private void SearchFromHome(string text)
        {
            searchField.value = text;
            var input = searchField.Q<TextField>();
            input.Focus();
            input.SelectRange(text.Length, text.Length);
            input.schedule.Execute(() => input.SelectRange(input.value.Length, input.value.Length));
        }

        private void GoHome()
        {
            Open(Library.Root, null);
        }

        private void GoBack()
        {
            Go(history.Back(Key(Current)));
        }

        private void GoForward()
        {
            Go(history.Forward(Key(Current)));
        }

        private void Go(string key)
        {
            var page = Library.Find(key);
            if (page == null)
            {
                return;
            }

            FinishEditing();
            LeaveResults();
            panel = null;
            Show(page, null);
        }

        private void ToggleEditing()
        {
            if (editing)
            {
                FinishEditing();
                Show(Current, null);
                return;
            }

            StartEditing();
        }

        private void StartEditing()
        {
            LeaveResults();
            editing = true;
            editor.Edit(Current);
            ShowMode();
            editor.Focus(true);
        }

        /// <summary>Stops writing, saving what was written.</summary>
        private void FinishEditing()
        {
            if (!editing)
            {
                return;
            }

            editing = false;
            draft = null;
            var saved = editor.Save();
            current = Key(saved) ?? current;
            ShowMode();
        }

        private void Saved(DocPage page)
        {
            current = Key(page);
            ShowTrail(page);
            tree.Select(page);
        }

        private void KeepDraft()
        {
            draft = editing && editor != null && editor.Dirty ? editor.Draft() : null;
            if (draft != null)
            {
                draft.page = current;
            }
        }

        /// <summary>After a script reload: back to writing the page that was being written, with what was not yet saved.</summary>
        private void RestoreEditing()
        {
            if (!editing)
            {
                return;
            }

            editor.Edit(Current);
            if (draft != null && draft.page == current)
            {
                editor.Restore(draft);
            }

            draft = null;
            ShowMode();
        }

        private void Moved(DocPage page, DocPage folder)
        {
            FinishEditing();
            var moved = Library.Move(page, folder ?? Library.Root);
            if (Key(page) == current && moved != null)
            {
                current = Key(moved);
            }

            Reloaded();
        }

        private void NewMenu(VisualElement near)
        {
            var menu = new GenericMenu();
            var folder = DocTree.FolderOf(Current, Library.Root);
            menu.AddItem(new GUIContent("New Page"), false, () => NewPage(folder, near));
            foreach (var template in DocTemplates.Names(Library.Source))
            {
                menu.AddItem(new GUIContent("New Page from Template/" + template), false, () => NewPageFromTemplate(folder, template, near));
            }

            menu.AddItem(new GUIContent("New Folder"), false, () => NewFolder(folder, near));
            menu.ShowAsContext();
        }

        /// <summary>The right-click menu: for a page or folder, or — clicked below them all — for the top folder.</summary>
        private void TreeMenu(DocPage page, ContextualMenuPopulateEvent evt)
        {
            if (page != null && page.IsCode)
            {
                evt.StopPropagation();
                CodeMenu(page, evt);
                return;
            }

            var folder = DocTree.FolderOf(page, Library.Root);
            evt.menu.AppendAction("New Page", _ => NewPage(folder, tree));
            foreach (var template in DocTemplates.Names(Library.Source))
            {
                evt.menu.AppendAction("New Page from Template/" + template, _ => NewPageFromTemplate(folder, template, tree));
            }

            evt.menu.AppendAction("New Folder", _ => NewFolder(folder, tree));
            if (page == null)
            {
                return;
            }

            evt.StopPropagation();
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Rename…", _ => Rename(page), page.IsProjectRoot ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            evt.menu.AppendAction("Copy [Documentation] Attribute", _ => tree.CopyReference(page));
            evt.menu.AppendAction("Show in Explorer", _ => EditorUtility.RevealInFinder(page.Source.LocalPath(page.FilePath)));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Remove…", _ => Remove(page));
        }

        /// <summary>The right-click menu for the Code Reference: made from the code, so nothing to rename or remove — but a note to write, and the script to open.</summary>
        private void CodeMenu(DocPage page, ContextualMenuPopulateEvent evt)
        {
            if (page.Code != null)
            {
                evt.menu.AppendAction(string.IsNullOrWhiteSpace(page.Note) ? "Write a Note" : "Edit Note", _ => EditNote(page));
                evt.menu.AppendAction("Open Script", _ => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(page.Code.Script)));
                evt.menu.AppendAction("Copy Link", _ => EditorGUIUtility.systemCopyBuffer = "[[" + page.Header.Id + "]]");
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendAction("Choose What's Documented…", _ => Open(Library.CodeRoot, null));
            evt.menu.AppendAction("Hide Code Reference", _ => ToggleCode());
        }

        private void EditNote(DocPage page)
        {
            Open(page, null);
            StartEditing();
        }

        /// <summary>Shows or hides the Code Reference — each person's own choice.</summary>
        private void ToggleCode()
        {
            Library.ShowCode = !Library.ShowCode;
            if (Current.IsCode && !Library.ShowCode)
            {
                Show(Library.Root, null);
            }
        }

        private void NewPage(DocPage folder, VisualElement near)
        {
            NamePrompt.ShowAt(this, near, "Name the new page", "New Page", name =>
            {
                Open(Library.CreatePage(folder, name), null);
                StartEditing();
            });
        }

        private void NewPageFromTemplate(DocPage folder, string template, VisualElement near)
        {
            NamePrompt.ShowAt(this, near, "Name the new " + template.ToLowerInvariant(), template, name =>
            {
                var (templateTags, body) = DocTemplates.Fill(Library.Source, template, name);
                Open(Library.CreatePage(folder, name, body, templateTags), null);
                StartEditing();
            });
        }

        private void NewFolder(DocPage folder, VisualElement near)
        {
            NamePrompt.ShowAt(this, near, "Name the new folder", "New Folder", name => Open(Library.CreateFolder(folder, name), null));
        }

        private void Rename(DocPage page)
        {
            NamePrompt.ShowAt(this, tree, "Rename “" + page.Title + "”", page.Title, name =>
            {
                FinishEditing();
                var renamed = Library.Rename(page, name);
                Show(renamed ?? Current, null);
            });
        }

        private void Remove(DocPage page)
        {
            var what = page.IsFolder ? "the folder “" + page.Title + "” and everything in it" : "“" + page.Title + "”";
            if (!EditorUtility.DisplayDialog("CookBook", "Remove " + what + "?\n\nIt goes to the .trash folder inside the CookBook's folder, so it can be got back.", "Remove", "Cancel"))
            {
                return;
            }

            var showing = Current;
            FinishEditing();
            var parent = page.Parent;
            Library.Remove(page);
            var removedShown = showing == page || DocLibrary.Within(page).Contains(showing);
            Show(removedShown ? Library.Find(Key(parent)) : Library.Find(current), null);
        }
    

        private void MoreMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Go to Page…  Ctrl+K"), false, OpenQuickOpen);
            menu.AddItem(new GUIContent("Docs Health"), panel == HealthPanel, ShowHealth);
            menu.AddItem(new GUIContent("Follow Selection"), following, ToggleFollowing);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Show Docs Found in the Project"), Library.ShowProject, ToggleProject);
            menu.AddItem(new GUIContent("Show Code Reference"), Library.ShowCode, ToggleCode);
            menu.ShowAsContext();
        }

        /// <summary>Everything in the CookBook that needs looking after, in place of the page.</summary>
        public void ShowHealth()
        {
            FinishEditing();
            LeaveResults();
            panel = HealthPanel;
            health.Show();
            ShowMode();
            reading.scrollOffset = Vector2.zero;
        }

        /// <summary>Following the selection shows the pages about whatever is selected, as soon as it is — until a page is opened.</summary>
        private void ToggleFollowing()
        {
            following = !following;
            if (following)
            {
                ShowSelection();
                return;
            }

            if (panel == SelectionPanel)
            {
                panel = null;
            }

            ShowMode();
        }

        /// <summary>A new selection while following: its pages are shown, even over a page being read.</summary>
        private void SelectionChanged()
        {
            if (following && !editing)
            {
                ShowSelection();
            }
        }

        /// <summary>
        /// Looked at a few times a second while the selection's pages are showing — Unity does not
        /// always say when a script changes the selection.
        /// </summary>
        private void CheckSelection()
        {
            if (following && !editing && panel == SelectionPanel && Selection.activeObject != shownSelection)
            {
                ShowSelection();
            }
        }

        private void ShowSelection()
        {
            LeaveResults();
            panel = SelectionPanel;
            shownSelection = Selection.activeObject;
            selectionView.Show(shownSelection);
            ShowMode();
            reading.scrollOffset = Vector2.zero;
        }

        /// <summary>After the pages change, whatever is shown in place of a page is worked out again.</summary>
        private void RefreshPanel()
        {
            if (panel == HealthPanel)
            {
                health.Show();
            }
            else if (panel == SelectionPanel)
            {
                shownSelection = Selection.activeObject;
                selectionView.Show(shownSelection);
            }

            ShowMode();
        }

        /// <summary>A page about something selected that has none yet: named after it, linking to it when it is an asset.</summary>
        private void WritePageAbout(string name, string assetPath)
        {
            var folder = DocTree.FolderOf(Current, Library.Root);
            var link = string.IsNullOrEmpty(assetPath) ? string.Empty : "[" + name + "](" + assetPath.Replace(" ", "%20") + ")\n\n";
            Open(Library.CreatePage(folder.IsProject ? Library.Root : folder, name, "# " + name + "\n\n" + link), null);
            StartEditing();
        }

        /// <summary>"It's still right" on an out-of-date page: saved as it is, which marks it up to date.</summary>
        private void StillRight(DocPage page)
        {
            Show(Library.Save(page, page.Title, page.Header.Tags, page.Body) ?? page, null);
        }

        private void ShowOnThisPage(DocPage page)
        {
            if (page.IsRoot || page.IsFolder)
            {
                onThisPage.Hide();
                return;
            }

            onThisPage.Show(pageView.Headings(), pageView.Anchor);
        }
    }
}
