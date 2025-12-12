using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace Table.Models
{
    public class GridColumn<TItem>
    {
        public string HeaderText { get; set; } = string.Empty;
        public bool Sortable { get; set; }
        public Expression<Func<TItem, object>>? SortExpression { get; set; }
        public RenderFragment<TItem> DisplayTemplate { get; set; } = default!;
        public RenderFragment<TItem> EditTemplate { get; set; } = default!;
    }
}
