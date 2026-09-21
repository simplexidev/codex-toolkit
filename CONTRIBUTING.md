# Contributing

Follow AGENTS.md. Extend tools/AgentTool.cs for production logic; add focused tests for
observable behavior. Synchronize config schemas and docs. Keep skill triggers narrow.
Run dotnet test tests/AgentTool.Tests/AgentTool.Tests.csproj, utility validate/eval and
git diff --check before review. Never use live JEV in tests or install into your real home.

Test-only NuGet dependencies are justified: xUnit/Test SDK provide standard discovery;
YamlDotNet, Tomlyn and JsonSchema.Net validate real standards instead of approximate
homegrown parsers. They are not runtime dependencies of the utility.
