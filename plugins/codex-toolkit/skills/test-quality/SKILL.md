---
name: test-quality
description: Assess targeted .NET test coverage, assertions or optional mutation testing.
---

# Test Quality

Detect test framework/platform and affected project dependencies. Run targeted tests; inspect assertions for the changed behavior, not test count alone. Use existing coverage tooling when relevant; Coverlet and Stryker.NET are optional/on-demand. Mutation and comprehensive coverage runs need an explicit request or material justification. Propose tests for observable risks, not implementation mirrors.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
