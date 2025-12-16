# DataGrid Performance Optimization

## Overview

The DataGrid component has been optimized to only update components that have changed, rather than rebuilding the entire grid. This significantly improves performance, especially with large datasets or when components within a row are dependent on each other.

## Architecture

The component hierarchy is now:

```
DataGridComponent
  └── DataGridRow (one per row)
        └── DataGridCell (one per column)
```

### 1. DataGridRow Component

**File:** `Components/DataGridRow.razor`

**Purpose:** Isolates each row as a separate component with its own rendering lifecycle.

**Key Features:**
- Uses `@key="item"` directive to maintain component identity across re-renders
- Implements `ShouldRender()` for custom render logic
- Receives all row-specific state as parameters
- Only re-renders when its parameters change

**Benefits:**
- When you edit one row, only that row re-renders
- Other rows remain unchanged
- Drag-and-drop operations only affect the dragged row

### 2. DataGridCell Component

**File:** `Components/DataGridCell.razor`

**Purpose:** Provides even more granular control over individual cell rendering.

**Key Features:**
- Implements `ShouldRender()` with custom logic
- Tracks previous state (`_previousIsEditing`, `_previousItem`)
- Only re-renders when:
  - Editing state changes
  - Item reference changes
  - First render

**Benefits:**
- When editing a row, only cells that switch between display/edit mode re-render
- If a cell's template doesn't change, it won't re-render
- Dependent cells can update independently

### 3. DataGridComponent Updates

**File:** `Components/DataGridComponent.razor`

**Key Changes:**
```razor
<DataGridRow TItem="TItem"
             Item="item"
             Columns="Columns"
             IsEditing="isEditing"
             IsNewRow="IsNewItem(item)"
             IsDragging="DraggedRowIndex == rowIndex"
             AllowRowReorder="AllowRowReorder"
             ReadOnly="ReadOnly"
             ValidationErrors="validationErrors"
             OnEdit="() => EditRow(item)"
             OnSave="() => SaveRow(item)"
             OnCancel="() => CancelEdit(item)"
             OnDelete="() => DeleteRow(item)"
             OnDragStart="(e) => OnRowDragStart(e, rowIndex)"
             OnDrop="() => OnRowDrop(rowIndex)"
             @key="item" />
```

**Important:** The `@key="item"` directive tells Blazor to track each row by its item reference, preventing unnecessary re-renders.

## How It Works

### Example: Editing a Row

**Before optimization:**
1. User clicks Edit on row 5
2. Component sets `EditingItem` state
3. `StateHasChanged()` is called
4. **Entire grid re-renders** (all 30 rows)
5. All cells re-render (30 rows × 10 columns = 300 cells)

**After optimization:**
1. User clicks Edit on row 5
2. Component sets `EditingItem` state
3. `StateHasChanged()` is called
4. Blazor checks each DataGridRow's parameters
5. **Only row 5's parameters changed** (IsEditing: false → true)
6. Only row 5 re-renders
7. DataGridCell components in row 5 check their state
8. Cells switch from DisplayTemplate to EditTemplate
9. Only 10 cells re-render instead of 300

### Example: Dependent Cells

Imagine you have a "Price" and "Tax" and "Total" column where Total depends on Price and Tax:

```razor
<DataGridColumn TItem="Product"
                HeaderText="Price">
    <EditTemplate>
        <input @bind="context.Price" @bind:event="oninput" />
    </EditTemplate>
</DataGridColumn>

<DataGridColumn TItem="Product"
                HeaderText="Tax">
    <EditTemplate>
        <input @bind="context.Tax" @bind:event="oninput" />
    </EditTemplate>
</DataGridColumn>

<DataGridColumn TItem="Product"
                HeaderText="Total">
    <DisplayTemplate>
        @((context.Price * (1 + context.Tax)).ToString("C"))
    </DisplayTemplate>
</DataGridColumn>
```

