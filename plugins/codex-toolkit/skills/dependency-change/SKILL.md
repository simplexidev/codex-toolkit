---
name: dependency-change
description: Change .NET package references or central package versions safely.
---

# Dependency Change

Locate PackageReference, Directory.Packages.props and lock files using search. Restore affected projects and run `dotnet package-audit --project PATH`; inspect transitive deltas and compatibility metadata. Verify impacted tests. Explain lock/version changes without hand-editing resolved dependency graphs. JEV may triage a large sanitized warning set but cannot override vulnerabilities or compatibility errors.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
