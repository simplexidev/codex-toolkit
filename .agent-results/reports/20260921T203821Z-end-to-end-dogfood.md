# Report: end to end dogfood

- Time (UTC): 2026-09-21T20:38:21.4458345+00:00
- HEAD: 2a5c0700cb9f42ef7d9c75ab5ffdc9447b9bce55
- Branch: main
- Status: complete

## Purpose

Perform an end-to-end dogfood run of the published installer and repository workflows without reading or writing the developer's real Codex profile.

## Findings or summary

- Installer lifecycle passed in `/tmp/codex-toolkit-lifecycle.y6OZAm` with isolated HOME, CODEX_HOME, XDG, and DOTNET_CLI_HOME paths: install, native agent/global instruction/24 skill/bin verification, repeat install, update, doctor, replacement preservation, uninstall, and repeat uninstall all behaved as documented.
- A temporary single-project repository and a library-plus-app repository passed changed-files, locate, affected-projects (including the reverse dependent), health, verify, Git state, prepare-commit, results lifecycle, log summary, and SARIF summary checks. Full command evidence is in `.agent-results/logs/dogfood-*.log`.
- A synthetic `TYPESAFE_API_KEY` was used only for the JEV dry run and a child-process verify invocation. It was absent from generated AgentTool artifacts and logs. No live JEV request was made.
- `validate` passed (54 JSON files, no errors) and offline evaluation integrity passed for all 24 installed skills.
- No product defect was confirmed. A formatter failure and test-run failure are attributable to this sandbox denying named pipes/local sockets; ordinary isolated builds succeeded. A suspected formatter argument issue was rejected after rerunning via the documented `dotnet ... -- <command>` invocation.

## Decisions

- No production code or tests were changed: the apparent formatter issue was a malformed test invocation, not advertised behavior.
- The durable report is the only repository change; transient command output remains in ignored `.agent-results/logs` and temporary target repositories remain under `/tmp`.

## Unresolved

- This sandbox cannot validate `dotnet format` or `dotnet test` execution because their hosts require IPC endpoints denied here. The formatter was nevertheless reached correctly, and prior normal test evidence remains available in the latest handoff.

## Follow-up

Stop. Re-run formatter and the full test suite in an environment that permits local IPC if fresh execution evidence is required.

## Artifact paths

- `.agent-results/logs/dogfood-lifecycle-20260921T203301Z-rerun.log`
- `.agent-results/logs/dogfood-targets-continuation-20260921T203506Z.log`
- `.agent-results/logs/dogfood-targets-final-20260921T203628Z.log`
- `.agent-results/logs/dogfood-advertised-20260921T203724Z.log`
- `.agent-results/logs/dogfood-single-20260921T203746Z.log`
