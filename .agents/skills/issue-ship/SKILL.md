---
name: issue-ship
description: 'Manual only. Trigger only via /issue-ship or when the user says "Use issue-ship". Do not auto-trigger.'
---

#### Guardrails
* Never commit or push directly to `main`.
* Abort if `git status` shows no changes or if `dotnet test` has not passed after the final edit.

#### Steps
1. Run `git status -uall` and `git diff --stat` to confirm all modified and untracked files are intentional.
2. Stage the changes and create a Conventional Commit referencing `#<number>`: `<type>(<scope>): <concise summary> (#<number>)` (or `<type>: <concise summary> (#<number>)` when no scope applies; allowed types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `build`, `perf`).
3. Push the branch with `git push -u origin HEAD`.
4. Open a PR using the host's PR tool (e.g., `pull_request_create`) when available, or fall back to `gh pr create --title "<type>(<scope>): <concise title>" --body "..."`. Include `Closes #<number>`, a brief summary of changes, and verification status.