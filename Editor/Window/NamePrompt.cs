using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>A small box under the mouse asking for a name: Enter to accept, Escape or clicking away to leave it.</summary>
    public sealed class NamePrompt : EditorWindow
    {
        private static readonly Vector2 Size = new Vector2(260f, 58f);

        private string question;
        private string initial;
        private Action<string> named;

        public static void Show(Vector2 screenPoint, string question, string initial, Action<string> named)
        {
            var prompt = CreateInstance<NamePrompt>();
            prompt.question = question;
            prompt.initial = initial;
            prompt.named = named;
            prompt.ShowAsDropDown(new Rect(screenPoint, Vector2.zero), Size);
        }

        /// <summary>Just under an element of a window.</summary>
        public static void ShowAt(EditorWindow owner, VisualElement near, string question, string initial, Action<string> named)
        {
            Show(owner.position.position + near.worldBound.position + new Vector2(0f, near.worldBound.height), question, initial, named);
        }

        private void CreateGUI()
        {
            CookbookStyle.Apply(rootVisualElement);
            rootVisualElement.AddToClassList("cookbook-prompt");
            rootVisualElement.Add(new Label(question) { name = "question" });

            var field = new TextField { value = initial };
            field.RegisterCallback<KeyDownEvent>(evt => Key(evt, field.value), TrickleDown.TrickleDown);
            rootVisualElement.Add(field);
            field.schedule.Execute(() =>
            {
                field.Focus();
                field.SelectAll();
            });
        }

        private void Key(KeyDownEvent evt, string value)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                return;
            }

            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            var answer = value.Trim();
            var done = named;
            Close();
            if (answer.Length > 0)
            {
                done?.Invoke(answer);
            }
        }
    }
}
