using System;
using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Pages;
using UnityEditor;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// The folders and pages down the side. Click to open; drag onto a folder to move into it;
    /// right-click for new pages and folders, renaming, removing, and copying a page's reference
    /// for [Documentation]. Which folders are open is remembered.
    /// </summary>
    public sealed class DocTree : VisualElement
    {
        private const int RootId = -1;

        private readonly TreeView tree;
        private readonly List<DocPage> pages = new List<DocPage>();
        private readonly Action<DocPage, ContextualMenuPopulateEvent> menu;

        private bool quiet;

        /// <param name="menu">Fills the right-click menu for a page, or for the empty space below when the page is null.</param>
        public DocTree(Action<DocPage, ContextualMenuPopulateEvent> menu)
        {
            this.menu = menu;
            AddToClassList("cookbook-tree");

            tree = new TreeView
            {
                fixedItemHeight = 28,
                selectionType = SelectionType.Single,
                reorderable = true,
                viewDataKey = "cookbook-tree",
                makeItem = () => new DocTreeRow((page, evt) => this.menu(page, evt)),
            };
            tree.bindItem = (element, index) => ((DocTreeRow)element).Show(tree.GetItemDataForIndex<DocPage>(index));
            tree.selectionChanged += Selected;
            tree.itemIndexChanged += Moved;
            tree.AddManipulator(new ContextualMenuManipulator(evt => this.menu(null, evt)));
            Add(tree);
        }

        /// <summary>A page or folder clicked.</summary>
        public event Action<DocPage> Chosen;

        /// <summary>A page or folder dragged into a folder.</summary>
        public event Action<DocPage, DocPage> MovedInto;

        /// <param name="top">The pages and folders along the top of the tree.</param>
        /// <param name="keep">Which pages to show — with the folders they are in — or null for all.</param>
        public void Show(IEnumerable<DocPage> top, Func<DocPage, bool> keep = null)
        {
            pages.Clear();
            var items = top.Where(c => Kept(c, keep)).Select(c => Item(c, keep)).ToList();
            tree.SetRootItems(items);
            tree.Rebuild();
            if (keep != null)
            {
                tree.ExpandAll();
            }
        }

        /// <summary>Selects the page, opening the folders above it, without reporting it as chosen.</summary>
        public void Select(DocPage page)
        {
            var id = page == null ? -1 : pages.IndexOf(pages.FirstOrDefault(p => p.Path == page.Path));
            quiet = true;
            if (id < 0)
            {
                tree.ClearSelection();
                quiet = false;
                return;
            }

            tree.SetSelectionById(id);
            tree.ScrollToItemById(id);
            quiet = false;
        }

        private TreeViewItemData<DocPage> Item(DocPage page, Func<DocPage, bool> keep)
        {
            var id = pages.Count;
            pages.Add(page);
            var children = page.Children.Where(c => Kept(c, keep)).Select(c => Item(c, keep)).ToList();
            return new TreeViewItemData<DocPage>(id, page, children);
        }

        /// <summary>Shown if it passes, or anything inside it does.</summary>
        private static bool Kept(DocPage page, Func<DocPage, bool> keep)
        {
            return keep == null || keep(page) || page.Children.Any(c => Kept(c, keep));
        }

        /// <summary>
        /// A page picked — by someone clicking or using the arrow keys in the tree, which has focus
        /// then. The tree also puts back the selection it had before a script reload, by then on a
        /// different row; that is not a choice, and must not open anything.
        /// </summary>
        private void Selected(IEnumerable<object> selection)
        {
            var page = selection.OfType<DocPage>().FirstOrDefault();
            if (!quiet && page != null && HasFocus())
            {
                Chosen?.Invoke(page);
            }
        }

        private bool HasFocus()
        {
            var focused = tree.focusController?.focusedElement as VisualElement;
            return focused != null && (focused == tree || tree.Contains(focused));
        }

        /// <summary>
        /// The tree has already moved the row; the move is made in the pages, and the tree drawn again
        /// from them. Dropped onto a page, it goes into that page's folder.
        /// </summary>
        private void Moved(int id, int parentId)
        {
            if (id < 0 || id >= pages.Count)
            {
                return;
            }

            var parent = parentId == RootId || parentId >= pages.Count ? null : pages[parentId];
            var folder = parent == null || parent.IsFolder ? parent : parent.Parent;
            schedule.Execute(() => MovedInto?.Invoke(pages[id], folder));
        }

        /// <summary>Where a new page goes when made from a page's menu: the folder, or the page's own folder — the top folder for the Code Reference, which has no pages written in it.</summary>
        public static DocPage FolderOf(DocPage page, DocPage root)
        {
            if (page == null || page.IsCode)
            {
                return root;
            }

            return page.IsFolder ? page : page.Parent ?? root;
        }

        public void CopyReference(DocPage page)
        {
            var reference = string.IsNullOrEmpty(page.Header.Id) ? page.Reference : page.Header.Id;
            EditorGUIUtility.systemCopyBuffer = "[Documentation(\"" + reference + "\")]";
        }
    }
}
