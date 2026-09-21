---
name: security-scan
description: Run or triage deterministic security analyzers and SARIF for a scoped change.
---

# Security Scan

Use configured CodeQL, DevSkim, .NET analyzers and package audit as applicable. Prefer `sarif summarize --file PATH` then open relevant locations. Preserve severity, rule IDs and suppression context; do not let JEV downgrade hard findings or authorize actions. Avoid external transmission of sensitive source; validate fixes with the originating rule.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
