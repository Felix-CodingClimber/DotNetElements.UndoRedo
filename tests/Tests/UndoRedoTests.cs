using CommunityToolkit.Mvvm.ComponentModel;
using Xunit;

namespace DotNetElements.UndoRedo.Tests;

public class UndoRedoTests
{
	[Fact]
	public void GeneratedPropertyTracksUndoAndRedoWithCommands()
	{
		UndoRedoManager undo = new();
		TestDocument doc = new() { Title = "Initial", Zoom = 1.0 };
		undo.Attach(doc);

		Assert.False(undo.CanUndo);
		Assert.False(undo.CanRedo);
		Assert.False(undo.UndoCommand.CanExecute(null));
		Assert.False(undo.RedoCommand.CanExecute(null));

		doc.Title = "Updated";

		Assert.True(undo.CanUndo);
		Assert.False(undo.CanRedo);
		Assert.Equal("Rename Document", undo.NextUndoDescription);
		Assert.True(undo.UndoCommand.CanExecute(null));

		undo.UndoCommand.Execute(null);

		Assert.Equal("Initial", doc.Title);
		Assert.False(undo.CanUndo);
		Assert.True(undo.CanRedo);
		Assert.Equal("Rename Document", undo.NextRedoDescription);

		undo.RedoCommand.Execute(null);

		Assert.Equal("Updated", doc.Title);
		Assert.True(undo.CanUndo);
		Assert.False(undo.CanRedo);
	}

	[Fact]
	public void UndoableIgnoreDoesNotRecordHistory()
	{
		UndoRedoManager undo = new();
		TestDocument doc = new();
		undo.Attach(doc);

		doc.IsSelected = true;
		doc.IsSelected = false;

		Assert.Equal(0, undo.UndoCount);
		Assert.False(undo.IsDirty);
	}

	[Fact]
	public void PropertyLevelUndoableTracksOnlyOptedInProperties()
	{
		UndoRedoManager undo = new();
		OptInViewModel vm = new() { Tracked = "A", Untracked = "X" };
		undo.Attach(vm);

		vm.Untracked = "Y";
		Assert.Equal(0, undo.UndoCount);

		vm.Tracked = "B";
		Assert.Equal(1, undo.UndoCount);

		undo.Undo();
		Assert.Equal("A", vm.Tracked);
		Assert.Equal("Y", vm.Untracked);
	}

	[Fact]
	public void SuspendSuppressesRecordingSupportsNesting()
	{
		UndoRedoManager undo = new();
		TestDocument doc = new() { Title = "A" };
		undo.Attach(doc);

		using (undo.Suspend())
		{
			doc.Title = "B";
			using (undo.Suspend())
			{
				doc.Title = "C";
			}
			Assert.True(undo.IsSuspended);
			doc.Title = "D";
		}

		Assert.False(undo.IsSuspended);
		Assert.Equal(0, undo.UndoCount);
		Assert.Equal("D", doc.Title);

		doc.Title = "E";
		Assert.Equal(1, undo.UndoCount);
	}

	[Fact]
	public void TransactionsGroupChangesIntoSingleStepSupportsNestingAndEmpty()
	{
		UndoRedoManager undo = new();
		TestDocument doc = new() { Title = "Start", Zoom = 1.0 };
		undo.Attach(doc);

		// Empty transaction should not push anything
		using (undo.BeginTransaction("Empty"))
		{
		}
		Assert.Equal(0, undo.UndoCount);

		// Nested transactions flatten into the outermost transaction
		using (undo.BeginTransaction("Reset Layout"))
		{
			doc.Title = "Mid";
			using (undo.BeginTransaction("Inner"))
			{
				doc.Zoom = 2.0;
				doc.Title = "Final";
			}
		}

		Assert.Equal(1, undo.UndoCount);
		Assert.Equal("Reset Layout", undo.NextUndoDescription);

		undo.Undo();
		Assert.Equal("Start", doc.Title);
		Assert.Equal(1.0, doc.Zoom);

		undo.Redo();
		Assert.Equal("Final", doc.Title);
		Assert.Equal(2.0, doc.Zoom);
	}

