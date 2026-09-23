# Third-party notices

No upstream skill or implementation source is vendored. Integration project references
and licenses are in upstream/*.json. The official .NET skill inventory references
`dotnet/skills` commit `4ed5f7c121da8dd31af31a35cef05070948c6556`, licensed
under MIT, copyright .NET Foundation and Contributors. Its source paths and license
strategy are recorded in `upstream/dotnet-skills.json`; no upstream skill body,
script, agent or reference is redistributed by this repository.

NuGet test dependencies are restored separately and retain their respective license
notices. They are excluded from source release archives. Review concrete dependency
versions before binary redistribution.

API/configuration formats follow [TypeSafe](https://docs.typesafe.ai/introduction/quickstart),
[Codex native agents](https://developers.openai.com/codex/subagents), and
[.NET file-based apps](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps).
