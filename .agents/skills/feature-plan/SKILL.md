---
name: feature-plan
description: 'Manual only. Trigger only via /feature-plan or when the user says "Use feature-plan". Do not auto-trigger.'
---

#### Feature Plan (Interactive Planning -> Approved GitHub Issue)

Collaborate with the user to design a new feature against the current codebase and docs/DESIGN.md, then create a clean, `issue-build`-ready GitHub issue once approved.

#### Guardrails

* **Read-Only Repository:** Never modify, create, or delete repository files, and never alter git state.
* **Strict Approval Gate:** Do **not** run `gh issue create` until the user explicitly approves the final issue draft.
* **Check Reality First:** Inspect docs/DESIGN.md, existing code patterns, and open GitHub issues (prefer the host's `list_issues` tool if available; otherwise `gh issue list --state open --limit 50`) before proposing a design.
* **Right-Sized Scope:** If the feature is too large for a single clean PR, proactively suggest splitting it into 2–3 ordered issues.
* **Allowed Commands:** Read-only file inspection, host issue tools (`list_issues`, `read_issue`) or `gh issue list` / `gh issue view <number> --json number,title,body,comments,labels,state` (never bare `--comments`), and `create_issue` / `gh issue create` (Phase 3 only, after approval).

#### Phase 1: Explore & Clarify

1. Read docs/DESIGN.md, inspect relevant projects, interfaces, and conventions, and check open issues (via `list_issues` if available, else `gh issue list --state open --limit 50`) to check for overlapping work.
2. Identify ambiguities, edge cases, architectural trade-offs, or conflicts with docs/DESIGN.md.
3. Ask concise, high-signal clarifying questions (grouped into a short numbered list, with recommended defaults where helpful).
   * If the user's initial description is already unambiguous, skip straight to Phase 2.

#### Phase 2: Draft Spec & Iterate

1. Present the complete proposed GitHub issue draft for review:
   * **Title:** Concise, action-oriented feature title.
   * **Goal / Problem:** Brief explanation of what this enables and why.
   * **Scope & Non-Goals:** Explicit boundaries to prevent scope creep during implementation.
   * **Specification & Acceptance Criteria:** Concrete behavior, edge cases, and a testable checklist.
   * **Design & Dependency Notes:** Whether docs/DESIGN.md must be updated, and any NuGet package explicitly approved for this feature (omit if none).
   * **Implementation Details (Optional):** Suggested approach, key interfaces/files likely affected, and test strategy. Keep high-level unless specific constraints matter.
2. **Stop** and ask for feedback, edits, or approval to create the issue (or split it into multiple issues). Repeat until explicitly approved.

#### Phase 3: Create Issue (After User Approval)

1. Run `gh issue create` with the approved title, body, and any requested labels.
2. Output the created issue number(s), title(s), and URL(s).