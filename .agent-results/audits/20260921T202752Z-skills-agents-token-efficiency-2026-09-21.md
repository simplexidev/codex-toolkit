# Audit: skills agents token efficiency 2026 09 21

- Time (UTC): 2026-09-21T20:27:52.0819902+00:00
- HEAD: 87660f17af3e8412333bd5dc504b779e62262b83
- Branch: main
- Status: complete

## Purpose

Implement the latest skills, agents, and token-efficiency audit after reading the prior
audit and credential-boundary handoff.

## Findings or summary

- SKAT-001: Removed the repeated six-line wrapper/footer from every skill and shortened
  every UI default prompt to `Use $skill.`.
- SKAT-002: Replaced clipped UI descriptions with complete, narrow descriptions.
- SKAT-003: Deleted the generic `repo-locate` skill and its evaluation; deterministic
  repository search remains ordinary AgentTool/rg work.
- SKAT-004: Deleted `code-mapper` and `log-analyzer`; their mechanical work remains
  bounded deterministic commands in the primary context.
- SKAT-005: Direct JEV and standalone package audits now require explicit activation;
  dependency changes still run the package audit once.
- SKAT-006: Removed JEV from structured package, architecture, and performance evidence;
  retained it only for bounded semantic ambiguity.
- SKAT-007: Skills never name or handle the TypeSafe key, and artifact reuse requires an
  exact caller path or a `results context/latest` selection.
- SKAT-008: Retained `reviewer` only for explicit or substantial high-risk independent
  review; ordinary edits do not trigger it.

## Decisions

Keep global platform safety wording because client-compatibility evidence was not in scope.
Add metadata regression coverage for deleted surfaces, explicit activation, complete UI
text, removed boilerplate, and credential-free skill bodies.

## Unresolved

- SKAT-D01: Measure reviewer defect yield before changing its model or reasoning level.
- SKAT-D02: Confirm supported-client safety guarantees before further reducing global
  safety wording.

## Follow-up

No action required. Future instruction changes should retain narrow triggers and run the
metadata regression plus affected offline evals.

## Artifact paths

- Targeted tests: `.agent-results/test-results/skills-agents-audit-targeted/skills-agents-audit-targeted.trx`
- Full tests: `.agent-results/test-results/skills-agents-audit-full/skills-agents-audit-full.trx`
- Measurements: audited instruction source fell from 4,264 to 2,462 words (-1,802;
  -42.3%), 25 to 24 skills, and 3 to 1 native agents; implicit skills fell from 18 to 15.
- Validation: structural validation, three representative offline evals, full suite, and
  `git diff --check` passed.
