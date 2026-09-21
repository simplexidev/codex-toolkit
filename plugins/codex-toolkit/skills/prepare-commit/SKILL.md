---
name: prepare-commit
description: Check an intended local commit's scope and Git safety.
---

# Prepare Commit

Run `git prepare-commit`; examine staged and unstaged diffs, whitespace results and changed file scope. Confirm relevant validation. Stage explicit paths only and preserve unrelated user work. Commit only when requested or covered by the task. Do not bypass unfinished Git operation checks.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
