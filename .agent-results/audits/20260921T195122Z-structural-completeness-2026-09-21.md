# Audit: structural completeness 2026-09-21

- Time (UTC): 2026-09-21T19:51:22.0524247+00:00
- Audited HEAD: ba627a34f8587ca2e5a4146fc4344cbe612c4dc3
- Branch: main
- Status: complete
- Scope: bounded read-only structural/completeness audit; implementation was not changed

## Summary

No CRITICAL findings. 2 HIGH, 7 MEDIUM, and 3 LOW findings are actionable. The initial worktree was clean with no unfinished Git operation. History contains three commits; the latest handoff (`results context handoff`) reports Phase 2 complete with no carry-forward. Fast structural validation passed (55 JSON files, zero reported errors). The full suite was deliberately not rerun.

Root `AGENTS.md` correctly owns toolkit-development constraints; `global/AGENTS.md` is a smaller installed policy that defers to project-local instructions, with no contradiction found. Current plugin identity/mirror/marketplace paths, 3 native-agent TOMLs, 25 skill directories, 25 UI metadata files, and 25 eval directories are present and parse structurally. The JEV credential boundary is narrow and consistent with its documentation: one environment secret, stripped from child processes, only applied to the JEV Authorization header, keyless fake-HTTP tests, redirects disabled, and response/cache bounds. No actionable credential-boundary defect was found.

## Findings

### HIGH

#### CTK-AUD-001 — Installed file removal or rename makes update and uninstall reject their own manifest

- Files: `tools/AgentTool.cs` (`Installer.Plan`, `Installer.Run`), `docs/installation.md`, `tests/AgentTool.Tests/InstallationTests.cs`
- Defect: `Installer.Run` requires every recorded entry to equal an entry in the plan generated from the *current* checkout. If a previously installed skill or native-agent source is removed or renamed by a later toolkit revision, that legitimate recorded entry is no longer in `allowed`, so both `update` and `uninstall` throw before removing the stale owned link.
- Consequence: the documented upgrade lifecycle breaks exactly when toolkit contents evolve; users retain dangling links and cannot use the owned uninstall path. The arbitrary-path tamper check and legitimate stale ownership are currently indistinguishable.
- Smallest fix: validate manifest destinations against the exact managed roots/naming rules and recorded checkout, while permitting stale recorded entries to be removed. Keep rejecting destinations outside those roots and sources outside the recorded toolkit.
- Validation needed: install from a temporary copied toolkit, remove/rename one installed agent and one skill source, then prove update/uninstall removes only the recorded stale links; retain the arbitrary-victim tamper regression.

#### CTK-AUD-002 — `release-verify` can succeed for a production project without running any tests

- Files: `tools/AgentTool.cs` (`Dotnet`), `plugins/codex-toolkit/skills/release-verify/SKILL.md`, `docs/release-process.md`, `tests/AgentTool.Tests/ProjectDiscoveryTests.cs`, `tests/AgentTool.Tests/SolutionDiscoveryTests.cs`
- Defect: tests are appended only when `--project` is a solution or the selected project itself has `IsTestProject=true`. A library/application project with dependent test projects is restored, built, formatted, and audited, but no tests run; the result can still be `ok` and its scope says “tests where detected.”
- Consequence: a command named and documented as release verification can produce a false release-ready signal while skipping the repository’s tests.
- Smallest fix: for release verification, resolve and run dependent test projects (or require a solution and fail clearly for a non-test project when tests cannot be established). Return an explicit executed/skipped gate inventory.
- Validation needed: an integration fixture containing a product project and a dependent failing test project; `release-verify --project <product>` must run/fail that test or refuse the incomplete scope.

### MEDIUM

#### CTK-AUD-003 — `github pr-status` always uses unsupported `gh` JSON fields

- Files: `tools/AgentTool.cs` (`github pr-status` dispatch), command help; no command-level test exists
- Defect: the command invokes `gh pr status --json currentBranch,createdBy,needsReview`. The installed GitHub CLI rejects `currentBranch` immediately; its supported fields include `headRefName`, `author`, `reviewDecision`, and `statusCheckRollup`, not any of the three requested names.
- Consequence: the advertised command fails before reading PR status on every repository with this CLI surface.
- Smallest fix: request supported fields that represent branch, author, review decision, and checks, and compact that stable shape.
- Validation needed: a fake `gh` process/argument contract test plus a non-network CLI capability check; observed reproduction was exit 1, `Unknown JSON field: "currentBranch"`.

#### CTK-AUD-004 — Release identity is duplicated and neither validation nor workflow binds it to the tag

