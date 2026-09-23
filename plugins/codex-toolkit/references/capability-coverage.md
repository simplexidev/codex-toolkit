# Capability coverage engineering reference

This is a compact runtime reference for agents and metrics consumers, not the
human product manual. The source of record is the versioned
[capability manifest](../../../config/capabilities.json), validated by its
[JSON schema](../../../schemas/capabilities.schema.json).

## Audit result

The audit inventories 31 common repository/workspace capability areas: 10 are
supported, 17 are partial, and 4 are gaps. Coverage means the toolkit has the
correct routing and a useful workflow; it does not imply every platform mutation
is automated. Commit, push, PR merge, publication, external tool installation,
and destructive cleanup remain authority boundaries.

The default routing order is deterministic tooling, bounded JEV judgment, normal
agent reasoning, then stronger reasoning only for unusually consequential or
difficult analysis. JEV is suitable only for sanitized ambiguous candidate sets;
it is not a substitute for Git state, compiler/analyzer output, advisories,
measurements, package identity, licenses, security findings, or authorization.

Official .NET skill routing and pinned source provenance are recorded separately
in [dotnet-skills-provenance.md](dotnet-skills-provenance.md). That inventory is
complete at its pinned SHA but remains lazy routing metadata, not default context.

## Portfolio findings

| Area | Finding | Engineering direction |
| --- | --- | --- |
| Repository, Git, .NET, tests | Strong structured core | Keep exact discovery and validation in AgentTool; use skills for narrow workflow policy. |
| GitHub | PR/review basics exist; Actions run diagnostics are absent | Add one bounded read-only run/job/log surface before a consolidated CI-triage skill. |
| Supply chain | Vulnerability checks exist; license compliance and package inspection do not | Define machine-readable policies and artifact schemas before skill instructions. |
| Build/release | Broad release checks exist; reproducibility, SBOM and remote asset verification remain separate | Emit composable evidence and eventually a release attestation rather than a monolithic command. |
| Maintenance | Upstream and plugin lifecycle are bounded and conservative | Move evaluation ownership to metrics in its own phase; let metrics ingest this manifest. |
| Repository hygiene | Known reference checks exist; generated/dead-file analysis is absent | Produce evidence-backed read-only candidates first; never auto-delete. |

## Recommended sequence

1. Add deterministic GitHub Actions run/job/failed-log inspection with strict
   output bounds and sanitized fixtures. Reuse `logs summarize`; add at most one
   CI-triage skill after measured need.
2. Define resolved dependency license and artifact/package schemas. Implement
   inventory and policy facts before adding any compliance workflow guidance.
3. Add non-mutating Git conflict forecasting and SARIF baseline/diff output to
   existing command families.
4. Add opt-in reproducibility and remote release-asset verification, then compose
   their results into a machine-readable release attestation.
5. Add generated/orphan candidate reporting only when provenance and reachability
   evidence can be explained per item.

No new skill is justified by this audit alone. The current overlaps are mostly
intentional composition: `dotnet-verify` executes tests while `test-quality`
assesses them; `dependency-change` consumes `package-audit`; diagnostics selects
evidence while performance investigation reasons from it; and release verification
orchestrates but does not claim optional API, SBOM, packaging, or reproducibility
gates. Metrics should track activation precision, static routing cost, unnecessary
broad operations, fallback rate, and correctness by capability ID.

## Deterministic expansion

The first post-audit expansion adds versioned structured contracts for repository
summaries and file ownership; .NET SDK, TFM, package and project-graph inspection;
resolved direct/transitive dependencies; non-executing build, test and diagnostics
plans; and bounded TRX, JUnit, Cobertura and OpenCover summaries.
`config/agent-tool-contracts.json` is the compact source
for metrics consumers. Plans expose executable argument arrays but do not cross
the existing mutation or sensitive diagnostics-collection boundaries.
