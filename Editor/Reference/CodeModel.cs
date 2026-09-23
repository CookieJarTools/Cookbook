using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CookieJarTools.Cookbook.Editor.Pages;
using CookieJarTools.Cookbook.Editor.Reference.Scanning;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// Everything the Code Reference shows: each source switched on, the public types in its
    /// scripts, and the <c>///</c> comments found in them.
    /// <para>
    /// Worked out when first wanted and kept until the sources change. A script reload starts it
    /// afresh — which every change to the code brings — so it is never behind the code.
    /// </para>
    /// </summary>
    public sealed class CodeModel
    {
        private static CodeModel current;

        private readonly Dictionary<string, string> comments = new Dictionary<string, string>();
        private readonly Dictionary<string, DocComment> parsed = new Dictionary<string, DocComment>();
        private readonly Dictionary<string, CodeType> byKey = new Dictionary<string, CodeType>();
        private readonly Dictionary<string, List<CodeType>> byName = new Dictionary<string, List<CodeType>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, Type>> assemblyTypes = new Dictionary<string, Dictionary<string, Type>>();
        private readonly Dictionary<string, Assembly> loaded;
        private readonly Dictionary<string, (string note, string markdown)> written = new Dictionary<string, (string, string)>();

        private ILookup<Type, CodeType> derived;

        private CodeModel()
        {
            loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).GroupBy(a => a.GetName().Name).ToDictionary(g => g.Key, g => g.First());
            foreach (var entry in CookbookSettings.instance.CodeSources.Where(e => e.Enabled))
            {
                Sources.Add((entry.Path, Read(entry.Path)));
            }
        }

        public static CodeModel Current => current ??= new CodeModel();

        /// <summary>Each source switched on, with its types in the order its scripts were read.</summary>
        public List<(string source, List<CodeType> types)> Sources { get; } = new List<(string, List<CodeType>)>();

        public IEnumerable<CodeType> Types => byKey.Values;

        /// <summary>Work it out again when next wanted: the sources have changed.</summary>
        public static void Forget()
        {
            current = null;
        }

        public CodeType Find(string key)
        {
            return key != null && byKey.TryGetValue(key, out var type) ? type : null;
        }

        /// <summary>A type by its name alone — <c>DocLibrary</c> — when only one type in the reference has it.</summary>
        public CodeType FindByName(string name)
        {
            return byName.TryGetValue(name, out var named) && named.Count == 1 ? named[0] : null;
        }

        /// <summary>The comment on what a key names, read when first asked for; null when it has none.</summary>
        public DocComment Comment(string key)
        {
            if (parsed.TryGetValue(key, out var comment))
            {
                return comment;
            }

            parsed[key] = comment = comments.TryGetValue(key, out var xml) ? DocComment.Parse(xml) : null;
            return comment;
        }

        /// <summary>The types in the reference that derive from this one or implement it, directly.</summary>
        public IEnumerable<CodeType> Derived(CodeType type)
        {
            derived ??= byKey.Values
                .SelectMany(t => t.Type.GetInterfaces().Select(Definition).Prepend(Definition(t.Type.BaseType)).Where(b => b != null).Distinct().Select(b => (parent: b, child: t)))
                .ToLookup(p => p.parent, p => p.child);
            return derived[type.Type].Where(t => t != type);
        }

        /// <summary>A type's page as Markdown, with its note: written once, and again only when the note changes.</summary>
        public string Page(CodeType type, string note)
        {
            if (written.TryGetValue(type.Key, out var page) && page.note == note)
            {
                return page.markdown;
            }

            var markdown = TypeMarkdown.Write(this, type, note);
            written[type.Key] = (note, markdown);
            return markdown;
        }

        private static Type Definition(Type type)
        {
            return type != null && type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }

        private List<CodeType> Read(string source)
        {
            var types = new List<CodeType>();
            foreach (var script in CodeScope.Scripts(source).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                var scanned = Scan(script);
                types.AddRange(scanned.Types.Select(key => Add(key, script, source)).Where(t => t != null));
            }

            return types;
        }

        private ScannedSource Scan(string script)
        {
            var scanned = SourceScanner.Scan(File.Exists(script) ? File.ReadAllText(script) : string.Empty);
            foreach (var comment in scanned.Comments.Where(c => !comments.ContainsKey(c.Key)))
            {
                comments[comment.Key] = comment.Value;
            }

            return scanned;
        }

        /// <summary>A type declared in a script, if it is public and not already in — a partial type is written in more than one.</summary>
        private CodeType Add(string key, string script, string source)
        {
            var type = TypeFor(key, CodeScope.AssemblyOf(script));
            if (type == null || !type.IsVisible || type.IsDefined(typeof(CompilerGeneratedAttribute), false) || byKey.ContainsKey(key))
            {
                return null;
            }

            var code = new CodeType(this, type, script, source);
            byKey[key] = code;
            var name = TypeNames.Stem(type.Name);
            if (!byName.TryGetValue(name, out var named))
            {
                byName[name] = named = new List<CodeType>();
            }

            named.Add(code);
            return code;
        }

        private Type TypeFor(string key, string assemblyName)
        {
            if (assemblyName == null || !loaded.TryGetValue(assemblyName, out var assembly))
            {
                return null;
            }

            if (!assemblyTypes.TryGetValue(assemblyName, out var types))
            {
                assemblyTypes[assemblyName] = types = Loadable(assembly).GroupBy(CodeKeys.Of).ToDictionary(g => g.Key, g => g.First());
            }

            return types.TryGetValue(key, out var type) ? type : null;
        }

        private static IEnumerable<Type> Loadable(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException partly)
            {
                return partly.Types.Where(t => t != null);
            }
        }
    }
}
