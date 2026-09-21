---
name: api-compatibility
description: Check public .NET API compatibility after an actual public surface change.
---

# Api Compatibility

Find changed public symbols and existing PublicApiAnalyzers or package validation baselines. Use `dotnet api-check --project PATH` only when configured; it refuses absent baselines/analyzers. Examine diagnostic IDs and API baseline deltas. Codex considers consumer impact only for actual changes. Do not automatically accept generated API baselines; intentional breaks require migration/versioning decisions.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
