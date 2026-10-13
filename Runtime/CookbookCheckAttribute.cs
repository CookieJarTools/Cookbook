using System;

namespace CookieJarTools.Cookbook
{
    /// <summary>
    /// Makes a static method a check a CookBook guide can show: a step that looks at the project and
    /// says whether it is done — "the scene has a main camera", "the addressables are built".
    /// <para>
    /// The method takes nothing and returns <c>bool</c> (true when it passes) or <c>string</c>:
    /// null or empty when it passes, otherwise what is still wrong, in words for the reader.
    /// </para>
    /// <para>
    /// A page shows it with a link on its own line: <c>[Scene is lit](check:scene-lit)</c>. The
    /// CookBook's editor has an Action… button that writes that for you.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class CookbookCheckAttribute : Attribute
    {
        public CookbookCheckAttribute(string name)
        {
            Name = name;
        }

        /// <summary>What the step says unless the page says otherwise.</summary>
        public string Name { get; }

        /// <summary>How pages name it. Defaults to the class and method name: <c>SceneChecks.HasCamera</c>.</summary>
        public string Id { get; set; }

        /// <summary>A line under the step saying what it looks for.</summary>
        public string Description { get; set; }
    }
}
