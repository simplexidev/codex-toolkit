# Report: final adversarial-review fixes

- Time (UTC): 2026-09-24
- Scope: `simplexidev/codex-toolkit` only
- Source review: `20260921-final-adversarial-release-readiness.md`
- Status: resolved

## Resolution

- CTK-RR-001: `upstream update` writes its managed report with `SafeFiles.Atomic`, which rejects a report-file symlink. The focused regression verifies that an empty, copied provenance configuration refuses the write and preserves the sentinel; the ordinary replacement case remains covered.
- CTK-RR-002: screen candidate IDs are checked for sensitive material before they are returned or used in requests. The common structured-output and overflow-artifact rendering boundary redacts JSON values, including escaped credentials.
- CTK-RR-003: update reconciles recorded ownership entries absent from the current plan while holding the installer lock, removes only matching owned links, preserves replacements, and records the reconciliation. The lifecycle regression covers removed agent and skill entries, dry-run disclosure, repeat update, and uninstall.
- CTK-RR-004: the shared lifecycle test requests the optional bin link only outside Windows; the Windows-specific contract test retains the deliberate rejection.
- CTK-RR-005: the live smoke step sets `JEV_MODE: required`; fake-client coverage requires a nonzero failure for missing credentials, HTTP failure, timeout, and malformed responses, while valid fake responses succeed.
- CTK-RR-006: typed probability parsing accepts only non-null numeric `JsonValue` entries. Invalid service/cache values fall back to REVIEW, required mode exits 3, and invalid cached values are refreshed.

## Validation

- Focused safety, credential, installer, JEV response, integration-validation, and metadata tests: 70 passed.
- Full product test suite: 176 passed.
- `AgentTool validate`: passed; 69 JSON files parsed with no schema, configuration, plugin, skill, agent, runtime-reference, or release-identity errors.
- `AgentTool eval`: passed; 29 offline skill scenario/fixture integrity checks passed.
- `upstream dotnet-skills check --dry-run`: passed; provenance query only, with no upstream source downloaded or executed.
- `git diff --check`: passed.
