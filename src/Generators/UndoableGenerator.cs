using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetElements.UndoRedo.Generators;

[Generator]
public sealed class UndoableGenerator : IIncrementalGenerator
{
	private const string UndoableAttributeName = "DotNetElements.UndoRedo.UndoableAttribute";
	private const string UndoableIgnoreAttributeName = "DotNetElements.UndoRedo.UndoableIgnoreAttribute";
	private const string ObservablePropertyAttributeName = "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute";
	private const string UndoableNodeInterfaceName = "DotNetElements.UndoRedo.IUndoableNode";

	private static readonly SymbolDisplayFormat FullTypeFormat =
		SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
			SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
			SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<UndoableClassModel?> candidateClasses = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => IsCandidateClass(node),
				transform: static (ctx, ct) => TransformClass(ctx, ct))
			.Where(static model => model is not null);

		context.RegisterSourceOutput(candidateClasses, static (spc, model) =>
		{
			if (model is null)
				return;

			string source = EmitSource(model);
			spc.AddSource($"{model.HintName}.Undoable.g.cs", SourceText.From(source, Encoding.UTF8));
		});
	}

	private static bool IsCandidateClass(SyntaxNode node)
	{
		if (node is not ClassDeclarationSyntax classDecl)
			return false;

		if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
			return false;

		if (classDecl.AttributeLists.Count > 0)
			return true;

		foreach (MemberDeclarationSyntax member in classDecl.Members)
		{
			if (member is PropertyDeclarationSyntax prop && prop.AttributeLists.Count > 0)
				return true;
		}

		return false;
	}

	private static UndoableClassModel? TransformClass(GeneratorSyntaxContext context, CancellationToken ct)
	{
		ClassDeclarationSyntax classDecl = (ClassDeclarationSyntax)context.Node;

		if (context.SemanticModel.GetDeclaredSymbol(classDecl, ct) is not INamedTypeSymbol classSymbol)
			return null;

		// Only emit once for the first syntax declaration of a partial class
		Location? firstLocation = classSymbol.Locations.FirstOrDefault();

		if (firstLocation is not null && firstLocation.SourceTree != classDecl.SyntaxTree)
			return null;

		AttributeData? classUndoableAttr = GetAttribute(classSymbol, UndoableAttributeName);
		bool isClassUndoable = classUndoableAttr is not null;
		int classMergeTimeout = GetNamedIntArg(classUndoableAttr, "MergeTimeoutMs");

		ImmutableArray<TrackedPropertyModel>.Builder trackedProperties = ImmutableArray.CreateBuilder<TrackedPropertyModel>();
		ImmutableArray<string>.Builder cascadeProperties = ImmutableArray.CreateBuilder<string>();
		bool hasManualUndoRedoProperty = false;

		foreach (ISymbol member in classSymbol.GetMembers())
		{
			if (member is not IPropertySymbol prop || prop.IsStatic || prop.IsIndexer)
				continue;

			if (prop.Name == "UndoRedo")
			{
				hasManualUndoRedoProperty = true;
				continue;
			}

			// Collect child properties that should receive cascading UndoRedo assignment
			if (prop.GetMethod is not null && CanBeUndoableNode(prop.Type))
				cascadeProperties.Add(prop.Name);

			bool hasObservableProp = GetAttribute(prop, ObservablePropertyAttributeName) is not null;

			if (!hasObservableProp)
				continue;

			if (GetAttribute(prop, UndoableIgnoreAttributeName) is not null)
				continue;

			AttributeData? propUndoableAttr = GetAttribute(prop, UndoableAttributeName);

			if (!isClassUndoable && propUndoableAttr is null)
				continue;

			string description = GetNamedStringArg(propUndoableAttr, "Description") ?? $"Change {prop.Name}";
			int mergeTimeoutMs = HasNamedArg(propUndoableAttr, "MergeTimeoutMs")
				? GetNamedIntArg(propUndoableAttr, "MergeTimeoutMs")
				: classMergeTimeout;

			string propType = prop.Type.ToDisplayString(FullTypeFormat);
			bool mightBeUndoableNode = CanBeUndoableNode(prop.Type);

			trackedProperties.Add(new TrackedPropertyModel(
				Name: prop.Name,
				PropertyType: propType,
				Description: description,
				MergeTimeoutMs: mergeTimeoutMs,
				MightBeUndoableNode: mightBeUndoableNode));
		}

		if (!isClassUndoable && trackedProperties.Count == 0)
			return null;

		string? @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
			? null
			: classSymbol.ContainingNamespace.ToDisplayString();

		ImmutableArray<ContainingTypeModel>.Builder containingTypes = ImmutableArray.CreateBuilder<ContainingTypeModel>();
		INamedTypeSymbol? current = classSymbol.ContainingType;

		while (current is not null)
		{
			containingTypes.Insert(0, new ContainingTypeModel(GetTypeKeyword(current), current.Name + GetTypeParameters(current)));
			current = current.ContainingType;
		}

		string typeNameWithParams = classSymbol.Name + GetTypeParameters(classSymbol);
		string hintName = (@namespace is null ? typeNameWithParams : $"{@namespace}.{typeNameWithParams}")
			.Replace('<', '_')
			.Replace('>', '_')
			.Replace(',', '_')
			.Replace(' ', '_');

		return new UndoableClassModel(
			Namespace: @namespace,
			ContainingTypes: containingTypes.ToImmutable(),
			ClassName: typeNameWithParams,
			HintName: hintName,
			GenerateUndoRedoProperty: !hasManualUndoRedoProperty,
			CascadeProperties: cascadeProperties.ToImmutable(),
			TrackedProperties: trackedProperties.ToImmutable());
	}

	private static bool CanBeUndoableNode(ITypeSymbol type)
	{
		if (type.IsValueType || type.SpecialType == SpecialType.System_String)
			return false;

		if (type.ToDisplayString() == UndoableNodeInterfaceName)
			return true;

		foreach (INamedTypeSymbol iface in type.AllInterfaces)
		{
			if (iface.ToDisplayString() == UndoableNodeInterfaceName)
				return true;
		}

		// Another [Undoable] class in the same compilation will also implement IUndoableNode after generation.
		if (GetAttribute(type, UndoableAttributeName) is not null)
			return true;

		return false;
	}

	private static AttributeData? GetAttribute(ISymbol symbol, string fullName)
	{
		foreach (AttributeData attr in symbol.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() == fullName)
				return attr;
		}

		return null;
	}

	private static bool HasNamedArg(AttributeData? attr, string name)
	{
		if (attr is null)
			return false;

		foreach (KeyValuePair<string, TypedConstant> arg in attr.NamedArguments)
		{
			if (arg.Key == name)
				return true;
		}

		return false;
	}

	private static string? GetNamedStringArg(AttributeData? attr, string name)
	{
		if (attr is null)
			return null;

		foreach (KeyValuePair<string, TypedConstant> arg in attr.NamedArguments)
		{
			if (arg.Key == name && arg.Value.Value is string s)
				return s;
		}

		return null;
	}

	private static int GetNamedIntArg(AttributeData? attr, string name)
	{
		if (attr is null)
			return 0;

		foreach (KeyValuePair<string, TypedConstant> arg in attr.NamedArguments)
		{
			if (arg.Key == name && arg.Value.Value is int i)
				return i;
		}

		return 0;
	}

	private static string GetTypeKeyword(INamedTypeSymbol type) => type.TypeKind switch
	{
		TypeKind.Struct => "struct",
		TypeKind.Interface => "interface",
		_ => "class"
	};

	private static string GetTypeParameters(INamedTypeSymbol type)
	{
		if (type.TypeParameters.Length == 0)
			return string.Empty;

		return "<" + string.Join(", ", type.TypeParameters.Select(tp => tp.Name)) + ">";
	}

	private static string EmitSource(UndoableClassModel model)
	{
		StringBuilder sb = new();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();

		if (model.Namespace is not null)
		{
			sb.AppendLine($"namespace {model.Namespace};");
			sb.AppendLine();
		}

		int indentLevel = 0;
		foreach (ContainingTypeModel container in model.ContainingTypes)
		{
			AppendIndent(sb, indentLevel);
			sb.AppendLine($"partial {container.Keyword} {container.Name}");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("{");
			indentLevel++;
		}

		AppendIndent(sb, indentLevel);
		string baseList = model.GenerateUndoRedoProperty
			? " : global::DotNetElements.UndoRedo.IUndoableNode"
			: string.Empty;
		sb.AppendLine($"partial class {model.ClassName}{baseList}");
		AppendIndent(sb, indentLevel);
		sb.AppendLine("{");
		indentLevel++;

		if (model.GenerateUndoRedoProperty)
		{
			AppendIndent(sb, indentLevel);
			sb.AppendLine("private global::DotNetElements.UndoRedo.UndoRedoManager? __undoRedo;");
			sb.AppendLine();

			AppendIndent(sb, indentLevel);
			sb.AppendLine("public global::DotNetElements.UndoRedo.UndoRedoManager? UndoRedo");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("{");
			indentLevel++;

			AppendIndent(sb, indentLevel);
			sb.AppendLine("get => __undoRedo;");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("set");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("{");
			indentLevel++;

			AppendIndent(sb, indentLevel);
			sb.AppendLine("if (global::System.Object.ReferenceEquals(__undoRedo, value)) return;");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("__undoRedo = value;");

			foreach (var childProp in model.CascadeProperties)
			{
				AppendIndent(sb, indentLevel);
				sb.AppendLine($"if (this.{childProp} is global::DotNetElements.UndoRedo.IUndoableNode __node_{childProp})");
				AppendIndent(sb, indentLevel);
				sb.AppendLine("{");
				AppendIndent(sb, indentLevel + 1);
				sb.AppendLine($"__node_{childProp}.UndoRedo = value;");
				AppendIndent(sb, indentLevel);
				sb.AppendLine("}");
			}

			indentLevel--;
			AppendIndent(sb, indentLevel);
			sb.AppendLine("}");

			indentLevel--;
			AppendIndent(sb, indentLevel);
			sb.AppendLine("}");
		}

		foreach (TrackedPropertyModel prop in model.TrackedProperties)
		{
			sb.AppendLine();
			AppendIndent(sb, indentLevel);
			sb.AppendLine($"private {prop.PropertyType} __prev_{prop.Name} = default!;");
			sb.AppendLine();
			AppendIndent(sb, indentLevel);
			sb.AppendLine($"partial void On{prop.Name}Changing({prop.PropertyType} value)");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("{");
			AppendIndent(sb, indentLevel + 1);
			sb.AppendLine($"__prev_{prop.Name} = this.{prop.Name};");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("}");
			sb.AppendLine();
			AppendIndent(sb, indentLevel);
			sb.AppendLine($"partial void On{prop.Name}Changed({prop.PropertyType} value)");
			AppendIndent(sb, indentLevel);
			sb.AppendLine("{");
			indentLevel++;

			AppendIndent(sb, indentLevel);
			sb.AppendLine($"var oldValue = __prev_{prop.Name};");
			AppendIndent(sb, indentLevel);
			sb.AppendLine($"__prev_{prop.Name} = default!;");

			if (prop.MightBeUndoableNode)
			{
				AppendIndent(sb, indentLevel);
				sb.AppendLine("if (oldValue is global::DotNetElements.UndoRedo.IUndoableNode __oldNode && !global::System.Object.ReferenceEquals(oldValue, value))");
				AppendIndent(sb, indentLevel);
				sb.AppendLine("{");
				AppendIndent(sb, indentLevel + 1);
				sb.AppendLine("__oldNode.UndoRedo = null;");
				AppendIndent(sb, indentLevel);
				sb.AppendLine("}");

				AppendIndent(sb, indentLevel);
				sb.AppendLine("if (value is global::DotNetElements.UndoRedo.IUndoableNode __newNode)");
				AppendIndent(sb, indentLevel);
				sb.AppendLine("{");
				AppendIndent(sb, indentLevel + 1);
				sb.AppendLine("__newNode.UndoRedo = this.UndoRedo;");
				AppendIndent(sb, indentLevel);
				sb.AppendLine("}");
			}

			string escapedDesc = SymbolDisplay.FormatLiteral(prop.Description, quote: true);

			AppendIndent(sb, indentLevel);
			sb.AppendLine($"this.UndoRedo?.RecordPropertyChange<{prop.PropertyType}>(");
			AppendIndent(sb, indentLevel + 1);
			sb.AppendLine("oldValue,");
			AppendIndent(sb, indentLevel + 1);
			sb.AppendLine("value,");
			AppendIndent(sb, indentLevel + 1);
			sb.AppendLine($"v => this.{prop.Name} = v,");
			AppendIndent(sb, indentLevel + 1);
			sb.AppendLine($"description: {escapedDesc},");
			AppendIndent(sb, indentLevel + 1);
			sb.AppendLine($"mergeKey: (this, nameof({prop.Name})),");
			AppendIndent(sb, indentLevel + 1);
			sb.AppendLine($"mergeTimeoutMs: {prop.MergeTimeoutMs});");

			indentLevel--;
			AppendIndent(sb, indentLevel);
			sb.AppendLine("}");
		}

		indentLevel--;
		AppendIndent(sb, indentLevel);
		sb.AppendLine("}");

		for (int i = 0; i < model.ContainingTypes.Length; i++)
		{
			indentLevel--;
			AppendIndent(sb, indentLevel);
			sb.AppendLine("}");
		}

		return sb.ToString();
	}

	private static void AppendIndent(StringBuilder sb, int level)
	{
		for (int i = 0; i < level; i++)
			sb.Append("    ");
	}

	private sealed record ContainingTypeModel(string Keyword, string Name);

	private sealed record TrackedPropertyModel(
		string Name,
		string PropertyType,
		string Description,
		int MergeTimeoutMs,
		bool MightBeUndoableNode);

	private sealed record UndoableClassModel(
		string? Namespace,
		ImmutableArray<ContainingTypeModel> ContainingTypes,
		string ClassName,
		string HintName,
		bool GenerateUndoRedoProperty,
		ImmutableArray<string> CascadeProperties,
		ImmutableArray<TrackedPropertyModel> TrackedProperties);
}
