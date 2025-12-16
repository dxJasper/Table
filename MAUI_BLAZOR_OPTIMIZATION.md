# MAUI Blazor Hybrid Specific Optimizations

## Overview

MAUI Blazor Hybrid apps have unique performance considerations compared to web-based Blazor. The app runs natively with a WebView component hosting the Blazor UI, creating specific challenges and opportunities for optimization.

## Key Differences from Web Blazor

1. **Native UI Thread** - All UI updates must happen on the main/UI thread
2. **WebView Bridge** - Communication between .NET and WebView has overhead
3. **Mobile Constraints** - Limited memory, battery life, and processing power
4. **Touch Interactions** - Different input patterns than mouse/keyboard
5. **Platform APIs** - Access to native platform features

## MAUI-Specific Optimizations

### 1. Virtualization for Mobile Devices

Mobile devices have limited screen real estate and memory. Use virtualization for grids with 20+ rows:

**Create a virtualized DataGrid component:**

```razor
@* VirtualizedDataGrid.razor *@
@typeparam TItem where TItem : class, new()
@using Microsoft.AspNetCore.Components.Web.Virtualization

<div class="@DataGridStyles.GridContainer">
    <table class="@DataGridStyles.CustomDatagrid">
        <thead>
            <!-- Header content -->
        </thead>
    </table>

    <Virtualize Items="Items" Context="item" ItemSize="50">
        <ItemContent>
            <DataGridRow TItem="TItem"
                         Item="item"
                         Columns="Columns"
                         @key="item" />
        </ItemContent>
        <Placeholder>
            <tr>
                <td colspan="@(Columns.Count + 2)">
                    <div class="loading-row">Loading...</div>
                </td>
            </tr>
        </Placeholder>
    </Virtualize>
</div>
```

**Benefits:**
- Only renders visible rows (typically 10-20 on mobile)
- Reduces memory usage by 80-90% for large datasets
- Smoother scrolling on lower-end devices

### 2. Platform-Specific Rendering

Use conditional compilation to optimize per platform:

```csharp
#if ANDROID
    // Android-specific optimizations
    private const int MaxVisibleRows = 15;
#elif IOS
    // iOS-specific optimizations
    private const int MaxVisibleRows = 20;
#elif WINDOWS
    // Windows-specific optimizations
    private const int MaxVisibleRows = 30;
#else
    private const int MaxVisibleRows = 25;
#endif
```

**In your DataGrid component:**

```csharp
@code {
    protected override void OnInitialized()
    {
        // Adjust based on platform
        #if ANDROID || IOS
            // Mobile: Enable virtualization by default
            UseVirtualization = true;
            // Mobile: Reduce debounce for better responsiveness
            InputDebounceMs = 150;
        #else
            // Desktop: Can handle more without virtualization
            UseVirtualization = Items.Count > 100;
            InputDebounceMs = 300;
        #endif
    }
}
```

### 3. Reduce WebView Bridge Calls

Minimize JavaScript interop calls as they cross the .NET-WebView bridge:

**Bad - Multiple JS calls:**
```csharp
await JSRuntime.InvokeVoidAsync("scrollToTop");
await JSRuntime.InvokeVoidAsync("highlightRow", rowId);
await JSRuntime.InvokeVoidAsync("updateCounter", count);
```

**Good - Batched JS call:**
```csharp
await JSRuntime.InvokeVoidAsync("batchUpdate", new
{
    scrollToTop = true,
    highlightRow = rowId,
    counter = count
});
```

**Create a batching helper:**

```csharp
public class JSBatchInvoker
{
    private readonly IJSRuntime _jsRuntime;
    private readonly List<object> _pendingCalls = new();
    private Timer? _timer;

    public void QueueCall(string method, params object[] args)
    {
        _pendingCalls.Add(new { method, args });

        _timer?.Dispose();
        _timer = new Timer(_ => FlushAsync().GetAwaiter().GetResult(),
            null, 50, Timeout.Infinite);
    }

    private async Task FlushAsync()
    {
        if (_pendingCalls.Count > 0)
        {
            await _jsRuntime.InvokeVoidAsync("executeBatch", _pendingCalls);
            _pendingCalls.Clear();
        }
    }
}
```

### 4. Touch-Optimized Drag and Drop

Replace HTML5 drag-and-drop with touch-friendly alternatives:

