using System;

namespace CookieJarTools.Cookbook.Editor.Window
{
    /// <summary>A page being written and not yet saved, kept by the window so a script reload does not lose it.</summary>
    [Serializable]
    public sealed class PageDraft
    {
        public string page;
        public string title;
        public string tags;
        public string banner;
        public string body;
    }
}
