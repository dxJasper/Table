# DataGrid Column Definition - Two Approaches

The DataGridComponent supports two ways to define columns:

## 1. Code-Behind Approach (Index.razor / SimpleMarkup.razor)

Define columns in the `@code` section using a `List<GridColumn<T>>`:

```razor
@code {
    private List<GridColumn<Employee>> columns = new();

    protected override void OnInitialized()
    {
        columns = new List<GridColumn<Employee>>
        {
            new GridColumn<Employee>
            {
                HeaderText = "Name",
                Sortable = true,
                SortExpression = e => e.Name,
                DisplayTemplate = item => @<text>@item.Name</text>,
                EditTemplate = item => @<input @bind="item.Name" style="width: 100%;" />
            },
            new GridColumn<Employee>
            {
                HeaderText = "Email",
                Sortable = true,
                SortExpression = e => e.Email,
                DisplayTemplate = item => @<text>@item.Email</text>,
                EditTemplate = item => @<input @bind="item.Email" type="email" style="width: 100%;" />
            }
        };
    }
}

<DataGridComponent TItem="Employee"
                   Items="@employees"
                   Columns="@columns"
                   ... />
```

## 2. Razor Markup Approach (RazorMarkup.razor)

Define columns directly in the markup using `<DataGridColumn>` components:

```razor
<DataGridComponent TItem="Employee"
                   Items="@employees"
                   CreateNewItem="@CreateNewEmployee"
                   CloneItem="@CloneEmployee"
                   OnItemSaved="@HandleItemSaved"
                   OnItemDeleted="@HandleItemDeleted"
                   OnItemAdded="@HandleItemAdded"
                   OrderPropertyName="DisplayOrder"
                   AllowRowReorder="true">

    <DataGridColumn TItem="Employee"
                    HeaderText="Name"
                    Sortable="true"
                    SortExpression="@(e => e.Name)">
        <DisplayTemplate>
            @context.Name
        </DisplayTemplate>
        <EditTemplate>
            <input @bind="context.Name" style="width: 100%;" />
        </EditTemplate>
    </DataGridColumn>

    <DataGridColumn TItem="Employee"
                    HeaderText="Email"
                    Sortable="true"
                    SortExpression="@(e => e.Email)">
        <DisplayTemplate>
            @context.Email
        </DisplayTemplate>
        <EditTemplate>
            <input @bind="context.Email" type="email" style="width: 100%;" />
        </EditTemplate>
    </DataGridColumn>

</DataGridComponent>
```

## Comparison

| Feature | Code-Behind | Razor Markup |
|---------|-------------|--------------|
| **Readability** | More compact | More verbose but clearer structure |
| **IntelliSense** | Better C# support | Better Razor markup support |
| **Separation** | Logic and UI separated | Everything in one place |
| **Reusability** | Easier to share column definitions | Each page defines its own |
| **Maintainability** | Good for dynamic columns | Good for static layouts |

## When to Use Each Approach

### Use Code-Behind When:
- You need to dynamically generate columns based on runtime data
- You want to reuse column definitions across multiple pages
- You prefer working primarily in C# code
- You need complex column logic or conditional columns

### Use Razor Markup When:
- You prefer declarative markup style
- Columns are static and page-specific
- You want better visual structure in the markup
- You're building a page-specific grid with fixed columns

## Parameters

Both approaches support the same parameters:

- **HeaderText**: Column header display text
- **Sortable**: Whether the column can be sorted
- **SortExpression**: Expression used for sorting
- **DisplayTemplate**: How to render the cell in display mode
- **EditTemplate**: How to render the cell in edit mode

In the Razor markup approach, use `@context` to access the current item in the templates.
