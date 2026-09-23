using System;
using System.Linq;
using System.Reflection;

namespace CookieJarTools.Cookbook.Editor.Reference
{
    /// <summary>
    /// The keys comments are kept by, made from reflection the same way the scanner makes them
    /// from source: <c>"Ns.Outer`1.Inner"</c> for a type, <c>"Ns.Type.speed"</c> for a field,
    /// property or event, <c>"Ns.Type.Move(target,speed)"</c> for a method and
    /// <c>"Ns.Type.#ctor(value)"</c> for a constructor.
    /// </summary>
    public static class CodeKeys
    {
        public const string Constructor = "#ctor";

        public static string Of(Type type)
        {
            return (type.FullName ?? type.Name).Replace('+', '.');
        }

        public static string Of(MemberInfo member)
        {
            var type = Of(member.DeclaringType);
            switch (member)
            {
                case ConstructorInfo constructor:
                    return type + "." + Constructor + Parameters(constructor);
                case MethodInfo method:
                    return type + "." + method.Name + Parameters(method);
                default:
                    return type + "." + member.Name;
            }
        }

        private static string Parameters(MethodBase method)
        {
            return "(" + string.Join(",", method.GetParameters().Select(p => p.Name)) + ")";
        }
    }
}