- Files: `config/toolkit.json`, `plugins/codex-toolkit/.codex-plugin/plugin.json`, `plugins/codex-toolkit/plugin.json`, `plugins/codex-toolkit/version.json`, `tools/AgentTool.cs` (`Validation.Run`, `Release`), `tests/AgentTool.Tests/MetadataTests.cs`, `.github/workflows/release.yml`, `docs/release-process.md`
- Defect: four machine-readable copies currently say `0.1.0`, but validation checks only the two plugin manifests for deep equality and never compares either to toolkit/version metadata. The tag workflow accepts any `v*.*.*` tag without checking its value against the archive metadata.
- Consequence: a `vX.Y.Z` draft can contain an archive identifying as another version, and ordinary validation will pass.
- Smallest fix: choose one canonical version, validate all required consumers against it, and make the release job reject a mismatched tag. `plugins/codex-toolkit/version.json` has no reader or documented consumer and is better deleted unless an upstream contract is identified.
- Validation needed: negative metadata tests for each mismatched copy and a workflow/script test for mismatched `GITHUB_REF_NAME`; inspect the packaged manifest.

#### CTK-AUD-005 — Runtime configuration silently accepts schema-forbidden keys; fast validation does not apply schemas

- Files: `tools/AgentTool.cs` (`AgentTool.Json`, `Settings.Load`, `Validation.Run`), `config/*.json`, `schemas/*.schema.json`, `tests/AgentTool.Tests/MetadataTests.cs`
- Defect: schemas set `additionalProperties: false`, but normal deserialization ignores unknown properties. `Validation.Run` parses JSON and performs a few manual checks but does not evaluate the schemas; only the test suite does. Thus a misspelled setting can be silently ignored by normal commands, and standalone `validate`/`release` can accept schema-invalid configuration.
- Consequence: operators can believe a safety/limit setting is active when defaults are actually used, and an invalid configuration can be packaged outside the full-test workflow.
- Smallest fix: disallow unmapped JSON members for runtime settings and make `validate` evaluate every config/upstream file against its declared schema (while retaining relational runtime checks such as threshold ordering).
- Validation needed: unknown-key and wrong-type cases for every config record; assert both command loading and `validate`/`release` reject them with the file/key named.

#### CTK-AUD-006 — Every non-help command eagerly loads all settings, blocking recovery commands on unrelated config damage

- Files: `tools/AgentTool.cs` (`Main`, `Execute`, `Settings.Load`), `docs/installation.md`, `README.md`; no CLI recovery test exists
- Defect: `Main` calls `Settings.Load` before dispatch even for `uninstall`, `install --dry-run`, `results context`, `upstream status`, and `validate`, although these commands do not need all four operational settings files.
- Consequence: after a bad checkout update or malformed unrelated JEV/output config, users cannot run the documented uninstall/recovery path or even use `validate` to aggregate structural failures; installed links may remain stranded.
- Smallest fix: load only the settings required by the selected command, and keep installation/results/validation recovery surfaces independent of unrelated configuration.
- Validation needed: CLI-level tests using a temporary toolkit with malformed `jev.json`; uninstall and results context must still work, while JEV-dependent commands must fail explicitly.

#### CTK-AUD-007 — Optional `--bin` installation is not invocable through Windows PATH

- Files: `tools/AgentTool.cs` (`Installer.Plan`, `Processes.OnPath`), `docs/installation.md`, `.github/workflows/ci.yml`, `tests/AgentTool.Tests/InstallationTests.cs`
- Defect: `--bin` always creates an extensionless `~/.local/bin/codex-agent-tool` symlink to the C# file. Unix can use its executable bit/shebang; Windows command discovery requires a recognized executable/script extension and does not execute that extensionless file-app link. Windows CI only checks link creation/removal, not invocation.
- Consequence: installation can report success on Windows while the advertised `codex-agent-tool` command is unavailable.
- Smallest fix: either explicitly reject `--bin` on Windows and direct users to `dotnet <path>`, or provide a tested Windows launcher without duplicating production logic.
- Validation needed: a Windows CI test that installs into a temporary home, prepends its bin directory to PATH, and runs `codex-agent-tool -- help` (or asserts the deliberate unsupported-platform error).

#### CTK-AUD-008 — API-check configuration detection ignores inherited MSBuild configuration

- Files: `tools/AgentTool.cs` (`Projects.HasApiChecks`, `Projects.Evaluate`, `Dotnet`), `plugins/codex-toolkit/skills/api-compatibility/SKILL.md`; no inherited API-check test exists
- Defect: `HasApiChecks` reads only the selected project XML for a literal `PackageReference` or `PackageValidationBaselineVersion`. Valid analyzer references/properties imported from `Directory.Build.props`, `Directory.Packages.props`, or another MSBuild import are invisible, unlike repository health which correctly uses evaluated properties.
- Consequence: `dotnet api-check` refuses valid centrally configured projects, a common repository layout.
- Smallest fix: use evaluated MSBuild items/properties for detection and distinguish PublicApiAnalyzers from package-validation baselines accurately.
- Validation needed: fixtures with inherited `PublicApiAnalyzers` and inherited package-validation baseline configuration, plus an unconfigured negative case.

