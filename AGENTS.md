# Agent Instructions

# Build

- Use the dotnet CLI to build and run the project

## Project

- Use modern .NET 10 and C# 14 features
- Use established best practices for .NET development without premature over-engineering.
- Group code files by feature with optional subfolders. Project can have a Core folder with optional subfolders for non project specific things like extension or helper methods.
- Use the framework’s existing host and DI container for hosted applications. Prefer Generic Host for standalone applications that need configuration, logging, DI, or managed lifetime.
- For Avalonia UI code (`src/UI`, `src/App`, and plugin views), use AXAML views with compiled bindings (`x:DataType`) and `CommunityToolkit.Mvvm` ViewModels; keep ViewModels free of Avalonia UI types and use shared design tokens from `src/UI`.

## Example project structure

- `src/`: Contains the main application projects.
  - Per project structure:
    - `Core/`: Non feature-specific utilities, extensions, and helpers.
    - `Features/`: Grouped by feature with optional subfolders.
- `tests/`: Contains test projects.
- `samples/`: Contains sample projects or example usage of the main application.
- `docs/`: Contains DESIGN.md and PLAN.md.
- `README.md`: Project overview and instructions.

## Libraries to prefer if they are needed

- Use Microsoft.Extensions.Hosting as generic host.
- Use Microsoft.Extensions.DependencyInjection for DI.
- Use Microsoft.Extensions.Logging for Logging.
- Use Microsoft.Extensions.Options for settings.
- Use System.Text.Json instead of other Json Libraries.

## Code Style

- Avoid using `var` unless the explicit type would be excessively verbose.
- Use `nameof` for references to code symbols, not for stable JSON names, configuration keys, protocol fields, or other external contracts unless explicitly intended.
- Use `is not null` and `is null` instead of `!= null` and `== null`.
- Prefer type patterns for compatibility checks and casts. Use runtime type equality only when exact type identity is required.
- Prefer left-hand type declarations with right-hand target-typed construction, for example `MyClass myClass = new();`.
- Omit braces for `if`, `else`, `foreach`, `for`, and `while` statements whose body is a single simple statement.
- Keep method and constructor arguments on one line when reasonably readable. Split arguments across lines only when their number, length, or complexity would make the line difficult to read.
- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables, except private constants, which should use PascalCase.
- Do not prefix private fields with "\_".
- Prefix interface names with "I" (e.g., IUserService).
- Prefer file-scoped namespace declarations and single-line using directives.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer ?. if applicable (e.g. scope?.Dispose()).
- Types should be `internal` by default unless the architecture requires something else.
- Classes and record classes should be `sealed` by default unless the architecture requires something else.
- Do not use primary constructors for classes.
- Use records for value-oriented data where value equality is appropriate. Prefer immutable properties. Mark properties required when callers must initialize them. Use the default record declaration `record <TypeName>(string Property1, string Property2)` only if the record has a max of 2 properties.
  Use the following syntax for records with more than 2 properties:

```
record <TypeName>
{
	public required string Property1 { get; init; }
	public required string Property2 { get; init; }
	public string? Property3 { get; init; }
}
```

### General Guidelines

- Async: Use asynchronous APIs for I/O, propagate cancellation where applicable, and avoid blocking on tasks.
- DI: Use constructor injection; avoid service location and building additional containers during registration; respect service lifetimes.
- Logging: Use structured message templates rather than interpolated log messages.

### Exception Message Guidelines

- Use exception types that provide structured information (e.g. `ArgumentNullException`, `ArgumentException`) instead of embedding parameter names in the message.
- Use `nameof(...)` when referring to parameters, properties, methods, classes, or other code symbols in exception messages.
- Use `typeof(...)` only when actual runtime type information is required (e.g. generic code, DI, serialization).
- Prefer `nameof(MyClass)` over `typeof(MyClass).Name` when only the symbol name is needed.
- Keep exception messages concise and actionable. Avoid repeating information already available from the exception type or stack trace.

## Project continuity

Use `docs/DESIGN.md` as the central living architecture document.
For an existing project, create it only when requested or when starting substantial multi-step work.
Task tracking lives in `docs/PLAN.md` (milestone workflow) or GitHub Issues (issue workflow).
Only update `docs/PLAN.md` when explicitly working in the milestone workflow or asked to do so.

Before substantial implementation:
- Read `docs/DESIGN.md` if present.
- Check relevant code before trusting documented assumptions.

`docs/DESIGN.md` contains:
- Goals and non-goals
- Constraints
- Architecture and important interfaces
- Significant decisions and brief rationale

During implementation:
- Update `docs/DESIGN.md` only when a change alters architecture, interfaces, or key decisions.
- Never claim a task is complete without verifying its acceptance criteria; if verification is unavailable, state that explicitly.

Keep documentation minimal:
- Prefer short bullets and small sections.
- Describe current state, not chronological activity.
- Do not duplicate code or repeat information between documents.
- Create supporting documents only for substantial topics; link them centrally.
- Do not create additional summaries, progress reports, or session logs.
- Preserve unrelated content and remove stale information within changed scope.