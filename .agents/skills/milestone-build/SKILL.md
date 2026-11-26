---
name: milestone-build
description: 'Manual only. Trigger only via /milestone-build or when the user says "Use milestone-build". Do not auto-trigger.'
---

#### Milestone Build

Implement only the requested milestone/phase from docs/PLAN.md while adhering to docs/DESIGN.md. 

#### Guardrails

* **Scope:** Implement only the target milestone. Do not start future milestones or refactor unrelated code.
* **Git State:** Do **not** run git commit, git push, or git reset. Leave all changes uncommitted in the working tree for review.
* **No Unapproved Packages (Pause & Propose):** Do not install or reference new NuGet dependencies unless explicitly specified in docs/DESIGN.md or docs/PLAN.md. However, if a requirement cannot be cleanly solved with the .NET BCL or existing dependencies without writing complex, error-prone, or security-sensitive custom code (e.g., parsers, cryptography, auth, protocol clients), **stop before implementing a custom workaround** and report:
  1. The specific problem and why standard .NET APIs are insufficient.
  2. The recommended NuGet package (and whether it is actively maintained).
  3. The rough effort/risk of a custom implementation vs. adding the package.
  Wait for approval before running `dotnet add package` or continuing. Do not pause for minor convenience or stylistic libraries.

#### Steps

1. Read docs/DESIGN.md and the target milestone in docs/PLAN.md.
2. Inspect existing code patterns, namespaces, conventions, and current NuGet packages (`.csproj`) before editing; flag any missing dependency needs now rather than mid-implementation.
3. Implement the milestone changes and add or update unit/integration tests covering the new behavior and edge cases.
4. Run dotnet build and dotnet test until both pass with zero errors.
5. Check git status and git diff --stat to confirm only milestone-relevant files were modified.

#### Plan Tracking (`docs/PLAN.md`)

Keep `docs/PLAN.md` accurate for the active milestone:

* Maintain current status, actionable Markdown checkboxes, blockers/open questions, verification (commands run, outcomes, remaining gaps), and the concrete next step.
* Update `docs/PLAN.md` at meaningful checkpoints and before handoff.
* Never check off a task without verifying its acceptance criteria.

#### Handoff Output

Provide a concise summary for the reviewer: 

* **Milestone:** Name/number completed
* **Files Changed:** Brief bullet list of added/modified files
* **Verification:** Confirmation that dotnet build and dotnet test passed
* **Notes / Trade-offs:** Any assumptions or edge cases to inspect