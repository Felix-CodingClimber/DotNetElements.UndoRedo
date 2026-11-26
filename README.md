# DotNetElements.UndoRedo

A lightweight, AOT-friendly, reflection-free Undo/Redo framework for **.NET 10**, designed to pair seamlessly with **CommunityToolkit.Mvvm** partial properties and a Roslyn Incremental Source Generator.

---

## Features

- **Zero-boilerplate MVVM tracking** via Roslyn Source Generation (`[Undoable]`, `[UndoableIgnore]`) hooking directly into CommunityToolkit.Mvvm's `[ObservableProperty]` partial properties.
- **Automatic object-graph propagation (`IUndoableNode`)**: Attach `UndoRedoManager` once at the root ViewModel and it automatically cascades to nested ViewModels and `UndoableCollection<T>` items (and detaches them when removed).
- **AvaloniaUI-ready**: `UndoRedoManager` inherits `ObservableObject` and exposes `UndoCommand`, `RedoCommand`, `CanUndo`, `CanRedo`, `NextUndoDescription`, `NextRedoDescription`, and `IsDirty`.
- **Action coalescing (merging)**: Rapid changes (e.g., slider drags or typing in a `TextBox`) merge into a single undo entry using a configurable `MergeTimeoutMs` window.
- **Nested transactions**: Group multiple property and collection mutations into one atomic undo step with `BeginTransaction(...)`.
- **Suspend recording**: Hydrate or load ViewModels without polluting history using `Suspend()`.
- **Dirty-state tracking**: `IsDirty` and `MarkSaved()` accurately track whether the current state matches the last saved checkpoint—even across undo, redo, coalescing, and capacity eviction.
- **Bounded history**: Optional `maxCapacity` to cap memory usage.
- **100% AOT & trimming friendly**: No reflection or runtime proxies.

---

## Solution Layout

| Project | Target | Description |
| :--- | :--- | :--- |
| `DotNetElements.UndoRedo` | `net10.0` | Core runtime library (`UndoRedoManager`, `UndoableCollection<T>`, `IUndoableAction`, `IUndoableNode`, attributes). |
| `DotNetElements.UndoRedo.Generators` | `netstandard2.0` | Roslyn Incremental Source Generator for `[Undoable]` partial classes and properties. |
| `DotNetElements.UndoRedo.Sample` | `net10.0` | Runnable console demo showcasing a 3-level ViewModel graph. |
| `DotNetElements.UndoRedo.Tests` | `net10.0` | xUnit test suite covering runtime features and generated code. |

---

## Installation (Project References)

Reference both the runtime library and the source generator (as an analyzer) in your application project:

```xml
<ItemGroup>
  <ProjectReference Include="..\DotNetElements.UndoRedo\DotNetElements.UndoRedo.csproj" />
  <ProjectReference Include="..\DotNetElements.UndoRedo.Generators\DotNetElements.UndoRedo.Generators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

---

## Quick Start

### 1. Define Your ViewModels

You can use either **Opt-Out mode** (`[Undoable]` on the class) or **Opt-In mode** (`[Undoable]` only on specific properties).

#### Opt-Out Mode (track all `[ObservableProperty]` members except `[UndoableIgnore]`)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using DotNetElements.UndoRedo;

[Undoable]
public partial class DocumentViewModel : ObservableObject
{
    [ObservableProperty]
    [Undoable(Description = "Rename Document")]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    [Undoable(MergeTimeoutMs = 300)]
    public partial double Zoom { get; set; } = 1.0;

    [ObservableProperty]
    [UndoableIgnore] // Excluded from undo history (UI-only state)
    public partial bool IsPreviewMode { get; set; }

    public UndoableCollection<LayerViewModel> Layers { get; } = [];
}

[Undoable]
public partial class LayerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Opacity { get; set; } = 1.0;

    public UndoableCollection<ShapeViewModel> Shapes { get; } = [];
}

[Undoable]
public partial class ShapeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial double X { get; set; }

    [ObservableProperty]
    public partial double Y { get; set; }
}
```

#### Opt-In Mode (track only decorated properties—no `[UndoableIgnore]` needed)

```csharp
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    [Undoable(MergeTimeoutMs = 400)]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty] // Automatically ignored
    public partial bool IsExpanded { get; set; }
}
```

### 2. Attach `UndoRedoManager` at the Root

Build your initial object graph **before** attaching so initialization doesn't pollute the undo stack, then call `undo.Attach(root)` once:

```csharp
var undo = new UndoRedoManager(maxCapacity: 100);

var document = new DocumentViewModel { Title = "Untitled", Zoom = 1.0 };
var layer = new LayerViewModel { Name = "Background" };
layer.Shapes.Add(new ShapeViewModel { X = 10, Y = 20 });
document.Layers.Add(layer);

// Cascades UndoRedoManager down through Document -> Layers -> Shapes
undo.Attach(document);

// All edits anywhere in the graph are now tracked automatically!
document.Title = "Poster Design";
layer.Shapes[0].X = 150;

undo.Undo(); // Shape.X -> 10
undo.Undo(); // Title -> "Untitled"
undo.Redo(); // Title -> "Poster Design"
```

