namespace DotNetElements.UndoRedo.Actions;

/// <summary>
/// Groups multiple <see cref="IUndoableAction"/> instances into a single atomic undo/redo step.
/// </summary>
public sealed class CompositeAction : IUndoableAction
{
	/// <summary>
	/// Gets the description of the composite action.
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// Gets the number of actions contained in this composite action.
	/// </summary>
	public int Count => actions.Count;

	private readonly IReadOnlyList<IUndoableAction> actions;

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositeAction"/> class.
	/// </summary>
	/// <param name="description">The description of the composite action.</param>
	/// <param name="actions">The actions to group, executed in order on redo and in reverse order on undo.</param>
	public CompositeAction(string description, IReadOnlyList<IUndoableAction> actions)
	{
		ArgumentNullException.ThrowIfNull(actions);

		Description = description;

		this.actions = actions;
	}

	/// <summary>
	/// Undoes all contained actions in reverse order.
	/// </summary>
	public void Undo()
	{
		for (int i = actions.Count - 1; i >= 0; i--)
			actions[i].Undo();
	}

	/// <summary>
	/// Redoes all contained actions in their original order.
	/// </summary>
	public void Redo()
	{
		for (int i = 0; i < actions.Count; i++)
			actions[i].Redo();
	}
}
