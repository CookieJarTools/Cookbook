using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Markdown.Code
{
    /// <summary>
    /// The languages the highlighter knows, by the name written after the opening fence:
    /// <c>```csharp</c>. Anything else is coloured plainly — strings, numbers and comments only.
    /// </summary>
    public static class CodeLanguages
    {
        private const string CSharpKeywords = "abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while var async await get set value yield when where record init nameof dynamic partial global";
        private const string ShaderKeywords = "float float2 float3 float4 float2x2 float3x3 float4x4 half half2 half3 half4 fixed fixed2 fixed3 fixed4 int int2 int3 int4 uint uint2 uint3 uint4 bool void struct return if else for while do break continue discard cbuffer uniform in out inout static const sampler sampler2D samplerCUBE Texture2D Texture3D TextureCube SamplerState true false Shader Properties SubShader Pass Tags Cull ZWrite ZTest Blend LOD CGPROGRAM ENDCG HLSLPROGRAM ENDHLSL Fallback";
        private const string ScriptKeywords = "break case catch class const continue debugger default delete do else export extends false finally for function if import in instanceof let new null return super switch this throw true try typeof var void while with yield async await of interface type enum implements readonly public private protected static undefined as from";
        private const string PythonKeywords = "False None True and as assert async await break class continue def del elif else except finally for from global if import in is lambda nonlocal not or pass raise return try while with yield self";
        private const string JsonKeywords = "true false null";
        private const string ShellKeywords = "if then else elif fi for while do done case esac function in return export local echo cd exit set";

        private static Dictionary<string, CodeLanguage> byName;

        /// <summary>Null when the name says there is no language: plain text.</summary>
        public static CodeLanguage For(string name)
        {
            var key = (name ?? string.Empty).Trim().ToLowerInvariant();
            if (key == "text" || key == "txt" || key == "plain")
            {
                return null;
            }

            return Names().TryGetValue(key, out var language) ? language : Generic;
        }

        private static CodeLanguage Generic { get; } = new CodeLanguage(string.Empty, "//", true) { TypesByCase = false };

        private static Dictionary<string, CodeLanguage> Names()
        {
            if (byName != null)
            {
                return byName;
            }

            var csharp = new CodeLanguage(CSharpKeywords, "//", true) { Attributes = true, Directives = true, PrefixedStrings = true };
            var shader = new CodeLanguage(ShaderKeywords, "//", true) { Directives = true, TypesByCase = false };
            var script = new CodeLanguage(ScriptKeywords, "//", true);
            var python = new CodeLanguage(PythonKeywords, "#", false);
            var json = new CodeLanguage(JsonKeywords, null, false) { Keys = true, TypesByCase = false };
            var shell = new CodeLanguage(ShellKeywords, "#", false) { TypesByCase = false };

            byName = new Dictionary<string, CodeLanguage>();
            Name(csharp, "cs", "csharp", "c#", "c", "cpp", "c++", "java");
            Name(shader, "hlsl", "shader", "shaderlab", "glsl", "cg");
            Name(script, "js", "javascript", "ts", "typescript", "jsx", "tsx");
            Name(python, "py", "python");
            Name(json, "json", "jsonc");
            Name(shell, "sh", "bash", "shell", "zsh", "powershell", "ps1", "cmd", "bat");
            return byName;
        }

        private static void Name(CodeLanguage language, params string[] names)
        {
            foreach (var name in names)
            {
                byName[name] = language;
            }
        }
    }
}
