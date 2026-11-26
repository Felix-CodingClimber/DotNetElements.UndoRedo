#### name: issue-build
description: Implement a single GitHub issue in .NET, run build and tests, and leave changes uncommitted on an issue branch for review.

#### Issue Build

Implement only the requested GitHub issue while adhering to docs/DESIGN.md.

#### Guardrails

* **Scope:** Implement only what is required by the target issue. Do not refactor unrelated code or address other issues.
* **Git State:** Create or switch to a branch named `issue/<number>-<short-slug>` if currently on `main`. Do **not** run `git commit`, `git push`, or `git reset`. Leave all changes uncommitted in the working tree for review.
* **No Unapproved Packages:** Do not add new NuGet dependencies unless explicitly specified in docs/DESIGN.md or the GitHub issue.
* **Ambiguity Stop:** If the issue lacks clear acceptance criteria or conflicts with docs/DESIGN.md, stop and ask before writing code.

#### Steps

1. Run `gh issue view <number> --comments` and read docs/DESIGN.md.
2. Inspect existing code patterns, namespaces, and conventions in the relevant project files before editing.
3. Implement the issue changes and add or update unit/integration tests covering the new behavior and edge cases.
4. Run `dotnet build` and `dotnet test` until both pass with zero errors.
5. Check `git status` and `git diff --stat` to confirm only issue-relevant files were modified.

#### Handoff Output

Provide a concise summary for the reviewer:

* **Issue:** #<number> — Title
* **Branch:** Current branch name
* **Files Changed:** Brief bullet list of added/modified files
* **Verification:** Confirmation that `dotnet build` and `dotnet test` passed
* **Notes / Trade-offs:** Any assumptions, edge cases, or docs/DESIGN.md impacts to inspect