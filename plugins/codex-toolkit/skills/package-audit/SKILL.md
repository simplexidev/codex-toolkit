---
name: package-audit
description: Audit .NET package vulnerability metadata on demand or after dependency changes.
---

# Package Audit

Run `dotnet package-audit --project PATH`; it requests vulnerable transitive packages in structured JSON and returns nonzero for vulnerabilities. Restore/network failures are failures, not clean audits. Report package, resolved version, advisory and affected project from the full artifact. Never substitute manual review of a huge package list or JEV for advisory metadata.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
