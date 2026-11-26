---
name: issue-ship
description: 'Manual only. Trigger only via /issue-ship or when the user says "Use issue-ship". Do not auto-trigger.'
---

#### Guardrails
* Never commit or push directly to `main`.
* Abort if `git status` shows no changes or if `dotnet test` has not passed.

#### Steps
1. Run `git status` and `git diff --stat` to confirm all changes are intentional.
2. Stage all changes and create a clear commit referencing `#<number>`.
3. Push the branch with `git push -u origin HEAD`.
4. Open a PR with `gh pr create --title "<type>: <concise title>" --body "..."` including `Closes #<number>`, a brief summary of changes, and verification status.