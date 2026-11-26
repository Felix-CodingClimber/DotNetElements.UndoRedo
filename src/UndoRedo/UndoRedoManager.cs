using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotNetElements.UndoRedo.Actions;

namespace DotNetElements.UndoRedo;

/// <summary>
/// Manages undo/redo history, transactions, action coalescing, dirty-state tracking,
/// and MVVM commands suitable for direct binding in AvaloniaUI.
/// </summary>
public sealed partial class UndoRedoManager : ObservableObject
{
	/// <summary>
	/// Gets the maximum number of actions retained in the undo stack. <c>0</c> or less means unlimited.
	/// </summary>
	public int MaxCapacity { get; private init; }

	/// <summary>
	/// Gets whether an undo or redo operation is currently executing.
	/// </summary>
	public bool IsRestoring => isRestoring;

	/// <summary>
	/// Gets whether history recording is currently suspended via <see cref="Suspend"/>.
	/// </summary>
	public bool IsSuspended => suspendDepth > 0;

	/// <summary>
	/// Gets whether a transaction scope is currently active.
	/// </summary>
	public bool IsInTransaction => transactions.Count > 0;

	/// <summary>
	/// Gets whether there is at least one action available to undo.
	/// </summary>
	public bool CanUndo => undoStack.Count > 0 && !isRestoring;

	/// <summary>
	/// Gets whether there is at least one action available to redo.
	/// </summary>
	public bool CanRedo => redoStack.Count > 0 && !isRestoring;

	/// <summary>
	/// Gets the number of actions currently in the undo stack.
	/// </summary>
	public int UndoCount => undoStack.Count;

	/// <summary>
	/// Gets the number of actions currently in the redo stack.
	/// </summary>
	public int RedoCount => redoStack.Count;

	/// <summary>
	/// Gets the description of the next action that will be undone, or <see langword="null"/> if none.
	/// </summary>
	public string? NextUndoDescription => undoStack.Last?.Value.Description;

	/// <summary>
	/// Gets the description of the next action that will be redone, or <see langword="null"/> if none.
	/// </summary>
	public string? NextRedoDescription => redoStack.Count > 0 ? redoStack.Peek().Description : null;

	/// <summary>
	/// Gets whether the current state differs from the last saved state (or initial state if never saved).
	/// </summary>
	public bool IsDirty => !savedMarkerValid || !ReferenceEquals(undoStack.Last?.Value, savedMarker);

	private readonly LinkedList<IUndoableAction> undoStack = [];
	private readonly Stack<IUndoableAction> redoStack = [];
	private readonly Stack<TransactionFrame> transactions = [];
	private readonly TimeProvider timeProvider;

	private IUndoableAction? savedMarker;
	private bool savedMarkerValid = true;
	private int suspendDepth;
	private bool isRestoring;

	/// <summary>
	/// Creates a new <see cref="UndoRedoManager"/>.
	/// </summary>
	/// <param name="maxCapacity">
	/// Maximum number of undo steps to retain. Use <c>0</c> or a negative value for unlimited history.
	/// </param>
	/// <param name="timeProvider">
	/// Optional <see cref="TimeProvider"/> used for action coalescing windows. Defaults to <see cref="TimeProvider.System"/>.
	/// </param>
	public UndoRedoManager(int maxCapacity = 0, TimeProvider? timeProvider = null)
	{
		MaxCapacity = maxCapacity;

		this.timeProvider = timeProvider ?? TimeProvider.System;
	}

	/// <summary>
	/// Attaches this manager to a root <see cref="IUndoableNode"/>, cascading to its object graph.
	/// </summary>
	public void Attach(IUndoableNode root)
	{
		ArgumentNullException.ThrowIfNull(root);

		root.UndoRedo = this;
	}

	/// <summary>
	/// Records an already-executed <see cref="IUndoableAction"/> into the history.
	/// Ignored while <see cref="IsRestoring"/> or <see cref="IsSuspended"/> is <see langword="true"/>.
	/// </summary>
	public void Record(IUndoableAction action)
	{
		ArgumentNullException.ThrowIfNull(action);

		if (isRestoring || suspendDepth > 0)
			return;

		if (transactions.Count > 0)
		{
			transactions.Peek().Actions.Add(action);
			return;
		}

		CommitAction(action);
	}

	/// <summary>
	/// Records a strongly-typed property change.
	/// </summary>
	/// <typeparam name="T">The type of the changed property.</typeparam>
	/// <param name="oldValue">The property value before the change.</param>
	/// <param name="newValue">The property value after the change.</param>
	/// <param name="setter">The delegate used to restore <paramref name="oldValue"/> or re-apply <paramref name="newValue"/>.</param>
	/// <param name="description">The description of the property change.</param>
	/// <param name="mergeKey">An optional key used to decide whether consecutive changes may be coalesced.</param>
	/// <param name="mergeTimeoutMs">The time window in milliseconds within which consecutive changes may be coalesced.</param>
	public void RecordPropertyChange<T>(
		T oldValue,
		T newValue,
		Action<T> setter,
		string description,
		object? mergeKey = null,
		int mergeTimeoutMs = 0)
	{
		if (isRestoring || suspendDepth > 0)
			return;

		TimeSpan mergeWindow = mergeTimeoutMs > 0 ? TimeSpan.FromMilliseconds(mergeTimeoutMs) : TimeSpan.Zero;
		DateTimeOffset timestamp = mergeWindow > TimeSpan.Zero ? timeProvider.GetUtcNow() : default;

		PropertyChangeAction<T> action = new(
			oldValue,
			newValue,
			setter,
			description,
			mergeKey,
			mergeWindow,
			timestamp);

		Record(action);
	}

