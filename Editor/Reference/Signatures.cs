using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>How a type or member is declared, written out as C#: <c>public static void Move(Vector3 to, float speed = 1f)</c>.</summary>
    public static class Signatures
    {
        public static string Of(Type type)
        {
            var words = Access(type) + Modifiers(type) + Keyword(type) + " " + TypeNames.Short(type);
            if (IsDelegate(type))
            {
                var invoke = type.GetMethod("Invoke");
                return Access(type) + "delegate " + TypeNames.Of(invoke.ReturnType) + " " + TypeNames.Short(type) + Parameters(invoke);
            }

            var bases = Bases(type);
            return bases.Length == 0 ? words : words + " : " + bases;
        }

        public static string Of(MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo field:
                    return Field(field);
                case PropertyInfo property:
                    return Property(property);
                case EventInfo evt:
                    return Access(evt.AddMethod) + Static(evt.AddMethod) + "event " + TypeNames.Of(evt.EventHandlerType) + " " + evt.Name;
                case ConstructorInfo constructor:
                    return Access(constructor) + TypeNames.Stem(constructor.DeclaringType.Name) + Parameters(constructor);
                case MethodInfo method:
                    return Access(method) + MethodModifiers(method) + TypeNames.Of(method.ReturnType) + " " + method.Name + GenericArguments(method) + Parameters(method);
                default:
                    return member.Name;
            }
        }

        public static bool IsDelegate(Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }

        public static bool IsStaticClass(Type type)
        {
            return type.IsClass && type.IsAbstract && type.IsSealed;
        }

        private static string Keyword(Type type)
        {
            return type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct" : "class";
        }

        private static string Modifiers(Type type)
        {
            if (!type.IsClass || type.IsInterface)
            {
                return string.Empty;
            }

            return IsStaticClass(type) ? "static " : type.IsAbstract ? "abstract " : type.IsSealed ? "sealed " : string.Empty;
        }

        private static string Bases(Type type)
        {
            if (type.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(type);
                return underlying == typeof(int) ? string.Empty : TypeNames.Of(underlying);
            }

            var parent = type.BaseType == null || type.BaseType == typeof(object) || type.BaseType == typeof(ValueType) ? null : type.BaseType;
            var inherited = parent?.GetInterfaces() ?? Array.Empty<Type>();
            var own = type.GetInterfaces().Where(i => !inherited.Contains(i) && i.IsVisible).Select(TypeNames.Of);
            return string.Join(", ", (parent == null ? own : own.Prepend(TypeNames.Of(parent))).ToArray());
        }

        private static string Field(FieldInfo field)
        {
            var serialized = !field.IsPublic && field.IsDefined(typeof(SerializeField), false) ? "[SerializeField] " : string.Empty;
            var reference = field.IsDefined(typeof(SerializeReference), false) ? "[SerializeReference] " : string.Empty;
            var access = Access(field.IsPublic, field.IsFamily, field.IsAssembly, field.IsFamilyOrAssembly);
            if (field.IsLiteral)
            {
                return access + "const " + TypeNames.Of(field.FieldType) + " " + field.Name + " = " + Literal(field.GetRawConstantValue(), field.FieldType);
            }

            var modifiers = (field.IsStatic ? "static " : string.Empty) + (field.IsInitOnly ? "readonly " : string.Empty);
            return serialized + reference + access + modifiers + TypeNames.Of(field.FieldType) + " " + field.Name;
        }

        private static string Property(PropertyInfo property)
        {
            var getter = property.GetMethod;
            var setter = property.SetMethod;
            var widest = getter != null && getter.IsPublic ? getter : setter ?? getter;
            var accessors = Accessor("get", getter, widest) + Accessor(IsInitOnly(setter) ? "init" : "set", setter, widest);
            return Access(widest) + MethodModifiers(widest) + TypeNames.Of(property.PropertyType) + " " + property.Name + " {" + accessors + " }";
        }

        private static string Accessor(string word, MethodInfo accessor, MethodInfo widest)
        {
            if (accessor == null)
            {
                return string.Empty;
            }

            var access = accessor == widest || Access(accessor) == Access(widest) ? string.Empty : Access(accessor);
            return " " + access + word + ";";
        }

        private static bool IsInitOnly(MethodInfo setter)
        {
            return setter != null && setter.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit");
        }

        private static string Parameters(MethodBase method)
        {
            var extension = method.IsDefined(typeof(ExtensionAttribute), false);
            var parameters = method.GetParameters().Select((p, i) => (i == 0 && extension ? "this " : string.Empty) + Parameter(p));
            return "(" + string.Join(", ", parameters) + ")";
        }

        private static string Parameter(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;
            var modifier = !type.IsByRef ? string.Empty : parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";
            var many = parameter.IsDefined(typeof(ParamArrayAttribute), false) ? "params " : string.Empty;
            var value = parameter.HasDefaultValue ? " = " + Literal(parameter.DefaultValue, type) : string.Empty;
            return many + modifier + TypeNames.Of(type) + " " + parameter.Name + value;
        }

        private static string GenericArguments(MethodInfo method)
        {
            return method.IsGenericMethodDefinition ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">" : string.Empty;
        }

        /// <summary>A default or constant value as it would be written: <c>1f</c>, <c>"text"</c>, <c>null</c>, <c>Mode.Fast</c>.</summary>
        private static string Literal(object value, Type type)
        {
            if (value == null || value == DBNull.Value)
            {
                return type.IsValueType && Nullable.GetUnderlyingType(type) == null ? "default" : "null";
            }

            if (type.IsEnum || (type.IsByRef && type.GetElementType().IsEnum))
            {
                var enumType = type.IsByRef ? type.GetElementType() : type;
                var name = Enum.GetName(enumType, value);
                return name == null ? "(" + enumType.Name + ")" + value : enumType.Name + "." + name;
            }

            switch (value)
            {
                case string text:
                    return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                case bool flag:
                    return flag ? "true" : "false";
                case float single:
                    return single.ToString(CultureInfo.InvariantCulture) + "f";
                case char character:
                    return "'" + character + "'";
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static string Access(Type type)
        {
            return type.IsPublic || type.IsNestedPublic ? "public " : type.IsNestedFamily ? "protected " : "internal ";
        }

        private static string Access(MethodBase method)
        {
            return method == null ? string.Empty : Access(method.IsPublic, method.IsFamily, method.IsAssembly, method.IsFamilyOrAssembly);
        }

        private static string Access(bool isPublic, bool isFamily, bool isAssembly, bool isEither)
        {
            return isPublic ? "public " : isFamily ? "protected " : isEither ? "protected internal " : isAssembly ? "internal " : "private ";
        }

        private static string Static(MethodBase method)
        {
            return method.IsStatic ? "static " : string.Empty;
        }

        private static string MethodModifiers(MethodInfo method)
        {
            if (method.IsStatic)
            {
                return "static ";
            }

            if (method.IsAbstract && !method.DeclaringType.IsInterface)
            {
                return "abstract ";
            }

            var overrides = method.GetBaseDefinition().DeclaringType != method.DeclaringType;
            return overrides ? "override " : method.IsVirtual && !method.IsFinal && !method.DeclaringType.IsInterface ? "virtual " : string.Empty;
        }
    }
}
