# Final cross-repository adversarial v2 review

- Review date: 2026-09-24
- Product: `simplexidev/codex-toolkit@5a4b35e5ccd96392e91a11c4048c4497cdcee655`
- Human docs: `simplexidev/codex-toolkit-docs@1aa4675662a0321fc4ee973658ab636bbc0a7628`
- Metrics: `simplexidev/codex-toolkit-metrics@8d7590705da9300b0f602dcce5c8bee600873c7f`
- Outcome: 4 HIGH and 1 MEDIUM actionable findings; no CRITICAL finding established.

## Findings

### CTK-V2-FR-001 — HIGH — The v2 acceptance aggregate is not bound to its evaluation plan or reviewed baseline identity

- Owning repository: `simplexidev/codex-toolkit-metrics`
- File/capability: `src/CodexToolkit.Metrics/V2AcceptanceAggregator.cs:7-40,64-99`; `tests/CodexToolkit.Metrics.Tests/V2AcceptanceAggregatorTests.cs`; v2 acceptance integrity.
- Consequence: `aggregate-v2-acceptance` accepts every schema-valid record found under any `records/` directory when the toolkit SHA and broad optimized-arm enums match. It does not load the acceptance plan, require the expected scenario/arm/repetition matrix, reject duplicates or unknown scenario IDs, compare compatibility hashes, or verify the baseline document's schema, subject revision, approval, and lineage. `acceptance-scenario-coverage` is then hard-coded to `1`, and a `gpt-` model prefix is reported as validated OpenAI provider compliance even though provider identity is absent from the record. An incomplete, duplicated, stale, or hand-assembled record directory and an unrelated aggregate with the three expected metric names can therefore publish 100% acceptance coverage, misleading pass/regression rates, and GPT-only compliance.
- Smallest fix: require the plan path and expected reviewed-baseline identity as aggregate inputs; validate both; require exactly one compatible record for every planned scenario/arm/repetition tuple and reject missing, duplicate, unexpected, or hash-incompatible records. Preserve executor provider or a signed/hashed plan identity in each record and compute scenario coverage from the verified matrix instead of a constant.
- Validation: add negative tests for a one-record subset of the 17-case plan, a duplicate passing record, an unknown scenario, a wrong repetition/arm/hash, a synthetic or wrong-revision baseline, and a non-OpenAI provider identity; all must fail aggregation. A complete exact matrix must reproduce the reviewed aggregate.

### CTK-V2-FR-002 — HIGH — Published capability coverage counts scenarios, not covered product capabilities

- Owning repository: `simplexidev/codex-toolkit-metrics`
- File/capability: `src/CodexToolkit.Metrics/V2AcceptanceAggregator.cs:26-37`; `scenarios/v2-acceptance-v1.json`; `data/public/v2-acceptance.json`; `reports/v2-acceptance.md:20-25`; declared capability evidence.
- Consequence: the published 54.84% value is `17 scenario IDs / 31`, with `31` hard-coded. The scenario capability labels are evaluator-local labels such as `single-test`, `compiler-build-failure`, and `agent-boundary`; they are not mapped to the 31 IDs in the product's `config/capabilities.json`, and several scenarios exercise the same product capability family. The number is therefore not a conservative measurement of unique declared capability coverage and can rise merely by adding another case for an already-covered capability. It is used as the stated reason for the REVISE decision and as a dashboard quality gate, so both numerator and release interpretation are unsupported.
- Smallest fix: add explicit product capability IDs to each acceptance scenario, validate them against the subject revision's capability manifest, and compute coverage from the distinct validated IDs over the manifest's actual count. Keep scenario completion coverage as a separate metric.
- Validation: duplicate scenarios for one capability must not increase capability coverage; unknown and removed capability IDs must fail validation; a fixture with a known mapped subset must yield the exact distinct-ID ratio; changing the product manifest must invalidate or deliberately version the acceptance plan.

### CTK-V2-FR-003 — HIGH — The prerequisite acceptance result is REVISE and lacks evidence for the advertised routing benefits

- Owning repository: `simplexidev/codex-toolkit-metrics`
- File/capability: `reports/v2-acceptance.md:3,20-25,40-50,78-92`; `data/public/v2-acceptance.json`; v2 release acceptance, delegation/routing/JEV evidence.
- Consequence: the final metrics artifact explicitly concludes `REVISE`. Delegation was not observed, tool/delegation observations and files/context plus build/test classification are absent, and JEV false-exclusion and avoided-context coverage are both 0%. The token improvements for the optimized agent boundary are therefore a prompt-boundary comparison, not evidence that the custom reviewer ran or saved context; JEV's exclusion safety and context benefit are also unmeasured. Treating the completed phase name or its 17/17 marker checks as full v2 acceptance would release unsupported efficiency, delegation, or JEV-benefit claims.
- Smallest fix: keep the current snapshot as bounded regression evidence, add only targeted behavior-measuring cases for the uncovered release claims, capture structured activation/delegation/tool/context/build-test observations, and add calibrated JEV inclusion ground truth plus an avoided-context comparator. Require a deliberate ACCEPT/REVISE release decision after CTK-V2-FR-001 and CTK-V2-FR-002 are corrected.
- Validation: the replacement acceptance report must distinguish executed custom-agent delegation from boundary prompts, report nonzero evidence coverage for every release claim or mark the claim unavailable, preserve quality-first gates, and reach ACCEPT before a v2 release is declared ready.

