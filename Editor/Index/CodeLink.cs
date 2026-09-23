using System;

namespace CookieJarTools.Cookbook.Editor.Index
{
    /// <summary>One [Documentation] in the code: which page it names, on what, and the script it is written in.</summary>
    public sealed class CodeLink
    {
        public CodeLink(string page, Type type, string member)
        {
            Page = page;
            Type = type;
            Member = member;
        }

        /// <summary>The page as the attribute names it: an id or a path.</summary>
        public string Page { get; }

        /// <summary>The class, or the class the field or method is in.</summary>
        public Type Type { get; }

        /// <summary>The field or method, or null when the attribute is on the class itself.</summary>
        public string Member { get; }

        /// <summary>"Enemy" or "Enemy.damage".</summary>
        public string Name => Member == null ? Type.Name : Type.Name + "." + Member;

        /// <summary>The script file, relative to the project; null when it cannot be found.</summary>
        public string Script => DocCodeIndex.ScriptOf(Type);
    }
}
