---
name: dotnet-verify
description: Validate a .NET change using affected project and test dependencies.
---

# Dotnet Verify

Use `repo affected-projects [--base REF]`, then `dotnet verify` with the same base. The utility evaluates Compile and ProjectReference and includes reverse dependents; shared, custom and multi-targeted inputs widen scope conservatively. MSBuild evaluation executes project logic, so use trusted repositories. Test detection uses IsTestProject. Select project-specific test-platform arguments when dotnet test requires them. Save full logs and report first causal failures.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
