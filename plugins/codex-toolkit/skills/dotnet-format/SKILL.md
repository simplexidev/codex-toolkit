---
name: dotnet-format
description: Format changed C# files or verify their formatting.
---

# Dotnet Format

Use `dotnet format --project PATH` to verify the existing changed .cs file set; pass `--apply` only when formatting is wanted. Confirm target project includes those files. Preserve unrelated edits and inspect the final diff. Whole-repository formatting requires an explicit reason such as a formatting policy migration.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
