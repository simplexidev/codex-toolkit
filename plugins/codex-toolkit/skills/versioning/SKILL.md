---
name: versioning
description: Apply an existing Git-derived .NET versioning policy for a release or package change.
---

# Versioning

Inspect version.json and Git history/tag state. Prefer configured Nerdbank.GitVersioning metadata when present; use its output rather than inventing versions. Discuss compatibility signals and prerelease policy. Modify version policy only within the request. Missing version tooling is reported, not replaced by guessed package numbers.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