**What happens when Price changes:**
1. User types in Price input
2. `context.Price` updates
3. Blazor detects the item reference hasn't changed
4. DataGridCell for "Total" column checks if it should re-render
5. **Total cell re-renders** to show updated calculation
6. Other rows and cells don't re-render

## Performance Tips

### 1. Use @key for Collections

Always use `@key` when rendering collections:
```razor
@foreach (var item in Items)
{
    <DataGridRow @key="item" ... />
}
```

### 2. Avoid Creating New Objects

Don't create new objects in templates:
```razor
<!-- BAD: Creates new object on every render -->
<div style="@(new { Color = "red" })">

<!-- GOOD: Use static values -->
<div style="color: red;">
```

### 3. Use EventCallback for Actions

EventCallback is optimized for Blazor's change detection:
```razor
[Parameter]
public EventCallback OnSave { get; set; }
```

### 4. Minimize StateHasChanged() Calls

Only call `StateHasChanged()` when necessary:
```csharp
// BAD: Called too frequently
private void UpdateValue(string value)
{
    _value = value;
    StateHasChanged(); // Not needed if using @bind
}

// GOOD: Let Blazor handle it
private void UpdateValue(string value)
{
    _value = value;
    // StateHasChanged() will be called automatically after event handler
}
```

### 5. Custom ShouldRender Logic

For expensive operations, implement `ShouldRender()`:
```csharp
private bool _lastEditState;

protected override bool ShouldRender()
{
    if (_lastEditState != IsEditing)
    {
        _lastEditState = IsEditing;
        return true;
    }
    return false;
}
```

## Measuring Performance

To see the optimization in action, you can add logging:

```csharp
protected override void OnAfterRender(bool firstRender)
{
    Console.WriteLine($"Row {Item.Id} rendered. IsEditing: {IsEditing}");
    base.OnAfterRender(firstRender);
}
```

Watch the console when:
- Editing a row (only that row logs)
- Sorting the grid (all rows log due to re-ordering)
- Adding a new row (only new row logs)

## Blazor Rendering Pipeline

Understanding Blazor's rendering helps optimize performance:

1. **Event occurs** (click, input, etc.)
2. **Event handler executes**
3. **StateHasChanged() called** (automatically after event handlers)
4. **Component tree diffing** begins
5. **Parameters compared** for each child component
6. **ShouldRender() checked** for each component
7. **Render only changed components**
8. **DOM diff and patch**

## Additional Optimizations

### Virtualization (Future Enhancement)

For very large datasets (1000+ rows), consider implementing virtualization:
```razor
<Virtualize Items="Items" Context="item">
    <DataGridRow Item="item" ... />
</Virtualize>
```

### Debouncing Input

For dependent cells with expensive calculations:
```csharp
private System.Threading.Timer? _debounceTimer;

private void OnPriceInput(ChangeEventArgs e)
{
    _debounceTimer?.Dispose();
    _debounceTimer = new Timer(_ =>
    {
        InvokeAsync(() =>
        {
            // Update calculation
            StateHasChanged();
        });
    }, null, 300, Timeout.Infinite);
}
```

### Memoization

Cache expensive calculations:
```csharp
private decimal? _cachedTotal;
private decimal _lastPrice;
private decimal _lastTax;

private decimal CalculateTotal()
{
    if (_cachedTotal.HasValue &&
        _lastPrice == Price &&
        _lastTax == Tax)
    {
        return _cachedTotal.Value;
    }

    _lastPrice = Price;
    _lastTax = Tax;
    _cachedTotal = Price * (1 + Tax);
    return _cachedTotal.Value;
}
```

## Summary

The DataGrid now uses a three-tier component architecture that ensures:

✅ Only modified rows re-render
✅ Only changed cells within a row update
✅ Dependent cells can update independently
✅ Large grids remain performant
✅ Drag-and-drop operations are smooth
✅ Edit operations are instant

This makes the component suitable for:
- Large datasets (100+ rows)
- Complex cell templates
- Dependent calculations
- Real-time data updates
- Frequent user interactions
