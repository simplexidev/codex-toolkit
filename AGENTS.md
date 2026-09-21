# Toolkit development

Prefer deterministic computation, then bounded JEV judgment, then Codex reasoning.
Search for relevant paths and symbols before opening files; avoid repository-wide reads.
Keep complete logs in ignored state storage and return counts, relevant failures and artifact paths.
Validate affected behavior first; run the full suite for release or cross-cutting changes.

All production executable logic belongs in `tools/AgentTool.cs`, a .NET 10 file-based app.
Keep commands composable and output bounded. Use BCL APIs unless a dependency has a
written correctness/interoperability justification. Tests may use established test and parser libraries.
Do not introduce helper scripts in other languages. Skills must have narrow triggers,
progressive disclosure, and observable regression cases. Reference upstream .NET content;
do not vendor it. Keep schemas and documentation synchronized with behavior changes.
Meaningful behavior changes require automated tests and relevant evaluation cases.

Preserve user work. Inspect Git state before mutations; never reset, stash, discard,
or recover unfinished operations without authorization. Do not leave an operation
unfinished. Never merge a PR without explicit user approval. Installer tests must use
temporary homes, never the developer's actual Codex configuration. JEV tests use fake
HTTP responses only; never make a live billable call during validation.

Use durable results only when a later independent chat needs non-obvious state; reference large artifacts by path. Git history is enough for trivial work, and normal repository discovery must not search historical results.
