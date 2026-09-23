using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Reference.Scanning
{
    /// <summary>What the words before a <c>{</c>, <c>;</c> or <c>=&gt;</c> in a script declare.</summary>
    public sealed class Declaration
    {
        public static readonly Declaration None = new Declaration(DeclarationKind.None, null);

        public Declaration(DeclarationKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public DeclarationKind Kind { get; }

        public string Name { get; }

        /// <summary>How many generic parameters a type has.</summary>
        public int Arity { get; set; }

        /// <summary>A method's parameter names, in order.</summary>
        public List<string> Parameters { get; } = new List<string>();

        /// <summary>Every field declared at once: <c>int a, b;</c> is two.</summary>
        public List<string> Names { get; } = new List<string>();

        /// <summary>A field given a value where it is declared: what follows is not a declaration.</summary>
        public bool HasInitializer { get; set; }
    }
}
