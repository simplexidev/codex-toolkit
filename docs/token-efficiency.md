# Token efficiency

Compute Git state, project dependencies, audit findings and log summaries in code.
Narrow search before opening source. Keep raw artifacts on disk; return a bounded subset
plus counts and paths. Use JEV only when exact filtering fails and a bounded answer avoids
larger reads. Retain uncertain candidates and escalate generation/debugging to Codex.

Measure correctness alongside tokens, turns, tool calls, elapsed time and file reads.
A cheaper result that misses a defect is a regression. Full release verification and
mutation testing are deliberately on-demand. No routine task is aborted at a token limit.
