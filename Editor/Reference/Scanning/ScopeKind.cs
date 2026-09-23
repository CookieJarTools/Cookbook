namespace CookieJarTools.Cookbook.Editor.Reference.Scanning
{
    /// <summary>What a pair of braces in a script holds, as far as reading its comments goes.</summary>
    public enum ScopeKind
    {
        Namespace,
        Type,
        Enum,

        /// <summary>A method's body, a property's accessors, an initializer: nothing inside is read.</summary>
        Other
    }
}
