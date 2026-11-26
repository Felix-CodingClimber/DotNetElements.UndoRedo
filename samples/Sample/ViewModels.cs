using CommunityToolkit.Mvvm.ComponentModel;

namespace DotNetElements.UndoRedo.Sample;

public partial class DocumentViewModel : ObservableObject
{
	[ObservableProperty]
	[Undoable(Description = "Rename Document")]
	public partial string Title { get; set; } = string.Empty;

	[ObservableProperty]
	[Undoable(MergeTimeoutMs = 300)]
	public partial double Zoom { get; set; } = 1.0;

	[ObservableProperty]
	[UndoableIgnore]
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
	public partial string Label { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double X { get; set; }

	[ObservableProperty]
	public partial double Y { get; set; }
}
