# Project integration

Copy only the relevant templates/project files into a target repository, merge them
with existing files and fill project-specific paths/rules. Local AGENTS.md records
architecture, validation targets and deployment constraints; global workflow stays central.
Do not blindly replace existing .codex/config.toml or build files.

Templates/dotnet provides optional modern defaults. Central package management has no
forced package dependencies. version.json is a Nerdbank.GitVersioning starting point,
not an automatically installed version tool. Review framework and SDK choices per project.

Invoke dotnet /absolute/toolkit/tools/AgentTool.cs repo affected-projects --root /target
from any location. MSBuild evaluation can execute project/import logic; only inspect trusted
repositories. Shared or unrecognized inputs widen validation rather than omit dependent tests.
