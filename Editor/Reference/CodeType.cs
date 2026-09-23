using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// A type in the Code Reference: where it is written and which source chose it, what kind of
    /// thing it is, its comment, and the members its page lists — the public ones, and fields
    /// Unity serializes whatever their access.
    /// </summary>
    public sealed class CodeType
    {
        public const string Values = "Values";
        public const string Constructors = "Constructors";
        public const string Fields = "Fields";
        public const string Properties = "Properties";
        public const string Methods = "Methods";
        public const string Events = "Events";

        private const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private readonly CodeModel model;

        private List<CodeMember> members;

        public CodeType(CodeModel model, Type type, string script, string source)
        {
            this.model = model;
            Type = type;
            Script = script;
            Source = source;
            Key = CodeKeys.Of(type);
        }

        public Type Type { get; }

        /// <summary>As comments and links know it: <c>"Ns.Outer`1.Inner"</c>.</summary>
        public string Key { get; }

        /// <summary>The script it is written in, relative to the project.</summary>
        public string Script { get; }

        /// <summary>The asset path of the source that chose it.</summary>
        public string Source { get; }

        public DocComment Comment => model.Comment(Key);

        public string Title => TypeNames.Short(Type);

        /// <summary>The namespace, or empty.</summary>
        public string Namespace => Type.Namespace ?? string.Empty;

        /// <summary>What sort of thing it is, as someone setting up a scene would say it.</summary>
        public string Kind
        {
            get
            {
                if (Type.IsInterface || Type.IsEnum || Signatures.IsDelegate(Type))
                {
                    return Type.IsInterface ? "Interface" : Type.IsEnum ? "Enum" : "Delegate";
                }

                if (Type.IsValueType)
                {
                    return "Struct";
                }

                return UnityKind() ?? (Signatures.IsStaticClass(Type) ? "Static class" : Type.IsAbstract ? "Abstract class" : "Class");
            }
        }

        public IReadOnlyList<CodeMember> Members => members ??= FindMembers();

        /// <summary>How many of its members have nothing written about them.</summary>
        public int Undocumented => Members.Count(m => !m.IsDocumented) + (Comment != null && Comment.HasWords ? 0 : 1);

        private string UnityKind()
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(Type))
            {
                return "Component";
            }

            if (typeof(EditorWindow).IsAssignableFrom(Type) || typeof(UnityEditor.Editor).IsAssignableFrom(Type))
            {
                return typeof(EditorWindow).IsAssignableFrom(Type) ? "Editor window" : "Custom editor";
            }

            if (typeof(ScriptableObject).IsAssignableFrom(Type))
            {
                return "ScriptableObject";
            }

            return typeof(Attribute).IsAssignableFrom(Type) ? "Attribute" : typeof(PropertyDrawer).IsAssignableFrom(Type) ? "Property drawer" : null;
        }

        private List<CodeMember> FindMembers()
        {
            var found = new List<CodeMember>();
            if (Type.IsEnum)
            {
                found.AddRange(Type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => Member(f, Values)));
                return found;
            }

            found.AddRange(Type.GetConstructors(Declared).Where(ShowConstructor).Select(c => Member(c, Constructors)));
            found.AddRange(Type.GetFields(Declared).Where(ShowField).Select(f => Member(f, Fields)));
            found.AddRange(Type.GetProperties(Declared).Where(ShowProperty).Select(p => Member(p, Properties)));
            found.AddRange(Type.GetMethods(Declared).Where(ShowMethod).Select(m => Member(m, Methods)));
            found.AddRange(Type.GetEvents(Declared).Where(e => e.AddMethod != null && e.AddMethod.IsPublic).Select(e => Member(e, Events)));
            return found.OrderBy(m => Order(m.Group)).ThenBy(m => m.Info.MetadataToken).ToList();
        }

        private CodeMember Member(MemberInfo info, string group)
        {
            return new CodeMember(info, group, CommentFor(info));
        }

        /// <summary>Its own comment; failing that, a serialized field's tooltip, or the comment on what it overrides.</summary>
        private DocComment CommentFor(MemberInfo info)
        {
            var own = model.Comment(CodeKeys.Of(info));
            if (own != null && !own.Inherits)
            {
                return own;
            }

            var tooltip = info.GetCustomAttribute<TooltipAttribute>();
            if (tooltip != null)
            {
                return DocComment.FromText(tooltip.tooltip);
            }

            var method = info as MethodInfo ?? (info as PropertyInfo)?.GetMethod;
            var definition = method?.GetBaseDefinition();
            return definition == null || definition == method ? own : model.Comment(info is PropertyInfo ? CodeKeys.Of(definition.DeclaringType) + "." + info.Name : CodeKeys.Of(definition)) ?? own;
        }

        /// <summary>A constructor someone would call: not the one every component and asset has, nor an unwritten default.</summary>
        private bool ShowConstructor(ConstructorInfo constructor)
        {
            var made = typeof(UnityEngine.Object).IsAssignableFrom(Type);
            var unwritten = constructor.GetParameters().Length == 0 && model.Comment(CodeKeys.Of(constructor)) == null;
            return constructor.IsPublic && !constructor.IsStatic && !made && !unwritten && !Type.IsAbstract;
        }

        private static bool ShowField(FieldInfo field)
        {
            if (field.IsDefined(typeof(CompilerGeneratedAttribute), false) || field.Name.Contains("<"))
            {
                return false;
            }

            return field.IsPublic || field.IsDefined(typeof(SerializeField), false) || field.IsDefined(typeof(SerializeReference), false);
        }

        private static bool ShowProperty(PropertyInfo property)
        {
            var visible = (property.GetMethod != null && property.GetMethod.IsPublic) || (property.SetMethod != null && property.SetMethod.IsPublic);
            return visible && property.GetIndexParameters().Length == 0;
        }

        private static bool ShowMethod(MethodInfo method)
        {
            return method.IsPublic && !method.IsSpecialName && !method.IsDefined(typeof(CompilerGeneratedAttribute), false);
        }

        private static int Order(string group)
        {
            return Array.IndexOf(new[] { Values, Constructors, Fields, Properties, Methods, Events }, group);
        }
    }
}
