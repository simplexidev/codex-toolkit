# Final bounded adversarial release-readiness review

- Date: 2026-09-21
- HEAD: `4ea1f85eb1f344101144bf7b39e70eae3851b52c`
- Branch: `main`; no unfinished Git operation detected.
- Status: complete; release blockers remain.
- Reviewed current worktree, including pre-existing changes in `tools/AgentTool.cs` and `tests/AgentTool.Tests/ProjectDiscoveryTests.cs`. Pre-existing untracked dogfood report preserved.
- Outcome: 2 HIGH and 4 MEDIUM findings; no CRITICAL finding established.

## Findings

### CTK-RR-001 — HIGH — Upstream report follows a destination symlink and overwrites user data

- Files: `tools/AgentTool.cs:312-313` (`Upstream`); `docs/security.md`.
- Scenario/consequence: `.agent-tool` is an ordinary directory, but `upstream-drift.json` is a symlink to a user-owned file. The parent-only `NoLinks` check passes and `File.WriteAllText` truncates the target. No race is required. This contradicts the documented managed-write boundary.
- Evidence: isolated CLI reproduction with an empty synthetic upstream repository list (zero network calls) changed an external sentinel file from `user-owned sentinel` to `[]`; command returned `ok`.
- Smallest fix: write this report through `SafeFiles.Atomic`, which checks the complete destination and replaces atomically.
- Required validation: temporary-directory test with a report-file symlink to a sentinel; require refusal and unchanged sentinel. Cover an ordinary report replacement as well.

### CTK-RR-002 — HIGH — Screen candidate IDs bypass the API-key output boundary

- Files: `tools/AgentTool.cs:59-67`, `tools/AgentTool.cs:277-291` (`Main`, `JevCommand`); `tests/AgentTool.Tests/CredentialBoundaryTests.cs`; `docs/security.md`.
- Scenario/consequence: a candidate ID contains the configured TypeSafe key while its text and query are benign. Sensitivity checking covers only the request, and the ID is copied directly into the result. Main serializes it unchanged to stdout, or to an overflow artifact. Thus a rejected/non-transmitted field can disclose the secret in agent context/log capture, including during dry-run.
- Evidence: a CLI dry-run with a deliberately synthetic `TYPESAFE_API_KEY` and that same synthetic string as candidate ID printed the complete string. No real credential was read or transmitted.
- Smallest fix: reject sensitive IDs before adding them to results; enforce credential redaction at the common structured-output and artifact boundary so other raw result fields cannot bypass it. Preserve valid JSON when redacting.
- Required validation: synthetic-key screen IDs in dry-run/live-fake/off modes; assert no key in stdout, stderr or overflow artifacts and no HTTP request for rejected input. Include JSON-escaped credential characters.

### CTK-RR-003 — MEDIUM — Update retains removed or renamed installed components

- Files: `tools/AgentTool.cs:998-1034` (`Installer.Run`); `tests/AgentTool.Tests/InstallationTests.cs`; `docs/installation.md`.
- Scenario/consequence: after a checkout upgrade removes/renames a skill or agent, update plans only current sources. Old manifest entries never enter its removal loop. Update reports success while dangling skills/agents remain discoverable at their old destinations. The earlier structural finding's uninstall half was fixed, but update cleanup remains incomplete.
- Evidence: temporary copied toolkit/profile install, rename `agents/reviewer.toml` to `renamed.toml`, then update: both destination links remained, with `reviewer.toml` dangling. The existing stale-link regression covers uninstall only.
- Smallest fix: reconcile recorded entries absent from the new plan under the existing installer lock; remove only matching owned links, preserve replacements, and persist the reconciled manifest. Include stale entries in dry-run output and parent checks.
- Required validation: remove/rename both an agent and skill between install/update; assert stale matching links disappear, replacements survive, new links work, and repeat update/uninstall remain safe.

### CTK-RR-004 — MEDIUM — Windows CI necessarily fails the installer lifecycle test

