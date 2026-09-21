# Architecture

Production logic lives in tools/AgentTool.cs: strict CLI parsing, process execution,
Git state, MSBuild queries, artifact compaction, installation ownership and HTTP judgments.
No NuGet packages are needed by the utility. PublishAot is disabled because this source
utility uses reflection-based JSON serialization; it runs on the installed .NET 10 SDK.
Tests link the same implementation, preventing a second production implementation.

Skills describe narrow workflows. Native agents are installed separately because plugin
installation does not populate Codex's native agent discovery directories. Upstreams
are informational references and never vendored or automatically installed.

Deterministic facts take precedence over probabilistic classifications. JEV failure
returns REVIEW; Codex retains uncertainty and all open-ended work.
