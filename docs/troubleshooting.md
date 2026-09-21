# Troubleshooting

Run doctor for required SDK/Git and optional gh/Codex/tools. Use help as a positional
command: dotnet tools/AgentTool.cs help. Some dotnet host options such as --help and
--project are intercepted unless separated: dotnet tools/AgentTool.cs -- dotnet verify
--project /path/app.csproj. Use -- before utility arguments whenever they overlap host options.

Missing toolkit root: pass --toolkit or CODEX_TOOLKIT_ROOT. Moved checkout: uninstall
from the recorded location before reinstalling. Installer conflict: preserve the file,
inspect its owner and relocate it yourself if you intend replacement. No force overwrite.
Windows symlink failure: enable Developer Mode or use the source without installing links.

MSBuild evaluation failure: check project imports/SDK availability; do not treat it as
an empty affected graph. JEV REVIEW: check mode, key presence, timeout and payload shape;
never print the key or provider error body. required mode uses exit 3 for service failure.

Sandboxed .NET cache writes may need XDG_DATA_HOME and DOTNET_CLI_HOME pointing at writable
temporary directories. Network access may be required for SDK/test restore and NuGet audit.
