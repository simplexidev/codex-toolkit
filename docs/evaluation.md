# Evaluation

Every skill has an eval.yaml scenario, expected behavior, safety invariant and fixture.
Files use JSON syntax, a YAML 1.2 subset. The default AgentTool eval performs inexpensive
offline scenario and fixture integrity checks only. Unit tests provide actual utility regression coverage.
Neither is represented as proof of agent skill quality or token savings.

For behavior evaluation, run each scenario with and without the skill using the same
model, repository revision and prompt. Grade correctness against expected and safety
fields, then supply measured records to eval --results results.json. Each record contains:

```json
[{
  "skill":"test-quality", "success":true, "tokens":1200, "turns":3,
  "toolCalls":5, "elapsedSeconds":20, "fileReads":2,
  "unnecessaryBroadOperations":0,
  "baseline":{"success":true,"tokens":1800}
}]
```

Use --skill test-quality for that single record. Missing measurements fail. The comparator
checks absolute budgets and a maximum 10% token regression versus a successful baseline.
Budgets are evaluation thresholds, never live development abort rules. Record provider
usage rather than guessed tokens. Do not bill model/JEV calls in normal CI.

The dotnet/skills skill-validator and evaluation infrastructure are preferred for deeper
agent runs from a separately reviewed checkout. This bootstrap does not guess a changing
upstream CLI. Manual CI accepts a checked-in measured results file and an optional skill.
