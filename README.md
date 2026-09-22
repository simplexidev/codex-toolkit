# codex-toolkit

A central Codex toolkit with 24 focused skills, one native agent and one .NET 10
file-based utility. Prefer **deterministic tools → bounded JEV judgment → Codex**:
compute exact facts cheaply, preserve uncertainty, reserve reasoning for real problems.

## Start

Requires .NET 10 SDK and Git. From this checkout:

```text
dotnet tools/AgentTool.cs install --dry-run
dotnet tools/AgentTool.cs install --bin
dotnet tools/AgentTool.cs doctor
```

Installation links instructions, skills and agents without overwriting user configuration.
After updating the checkout, run `dotnet tools/AgentTool.cs update`. Remove owned links
with `dotnet tools/AgentTool.cs uninstall`. [Installation details](docs/installation.md).

## Common commands

```text
dotnet tools/AgentTool.cs repo changed-files
dotnet tools/AgentTool.cs repo affected-projects --base main
dotnet tools/AgentTool.cs -- dotnet verify --project tests/MyTests.csproj
dotnet tools/AgentTool.cs -- logs summarize --file build.log
dotnet tools/AgentTool.cs git prepare-commit
dotnet tools/AgentTool.cs results init
dotnet tools/AgentTool.cs results new handoff phase-1
dotnet tools/AgentTool.cs results context handoff
dotnet tools/AgentTool.cs help
```

Output is compact JSON in both default and --json modes. Full command logs are stored
in ignored .agent-tool/. The utility never commits, pushes, merges or creates remote
repositories. Branch creation checks clean Git state and open issue status.

## Durable agent results

`results init` creates `.agent-results/`: durable audits, handoffs, reviews, and reports are tracked; generated evaluations, logs, traces, SARIF, binlogs, test results, and temporary files are ignored. Normal discovery deliberately skips this store. Use it only for non-obvious state that needs to cross independent chats, and link large evidence by path instead of copying it into a handoff.

JEV is optional: inject `TYPESAFE_API_KEY` only into the specific AgentTool process or
session used for live JEV work; storage and injection remain outside this toolkit. Inspect a sanitized input
with `jev noul --input safe.json --dry-run`, and explicitly mark reviewed input with
--safe-input before transmission. Failures/uncertainty return REVIEW for Codex.
[JEV setup](docs/jev.md) · [Configuration](docs/configuration.md)

Use small project-local instructions from [templates/project](templates/project/AGENTS.md)
for target-specific rules. [Project integration](docs/project-integration.md).
Official .NET integrations are references, never vendored: [upstream policy](docs/upstream-integrations.md).

## Development

```text
dotnet test tests/AgentTool.Tests/AgentTool.Tests.csproj
dotnet tools/AgentTool.cs validate
dotnet tools/AgentTool.cs eval
```

Tests cover real Git repositories, fake HTTP, temporary-home installation and metadata.
Offline evals are smoke checks; measured agent/token comparisons are separate
[regression inputs](docs/evaluation.md). No live billable JEV tests.

The bootstrap intentionally leaves optional tool installation, full binlog interpretation,
project-specific SBOM/API/reproducibility gates and billable skill evaluations on demand.
[Architecture](docs/architecture.md) · [Security](docs/security.md) · [Troubleshooting](docs/troubleshooting.md)
