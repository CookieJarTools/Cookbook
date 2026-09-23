namespace CookieJarTools.Cookbook.Editor.Reference.Scanning
{
    /// <summary>One open pair of braces while a script is read: a namespace's name, or a type's key and name.</summary>
    public sealed class ScanScope
    {
        public ScanScope(ScopeKind kind, string key, string name)
        {
            Kind = kind;
            Key = key;
            Name = name;
        }

        public ScopeKind Kind { get; }

        /// <summary>A namespace's dotted name, or a type's key: <c>"Ns.Outer`1.Inner"</c>.</summary>
        public string Key { get; }

        /// <summary>A type's own name, without its generic arity: how its constructors are known.</summary>
        public string Name { get; }
    }
}
