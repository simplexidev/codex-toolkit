---
name: roadmap-next
description: Identify an actionable next issue from explicit roadmap and dependency metadata.
---

# Roadmap Next

Read bounded issue metadata and dependency state. Respect declared ordering, milestones and blockers. JEV Choice may classify sanitized readiness into actionable, blocked or unclear; unresolved cases stay with Codex. Do not infer strategic priorities from semantic scores. Explain the selected item using roadmap and dependency evidence.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
