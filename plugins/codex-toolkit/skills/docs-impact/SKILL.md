---
name: docs-impact
description: Assess documentation changes required by a code, configuration or public API change.
---

# Docs Impact

Start with `repo changed-files` and identify changed user-visible behavior, configuration keys and public APIs. Search doc references for those names. If deterministic matches suffice, skip JEV. For a large ambiguous set, screen only sanitized titles/excerpts and open INCLUDE plus REVIEW. Update examples and migration notes that are now inaccurate; avoid unrelated prose cleanup.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
