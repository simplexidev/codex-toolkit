---
name: finish-pr
description: Prepare an authorized push and pull request after implementation is ready.
---

# Finish Pr

Use `github prepare-pr` and inspect the exact diff, checks, branch and upstream. Stage only intended files, commit and push only within the user's scope, then open/update a PR with a concrete summary and validation. The utility performs preparation checks only; use available GitHub tools or gh for authorized external actions. Never merge without explicit user approval. Never use destructive recovery for an unfinished Git operation.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