	/// <summary>
	/// Temporarily suspends recording until the returned token is disposed. Supports nesting.
	/// Useful when loading/hydrating view models.
	/// </summary>
	public IDisposable Suspend()
	{
		bool wasSuspended = IsSuspended;
		suspendDepth++;

		if (!wasSuspended)
			OnPropertyChanged(nameof(IsSuspended));

		return new DisposableScope(EndSuspend);
	}

	/// <summary>
	/// Begins a transaction that groups all recorded actions until disposed into a single undoable step.
	/// Supports nesting; nested transactions merge into the outermost transaction.
	/// </summary>
	public IDisposable BeginTransaction(string description)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(description);

		TransactionFrame frame = new(description);
		transactions.Push(frame);

		if (transactions.Count == 1)
			OnPropertyChanged(nameof(IsInTransaction));

		return new DisposableScope(() => EndTransaction(frame));
	}

	/// <summary>
	/// Undoes the most recent action on the undo stack.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanUndo))]
	public void Undo()
	{
		if (!CanUndo)
			return;

		IUndoableAction action = undoStack.Last!.Value;
		undoStack.RemoveLast();

		isRestoring = true;
		OnPropertyChanged(nameof(IsRestoring));
		try
		{
			action.Undo();
			redoStack.Push(action);
		}
		finally
		{
			isRestoring = false;

			OnPropertyChanged(nameof(IsRestoring));
			NotifyStateChanged();
		}
	}

	/// <summary>
	/// Redoes the most recently undone action on the redo stack.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanRedo))]
	public void Redo()
	{
		if (!CanRedo)
			return;

		IUndoableAction action = redoStack.Pop();

		isRestoring = true;
		OnPropertyChanged(nameof(IsRestoring));

		try
		{
			action.Redo();
			undoStack.AddLast(action);
		}
		finally
		{
			isRestoring = false;
			OnPropertyChanged(nameof(IsRestoring));
			NotifyStateChanged();
		}
	}

	/// <summary>
	/// Marks the current point in history as the clean (saved) state.
	/// </summary>
	public void MarkSaved()
	{
		savedMarker = undoStack.Last?.Value;
		savedMarkerValid = true;

		OnPropertyChanged(nameof(IsDirty));
	}

	/// <summary>
	/// Clears all undo and redo history.
	/// </summary>
	public void Clear()
	{
		undoStack.Clear();
		redoStack.Clear();
		savedMarker = null;
		savedMarkerValid = true;

		NotifyStateChanged();
	}

	private void CommitAction(IUndoableAction action)
	{
		redoStack.Clear();

		if (undoStack.Last is { Value: var lastAction } && lastAction.TryMerge(action))
		{
			if (savedMarkerValid && ReferenceEquals(lastAction, savedMarker))
				savedMarkerValid = false;

			NotifyStateChanged();
			return;
		}

		undoStack.AddLast(action);

		if (MaxCapacity > 0 && undoStack.Count > MaxCapacity)
		{
			IUndoableAction evicted = undoStack.First!.Value;
			undoStack.RemoveFirst();

			if (savedMarkerValid && (savedMarker is null || ReferenceEquals(evicted, savedMarker)))
				savedMarkerValid = false;
		}

		NotifyStateChanged();
	}

	private void EndSuspend()
	{
		if (suspendDepth > 0)
		{
			suspendDepth--;

			if (suspendDepth == 0)
				OnPropertyChanged(nameof(IsSuspended));
		}
	}

	private void EndTransaction(TransactionFrame frame)
	{
		if (transactions.Count == 0 || !ReferenceEquals(transactions.Peek(), frame))
			throw new InvalidOperationException("Transactions must be disposed in LIFO (nested) order.");

		transactions.Pop();

		if (transactions.Count > 0)
		{
			if (frame.Actions.Count > 0)
				transactions.Peek().Actions.AddRange(frame.Actions);

			return;
		}

		OnPropertyChanged(nameof(IsInTransaction));

		if (frame.Actions.Count > 0)
			CommitAction(new CompositeAction(frame.Description, frame.Actions));
	}

	private void NotifyStateChanged()
	{
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
		OnPropertyChanged(nameof(UndoCount));
		OnPropertyChanged(nameof(RedoCount));
		OnPropertyChanged(nameof(NextUndoDescription));
		OnPropertyChanged(nameof(NextRedoDescription));
		OnPropertyChanged(nameof(IsDirty));

		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
	}

	private sealed class TransactionFrame
	{
		public string Description { get; private init; }
		public List<IUndoableAction> Actions { get; private init; } = [];

		public TransactionFrame(string description)
		{
			Description = description;
		}
	}

	private sealed class DisposableScope : IDisposable
	{
		private Action? onDispose;

		public DisposableScope(Action onDispose)
		{
			this.onDispose = onDispose;
		}

		public void Dispose()
		{
			Interlocked.Exchange(ref onDispose, null)?.Invoke();
		}
	}
}
