namespace DotNetElements.UndoRedo.Actions;

/// <summary>
/// A general-purpose <see cref="IUndoableAction"/> backed by undo and redo delegates.
/// </summary>
public sealed class DelegateUndoableAction : IUndoableAction
{
	/// <summary>
	/// Gets the description of the action.
	/// </summary>
	public string Description { get; }

	private readonly Action undo;
	private readonly Action redo;

	/// <summary>
	/// Initializes a new instance of the <see cref="DelegateUndoableAction"/> class.
	/// </summary>
	/// <param name="description">The description of the action.</param>
	/// <param name="undo">The delegate that reverts the change.</param>
	/// <param name="redo">The delegate that re-applies the change.</param>
	public DelegateUndoableAction(string description, Action undo, Action redo)
	{
		ArgumentNullException.ThrowIfNull(undo);
		ArgumentNullException.ThrowIfNull(redo);

		Description = description;

		this.undo = undo;
		this.redo = redo;
	}

	/// <summary>
	/// Invokes the undo delegate.
	/// </summary>
	public void Undo() => undo();

	/// <summary>
	/// Invokes the redo delegate.
	/// </summary>
	public void Redo() => redo();
}
