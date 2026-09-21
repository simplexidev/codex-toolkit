# Upstream integrations

[dotnet/skills](https://github.com/dotnet/skills) is referenced through manifests for
MSBuild, tests, diagnostics and advanced C#. Its [README](https://github.com/dotnet/skills/blob/main/README.md)
distinguishes portable skills from host-specific agents; this toolkit provides its own
native Codex TOML agents. Add its marketplace separately when wanted.

upstream/tools.json lists recommended/optional/on-demand .NET tooling. These are project
references, not guaranteed package versions or verified installation recipes. This bootstrap
does not download them. Confirm the selected tool's current instructions before use.
No third-party implementation source is copied. License labels are informational; review
actual package/version notices before redistribution.

upstream status shows policy. upstream update queries tracked repository HEAD via gh
and writes .agent-tool/upstream-drift.json for review. --dry-run needs no network. An unpinned
revision explicitly means no lock was chosen; the workflow reports drift, never auto-merges.
Optional tool versions are intentionally not exhaustively pinned or researched.