### CTK-V2-FR-004 — HIGH — The public dashboard presents synthetic fixtures as current product measurements

- Owning repository: `simplexidev/codex-toolkit-metrics`
- File/capability: `data/public/publication-manifest.json:3-10`; `data/public/example-summary.json`; `data/public/history-baseline.json`; `dashboard/app.js:121-157,161-192,240-252`; Pages publication and public metrics provenance.
- Consequence: the production manifest publishes two artifacts whose provenance is `synthetic` and whose revisions are obvious placeholders. The dashboard sorts all artifacts together, overwrites a global `latestMetrics` map by metric name, and renders cards without the source revision or approval. Values available only from `example-summary.json`—including 94% skill-activation precision, 88% delegation success, 96% tool success, and 67% JEV bounded coverage—therefore appear in the product overview as the newest available measurements. The history chart also draws trends across synthetic and reviewed, incompatible scenario sets. The hero shows only the newest snapshot's revision and a combined source-run count, which cannot identify the provenance of those cards.
- Smallest fix: remove demonstration fixtures from the production publication manifest and move them to test fixtures, or segregate them into an unmistakable demo view. For real history, retain each card's source snapshot/revision/approval and compare trends only within an explicitly compatible series.
- Validation: dashboard tests must prove a synthetic artifact cannot satisfy a production gate or current overview, every displayed value exposes its source revision and approval, and incompatible scenario series are not connected as a trend. Regenerate Pages and verify the deployed project-path artifact.

### CTK-V2-FR-005 — MEDIUM — The mandatory agent-consumed v2 baseline still exposes the v1 inventory as the active release baseline

- Owning repository: `simplexidev/codex-toolkit`
- File/capability: `global/AGENTS.md:7-8`; `plugins/codex-toolkit/references/v2-baseline.md:1-7,23-65`; runtime release-line and capability-baseline guidance.
- Consequence: installed global instructions direct agents to this file for release-line and capability-baseline questions. Its `Release inventory` lists the historical 24 skills, obsolete skill IDs such as `diagnostics`, `dotnet-verify`, `performance-investigation`, and `test-quality`, 37 commands, and 19 test suites, while the accepted candidate has 29 skill directories, a substantially expanded help surface, and 23 focused test classes. Its limitations also say human documents and evaluation scenarios still need migration after those sibling-repository phases completed. Although the introduction mentions the v1.0.0 anchor, the operative heading and mandatory consumer path do not separate the immutable v1 starting snapshot from current v2 candidate state, so agents can make stale ownership, coverage, and release decisions.
- Smallest fix: preserve the v1 data as explicitly historical, add a compact current v2 candidate inventory generated or checked from canonical manifests/help/config, and point `global/AGENTS.md` to the current baseline for active decisions. Avoid manually duplicated counts where a canonical machine-readable source exists.
- Validation: extend metadata tests so current skill IDs/count, command inventory, test/eval inventory, completed migration state, and the runtime baseline cannot drift; confirm the installed reference remains product-local and all relative consumers resolve.

## Review evidence

- Confirmed the prerequisite commits are merged: metrics full-v2 acceptance on `main` and docs final human review on `main`.
- Product: 175/175 tests passed; structural validation parsed 69 JSON files with no errors; all 29 offline fixture-integrity checks passed; isolated install/update/doctor/uninstall and release archive smoke checks passed.
- Metrics: 59/59 tests passed; the current static-cost aggregate reproduced exactly from the product revision; the Pages workflow and latest deployment succeeded; GitHub reports the required project Pages URL with workflow deployment and HTTPS.
- Runtime/docs: one plugin directory, one template directory, one native agent, no missing product runtime-reference target, no broken local human-doc link, and no failing checked external documentation link. Current official OpenAI documentation supports the configured `gpt-5.6-terra` model and `high` reasoning effort.
- Safety/ownership: no tracked secret-like credential or tracked local absolute path was established; normal product and metrics tests remained keyless; lifecycle child-process handling strips `TYPESAFE_API_KEY`; no Claude executor/judge or product-owned metrics/dashboard implementation was found; the pinned `dotnet/skills` commit and MIT notice were verified.

No implementation fix was made in this review phase.
