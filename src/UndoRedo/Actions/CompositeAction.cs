namespace DotNetElements.UndoRedo.Actions;

/// <summary>
/// Groups multiple <see cref="IUndoableAction"/> instances into a single atomic undo/redo step.
/// </summary>
public sealed class CompositeAction : IUndoableAction
{
	public string Description { get; }

	public int Count => actions.Count;

	private readonly IReadOnlyList<IUndoableAction> actions;

	public CompositeAction(string description, IReadOnlyList<IUndoableAction> actions)
	{
		ArgumentNullException.ThrowIfNull(actions);

		Description = description;

		this.actions = actions;
	}

	public void Undo()
	{
		for (int i = actions.Count - 1; i >= 0; i--)
			actions[i].Undo();
	}

	public void Redo()
	{
		for (int i = 0; i < actions.Count; i++)
			actions[i].Redo();
	}
}