```razor
@* DataGridRow.razor - Touch optimized *@
<tr @ontouchstart="OnTouchStart"
    @ontouchmove="OnTouchMove"
    @ontouchend="OnTouchEnd">

    @if (AllowRowReorder && !ReadOnly)
    {
        <td class="drag-handle" @ontouchstart:stopPropagation>
            <span class="drag-icon">☰</span>
        </td>
    }
    <!-- Rest of row -->
</tr>

@code {
    private double _startY;
    private bool _isDragging;

    private void OnTouchStart(TouchEventArgs e)
    {
        if (e.TargetTouches.Length > 0)
        {
            _startY = e.TargetTouches[0].ClientY;
            _isDragging = true;
        }
    }

    private async Task OnTouchMove(TouchEventArgs e)
    {
        if (_isDragging && e.TargetTouches.Length > 0)
        {
            var currentY = e.TargetTouches[0].ClientY;
            var deltaY = currentY - _startY;

            // Use CSS transform for smooth animation (GPU accelerated)
            await JSRuntime.InvokeVoidAsync("setTransform",
                $"translateY({deltaY}px)");
        }
    }

    private void OnTouchEnd(TouchEventArgs e)
    {
        _isDragging = false;
        OnDrop.InvokeAsync();
    }
}
```

**CSS for GPU acceleration:**
```css
.data-row {
    will-change: transform;
    transform: translateZ(0); /* Force GPU layer */
}
```

### 5. Memory Management

Mobile devices have strict memory limits. Implement aggressive cleanup:

```csharp
public class DataGridComponent<TItem> : ComponentBase, IDisposable
{
    private List<IDisposable> _subscriptions = new();

    protected override void OnInitialized()
    {
        // Track all subscriptions
        var subscription = SomeObservable.Subscribe(x => { });
        _subscriptions.Add(subscription);
    }

    public void Dispose()
    {
        // Clean up all subscriptions
        foreach (var sub in _subscriptions)
        {
            sub?.Dispose();
        }
        _subscriptions.Clear();

        // Clear large collections
        Items?.Clear();
        Columns?.Clear();
        ValidationErrors?.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
```

**Monitor memory usage:**
```csharp
#if ANDROID || IOS
[Inject]
public IDeviceInfo DeviceInfo { get; set; }

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Check available memory
        var memoryInfo = await DeviceInfo.GetMemoryInfoAsync();

        if (memoryInfo.AvailableMemoryMB < 100)
        {
            // Enable aggressive optimizations
            EnableVirtualization = true;
            MaxCachedRows = 10;
        }
    }
}
#endif
```

### 6. Lazy Loading Images/Content

Don't load all images at once on mobile:

```razor
@* DataGridCell.razor *@
@if (IsEditing)
{
    @Column.EditTemplate(Item)
}
else
{
    @if (Column.HasImage && IsVisible)
    {
        <img src="@GetImageUrl()" loading="lazy" />
    }
    else
    {
        @Column.DisplayTemplate(Item)
    }
}

@code {
    private bool IsVisible { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Use IntersectionObserver to detect visibility
            await JSRuntime.InvokeVoidAsync("observeElement",
                DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void OnElementVisible()
    {
        IsVisible = true;
        StateHasChanged();
    }
}
```

**JavaScript side:**
```javascript
window.observeElement = (dotnetRef) => {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                dotnetRef.invokeMethodAsync('OnElementVisible');
                observer.disconnect();
            }
        });
    });
    observer.observe(element);
};
```

### 7. Platform-Specific CSS

Optimize CSS for mobile performance:

```css
/* Use hardware acceleration on mobile */
@media (max-width: 768px) {
    .data-row {
        transform: translateZ(0);
        backface-visibility: hidden;
        perspective: 1000px;
    }

    /* Reduce animations on low-end devices */
    @media (prefers-reduced-motion: reduce) {
        .data-row {
            transition: none !important;
        }
    }

    /* Use native scrolling on iOS */
    .grid-container {
        -webkit-overflow-scrolling: touch;
    }
}

/* Larger touch targets for mobile */
@media (max-width: 768px) {
    .btn {
        min-height: 44px; /* iOS recommended */
        min-width: 44px;
        padding: 12px 16px;
    }

    .drag-handle {
        min-width: 44px;
        min-height: 44px;
    }
}
```

### 8. Background Task Optimization

Use MAUI's native threading for heavy operations:

```csharp
private async Task SaveRowAsync(TItem item)
{
    // Run heavy validation on background thread
    var result = await Task.Run(() =>
    {
        return ValidateItem(item);
    });

    // Update UI on main thread
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        if (!result.IsValid)
        {
            ValidationErrors[item] = result.Errors;
            StateHasChanged();
        }
    });
}
```

### 9. Reduce Bundle Size

MAUI apps include all dependencies in the app package:

**Create a trimmer configuration:**

```xml
<!-- TrimmerRoots.xml -->
<linker>
    <assembly fullname="Table.Components">
        <type fullname="Table.Components.DataGridComponent`1" preserve="all"/>
        <type fullname="Table.Components.DataGridRow`1" preserve="all"/>
        <type fullname="Table.Components.DataGridCell`1" preserve="all"/>
    </assembly>
