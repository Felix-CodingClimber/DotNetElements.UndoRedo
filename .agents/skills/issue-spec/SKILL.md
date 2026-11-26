#### name: issue-spec
description: Read-only analysis of a GitHub issue to propose acceptance criteria and a brief .NET implementation plan before building.

#### Guardrails
* Do not edit any files or alter git state.
* Do not post to GitHub unless explicitly asked.

#### Steps
1. Run `gh issue view <number> --comments` and read docs/DESIGN.md.
2. Locate the relevant projects, classes, and tests in the codebase.
3. Output: **Acceptance Criteria** (bulleted checklist), **Files Likely Affected**, **Edge Cases / Risks**, and **Open Questions** (if any).