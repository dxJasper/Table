# Generic Blazor Data Grid Component

A fully generic, customizable Blazor data grid component for .NET 10 with support for custom input field types.

## Features

✅ **Generic** - Works with any data type using `TItem`
✅ **Custom Field Types** - Text, email, number, date, checkbox, select, textarea, and more
✅ **Sortable Columns** - Click headers to sort ascending/descending
✅ **Draggable Columns** - Reorder non-sortable columns by dragging
✅ **Draggable Rows** - Reorder rows when grid is not sortable
✅ **Inline Editing** - Edit rows with custom input controls
✅ **CRUD Operations** - Add, edit, delete, and cancel
✅ **Compact Design** - Optimized for displaying large amounts of data
✅ **Fixed Width Cells** - Components never exceed cell boundaries
✅ **Optional Vertical Borders** - Clean, minimal styling
✅ **Separate Files** - Component (.razor), code (.cs), and CSS (.css) properly separated
✅ **Type Safe** - Full C# type safety with `RenderFragment<TItem>`

## Project Structure

```
Table/
├── Components/
│   ├── DataGridComponent.razor        # Main grid component markup
│   └── DataGridComponent.razor.css    # Scoped grid styles
├── Models/
│   ├── Employee.cs                    # Example data model
│   └── GridColumn.cs                  # Column configuration class
├── Pages/
│   └── Index.razor                    # Demo page with 10 field types
├── wwwroot/
│   └── css/
│       └── app.css                    # Global styles
├── App.razor                          # Root app component
├── Routes.razor                       # Routing configuration
├── _Imports.razor                     # Global using statements
├── Program.cs                         # App startup
└── table.csproj                       # Project file
```

## Component Usage

There are **two ways** to use the data grid:

### 1. Markup-Based Approach (Recommended)

Define columns directly in the view without code-behind:

```razor
<DataGrid TItem="Employee"
          Items="@employees"
          CreateNewItem="@CreateNewEmployee"
          CloneItem="@CloneEmployee">
    <Columns>
        <DataGridColumn TItem="Employee"
                        HeaderText="Name"
                        Sortable="true"
                        SortBy="@(e => e.Name)">
            <DisplayTemplate>
                <span>@context.Name</span>
            </DisplayTemplate>
            <EditTemplate>
                <input type="text" @bind="context.Name" />
            </EditTemplate>
        </DataGridColumn>

        <DataGridColumn TItem="Employee"
                        HeaderText="Department"
                        Sortable="true"
                        SortBy="@(e => e.Department)">
            <DisplayTemplate>
                <span>@context.Department</span>
            </DisplayTemplate>
            <EditTemplate>
                <select @bind="context.Department">
                    <option value="IT">IT</option>
                    <option value="HR">HR</option>
                </select>
            </EditTemplate>
        </DataGridColumn>
    </Columns>
</DataGrid>
```

See `Pages/MarkupExample.razor` for a complete example.

### 2. Code-Behind Approach

Define columns programmatically in the code section:

```razor
<DataGridComponent TItem="Employee"
                   Items="@employees"
                   Columns="@columns"
                   CreateNewItem="@CreateNewEmployee"
                   CloneItem="@CloneEmployee" />
```

See `Pages/Index.razor` for a complete example.

### Defining Columns

```csharp
var columns = new List<GridColumn<Employee>>
{
    // Text input
    new GridColumn<Employee>
    {
        HeaderText = "Name",
        Sortable = true,
        SortExpression = e => e.Name,
        DisplayTemplate = employee => @<span>@employee.Name</span>,
        EditTemplate = employee => @<input type="text" @bind="employee.Name" />
    },

    // Select dropdown
    new GridColumn<Employee>
    {
        HeaderText = "Department",
        Sortable = true,
        SortExpression = e => e.Department,
        DisplayTemplate = employee => @<span>@employee.Department</span>,
        EditTemplate = employee =>
            @<select @bind="employee.Department">
                <option value="IT">IT</option>
                <option value="HR">HR</option>
                <option value="Finance">Finance</option>
            </select>
    },

    // Checkbox
    new GridColumn<Employee>
    {
        HeaderText = "Active",
        Sortable = true,
        SortExpression = e => e.IsActive,
        DisplayTemplate = employee => @<span>@(employee.IsActive ? "Yes" : "No")</span>,
        EditTemplate = employee =>
            @<input type="checkbox" @bind="employee.IsActive" />
    }
};
```

## Field Types Demonstrated

The included demo (`Pages/Index.razor`) shows 10 different input types:

1. **Readonly Text** - ID field
2. **Text Input** - Name field
3. **Email Input** - Email with validation
4. **Number Input** - Age and Salary
5. **Date Input** - Hire date picker
6. **Checkbox** - Active status toggle
7. **Select Dropdown** - Department selection
8. **Textarea** - Notes field
9. **Custom Display** - Salary with currency formatting
10. **Custom Badges** - Active/Inactive status badges

## Customization

### Adding Your Own Data Type

1. Create your model:
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }

    public Product Clone() => new Product
    {
        Id = this.Id,
        Name = this.Name,
        Price = this.Price
    };
}
```

2. Configure columns:
```csharp
var columns = new List<GridColumn<Product>>
{
    new GridColumn<Product>
    {
        HeaderText = "Product Name",
        Sortable = true,
        SortExpression = p => p.Name,
        DisplayTemplate = p => @<span>@p.Name</span>,
        EditTemplate = p => @<input type="text" @bind="p.Name" />
    }
};
```

3. Use the component:
```razor
<DataGridComponent TItem="Product"
                   Items="@products"
                   Columns="@columns" />
```

## How to Run

```bash
cd C:\Temp\Claude\Table
dotnet restore
dotnet build
dotnet run
```

Then navigate to: `https://localhost:5001`

## Requirements

- .NET 10.0 SDK
- Modern web browser

## Key Files to Modify

- **Components/DataGridComponent.razor** - Grid layout and structure
- **Components/DataGridComponent.razor.css** - Grid styling
- **Models/GridColumn.cs** - Column configuration options
- **Pages/Index.razor** - Example usage and column definitions

## Notes

This component uses:
- `RenderFragment<TItem>` for flexible templating
- Two-way data binding with `@bind`
- Event callbacks for CRUD operations
- Expression trees for sortable columns
- Scoped CSS for style isolation

Enjoy building with the generic Blazor DataGrid!
