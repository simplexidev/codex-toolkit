---
name: benchmark
description: Create or run an explicitly requested .NET performance benchmark.
---

# Benchmark

Use BenchmarkDotNet from upstream/tools.json when appropriate and already available or authorized. Define input sizes, baseline, runtime and hardware. Separate setup from measured operations. Preserve raw output; report distributions, allocations and reproducibility conditions. Do not infer improvement from code appearance. Do not install or benchmark unrelated changes.

Commands above use `codex-agent-tool --` from the optional bin link, or
`dotnet /path/to/codex-toolkit/tools/AgentTool.cs --`. The separator prevents SDK option interception.
Read target-project AGENTS.md first.
If the utility is unavailable, use equivalent deterministic tools and preserve the same safety boundaries.
Return a compact outcome, evidence and artifact paths; full logs stay on disk.
