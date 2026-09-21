---
name: performance-investigation
description: Investigate an observed .NET latency, CPU, allocation or memory regression.
---

# Performance Investigation

State the symptom, workload and measurement window. Reuse a caller-provided artifact or `results context/latest` selection before collecting more; never enumerate historical artifacts. Separate CPU, contention, I/O, GC and retention hypotheses using evidence. Follow the narrowest signal to code. Confirm the fix under a comparable workload; create benchmarks only if they answer the observed question.