	[Fact]
	public void CoalescingMergesRapidChangesWithinWindowSplitsAfterWindow()
	{
		ManualTimeProvider clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		UndoRedoManager undo = new(timeProvider: clock);
		TestDocument doc = new() { Zoom = 1.0 };
		undo.Attach(doc);

		doc.Zoom = 1.2;
		clock.Advance(TimeSpan.FromMilliseconds(100));
		doc.Zoom = 1.5;
		clock.Advance(TimeSpan.FromMilliseconds(200));
		doc.Zoom = 2.0;

		// All within 300ms rolling window -> merged into 1 action
		Assert.Equal(1, undo.UndoCount);

		// Advance past the 300ms window -> creates a new action
		clock.Advance(TimeSpan.FromMilliseconds(500));
		doc.Zoom = 3.0;
		Assert.Equal(2, undo.UndoCount);

		undo.Undo();
		Assert.Equal(2.0, doc.Zoom);

		undo.Undo();
		Assert.Equal(1.0, doc.Zoom);
	}

	[Fact]
	public void MaxCapacityEvictsOldestEntries()
	{
		UndoRedoManager undo = new(maxCapacity: 2);
		TestDocument doc = new() { Title = "0" };
		undo.Attach(doc);

		doc.Title = "1";
		doc.Title = "2";
		doc.Title = "3";

		Assert.Equal(2, undo.UndoCount);

		undo.Undo();
		Assert.Equal("2", doc.Title);

		undo.Undo();
		Assert.Equal("1", doc.Title);

		Assert.False(undo.CanUndo);
	}

	[Fact]
	public void DirtyTrackingTracksSavedBookmarkAcrossUndoRedoAndCoalescing()
	{
		ManualTimeProvider clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		UndoRedoManager undo = new(timeProvider: clock);
		TestDocument doc = new() { Title = "A", Zoom = 1.0 };
		undo.Attach(doc);

		Assert.False(undo.IsDirty);

		doc.Title = "B";
		Assert.True(undo.IsDirty);

		undo.Undo();
		Assert.False(undo.IsDirty);

		undo.Redo();
		Assert.True(undo.IsDirty);

		undo.MarkSaved();
		Assert.False(undo.IsDirty);

		doc.Title = "C";
		Assert.True(undo.IsDirty);

		undo.Undo();
		Assert.False(undo.IsDirty);

		// Undoing past the saved state makes it dirty again
		undo.Undo();
		Assert.True(undo.IsDirty);

		// Redoing back to saved state makes it clean again
		undo.Redo();
		Assert.False(undo.IsDirty);

		// Coalesced edit mutating the saved action invalidates the saved marker
		doc.Zoom = 2.0;
		undo.MarkSaved();
		Assert.False(undo.IsDirty);

		clock.Advance(TimeSpan.FromMilliseconds(50));
		doc.Zoom = 2.5;
		Assert.True(undo.IsDirty);
	}

	[Fact]
	public void UndoableCollectionTracksAddInsertRemoveReplaceMoveClear()
	{
		UndoRedoManager undo = new();
		UndoableCollection<string> list = new(undo);

		list.Add("A");
		list.Add("B");
		list.Insert(1, "Mid");
		Assert.Equal(["A", "Mid", "B"], list);

		list[1] = "Center";
		Assert.Equal(["A", "Center", "B"], list);

		list.Move(2, 0);
		Assert.Equal(["B", "A", "Center"], list);

		list.Remove("A");
		Assert.Equal(["B", "Center"], list);

		list.Clear();
		Assert.Empty(list);

		// Undo Clear
		undo.Undo();
		Assert.Equal(["B", "Center"], list);

		// Undo Remove
		undo.Undo();
		Assert.Equal(["B", "A", "Center"], list);

		// Undo Move
		undo.Undo();
		Assert.Equal(["A", "Center", "B"], list);

		// Undo Replace
		undo.Undo();
		Assert.Equal(["A", "Mid", "B"], list);

		// Undo Insert
		undo.Undo();
		Assert.Equal(["A", "B"], list);

		// Undo Add("B") and Add("A")
		undo.Undo();
		undo.Undo();
		Assert.Empty(list);

		// Redo all the way back to Clear
		while (undo.CanRedo)
			undo.Redo();

		Assert.Empty(list);
	}

