---
name: issue-review
description: 'Manual only. Trigger only via /issue-review or when the user says "Use issue-review". Do not auto-trigger.'
---

#### Issue Review (Read-Only)

Perform a strict, read-only review of the uncommitted changes for the active GitHub issue.

#### Guardrails

* **Do not modify, create, or delete any files.**
* **Do not run `git commit` or alter git state.**
* Only run read-only inspection or verification commands (`gh issue view`, `git status -uall`, `git diff`, `git diff --cached`, reading untracked files, `dotnet build --no-restore`, `dotnet test --no-build`).

#### Steps

1. Run `gh issue view <number> --comments` and read docs/DESIGN.md.
2. Run `git status -uall`, `git diff`, and `git diff --cached` to inspect all modified and staged changes and enumerate every untracked file (including files inside new directories).
3. Read the full contents of every untracked source, config, and test file listed by `git status -uall` so newly added files are reviewed with the same rigor as tracked diffs.
4. Verify `dotnet build --no-restore` and `dotnet test --no-build` pass cleanly if test output was not already provided.
5. Audit all changes (tracked diffs and untracked files) against five criteria:

  * **Spec & Doc Drift:** Any issue requirement missed or altered, or an architectural/contract change that requires updating docs/DESIGN.md.
  * **.NET Bugs & Edge Cases:** Nullability issues, unawaited tasks (async/await), undisposed `IDisposable` resources, exception handling, or concurrency flaws.
  * **Missing Tests:** Core logic or edge cases added without corresponding unit/integration tests.
  * **Scope Creep:** Unrequested edits outside the target issue.
  * **Unapproved Dependencies:** Any new NuGet package not authorized by docs/DESIGN.md or the issue.

#### Output Format

* **Verdict:** PASS or CHANGES_REQUESTED
* **Blocking Issues:** Numbered list with `path/File.cs:line` and concrete fix guidance (omit if PASS).
* **Notes:** Optional brief observations (max 3 bullets, no stylistic nitpicks).