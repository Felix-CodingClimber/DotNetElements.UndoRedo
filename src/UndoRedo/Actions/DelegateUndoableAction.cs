namespace DotNetElements.UndoRedo.Actions;

/// <summary>
/// A general-purpose <see cref="IUndoableAction"/> backed by undo and redo delegates.
/// </summary>
public sealed class DelegateUndoableAction : IUndoableAction
{
	public string Description { get; }

	private readonly Action undo;
	private readonly Action redo;

	public DelegateUndoableAction(string description, Action undo, Action redo)
	{
		ArgumentNullException.ThrowIfNull(undo);
		ArgumentNullException.ThrowIfNull(redo);

		Description = description;

		this.undo = undo;
		this.redo = redo;
	}

	public void Undo() => undo();

	public void Redo() => redo();
}
