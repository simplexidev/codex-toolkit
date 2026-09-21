---
name: repo-locate
description: Locate relevant repository files or symbols before broad reading.
---

# Repo Locate

Use `repo locate --query TEXT` for Git-known nonignored paths, then rg for symbols. Narrow with project/reference metadata. For a large ambiguous set only, prepare sanitized candidate excerpts and use `jev screen`; open INCLUDE and REVIEW items. Preserve all candidates if JEV fails. Read exact ranges after locating owners.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
