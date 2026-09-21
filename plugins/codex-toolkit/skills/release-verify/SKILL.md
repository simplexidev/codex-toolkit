---
name: release-verify
description: Run explicitly requested release or complete .NET validation.
---

# Release Verify

Use `dotnet release-verify --project PATH` for restore, build, format verification, detected tests and audit. Add project-configured package validation/API baselines, reproducibility checks and SBOM separately; the utility does not certify absent gates. Release verification is intentionally broader than ordinary change validation. Preserve reports and list every executed, skipped or blocked gate.