#### CTK-AUD-009 — Skill “evaluation” gates do not execute or grade the stated behaviors

- Files: `tools/AgentTool.cs` (`Evaluation.Run`), `evals/*/eval.yaml`, `evals/*/fixtures/scenario.json`, `.github/workflows/eval-skills.yml`, `.github/workflows/release.yml`, `docs/evaluation.md`
- Defect: offline eval checks only that a fixture is an object and three strings exceed ten characters. Measured mode trusts caller-supplied `success` and counters; it does not execute a skill or grade output against `expected`/`safety`. The release workflow runs only the offline integrity check.
- Consequence: all 25 skill instructions or triggers can regress semantically while CI and release “eval” remain green; the repository rule requiring observable skill regression cases is not enforced.
- Smallest fix: keep keyless CI, but add deterministic fixture assertions where possible and a separate manual trusted runner/grader that produces measured records tied to revision/model/prompt. Rename the smoke gate if it remains metadata-only.
- Validation needed: intentionally violate one skill’s expected safety behavior and prove the behavior gate fails; reject unattested or wrong-revision measured records.

### LOW

#### CTK-AUD-010 — Native code-mapper instructions name a nonexistent top-level command

- Files: `agents/code-mapper.toml`, `tools/AgentTool.cs` help/dispatch
- Defect: instructions say to prefer `repo locate and affected-projects`; the actual command is `repo affected-projects`.
- Consequence: the agent may attempt `affected-projects` and fall back to manual/broader discovery.
- Smallest fix: spell the fully qualified command exactly.
- Validation needed: metadata test that declared AgentTool command references resolve to help/dispatch entries.

#### CTK-AUD-011 — Help advertises `--bin` for uninstall although the parser rejects it

- Files: `tools/AgentTool.cs` (`Help`, `Cli.ValidateCommand`), `docs/installation.md`, `tests/AgentTool.Tests/CliParsingTests.cs`
- Defect: `install | update | uninstall [...] [--bin]` visually applies `--bin` to all three, while `ValidateCommand` permits it only for install/update.
- Consequence: a user following help receives an avoidable unsupported-option error during uninstall.
- Smallest fix: split the help lines or explicitly scope `--bin` to install/update.
- Validation needed: snapshot/contract test aligning help option declarations with `ValidateCommand`.

#### CTK-AUD-012 — UI/native-agent metadata validation is mostly existence/parse-only

- Files: `tests/AgentTool.Tests/MetadataTests.cs`, `tools/AgentTool.cs` (`Validation.Run`), `agents/*.toml`, `plugins/codex-toolkit/skills/*/agents/openai.yaml`
- Defect: skill UI YAML is only required to exist and parse; required keys, `$skill` prompt reference, length/identity, and unsupported keys are not checked. Native agents are checked for four fields but not model/reasoning value validity or unique names. Current files appear internally consistent, but malformed metadata would pass the repository’s structural gates.
- Consequence: a release can contain undiscoverable/misrouted skills or unusable native agents even though validate/metadata CI passes.
- Smallest fix: add explicit schemas/contracts for both metadata shapes and uniqueness/reference checks; avoid duplicating description text if the host can derive it.
- Validation needed: negative cases for missing/default prompt, wrong `$skill`, duplicate native name, and invalid reasoning value.

## Better deleted or consolidated

- Delete `plugins/codex-toolkit/version.json` unless a concrete consumer requires it; it is an unvalidated fourth version copy.
- If no supported consumer requires `plugins/codex-toolkit/plugin.json`, remove that mirror and validate only `.codex-plugin/plugin.json`; otherwise document the consumer and generate/compare the mirror from the canonical manifest.
- Do not delete `global/AGENTS.md`: it has a distinct installed scope and appropriately defers to local instructions.

## Evidence and validation performed

- `git status --short --branch`: clean `main` at start.
- `git log -8 --oneline --decorate` and the three commit stats inspected; no tags exist.
- `dotnet tools/AgentTool.cs results context handoff --json`: latest Phase-2 handoff complete, no carry-forward.
- `dotnet tools/AgentTool.cs validate --json`: status `ok`, 55 JSON files, zero errors.
- Counts: 25 skills, 25 eval directories, 25 skill UI metadata files, 3 native agents.
- `gh pr status --json currentBranch,createdBy,needsReview`: deterministic exit 1 on unsupported field before network access.
- Full test/format/release validation was not rerun because this audit was read-only and the request explicitly excluded re-proving known full-validation state.

## Unresolved

None. Findings are implementation work for a separate authorized task.

## Follow-up

Address HIGH findings before relying on upgrade lifecycle or project-scoped release verification. MEDIUM findings should precede the first tagged release. Re-run targeted regressions for each fix, then the full suite for the cross-cutting installer/config/release changes.

## Artifact paths

- `.agent-results/audits/20260921T195122Z-structural-completeness-2026-09-21.md`