---

## AvaloniaUI Integration

`UndoRedoManager` is an `ObservableObject` with generated `[RelayCommand]` properties, so it binds directly in XAML:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Window.KeyBindings>
    <KeyBinding Gesture="Ctrl+Z" Command="{Binding UndoRedo.UndoCommand}" />
    <KeyBinding Gesture="Ctrl+Y" Command="{Binding UndoRedo.RedoCommand}" />
    <KeyBinding Gesture="Ctrl+Shift+Z" Command="{Binding UndoRedo.RedoCommand}" />
  </Window.KeyBindings>

  <StackPanel Orientation="Horizontal" Spacing="8">
    <Button Content="Undo"
            Command="{Binding UndoRedo.UndoCommand}"
            ToolTip.Tip="{Binding UndoRedo.NextUndoDescription}" />

    <Button Content="Redo"
            Command="{Binding UndoRedo.RedoCommand}"
            ToolTip.Tip="{Binding UndoRedo.NextRedoDescription}" />

    <TextBlock Text="*"
               IsVisible="{Binding UndoRedo.IsDirty}" />
  </StackPanel>
</Window>
```

---

## Core Concepts & API Reference

### Automatic Object Graph Propagation (`IUndoableNode` & `UndoableCollection<T>`)

Every class processed by the source generator automatically implements `IUndoableNode` (`UndoRedoManager? UndoRedo { get; set; }`):

- Setting `UndoRedo` on a parent cascades to all child `IUndoableNode` properties and `UndoableCollection<T>` properties.
- Adding a new `IUndoableNode` to an `UndoableCollection<T>` automatically attaches the collection's `UndoRedoManager`.
- Removing an item detaches it (`UndoRedo = null`) so off-tree mutations don't record phantom history; undoing the removal re-attaches it cleanly.
- Replacing a tracked `IUndoableNode` property automatically detaches the old value and attaches the new value.

### Transactions (`BeginTransaction`)

Group multiple property and collection mutations into a single atomic undo/redo entry. Transactions can be nested and empty transactions are ignored:

```csharp
using (undo.BeginTransaction("Reset Canvas"))
{
    document.Title = "Blank";
    document.Zoom = 1.0;
    document.Layers.Clear();
}
// A single undo.Undo() reverts all three changes in reverse order.
```

### Action Coalescing (`MergeTimeoutMs`)

Specify `MergeTimeoutMs` on `[Undoable]` (at class or property level) to merge consecutive edits to the same property within a rolling time window:

```csharp
[ObservableProperty]
[Undoable(MergeTimeoutMs = 300)]
public partial double Opacity { get; set; } = 1.0;
```

For deterministic unit testing without `Thread.Sleep`, pass a custom `TimeProvider` to `new UndoRedoManager(timeProvider: myFakeClock)`.

### Suspending Recording (`Suspend`)

Temporarily silence history recording (e.g., while loading a file or hydrating state). Supports nesting:

```csharp
using (undo.Suspend())
{
    document.Title = loaded.Title;
    document.Zoom = loaded.Zoom;
}
```

### Dirty-State Tracking (`IsDirty` & `MarkSaved`)

- Starts as `IsDirty == false`.
- Becomes `true` after any recorded change, and automatically returns to `false` if the user undoes back to the saved state.
- Call `undo.MarkSaved()` after persisting the document to move the clean checkpoint to the current history position.

### Custom Actions (`IUndoableAction` & `DelegateUndoableAction`)

For operations outside simple property or collection changes, record a `DelegateUndoableAction` or implement `IUndoableAction`:

```csharp
undo.Record(new DelegateUndoableAction(
    description: "Rotate Canvas 90°",
    undo: () => Rotate(-90),
    redo: () => Rotate(90)));
```

---

## How the Source Generator Works

For each tracked `[ObservableProperty]` partial property, `UndoableGenerator` emits implementations of CommunityToolkit.Mvvm's `On<Prop>Changing(T value)` and `On<Prop>Changed(T value)` partial methods:

1. `On<Prop>Changing` captures the current property value before the backing `field` is overwritten.
2. `On<Prop>Changed` reads and clears the temporary previous value (preventing reference retention), attaches/detaches `IUndoableNode` values when applicable, and calls `UndoRedo?.RecordPropertyChange(...)`.

Because `UndoRedoManager.IsRestoring` is `true` during `Undo()` and `Redo()`, restoring values via the setter delegate never re-records duplicate entries.

---

## Building & Running Tests

```powershell
# Build the entire solution
dotnet build DotNetElements.UndoRedo.slnx

# Run all unit tests
dotnet test DotNetElements.UndoRedo.slnx

# Run the console sample
dotnet run --project DotNetElements.UndoRedo.Sample
```
