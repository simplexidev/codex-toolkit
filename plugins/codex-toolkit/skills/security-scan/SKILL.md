---
name: security-scan
description: Run or triage deterministic security analyzers and SARIF for a scoped change.
---

# Security Scan

Use configured CodeQL, DevSkim, .NET analyzers and package audit as applicable. Prefer `sarif summarize --file PATH` then open relevant locations. Preserve severity, rule IDs and suppression context; do not let JEV downgrade hard findings or authorize actions. Avoid external transmission of sensitive source; validate fixes with the originating rule.
