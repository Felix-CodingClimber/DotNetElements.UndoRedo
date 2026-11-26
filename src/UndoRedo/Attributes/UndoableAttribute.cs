namespace DotNetElements.UndoRedo;

/// <summary>
/// Marks a partial class or an <c>[ObservableProperty]</c> partial property for automatic
/// undo/redo source generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class UndoableAttribute : Attribute
{
	/// <summary>
	/// Optional human-readable description for changes to the decorated property.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Optional coalescing window in milliseconds. Consecutive changes to the same property
	/// within this window are merged into a single undo entry.
	/// </summary>
	public int MergeTimeoutMs { get; set; }
}
