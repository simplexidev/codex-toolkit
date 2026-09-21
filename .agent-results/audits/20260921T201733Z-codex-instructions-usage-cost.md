# Audit: codex instructions usage cost

- Time (UTC): 2026-09-21T20:17:33.1874366+00:00
- HEAD: 3ae404b1b01b971fce02665b0e0f52e20c37e28c
- Branch: main
- Status: complete

## Purpose

Audit only Codex-facing instructions and their expected usage cost. No implementation,
skill, agent, schema, or documentation source was changed.

Scope read in full:

- `AGENTS.md` and `global/AGENTS.md`
- `agents/code-mapper.toml`, `agents/log-analyzer.toml`, and `agents/reviewer.toml`
- all 25 `plugins/codex-toolkit/skills/*/SKILL.md` files
- all 25 `plugins/codex-toolkit/skills/*/agents/openai.yaml` files
- the latest handoff, `20260921T194238Z-phase-2-credential-boundary.md`
- directly needed references: the two JEV skill references plus token-efficiency,
  security, JEV, configuration, and model-routing policy

History inspected: `c441205` (initial instruction set), `5616a52` (durable results),
`ba627a3` (Phase 2 credential boundary), `e70f74c` (structural audit), and `3ae404b`
(structural-audit fixes). Current files are authoritative.

## Findings or summary

### Executive result

The instruction set is generally disciplined: triggers usually name a concrete task;
release-scale validation is opt-in; normal discovery excludes `.agent-results`; hard
security/API facts stay deterministic; and no skill asks for a giant evaluation matrix.
The largest avoidable costs are repeated skill boilerplate, two mechanical subagents,
and three implicitly invocable skills that overlap baseline behavior or another skill.

Measured source size is 4,264 words across the audited surfaces. The identical footer in
all 25 skills accounts for 1,300 words in source and about 52 words on every activated
skill. This is not necessarily 1,300 words per prompt because skill bodies are lazy, but
it creates recurring per-activation cost and 25 maintenance copies. Eighteen of 25 skills
permit implicit invocation; seven require explicit invocation. A subagent costs an
additional model turn and context load, while a live JEV use can incur provider usage, so
those activation boundaries matter more than small wording savings.

### Recommended changes, ordered DELETE > SHORTEN > MOVE > ADD

#### CODX-001 — Delete the repeated skill footer

- Files: all 25 `plugins/codex-toolkit/skills/*/SKILL.md`
- Problem: every skill repeats command-wrapper syntax, “read AGENTS,” tool-unavailable
  fallback, compact-output, and on-disk-log instructions. Reading applicable AGENTS,
  graceful fallback, and concise reporting are already global/platform behavior. The
  command syntax is reference material, not task guidance.
- Recommended change: delete the four-line footer from every skill. Put wrapper syntax in
  one lazy `references/commands.md` only if users actually need both invocation forms;
  link it only from skills whose command spelling is ambiguous.
- Impact category: prompt/context cost; duplication; maintenance drift.
- Simplification risk: low. Preserve the `--` separator detail in the lazy reference.

#### CODX-002 — Shorten generic AGENTS duplication

- Files: `AGENTS.md`, `global/AGENTS.md`
- Problem: both files repeat deterministic-first routing, search-before-read, targeted
  validation, bounded output, Git preservation, and merge safety. The final safety
  sentence in `global/AGENTS.md` also restates modern platform safety behavior. In this
  repository both files are in force, so the duplication is always-on rather than lazy.
- Recommended change: keep cross-repository routing/output rules only in
  `global/AGENTS.md`; keep repository-specific production, test, installer, fake-JEV, and
  durable-result rules only in `AGENTS.md`. Delete generic merge/destructive-work wording
  already guaranteed by the platform unless the installer must support older clients.
- Impact category: always-on token cost; contradiction/drift risk.
- Simplification risk: medium because older or non-Codex consumers may lack equivalent
  safety defaults. Confirm supported clients before deleting those safety clauses.

#### CODX-003 — Delete or make `repo-locate` explicit

- Files: `plugins/codex-toolkit/skills/repo-locate/SKILL.md`,
  `plugins/codex-toolkit/skills/repo-locate/agents/openai.yaml`
- Problem: “locate relevant repository files or symbols before broad reading” matches
  nearly every coding task, duplicates both AGENTS files, and permits implicit activation.
  The work itself is deterministic (`repo locate`, `rg`, project metadata). Loading a
  skill—and potentially considering JEV—for the baseline discovery step adds cost without
  a distinct user intent.
- Recommended change: delete the skill and document `repo locate` as ordinary tool help.
  If retained for discoverability, set `allow_implicit_invocation: false` and require an
  explicit repository-mapping/search request.
- Impact category: accidental activation; prompt/tool/JEV cost.
- Simplification risk: low. The deterministic command remains available.

#### CODX-004 — Make live JEV activation explicit

- Files: `plugins/codex-toolkit/skills/jev-judgment/SKILL.md`,
  `plugins/codex-toolkit/skills/jev-judgment/agents/openai.yaml`
