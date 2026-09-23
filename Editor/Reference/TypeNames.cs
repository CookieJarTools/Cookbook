using System;
using System.Collections.Generic;
using System.Linq;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>Types named as they are written in C#: <c>int</c>, <c>List&lt;Vector3&gt;</c>, <c>float?</c>, <c>Outer.Inner</c>.</summary>
    public static class TypeNames
    {
        private static readonly Dictionary<Type, string> Keywords = new Dictionary<Type, string>
        {
            { typeof(void), "void" }, { typeof(bool), "bool" }, { typeof(byte), "byte" }, { typeof(sbyte), "sbyte" },
            { typeof(char), "char" }, { typeof(short), "short" }, { typeof(ushort), "ushort" }, { typeof(int), "int" },
            { typeof(uint), "uint" }, { typeof(long), "long" }, { typeof(ulong), "ulong" }, { typeof(float), "float" },
            { typeof(double), "double" }, { typeof(decimal), "decimal" }, { typeof(string), "string" }, { typeof(object), "object" },
        };

        public static string Of(Type type)
        {
            if (type.IsByRef)
            {
                return Of(type.GetElementType());
            }

            if (type.IsArray)
            {
                return Of(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (Keywords.TryGetValue(type, out var keyword))
            {
                return keyword;
            }

            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
            {
                return Of(nullable) + "?";
            }

            return type.IsGenericParameter ? type.Name : Named(type);
        }

        /// <summary>Its own name, generic parameters written out, without the types it is inside: <c>Pool&lt;T&gt;</c>.</summary>
        public static string Short(Type type)
        {
            var name = Stem(type.Name);
            var own = OwnArguments(type);
            return own.Count == 0 ? name : name + "<" + string.Join(", ", own.Select(Of)) + ">";
        }

        public static string Stem(string name)
        {
            var tick = name.IndexOf('`');
            return tick < 0 ? name : name.Substring(0, tick);
        }

        /// <summary>With the types it is nested in, and its generic arguments.</summary>
        private static string Named(Type type)
        {
            var outer = type.IsNested && !type.IsGenericParameter ? Named(type.DeclaringType) + "." : string.Empty;
            return outer + Short(type);
        }

        /// <summary>
        /// The generic arguments that are its own: a type nested in a generic type is given its
        /// outer type's arguments too, first.
        /// </summary>
        private static List<Type> OwnArguments(Type type)
        {
            if (!type.IsGenericType)
            {
                return new List<Type>();
            }

            var all = type.GetGenericArguments();
            var inherited = type.IsNested && type.DeclaringType.IsGenericType ? type.DeclaringType.GetGenericArguments().Length : 0;
            return all.Skip(inherited).ToList();
        }
    }
}
