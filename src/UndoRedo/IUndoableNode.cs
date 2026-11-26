namespace DotNetElements.UndoRedo;

/// <summary>
/// Implemented by view models and collections that participate in automatic
/// <see cref="UndoRedoManager"/> propagation across an object graph.
/// </summary>
public interface IUndoableNode
{
	/// <summary>
	/// Gets or sets the <see cref="UndoRedoManager"/> attached to this node.
	/// Setting this property cascades the manager to child nodes and collections.
	/// </summary>
	UndoRedoManager? UndoRedo { get; set; }
}
