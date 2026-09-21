---
name: diagnostics
description: Collect or inspect .NET runtime counters, traces, dumps or GC artifacts for a reported issue.
---

# Diagnostics

Choose the smallest evidence source: dotnet-counters for live symptoms, dotnet-trace for timing, dotnet-gcdump for managed retention, dotnet-dump for state; dotnet-monitor for ongoing collection when appropriate. Specify process and bounded collection duration. Dumps may contain secrets; keep local. Parse a caller-provided artifact or `results context/latest` selection before collecting more; never enumerate historical artifacts.
