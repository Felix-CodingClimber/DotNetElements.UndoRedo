using System.Collections.ObjectModel;
using DotNetElements.UndoRedo.Actions;

namespace DotNetElements.UndoRedo;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that automatically records mutations into an
/// <see cref="UndoRedoManager"/> and propagates <see cref="IUndoableNode.UndoRedo"/> to child items.
/// </summary>
public class UndoableCollection<T> : ObservableCollection<T>, IUndoableNode
{
	/// <summary>
	/// Gets or sets the <see cref="UndoRedoManager"/> for this collection.
	/// Updating this property cascades the manager to all <see cref="IUndoableNode"/> items in the collection.
	/// </summary>
	public UndoRedoManager? UndoRedo
	{
		get => undoRedo;
		set
		{
			if (ReferenceEquals(undoRedo, value))
				return;

			undoRedo = value;

			foreach (var item in Items)
			{
				if (item is IUndoableNode node)
					node.UndoRedo = value;
			}
		}
	}

	private UndoRedoManager? undoRedo;

	public UndoableCollection(UndoRedoManager? undoRedo = null)
	{
		this.undoRedo = undoRedo;
	}

	public UndoableCollection(IEnumerable<T> items, UndoRedoManager? undoRedo = null) : base(items)
	{
		UndoRedo = undoRedo;
	}

	protected override void InsertItem(int index, T item)
	{
		base.InsertItem(index, item);

		AttachNode(item);

		undoRedo?.Record(new DelegateUndoableAction(
			description: $"Add {typeof(T).Name}",
			undo: () => RemoveAt(index),
			redo: () => Insert(index, item)));
	}

	protected override void RemoveItem(int index)
	{
		T removed = Items[index];

		base.RemoveItem(index);

		DetachNode(removed);

		undoRedo?.Record(new DelegateUndoableAction(
			description: $"Remove {typeof(T).Name}",
			undo: () => Insert(index, removed),
			redo: () => RemoveAt(index)));
	}

	protected override void SetItem(int index, T item)
	{
		T oldItem = Items[index];

		base.SetItem(index, item);

		if (!ReferenceEquals(oldItem, item))
		{
			DetachNode(oldItem);
			AttachNode(item);
		}

		undoRedo?.Record(new DelegateUndoableAction(
			description: $"Replace {typeof(T).Name}",
			undo: () => this[index] = oldItem,
			redo: () => this[index] = item));
	}

	protected override void MoveItem(int oldIndex, int newIndex)
	{
		base.MoveItem(oldIndex, newIndex);

		if (oldIndex == newIndex)
			return;

		undoRedo?.Record(new DelegateUndoableAction(
			description: $"Move {typeof(T).Name}",
			undo: () => Move(newIndex, oldIndex),
			redo: () => Move(oldIndex, newIndex)));
	}

	protected override void ClearItems()
	{
		if (Items.Count == 0)
		{
			base.ClearItems();
			return;
		}

		List<T> snapshot = [.. Items];

		base.ClearItems();

		foreach (T item in snapshot)
			DetachNode(item);

		undoRedo?.Record(new DelegateUndoableAction(
			description: $"Clear {typeof(T).Name}s",
			undo: () =>
			{
				foreach (T item in snapshot)
					Add(item); // todo here we could implement AddRange to avoid multiple notifications
			},
			redo: Clear));
	}

	private void AttachNode(T item)
	{
		if (item is IUndoableNode node)
			node.UndoRedo = undoRedo;
	}

	private static void DetachNode(T item)
	{
		if (item is IUndoableNode node)
			node.UndoRedo = null;
	}
}
