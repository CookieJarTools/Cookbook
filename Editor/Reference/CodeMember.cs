using System.Reflection;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>A field, property, method, constructor, event or enum value on a type's page.</summary>
    public sealed class CodeMember
    {
        public CodeMember(MemberInfo info, string group, DocComment comment)
        {
            Info = info;
            Group = group;
            Comment = comment;
        }

        public MemberInfo Info { get; }

        /// <summary>The heading it is listed under: "Fields", "Methods" and so on.</summary>
        public string Group { get; }

        /// <summary>Its comment — or its tooltip, or its base member's comment — or null when it has none.</summary>
        public DocComment Comment { get; }

        public string Name => Info is ConstructorInfo ? TypeNames.Stem(Info.DeclaringType.Name) : Info.Name;

        public bool IsDocumented => Comment != null && Comment.HasWords;
    }
}
