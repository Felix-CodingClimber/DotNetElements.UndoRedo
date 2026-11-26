using DotNetElements.UndoRedo;
using DotNetElements.UndoRedo.Sample;

UndoRedoManager undo = new(maxCapacity: 50);

// Build an entire view-model tree BEFORE attaching — zero history pollution!
var document = new DocumentViewModel
{
	Title = "Untitled",
	Zoom = 1.0
};

LayerViewModel background = new() { Name = "Background", Opacity = 1.0 };
background.Shapes.Add(new ShapeViewModel { Label = "Rect 1", X = 10, Y = 20 });
document.Layers.Add(background);

// Attach once at the root — automatically cascades to Layers -> Shapes!
undo.Attach(document);

PrintState("Initial");

// 1. Property edit on root
document.Title = "Poster Design";
PrintState("Renamed document");

// 2. Ignored property does NOT record history
document.IsPreviewMode = true;
Console.WriteLine($"After toggling [UndoableIgnore] IsPreviewMode: UndoCount = {undo.UndoCount}");

// 3. Edit deep inside the graph (ShapeViewModel never took UndoRedoManager in its constructor!)
background.Shapes[0].X = 150;
PrintState("Moved shape inside nested collection");

// 4. Add a brand new layer — automatically attached upon Add()
LayerViewModel foreground = new() { Name = "Foreground", Opacity = 0.75 };
document.Layers.Add(foreground);
foreground.Name = "Overlay";
PrintState("Added foreground layer & renamed it");

// 5. Transaction groups multiple changes into a single undo step
using (undo.BeginTransaction("Reset Canvas"))
{
	document.Title = "Blank";
	document.Zoom = 0.5;
	background.Opacity = 0.25;
}
PrintState("After 'Reset Canvas' transaction");

// 6. Undo everything step-by-step using the MVVM RelayCommand
Console.WriteLine("\n--- Undoing ---");
while (undo.CanUndo)
{
	Console.WriteLine($"Undo: {undo.NextUndoDescription}");
	undo.UndoCommand.Execute(null);
	PrintState("  ->");
}

Console.WriteLine("\n--- Redoing 2 steps ---");
for (int i = 0; i < 2 && undo.CanRedo; i++)
{
	Console.WriteLine($"Redo: {undo.NextRedoDescription}");
	undo.RedoCommand.Execute(null);
	PrintState("  ->");
}

void PrintState(string label)
{
	var shape = background.Shapes[0];
	Console.WriteLine(
		$"[{label}] Title='{document.Title}', Zoom={document.Zoom}, Layers={document.Layers.Count}, " +
		$"BgOpacity={background.Opacity}, Shape.X={shape.X} | Undo={undo.UndoCount}, Redo={undo.RedoCount}, Dirty={undo.IsDirty}");
}

