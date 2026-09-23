---
name: security-scan
description: Run or triage deterministic security analyzers and SARIF for a scoped change.
---

# Security Scan

Use configured CodeQL, DevSkim, language analyzers and dependency audits as applicable. Prefer `sarif summarize --file PATH`; when a trusted prior scan exists, pass `--baseline PATH` and investigate new findings first without hiding unchanged or fixed counts. Preserve severity, rule IDs, locations and suppression context. Do not let JEV downgrade findings or authorize actions. Avoid external transmission of sensitive source; validate fixes with the originating rule.
