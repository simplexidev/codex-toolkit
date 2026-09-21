---
name: sbom
description: Generate or verify a release software bill of materials on request.
---

# Sbom

Use Microsoft SBOM Tool from upstream/tools.json, subject to availability and installation authorization. Point it at the actual release drop and source/package metadata. Validate its manifest and correlate package versions with restored inputs. Keep generated artifacts outside tracked source. Report SPDX output and validation evidence; ordinary builds do not imply an SBOM exists.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
