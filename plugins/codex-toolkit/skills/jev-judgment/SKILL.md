---
name: jev-judgment
description: Use bounded semantic Choice, Noul or Score judgment to reduce a large ambiguous candidate set.
---

# Jev Judgment

Try deterministic filtering first. Read references/primitives.md only for request shape and references/thresholds.md for calibration. Use `jev TYPE --input sanitized.json --dry-run` to inspect the payload. Actual transmission uses `--safe-input` after minimizing and reviewing the text. Use TYPESAFE_API_KEY from environment. Off, missing key, invalid responses, timeouts and uncertainty route to Codex; never discard REVIEW. No code generation, architecture, debugging, prose, calculations, Git mutations or authorization decisions.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
