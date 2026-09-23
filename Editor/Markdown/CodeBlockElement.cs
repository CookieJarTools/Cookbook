using CookieJarTools.Cookbook.Editor.Markdown.Code;
using Markdig.Syntax;
using UnityEditor;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>
    /// A block of code: its language, a copy button, and the code coloured by its language, in a
    /// monospaced font that scrolls sideways rather than wrapping.
    /// <para>
    /// A <c>```signature</c> block — how the Code Reference shows a declaration — is C# drawn
    /// quietly instead: no bar, and wrapped, since a page may hold a great many.
    /// </para>
    /// </summary>
    public sealed class CodeBlockElement : VisualElement
    {
        public const string SignatureLanguage = "signature";

        private const string Prefix = "cookbook-code";
        private const string SignatureCode = "csharp";

        public CodeBlockElement(CodeBlock block)
        {
            AddToClassList(Prefix);
            var code = block.Lines.ToString().TrimEnd('\n', '\r');
            var language = (block as FencedCodeBlock)?.Info;
            if (language == SignatureLanguage)
            {
                AddToClassList(Prefix + "--signature");
                Add(Lines(code, SignatureCode));
                return;
            }

            var bar = BlockBuilder.Classed(new VisualElement(), Prefix + "__bar");
            bar.Add(BlockBuilder.Classed(new Label(string.IsNullOrEmpty(language) ? "code" : language), Prefix + "__language"));
            var copy = BlockBuilder.Classed(new Button(() => EditorGUIUtility.systemCopyBuffer = code) { text = "Copy", tooltip = "Copy the code" }, Prefix + "__copy");
            bar.Add(copy);
            Add(bar);

            var scroll = BlockBuilder.Classed(new ScrollView(ScrollViewMode.Horizontal), Prefix + "__scroll");
            scroll.Add(Lines(code, language));
            Add(scroll);
        }

        private static VisualElement Lines(string code, string language)
        {
            var lines = BlockBuilder.Classed(new VisualElement(), Prefix + "__lines");
            foreach (var part in CodeHighlighter.Highlight(code, language))
            {
                var text = BlockBuilder.Classed(new Label(part) { enableRichText = true }, Prefix + "__text");
                text.selection.isSelectable = true;
                lines.Add(text);
            }

            return lines;
        }
    }
}