	[Fact]
	public void ObjectGraphCascadesAttachDetachAndReattachAcrossCollectionsAndProperties()
	{
		UndoRedoManager undo = new();
		TestDocument doc = new() { Title = "Doc" };
		TestLayer layer = new() { Name = "Layer 1" };
		TestShape shape = new() { X = 10 };

		layer.Shapes.Add(shape);
		doc.Layers.Add(layer);

		// Nothing recorded before attaching!
		Assert.Equal(0, undo.UndoCount);
		Assert.Null(shape.UndoRedo);

		// Attaching root cascades all the way down to Shape
		undo.Attach(doc);
		Assert.Same(undo, layer.UndoRedo);
		Assert.Same(undo, shape.UndoRedo);

		// Deep edit is recorded
		shape.X = 99;
		Assert.Equal(1, undo.UndoCount);

		// Removing layer detaches it and its child shapes
		doc.Layers.Remove(layer);
		Assert.Null(layer.UndoRedo);
		Assert.Null(shape.UndoRedo);
		Assert.Equal(2, undo.UndoCount);

		// Mutating detached node does NOT pollute undo stack
		shape.X = 500;
		Assert.Equal(2, undo.UndoCount);

		// Undoing removal re-attaches layer and its child shapes
		undo.Undo();
		Assert.Same(undo, layer.UndoRedo);
		Assert.Same(undo, shape.UndoRedo);

		// Property-based child node replacement detaches old and attaches new
		TestLayer active1 = new() { Name = "Active 1" };
		TestLayer active2 = new() { Name = "Active 2" };
		doc.ActiveLayer = active1;
		Assert.Same(undo, active1.UndoRedo);

		doc.ActiveLayer = active2;
		Assert.Null(active1.UndoRedo);
		Assert.Same(undo, active2.UndoRedo);

		undo.Undo();
		Assert.Same(active1, doc.ActiveLayer);
		Assert.Same(undo, active1.UndoRedo);
		Assert.Null(active2.UndoRedo);
	}

	private sealed class ManualTimeProvider : TimeProvider
	{
		private DateTimeOffset now;

		public ManualTimeProvider(DateTimeOffset start)
		{
			now = start;
		}

		public override DateTimeOffset GetUtcNow() => now;

		public void Advance(TimeSpan delta) => now += delta;
	}
}

[Undoable]
public partial class TestDocument : ObservableObject
{
	[ObservableProperty]
	[Undoable(Description = "Rename Document")]
	public partial string Title { get; set; } = string.Empty;

	[ObservableProperty]
	[Undoable(MergeTimeoutMs = 300)]
	public partial double Zoom { get; set; } = 1.0;

	[ObservableProperty]
	[UndoableIgnore]
	public partial bool IsSelected { get; set; }

	[ObservableProperty]
	public partial TestLayer? ActiveLayer { get; set; }

	public UndoableCollection<TestLayer> Layers { get; } = [];
}

[Undoable]
public partial class TestLayer : ObservableObject
{
	[ObservableProperty]
	public partial string Name { get; set; } = string.Empty;

	public UndoableCollection<TestShape> Shapes { get; } = [];
}

[Undoable]
public partial class TestShape : ObservableObject
{
	[ObservableProperty]
	public partial double X { get; set; }
}

public partial class OptInViewModel : ObservableObject
{
	[ObservableProperty]
	[Undoable]
	public partial string Tracked { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Untracked { get; set; } = string.Empty;
}
