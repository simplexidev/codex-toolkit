---
name: diagnostics
description: Collect or inspect .NET runtime counters, traces, dumps or GC artifacts for a reported issue.
---

# Diagnostics

Choose the smallest evidence source: dotnet-counters for live symptoms, dotnet-trace for timing, dotnet-gcdump for managed retention, dotnet-dump for state; dotnet-monitor for ongoing collection when appropriate. Specify process and bounded collection duration. Dumps may contain secrets; keep local. Parse existing artifacts before collecting more. Use log-analyzer only when delegation is authorized.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