- Problem: a potentially billable external judgment skill allows implicit invocation.
  Its narrow description helps, but model-selected activation is still a weaker spending
  boundary than an explicit request or a named parent-skill step.
- Recommended change: set `allow_implicit_invocation: false`. Other skills may still use
  a bounded JEV step when their own instructions explicitly require the post-deterministic
  filter; direct `$jev-judgment` use remains available.
- Impact category: external usage cost; accidental activation; data egress.
- Simplification risk: low. Some beneficial semantic filtering will require one explicit
  step instead of auto-activation.

#### CODX-005 — Repair the Phase 2 secret-handling wording

- Files: `plugins/codex-toolkit/skills/jev-judgment/SKILL.md`
- Problem: “Use TYPESAFE_API_KEY from environment” is broader than the latest handoff and
  current security/JEV policy. It can be read as an instruction for Codex to obtain, set,
  or manage the secret, while Phase 2 says injection/storage are external and the key may
  exist only in the specific AgentTool process or narrow live-JEV session. This is the
  only Phase 2 instruction gap found; no instruction asks to print, persist, pass, or test
  with a live key.
- Recommended change: replace that sentence with a boundary, not a procedure: Codex must
  never request, store, echo, or inject the key; a live call is allowed only when the
  credential is already externally and narrowly configured for AgentTool. Move the
  variable name and setup details to the existing lazy JEV/security reference.
- Impact category: secret handling; policy contradiction; external usage cost.
- Simplification risk: low. Users retain setup documentation outside the prompt.

#### CODX-006 — Prevent duplicate package-audit activation

- Files: `plugins/codex-toolkit/skills/dependency-change/SKILL.md`,
  `plugins/codex-toolkit/skills/package-audit/SKILL.md`, and
  `plugins/codex-toolkit/skills/package-audit/agents/openai.yaml`
- Problem: `dependency-change` already mandates `dotnet package-audit`, while the package
  audit skill is implicitly invocable “after dependency changes.” Both can activate for
  the same task and encourage duplicate restore/network/audit work.
- Recommended change: set package-audit implicit invocation to false and describe it as a
  standalone explicit audit. Keep the deterministic audit step inside dependency-change.
- Impact category: duplicate validation; network/tool time; context cost.
- Simplification risk: low. Dependency changes still receive the audit once.

#### CODX-007 — Delete JEV suggestions for structured deterministic evidence

- Files: `agents/log-analyzer.toml`,
  `plugins/codex-toolkit/skills/architecture-change/SKILL.md`,
  `plugins/codex-toolkit/skills/dependency-change/SKILL.md`, and
  `plugins/codex-toolkit/skills/performance-investigation/SKILL.md`
- Problem: affected-file graphs, package warnings, structured diagnostics, and
  CPU/GC/I/O categories already have deterministic metadata or are better handled by
  Codex reasoning after bounded summaries. JEV classification here adds a billable
  probabilistic layer without materially reducing context or deciding the task.
- Recommended change: delete those JEV clauses. Retain JEV only for genuinely semantic,
  large candidate sets such as review-comment disposition, documentation relevance, or
  sanitized text relevance after exact filtering. No missing high-value JEV opportunity
  was found in the audited instructions.
- Impact category: unnecessary JEV calls; correctness/false-confidence risk.
- Simplification risk: low. Existing deterministic summaries and Codex remain.

#### CODX-008 — Delete mechanical subagents

- Files: `agents/code-mapper.toml`, `agents/log-analyzer.toml`
- Problem: code mapping is already `repo locate`/`rg`/project metadata, and log triage is
  already `logs summarize`/`sarif summarize`. Spawning a model for either repeats tool
  output in a second context and is the largest avoidable model cost in this audit.
  Their instructions mostly restate the deterministic commands and global boundaries.
- Recommended change: delete both agent definitions. Let the primary Codex call the
  deterministic tools and reason only over bounded output. If log analysis is retained,
  narrow it to an explicitly requested, multi-artifact ambiguity that remains after the
  summarizer, and inherit the parent model rather than promising runtime fallback inside
  the agent prompt.
- Impact category: subagent/model-call cost; duplicated context; deterministic delegation.
- Simplification risk: medium. A separate context can help with unusually large evidence
  sets, so preserve that as an explicit exceptional workflow if it is measured useful.

#### CODX-009 — Bound durable-artifact reads by exact path

- Files: `agents/log-analyzer.toml`,
  `plugins/codex-toolkit/skills/diagnostics/SKILL.md`,
  `plugins/codex-toolkit/skills/performance-investigation/SKILL.md`
- Problem: “inspect/reuse existing artifacts first” is sound, but without an exact-path
  rule it can lead to broad `.agent-results` enumeration, contradicting `AGENTS.md`.
- Recommended change: if these surfaces remain, say to use a caller-provided path or
  `results context/latest TYPE`, then open only the selected artifact. Never scan
  historical results during normal discovery.
- Impact category: broad reads; context/I/O cost; stale-evidence risk.
- Simplification risk: low. The latest/context lookup preserves discovery without a sweep.

