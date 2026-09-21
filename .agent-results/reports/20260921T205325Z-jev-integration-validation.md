# Report: JEV integration validation

- Time (UTC): 2026-09-21T20:53:25Z
- Starting HEAD: 2a5c0700cb9f42ef7d9c75ab5ffdc9447b9bce55
- Branch: main
- Status: complete

## Scope and policy

Validated the TypeSafe/JEV integration with deterministic fake HTTP responses and synthetic
credentials only. `TYPESAFE_API_KEY` was absent from this process, so live validation was
NOT PERFORMED. No filesystem, profile, configuration, environment.d, or secret-store search
was made for a credential.

## Results

- Request formation matches the documented endpoint, bearer authorization, model/state/questions
  envelope, and Noul, Choice, and Score shapes. Screen dry-run creates one bounded Noul request
  per candidate.
- Default Noul thresholds route 0.70+ to INCLUDE, 0.10- to EXCLUDE, and ambiguity to REVIEW.
  The regression flow deterministically narrows candidates, screens the remainder, and sends only
  INCLUDE plus REVIEW to Codex.
- Choice and Score enforce distributions and minimum confidence. Score now also requires the
  provider's required legend and preserves that normalized field in cache.
- Cache hashing is canonical over request objects, preserves ordered rubrics, includes the endpoint,
  reads/writes deterministically, avoids a repeated HTTP request, supports expiry/disable behavior,
  and persists neither requests nor credentials.
- JEV_MODE off/auto/required, absent credentials, timeouts, HTTP failures, malformed and oversized
  responses, sensitive inputs, state/request limits, and candidate limits all retain REVIEW.
  Required-mode service failures additionally return exit 3.
- Malformed screen input (missing query/candidates/id/text, wrong shapes, or duplicate ids) now
  returns REVIEW before classification, with no candidate discarded.
- Synthetic credentials are redacted, confined to bearer authorization, stripped from child-process
  environments, rejected in child arguments, and absent from cache, configuration, and logs.

## Verification

- Focused JEV/configuration/credential suite: 66 passed, 0 failed, 0 skipped.
- Final full suite (run once after changes): 118 passed, 0 failed, 0 skipped.
- Repository structural validation: 53 JSON files, 0 errors.
- JEV evaluation fixture integrity: passed.
- `git diff --check`: passed before report creation; repeated before commit.

## Changes

- Hardened screen input validation and conservative REVIEW fallback in `tools/AgentTool.cs`.
- Validated and cached required Score legends.
- Added `tests/AgentTool.Tests/JevIntegrationValidationTests.cs` for the integration matrix and
  token-saving flow.
- Synchronized JEV documentation and the JEV evaluation fixture.

## Unresolved

None. Optional live validation remains NOT PERFORMED because the credential was absent.
