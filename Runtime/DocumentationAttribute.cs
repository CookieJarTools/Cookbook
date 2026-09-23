using System;
using UnityEngine;

namespace CookieJarTools.Cookbook
{
    /// <summary>
    /// Links a class, field, property or method to a Cookbook page. The inspector shows a book
    /// button beside it that opens the page.
    /// <para>
    /// The page is named by its id (from the header at the top of the page file) or by its path in
    /// the Cookbook, folders and all: <c>"Combat/Damage"</c>. An id keeps working when the page is
    /// renamed or moved; a path is easier to write.
    /// </para>
    /// <para>
    /// Right-click a page in the Cookbook's tree and choose Copy [Documentation] Attribute to get
    /// one ready to paste.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true)]
    public sealed class DocumentationAttribute : PropertyAttribute
    {
        public DocumentationAttribute(string page)
        {
            Page = page;
        }

        public string Page { get; }
    }
}