#### CODX-010 — Move AgentTool implementation detail out of `dotnet-verify`

- Files: `plugins/codex-toolkit/skills/dotnet-verify/SKILL.md`
- Problem: Compile/ProjectReference traversal, conservative widening, and IsTestProject
  detection explain deterministic implementation that the command already performs.
  This invites Codex to re-derive or revalidate the graph and makes the task prompt longer.
- Recommended change: keep “run affected-projects, then verify with the same base” and the
  trusted-MSBuild warning. Move graph/detection mechanics to command help or a lazy
  reference used only for troubleshooting surprising scope.
- Impact category: prompt cost; duplicate reasoning; unnecessary revalidation.
- Simplification risk: low. The trust warning and observable command output remain.

#### CODX-011 — Shorten YAML interface copy and remove clipped descriptions

- Files: all 25 `plugins/codex-toolkit/skills/*/agents/openai.yaml`
- Problem: every default prompt repeats “for the scoped task and return evidence,” which
  is already in global instructions and the common skill footer. Many generated short
  descriptions end mid-word with `...`, weakening UI clarity and trigger review.
- Recommended change: shorten default prompts to `Use $skill-name.` and write complete,
  narrow short descriptions rather than character-sliced copies. Keep interface metadata
  only where the host requires it.
- Impact category: invocation prompt cost; UI/trigger clarity; generated duplication.
- Simplification risk: low.

#### CODX-012 — Narrow the retained reviewer agent trigger

- Files: `agents/reviewer.toml`
- Problem: the reviewer is the one subagent with a strong independent-context use, but
  its current description can cover any completed change while pinning high reasoning.
  That makes routine edits eligible for an expensive second pass.
- Recommended change: retain it only for an explicitly requested independent review or a
  substantial/high-risk completed diff. Keep read-only behavior and defect-only output.
  Do not automatically invoke it after ordinary implementation.
- Impact category: subagent/reasoning cost; accidental revalidation.
- Simplification risk: medium. Narrowing can reduce defense in depth on medium changes;
  targeted tests and primary-agent review remain.

### Coverage and no-change conclusions

- `address-pr-review`, `agent-maintenance`, `api-compatibility`, `benchmark`,
  `docs-impact`, `dotnet-format`, `finish-pr`, `issue-start`, `release-verify`,
  `repo-health`, `reproducible-build`, `sbom`, `security-scan`, `test-quality`, and
  `versioning`: triggers and task bodies are acceptably narrow after CODX-001. Their
  explicit on-demand/cross-cutting validation boundaries should remain.
- `diagnostics`: trigger and evidence selection are narrow; keep dump-locality and
  bounded-duration guidance. Apply CODX-001 and CODX-009 only.
- `roadmap-next`: current JEV use is acceptable only for ambiguity in issue prose after
  explicit blocker/milestone metadata is evaluated deterministically. Shorten the clause
  to state that condition; do not use scores to choose strategy.
- `prepare-commit`: implicit activation is acceptable because it performs scoped local
  safety checks when commit preparation is actually intended. It must not cause a second
  full test run.
- `jev-judgment/references/primitives.md` and `thresholds.md`: appropriate lazy split.
  Read the former only to construct a direct request and the latter only when interpreting
  or calibrating a result; do not load both automatically.
- Full builds, release verification, mutation testing, SBOM, reproducibility checks, and
  broad formatting are already opt-in. No additional full-build/test restriction is
  needed.
- No instruction recommends repository-wide source reads. The only broad-read risk is
  historical artifact discovery covered by CODX-009.

## Decisions

- Recommend instruction-only changes; do not modify AgentTool behavior.
- Prefer deletion of redundant instructions and agents over moving them into always-on
  global text.
- Keep one independent reviewer workflow, but make its expensive trigger explicit.
- Keep semantic JEV only where exact filtering leaves a large ambiguous text set; do not
  add JEV anywhere else in this instruction set.
- No representative eval was run: the task changed no behavior, and the relevant evidence
  was exact file coverage, word/activation counts, Git history, and policy comparison.
  A future instruction change should run only metadata/skill regression cases covering
  trigger policy, generated YAML completeness, and the Phase 2 wording.

## Unresolved

- Verify whether every supported Codex client supplies modern merge/destructive-action
  safety before deleting the duplicated global clauses in CODX-002.
- Measure whether `reviewer` finds material defects often enough on medium changes to
  justify high reasoning; no usage telemetry was available in scope.
- Confirm whether `agents/openai.yaml` requires nonempty `short_description` and
  `default_prompt` before deleting fields rather than shortening them.

## Follow-up

Apply findings in priority order: CODX-005, CODX-004, CODX-003, CODX-006, CODX-008,
CODX-001, then the lower-cost shortening/move items. Keep each behavior change paired
with its existing metadata/evaluation coverage. This audit itself makes no such changes.

## Artifact paths

- Durable audit: `.agent-results/audits/20260921T201733Z-codex-instructions-usage-cost.md`
- Phase 2 handoff: `.agent-results/handoffs/20260921T194238Z-phase-2-credential-boundary.md`
