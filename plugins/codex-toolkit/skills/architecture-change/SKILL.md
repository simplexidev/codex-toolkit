---
name: architecture-change
description: Plan or review a real change to component boundaries or system architecture.
---

# Architecture Change

Map dependency direction and existing extension points first. State compatibility, public API, migration, testing and operational implications. Compare concrete alternatives against project constraints. JEV can screen a large sanitized affected-file list, never select architecture. Keep decisions and implementation scope with Codex; record an ADR only when useful or required.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
