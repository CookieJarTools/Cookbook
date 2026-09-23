using System;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Actions
{
    /// <summary>
    /// Which guide steps a person has done, kept on their machine for this project — through
    /// restarts, not shared with the team. A step is known by its page and its link.
    /// </summary>
    public static class StepMemory
    {
        private const string Prefix = "CookieJar.Cookbook.Step.";

        private static string project;

        /// <summary>A step was done, or progress reset: anything showing progress should look again.</summary>
        public static event Action Changed;

        public static bool IsDone(string page, string step)
        {
            return EditorPrefs.GetBool(Key(page, step), false);
        }

        public static void Set(string page, string step, bool done)
        {
            if (IsDone(page, step) == done)
            {
                return;
            }

            if (done)
            {
                EditorPrefs.SetBool(Key(page, step), true);
            }
            else
            {
                EditorPrefs.DeleteKey(Key(page, step));
            }

            Changed?.Invoke();
        }

        private static string Key(string page, string step)
        {
            return Prefix + Project() + "." + page + "." + step;
        }

        /// <summary>This project, as a short stable name: a hash of where it is.</summary>
        private static string Project()
        {
            if (project != null)
            {
                return project;
            }

            var hash = 2166136261u;
            foreach (var character in Application.dataPath)
            {
                hash = unchecked((hash ^ character) * 16777619u);
            }

            return project = hash.ToString("x8");
        }
    }
}
