namespace DotNetElements.UndoRedo;

/// <summary>
/// Excludes a specific <c>[ObservableProperty]</c> from automatic undo/redo tracking
/// inside an <see cref="UndoableAttribute"/>-decorated class.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class UndoableIgnoreAttribute : Attribute;
