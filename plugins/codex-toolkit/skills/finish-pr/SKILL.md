---
name: finish-pr
description: Prepare an authorized push and pull request after implementation is ready.
---

# Finish Pr

Use `github prepare-pr` and inspect the exact diff, checks, branch and upstream. Stage only intended files, commit and push only within the user's scope, then open/update a PR with a concrete summary and validation. The utility performs preparation checks only; use available GitHub tools or gh for authorized external actions. Never merge without explicit user approval. Never use destructive recovery for an unfinished Git operation.
