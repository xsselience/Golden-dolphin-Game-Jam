---
name: claude-md-constraints
description: CLAUDE.md key behavior rules and constraints for the session
metadata:
  type: project
---

Adopted all constraints from `CLAUDE.md` (2026-07-28).
Key rules:
1. No looping bash brace checks.
2. If Unity compiles & runs, the code is fine — don't micro-polish.
3. At most 3 .cs file reads per task.
4. No repeated fixing of the same non-critical issue.
5. Stop after one fix attempt; report status and wait.
6. Don't iterate endlessly on file reads/searches.

**Why:** Previous sessions had run-away loops auditing braces; new rules prevent that.
**How to apply:** Before every tool call, check whether it violates the constraints above. If it does, stop and wait.
