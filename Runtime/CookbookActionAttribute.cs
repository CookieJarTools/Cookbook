using System;

namespace CookieJarTools.Cookbook
{
    /// <summary>
    /// Makes a static method something a CookBook page can run with a button — a step in a guide
    /// that does the work for the reader: "make the enemy data", "set up the scene".
    /// <para>
    /// The method can take no parameters, or one <c>string path</c>: then the CookBook asks the
    /// reader where to save (starting at <see cref="DefaultFolder"/> with <see cref="DefaultName"/>)
    /// and passes the path chosen, relative to the project. Without it, the method decides where
    /// things go. Whatever it returns that is a Unity object is selected and shown afterwards.
    /// </para>
    /// <para>
    /// A page runs it with a link on its own line: <c>[Make the enemy data](action:enemy-data)</c>.
    /// The CookBook's editor has an Action… button that writes that for you.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class CookbookActionAttribute : Attribute
    {
        public CookbookActionAttribute(string name)
        {
            Name = name;
        }

        /// <summary>What the button says unless the page says otherwise.</summary>
        public string Name { get; }

        /// <summary>How pages name it. Defaults to the class and method name: <c>EnemyTools.MakeData</c>.</summary>
        public string Id { get; set; }

        /// <summary>A line under the button saying what it will do.</summary>
        public string Description { get; set; }

        /// <summary>The file name offered when the reader is asked where to save.</summary>
        public string DefaultName { get; set; }

        /// <summary>The folder the save dialog starts in, such as <c>Assets/Data</c>.</summary>
        public string DefaultFolder { get; set; }

        /// <summary>The file extension for the save dialog. Defaults to <c>asset</c>.</summary>
        public string Extension { get; set; } = "asset";
    }
}
