# Installation

Requires .NET 10 SDK and Git. Keep the checkout in a stable location.
From its root:

```text
dotnet tools/AgentTool.cs install --dry-run
dotnet tools/AgentTool.cs install --bin
dotnet tools/AgentTool.cs doctor
dotnet tools/AgentTool.cs update
dotnet tools/AgentTool.cs uninstall
```

The installer creates individual symlinks for global instructions, three agents and
25 skills; on Unix, --bin adds ~/.local/bin/codex-agent-tool. Add that directory to PATH if needed.
Unix executable mode is tracked; after extracting a ZIP, chmod +x tools/AgentTool.cs
before using --bin, or invoke dotnet directly. On Windows, --bin is deliberately unsupported:
invoke `dotnet <toolkit>\\tools\\AgentTool.cs` directly. Windows still needs symlink permission/Developer
Mode for instructions and skills; there is no silent copy fallback. A symlink failure stops with recoverable ownership
metadata for any completed entries. Never run as administrator merely to bypass conflicts.

Conflicts are reported before installation writes. Repeat installs and update are
idempotent. Ownership metadata is under the selected Codex directory. Uninstall removes
only recorded links whose targets still match; replacements and parent directories remain.
Do not move the checkout while installed: uninstall first, move, then reinstall. Update
refreshes links after you update the checkout yourself and removes recorded links for components no longer in that checkout; it never fetches, pulls or resets.

Use --home /tmp/toolkit-profile for testing. No production home is changed by repository
tests. Installation refuses symlinked state/destination parents to prevent redirected writes.

Optional plugin UI registration: codex plugin marketplace add /absolute/path/to/codex-toolkit.
The marketplace root is the checkout, so ./plugins/codex-toolkit resolves correctly.
Choose plugin installation or direct skill links to avoid duplicate skill discovery.
Global instructions and native agents still require the utility installer.
