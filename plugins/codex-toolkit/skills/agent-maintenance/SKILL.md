---
name: agent-maintenance
description: Maintain installed toolkit instructions, skills, agents or configuration.
---

# Agent Maintenance

Use `doctor` and `install --dry-run` to inspect ownership and conflicts. Edit the central checkout, validate affected skills and tests, then use `update --dry-run` before `update`. Do not overwrite user files or expand global instructions for one project's rules. Check references after renames. Uninstall preserves replacements.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
