---
name: repo-health
description: Audit repository build and .NET configuration against toolkit policy.
---

# Repo Health

Run `repo health`; inspect evaluated properties for framework, nullable, central packages, analyzers, deterministic builds and optional lock files. Read config/repo-health.json from the toolkit for policy. Report findings with actual versus expected values; propose targeted changes. Do not impose optional release tooling or silently edit policy to obtain a pass.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