- Files: `tests/AgentTool.Tests/InstallationTests.cs:8`; `tools/AgentTool.cs:963`; `.github/workflows/ci.yml`.
- Scenario/consequence: the Windows matrix runs `InstallUpdateIdempotentAndUninstallPreservesReplacement`, which unconditionally requests `bin: true`. `Installer.Plan` deliberately throws `PlatformNotSupportedException` for that request on Windows. The platform contract and test disagree, blocking the cross-platform CI gate regardless of symlink privileges.
- Evidence: deterministic source-level contradiction; Windows execution was not available in this Linux review.
- Smallest fix: run the common lifecycle with bin disabled on Windows; separately assert the deliberate Windows rejection and retain Unix bin lifecycle coverage.
- Required validation: focused installation tests on Windows and Linux, followed by the existing matrix gate. Do not weaken the production platform guard.

### CTK-RR-005 — MEDIUM — Live integration workflow passes when no integration succeeded

- Files: `.github/workflows/jev-integration.yml:21-28`; `config/jev.json`; `tools/AgentTool.cs:899` (`Fallback`).
- Scenario/consequence: the workflow inherits `mode: auto`. Missing credentials, HTTP errors, timeouts and malformed responses yield REVIEW with exit 0, so its sole smoke step can be green without a successful provider request. This makes the live integration signal misleading.
- Evidence: the workflow-equivalent synthetic Noul invocation with the credential explicitly absent returned `JEV credentials unavailable`, REVIEW and exit 0. No live service was called.
- Smallest fix: set `JEV_MODE: required` on the smoke-call step. Keep valid ambiguous judgments distinct from service failures.
- Required validation: keyless/fake-HTTP contract tests for missing credentials, unauthorized response, timeout and malformed response must make the smoke invocation fail; a valid fake Noul response must succeed. Assert required mode in workflow metadata coverage.

### CTK-RR-006 — MEDIUM — Null probability values escape conservative JEV fallback

- Files: `tools/AgentTool.cs:874-876`, `tools/AgentTool.cs:895-897`, `tools/AgentTool.cs:925-928` (`Judge`, `Parse`); `tests/AgentTool.Tests/JevResponseTests.cs`.
- Scenario/consequence: a Choice/Score response or cache entry has the expected probability labels but a JSON null value, e.g. `{"a":null,"b":1}` for matching Choice criteria. The null-forgiving operator at `x.Value!.GetValue<double>()` does not validate at runtime; it throws `NullReferenceException`. Neither cache recovery, Judge fallback nor Main handles that exception. The command crashes instead of returning REVIEW; a fresh malformed cache repeats the failure.
- Evidence: source-level null dereference and exception-filter trace; no live call or provider assumptions required.
- Smallest fix: explicitly validate each probability as a non-null numeric JsonValue and throw the existing handled `JsonException` on invalid values.
- Required validation: fake HTTP and fresh-cache null probability cases for Choice and Score. Auto must return REVIEW; required must return REVIEW/exit 3 on invalid service output; invalid cache must be ignored and safely refreshed. Assert no unhandled exception.

## Review bounds and evidence

Read root instructions, current Git state/recent history, latest handoff, latest structural audit, latest JEV integration report and latest dogfood report. Inspected the requested Git/ownership/path/process/cleanup/credential/cache/skill/usage/metadata/CI/release/update/data-write surfaces. No broad research, subagents, full-suite reruns or live JEV calls. Findings above are the actionable issues established by this bounded pass, not a claim of exhaustive safety.

CLI reproductions used `/tmp/ctk-release-review.AOCT9P`, copied toolkit data, temporary installer homes and synthetic inputs only. Initial CLI startup was blocked by the sandbox's read-only default .NET runfile cache; rerunning with temporary .NET/XDG directories succeeded. No implementation edits were made. Windows and null-probability findings are explicitly source-confirmed rather than runtime-reproduced.

## Follow-up

Address CTK-RR-001 through CTK-RR-006 in a separate implementation task, with the specified targeted regressions. This review does not authorize additional implementation work or repeated validation loops.