</linker>
```

**In .csproj:**
```xml
<PropertyGroup>
    <!-- Enable trimming for Release builds -->
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>

    <!-- Reduce assembly size -->
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>

    <!-- Android specific -->
    <AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>
    <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

### 10. Handle Platform Lifecycle Events

Respond to app lifecycle to save resources:

```csharp
public partial class MainPage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Resume data updates
        _dataGrid?.ResumeUpdates();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Pause data updates to save battery
        _dataGrid?.PauseUpdates();
    }
}
```

**In DataGridComponent:**
```csharp
private Timer? _updateTimer;
private bool _isPaused;

public void PauseUpdates()
{
    _isPaused = true;
    _updateTimer?.Dispose();
}

public void ResumeUpdates()
{
    _isPaused = false;
    // Restart any periodic updates
}
```

### 11. Connection-Aware Loading

Check network connectivity before loading data:

```csharp
[Inject]
public IConnectivity Connectivity { get; set; }

private async Task LoadDataAsync()
{
    var networkAccess = Connectivity.NetworkAccess;

    if (networkAccess == NetworkAccess.Internet)
    {
        // Full data load
        await LoadAllDataAsync();
    }
    else if (networkAccess == NetworkAccess.Local)
    {
        // Load cached data only
        await LoadCachedDataAsync();
    }
    else
    {
        // Show offline message
        ShowOfflineMessage();
    }
}
```

### 12. Platform-Specific Input Handling

Optimize for touch vs mouse/keyboard:

```csharp
@code {
    private bool IsTouchDevice =>
        DeviceInfo.Idiom == DeviceIdiom.Phone ||
        DeviceInfo.Idiom == DeviceIdiom.Tablet;

    protected override void OnInitialized()
    {
        if (IsTouchDevice)
        {
            // Show touch-optimized controls
            ShowDragHandles = true;
            EnableSwipeGestures = true;
            ButtonSize = "large";
        }
        else
        {
            // Show desktop controls
            ShowDragHandles = false;
            EnableSwipeGestures = false;
            ButtonSize = "normal";
        }
    }
}
```

## Performance Checklist for MAUI Blazor

### Essential Optimizations

- [ ] **Enable virtualization** for grids with 20+ items
- [ ] **Use `@key` directives** for all collections
- [ ] **Implement `ShouldRender()`** in custom components
- [ ] **Batch JavaScript interop calls** to reduce bridge overhead
- [ ] **Use native touch events** instead of HTML5 drag-and-drop on mobile
- [ ] **Implement proper disposal** in all components
- [ ] **Use lazy loading** for images and heavy content
- [ ] **Test on actual devices**, not just emulators

### Platform-Specific

- [ ] **Android**: Enable AOT compilation for faster startup
- [ ] **iOS**: Use `-webkit-overflow-scrolling: touch`
- [ ] **Android**: Test on low-end devices (2GB RAM)
- [ ] **iOS**: Test on older iPhones (iPhone 8, SE)
- [ ] **All**: Implement responsive design for different screen sizes

### Memory Management

- [ ] **Monitor memory usage** during development
- [ ] **Clear caches** when app goes to background
- [ ] **Dispose subscriptions** and event handlers
- [ ] **Use weak references** for large cached data
- [ ] **Profile memory** with platform tools (Android Profiler, Xcode Instruments)

### Battery Optimization

- [ ] **Pause updates** when app is backgrounded
- [ ] **Reduce animation** on battery saver mode
- [ ] **Debounce rapid events** (input, scroll)
- [ ] **Use efficient data binding** to reduce re-renders

## Testing Tools

### Android
```bash
# Memory profiling
adb shell dumpsys meminfo com.yourapp

# CPU profiling
adb shell top -m 10

# Network monitoring
adb shell tcpdump -i any -w /sdcard/capture.pcap
```

### iOS
- Xcode Instruments (Time Profiler, Allocations)
- Safari Web Inspector for WebView debugging
- Console app for crash logs

### Cross-Platform
```csharp
// Add performance logging
protected override void OnAfterRender(bool firstRender)
{
    var renderTime = DateTime.Now - _renderStart;
    Debug.WriteLine($"Render time: {renderTime.TotalMilliseconds}ms");
}
```

## Summary

MAUI Blazor Hybrid apps need special attention to:

✅ **Virtualization** - Critical for mobile performance
✅ **Touch optimization** - Different interaction model than web
✅ **Memory management** - Aggressive cleanup required
✅ **Platform APIs** - Leverage native capabilities
✅ **Battery life** - Pause non-essential updates
✅ **Network awareness** - Handle offline scenarios
✅ **Device testing** - Emulators don't show real performance

These optimizations can improve performance by 2-5x on mobile devices compared to a direct web-to-MAUI port.
