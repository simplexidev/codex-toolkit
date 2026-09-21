---
name: performance-investigation
description: Investigate an observed .NET latency, CPU, allocation or memory regression.
---

# Performance Investigation

State the symptom, workload and measurement window. Reuse counters/traces/profiles before collecting more. Separate CPU, contention, I/O, GC and retention hypotheses using evidence. Follow the narrowest signal to code. Confirm the fix under a comparable workload; create benchmarks only if they answer the observed question. JEV may categorize sanitized diagnostic summaries, never invent causes.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
