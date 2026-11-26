namespace DotNetElements.UndoRedo.Actions;

/// <summary>
/// Records a strongly-typed property change with optional time-window coalescing.
/// </summary>
/// <typeparam name="T">The type of the changed property.</typeparam>
public sealed class PropertyChangeAction<T> : IUndoableAction
{
	/// <summary>
	/// Gets the description of the property change.
	/// </summary>
	public string Description { get; }

	private readonly T oldValue;
	private T newValue;
	private readonly Action<T> setter;
	private readonly object? mergeKey;
	private readonly TimeSpan mergeWindow;
	private DateTimeOffset timestamp;

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyChangeAction{T}"/> class.
	/// </summary>
	/// <param name="oldValue">The property value before the change.</param>
	/// <param name="newValue">The property value after the change.</param>
	/// <param name="setter">The delegate used to restore <paramref name="oldValue"/> or re-apply <paramref name="newValue"/>.</param>
	/// <param name="description">The description of the property change.</param>
	/// <param name="mergeKey">An optional key used to decide whether consecutive changes may be coalesced.</param>
	/// <param name="mergeWindow">The time window within which consecutive changes may be coalesced.</param>
	/// <param name="timestamp">The UTC timestamp of the change, used to evaluate <paramref name="mergeWindow"/>.</param>
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

	/// <summary>
	/// Restores the property to its value before the change.
	/// </summary>
	public void Undo() => setter(oldValue);

	/// <summary>
	/// Re-applies the property value after the change.
	/// </summary>
	public void Redo() => setter(newValue);

	/// <summary>
	/// Attempts to coalesce <paramref name="nextAction"/> into this action when it targets the same
	/// property and falls within the configured merge window.
	/// </summary>
	/// <param name="nextAction">The newer action to merge.</param>
	/// <returns><see langword="true"/> if merged; otherwise, <see langword="false"/>.</returns>
	public bool TryMerge(IUndoableAction nextAction)
	{
		if (mergeKey is null || mergeWindow <= TimeSpan.Zero)
			return false;

		if (nextAction is not PropertyChangeAction<T> other)
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
