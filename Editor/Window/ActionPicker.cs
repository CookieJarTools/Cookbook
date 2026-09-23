using System.Collections.Generic;
using System.Linq;
using CookieJarTools.Cookbook.Editor.Actions;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>
    /// What the editor's Action… button offers: the project's own actions, making any of its
    /// ScriptableObjects, and a few ready-made steps. Each comes out as a button line to put in the
    /// page — on its own line, so it is drawn as a button.
    /// </summary>
    public static class ActionPicker
    {
        public static List<(string name, object value)> Items()
        {
            var items = new List<(string name, object value)>();
            items.AddRange(DocActions.Marked.Select(a => ("Project actions/" + a.Name, (object)Line(a.Name, a.Id))));
            items.AddRange(DocActions.Creatable.Select(t => ("Create an asset/" + ObjectNames.NicifyVariableName(t.Name), (object)Line("Create " + ObjectNames.NicifyVariableName(t.Name), DocActions.CreatePrefix + t.Name))));
            items.AddRange(DocChecks.Marked.Select(c => ("Checks/Project checks/" + c.Name, (object)Line(c.Name, c.Id, DocChecks.Scheme))));
            items.Add(("Checks/An asset exists", Line(SelectedName() + " exists", DocChecks.AssetPrefix + SelectedPath(), DocChecks.Scheme)));
            items.Add(("Checks/The scene has a component", Line("The scene has a Main Camera", DocChecks.ComponentPrefix + "Camera", DocChecks.Scheme)));
            items.Add(("Checks/Something has a tag", Line("The player is tagged", DocChecks.TagPrefix + "Player", DocChecks.Scheme)));
            items.Add(("Checks/A layer exists", Line("The Enemies layer exists", DocChecks.LayerPrefix + "Enemies", DocChecks.Scheme)));
            items.Add(("Checks/A package is installed", Line("The Input System is installed", DocChecks.PackagePrefix + "com.unity.inputsystem", DocChecks.Scheme)));
            items.Add(("Other steps/Open the selected asset or scene", Line("Open " + SelectedName(), DocActions.OpenPrefix + SelectedPath())));
            items.Add(("Other steps/Show the selected asset", Line("Show " + SelectedName(), DocActions.SelectPrefix + SelectedPath())));
            items.Add(("Other steps/Run a menu item", Line("Open the Console", DocActions.MenuPrefix + "Window/General/Console")));
            return items;
        }

        /// <summary>A button line. A link with spaces in it goes in angle brackets, as Markdown needs.</summary>
        private static string Line(string label, string id, string scheme = DocActions.Scheme)
        {
            var url = scheme + id;
            return "\n[" + label + "](" + (url.Contains(" ") ? "<" + url + ">" : url) + ")\n";
        }

        private static string SelectedPath()
        {
            var path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : string.Empty;
            return string.IsNullOrEmpty(path) ? "Assets/Path/To/Asset" : path;
        }

        private static string SelectedName()
        {
            return Selection.activeObject != null ? Selection.activeObject.name : "the asset";
        }
    }
}
