namespace DotNetElements.UndoRedo;

/// <summary>
/// Represents a single reversible unit of work in an <see cref="UndoRedoManager"/>.
/// </summary>
public interface IUndoableAction
{
	/// <summary>
	/// Gets a human-readable description of the action (for UI tooltips, menus, or history lists).
	/// </summary>
	string Description { get; }

	/// <summary>
	/// Reverts the change performed by this action.
	/// </summary>
	void Undo();

	/// <summary>
	/// Re-applies the change performed by this action.
	/// </summary>
	void Redo();

	/// <summary>
	/// Attempts to coalesce <paramref name="next"/> into this action.
	/// Returns <see langword="true"/> if merged (in which case <paramref name="next"/> is discarded).
	/// </summary>
	bool TryMerge(IUndoableAction next) => false;
}
