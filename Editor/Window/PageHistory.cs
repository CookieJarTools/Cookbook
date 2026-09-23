using System;
using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>Back and forward through the pages visited, as a browser does. Pages are kept by id or reference, so it survives a reload.</summary>
    [Serializable]
    public sealed class PageHistory
    {
        private const int Limit = 50;

        public List<string> back = new List<string>();
        public List<string> forward = new List<string>();

        public bool CanGoBack => back.Count > 0;

        public bool CanGoForward => forward.Count > 0;

        /// <summary>Leaving a page for another: it goes on the back list, and forward is forgotten.</summary>
        public void Left(string page)
        {
            if (string.IsNullOrEmpty(page) || (back.Count > 0 && back[back.Count - 1] == page))
            {
                return;
            }

            back.Add(page);
            forward.Clear();
            if (back.Count > Limit)
            {
                back.RemoveAt(0);
            }
        }

        public string Back(string current)
        {
            return Step(back, forward, current);
        }

        public string Forward(string current)
        {
            return Step(forward, back, current);
        }

        private static string Step(List<string> from, List<string> to, string current)
        {
            if (from.Count == 0)
            {
                return null;
            }

            var page = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            if (!string.IsNullOrEmpty(current))
            {
                to.Add(current);
            }

            return page;
        }
    }
}
