---
name: address-pr-review
description: Address concrete pull request review comments after a review arrives.
---

# Address Pr Review

Read `github review-comments --pr NUMBER` and `git state`. Separate actionable defects from questions and already-resolved feedback. Inspect only cited code and direct dependents; apply warranted fixes and targeted tests. JEV may categorize a large sanitized comment set, but must not dismiss security or uncertain feedback. Report each comment's disposition with evidence. Posting replies requires the user's instruction.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
