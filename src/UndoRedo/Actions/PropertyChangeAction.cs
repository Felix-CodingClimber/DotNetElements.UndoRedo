namespace DotNetElements.UndoRedo.Actions;

/// <summary>
/// Records a strongly-typed property change with optional time-window coalescing.
/// </summary>
public sealed class PropertyChangeAction<T> : IUndoableAction
{
	public string Description { get; }

	private readonly T oldValue;
	private T newValue;
	private readonly Action<T> setter;
	private readonly object? mergeKey;
	private readonly TimeSpan mergeWindow;
	private DateTimeOffset timestamp;

	public PropertyChangeAction(
		T oldValue,
		T newValue,
		Action<T> setter,
		string description,
		object? mergeKey = null,
		TimeSpan mergeWindow = default,
		DateTimeOffset timestamp = default)
	{
		ArgumentNullException.ThrowIfNull(setter);

		Description = description;

		this.oldValue = oldValue;
		this.newValue = newValue;
		this.setter = setter;
		this.mergeKey = mergeKey;
		this.mergeWindow = mergeWindow;
		this.timestamp = timestamp;
	}

	public void Undo() => setter(oldValue);

	public void Redo() => setter(newValue);

	public bool TryMerge(IUndoableAction next)
	{
		if (mergeKey is null || mergeWindow <= TimeSpan.Zero)
			return false;

		if (next is not PropertyChangeAction<T> other)
			return false;

		if (!Equals(mergeKey, other.mergeKey))
			return false;

		if (other.timestamp - timestamp > mergeWindow)
			return false;

		newValue = other.newValue;
		timestamp = other.timestamp;

		return true;
	}
}
