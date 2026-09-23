using System.Linq;
using Markdig.Extensions.Tables;
using UnityEngine.UIElements;

namespace CookieJarTools.Cookbook.Editor.Markdown
{
    /// <summary>A Markdown table: header row, then the rows, each column as wide as the others unless the table is too narrow for its words.</summary>
    public sealed class TableBuilder
    {
        private const string Prefix = "cookbook-table";

        private readonly BlockBuilder blocks;

        public TableBuilder(BlockBuilder blocks)
        {
            this.blocks = blocks;
        }

        public VisualElement Build(Table table)
        {
            var element = BlockBuilder.Classed(new VisualElement(), Prefix);
            var odd = false;
            foreach (var row in table.OfType<TableRow>())
            {
                element.Add(Row(table, row, odd));
                odd = !row.IsHeader && !odd;
            }

            return element;
        }

        private VisualElement Row(Table table, TableRow row, bool odd)
        {
            var element = BlockBuilder.Classed(new VisualElement(), Prefix + "__row");
            element.EnableInClassList(Prefix + "__row--header", row.IsHeader);
            element.EnableInClassList(Prefix + "__row--odd", odd);
            foreach (var cell in row.OfType<TableCell>())
            {
                element.Add(Cell(table, cell));
            }

            return element;
        }

        private VisualElement Cell(Table table, TableCell cell)
        {
            var element = BlockBuilder.Classed(new VisualElement(), Prefix + "__cell");
            var column = cell.ColumnIndex >= 0 && cell.ColumnIndex < table.ColumnDefinitions.Count ? table.ColumnDefinitions[cell.ColumnIndex] : null;
            var align = column?.Alignment;
            if (align == TableColumnAlign.Center || align == TableColumnAlign.Right)
            {
                element.AddToClassList(Prefix + "__cell--" + align.ToString().ToLowerInvariant());
            }

            element.style.flexGrow = cell.ColumnSpan;
            blocks.AddBlocks(element, cell);
            return element;
        }
    }
}
