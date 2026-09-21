---
name: api-compatibility
description: Check public .NET API compatibility after an actual public surface change.
---

# Api Compatibility

Find changed public symbols and existing PublicApiAnalyzers or package validation baselines. Use `dotnet api-check --project PATH` only when configured; it refuses absent baselines/analyzers. Examine diagnostic IDs and API baseline deltas. Codex considers consumer impact only for actual changes. Do not automatically accept generated API baselines; intentional breaks require migration/versioning decisions.
