using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// Where a <c>cref</c>, or a type in a page's facts, links to: its page in the Code
    /// Reference (<c>[[code:Ns.Type#member]]</c>), Unity's scripting reference for Unity's own types,
    /// Microsoft's for .NET's — or, with nowhere to go, just its name as code.
    /// </summary>
    public static class CodeLinks
    {
        public const string Scheme = "code:";

        private const string UnityDocs = "https://docs.unity3d.com/ScriptReference/";
        private const string DotNetDocs = "https://learn.microsoft.com/dotnet/api/";

        private static readonly Regex Generic = new Regex(@"\{([^{}]*)\}");
        private static Dictionary<string, Type> outsideTypes;

        /// <summary>The page id of a type: <c>"code:Ns.Type"</c>.</summary>
        public static string Id(string key)
        {
            return Scheme + key;
        }

        /// <summary>Markdown for a <c>cref</c> written on <paramref name="from"/>, showing <paramref name="words"/> or, if none, its name.</summary>
        public static string Cref(CodeModel model, CodeType from, string cref, string words)
        {
            var target = Clean(cref);
            var type = Resolve(model, from, target);
            if (type != null)
            {
                return Page(type, null, words.Length > 0 ? words : type.Title);
            }

            var dot = target.LastIndexOf('.');
            var owner = dot < 0 ? from : Resolve(model, from, target.Substring(0, dot));
            var member = dot < 0 ? target : target.Substring(dot + 1);
            if (owner != null && owner.Members.Any(m => m.Name == member))
            {
                var label = words.Length > 0 ? words : owner == from ? member : owner.Title + "." + member;
                return Page(owner, member.ToLowerInvariant(), label);
            }

            return Outside(target, words.Length > 0 ? words : Label(target));
        }

        /// <summary>Markdown for a type named in a page's facts or a signature: its page, its reference online, or its name.</summary>
        public static string ForType(CodeModel model, Type type)
        {
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            var code = model.Find(CodeKeys.Of(definition));
            if (code != null)
            {
                return Page(code, null, TypeNames.Of(type));
            }

            var url = Url(definition);
            return url == null ? XmlMarkdown.Code(TypeNames.Of(type)) : "[" + XmlMarkdown.Escape(TypeNames.Of(type)) + "](" + url + ")";
        }

        public static string Page(CodeType type, string anchor, string label)
        {
            return "[[" + Id(type.Key) + (anchor == null ? string.Empty : "#" + anchor) + "|" + label.Replace("]]", "] ]") + "]]";
        }

        /// <summary>Unity's or Microsoft's page about a type, or null for anyone else's.</summary>
        public static string Url(Type type)
        {
            var space = type.Namespace ?? string.Empty;
            if (space.StartsWith("UnityEngine") || space.StartsWith("UnityEditor"))
            {
                var nested = type.IsNested ? TypeNames.Stem(type.DeclaringType.Name) + "." : string.Empty;
                var prefix = space.Contains("UIElements") ? space + "." : string.Empty;
                return UnityDocs + prefix + nested + TypeNames.Stem(type.Name) + ".html";
            }

            if (space == "System" || space.StartsWith("System."))
            {
                return DotNetDocs + (type.FullName ?? type.Name).Replace('+', '.').Replace('`', '-').ToLowerInvariant();
            }

            return null;
        }

        /// <summary>A cref as a key: <c>"T:Ns.Pool{T}"</c> is <c>"Ns.Pool`1"</c>; a method's parameter list is dropped.</summary>
        private static string Clean(string cref)
        {
            var target = cref.Length > 2 && cref[1] == ':' ? cref.Substring(2) : cref;
            var paren = target.IndexOf('(');
            target = paren < 0 ? target : target.Substring(0, paren);
            return Generic.Replace(target, m => "`" + (m.Groups[1].Value.Count(c => c == ',') + 1)).Trim();
        }

        private static CodeType Resolve(CodeModel model, CodeType from, string target)
        {
            var near = from == null || from.Namespace.Length == 0 ? null : model.Find(from.Namespace + "." + target);
            var nested = from == null ? null : model.Find(from.Key + "." + target);
            return model.Find(target) ?? nested ?? near ?? (target.Contains(".") ? null : model.FindByName(TypeNames.Stem(target)));
        }

        private static string Outside(string target, string label)
        {
            var type = OutsideType(target);
            var url = type == null ? null : Url(type);
            return url == null ? XmlMarkdown.Code(label) : "[" + XmlMarkdown.Escape(label) + "](" + url + ")";
        }

        /// <summary>A Unity or .NET type by its full name, or by its name alone when that is how the comment wrote it.</summary>
        private static Type OutsideType(string target)
        {
            if (outsideTypes == null)
            {
                outsideTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("UnityEngine") || a.GetName().Name.StartsWith("UnityEditor") || a.GetName().Name == "mscorlib" || a.GetName().Name == "netstandard");
                foreach (var type in assemblies.SelectMany(Exported).Where(t => t.FullName != null))
                {
                    outsideTypes[type.FullName.Replace('+', '.')] = type;
                    outsideTypes[type.Name] = outsideTypes.ContainsKey(type.Name) ? outsideTypes[type.Name] : type;
                }
            }

            return outsideTypes.TryGetValue(target, out var found) ? found : null;
        }

        private static IEnumerable<Type> Exported(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetExportedTypes();
            }
            catch (Exception)
            {
                return Enumerable.Empty<Type>();
            }
        }

        private static string Label(string target)
        {
            return TypeNames.Stem(target.Substring(target.LastIndexOf('.') + 1));
        }
    }
}
