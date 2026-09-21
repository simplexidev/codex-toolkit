---
name: dotnet-verify
description: Validate a .NET change using affected project and test dependencies.
---

# Dotnet Verify

Use `repo affected-projects [--base REF]`, then `dotnet verify` with the same base. MSBuild evaluation executes project logic, so use trusted repositories. Select project-specific test-platform arguments when dotnet test requires them. Save full logs and report first causal failures.
