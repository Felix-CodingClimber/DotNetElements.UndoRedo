---
name: issue-spec
description: 'Manual only. Trigger only via /issue-spec or when the user says "Use issue-spec". Do not auto-trigger.'
---

#### Guardrails
* Do not edit any files or alter git state.
* Do not post to GitHub unless explicitly asked.

#### Steps
1. Read the issue (prefer the host's `read_issue` tool if available; otherwise run `gh issue view <number> --json number,title,body,comments,labels,state` — never bare `--comments`) and read docs/DESIGN.md.
2. Locate the relevant projects, classes, and tests in the codebase.
3. Output: **Acceptance Criteria** (bulleted checklist), **Brief Implementation Plan**, **Files Likely Affected**, **Edge Cases / Risks**, and **Open Questions** (if any).