#### name: codebase-audit
description: Read-only .NET codebase audit (general, topic-focused, or user-provided list) that reports findings with proposed fixes and creates GitHub issues only after explicit approval.

#### Codebase Audit (Read-Only Investigation -> Approved Issue Creation)

Investigate the codebase based on the user's requested scope, report concrete findings with possible fixes, and create GitHub issues only for items the user explicitly approves.

#### Guardrails

* **Read-Only Repository:** Never modify, create, or delete repository files, and never alter git state.
* **Strict Approval Gate:** Do **not** run `gh issue create` in Phase 1. Wait for explicit user approval of which findings or groups to file.
* **Deduplicate First:** Check open GitHub issues (`gh issue list --state open`) before reporting so existing issues are not duplicated.
* **High Signal Only:** Focus on concrete bugs, architectural drift from docs/DESIGN.md, security/performance/reliability risks, meaningful test gaps and UI if requested. No stylistic nitpicks.
* **Allowed Commands:** Read-only file inspection, `gh issue list`, `gh issue view`, `dotnet build --no-restore` / `dotnet test --no-build` (if verifying a suspected bug), and `gh issue create` (Phase 2 only, after approval).

#### Phase 1: Investigate & Report

1. Read docs/DESIGN.md and run `gh issue list --state open --limit 50` to understand system invariants and already-tracked work.
2. Execute the requested audit mode:
   * **General Audit:** Review solution structure, core projects, and tests against docs/DESIGN.md and .NET best practices (nullability, async/await, `IDisposable`, exception handling, concurrency, resource/DI lifetimes, test coverage).
   * **Topic Audit:** Deep-dive into the requested topic (e.g., error handling, security, performance, logging, data access).
   * **User List Investigation:** Inspect every item provided by the user. Explicitly mark each as **Confirmed**, **Partially True**, or **False Positive / Already Handled** with code evidence.
3. Output a numbered **Findings Report**:
   * **ID & Severity:** e.g., `F1 [High]`, `F2 [Medium]`, `F3 [Low]`
   * **Location:** `path/File.cs:line`
   * **Problem:** Concrete explanation of the issue and its impact.
   * **Proposed Fix:** Actionable direction (note if docs/DESIGN.md or a NuGet package is affected).
   * **Tracking Status:** New, or reference to a related open `#<issue>`.
4. Output a **Suggested Issue Plan**:
   * Propose standalone issues for High-severity or self-contained Medium findings.
   * Propose grouped issues for small, related fixes (e.g., `Group A: F4 + F5 + F6 — Small nullability & disposal fixes in Services`).
5. **Stop** and ask the user which findings/groups to create as GitHub issues (and whether to adjust titles, groupings, or labels).

#### Phase 2: Create Approved Issues (After User Approval)

1. For each approved finding or group, run `gh issue create` with a concise title and a structured body ready for `issue-build`:
   * **Problem & Evidence:** Summary with `path/File.cs:line` references.
   * **Proposed Fix:** Concrete implementation direction.
   * **Acceptance Criteria:** Bulleted, testable checklist.
   * **Likely Files Affected:** Source and test files to touch.
   * **Design / Dependency Notes:** Whether docs/DESIGN.md needs an update or a NuGet package is needed (omit if none).
2. Output a concise summary table of the created issues (`#<number>`, Title, URL).