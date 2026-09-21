# Skill authoring

Use a precise trigger in frontmatter and a short SKILL.md focused on non-obvious decisions.
Place detailed formats in references only when needed. agents/openai.yaml supplies UI
metadata; on-demand expensive/mutating skills disable implicit invocation. Preserve the
user's intent and existing authorization. Do not copy upstream .NET skills.

Add a realistic positive scenario, negative trigger and safety invariant under evals.
Test deterministic supporting behavior in AgentTool.Tests. Use standard YAML/TOML/schema
parsers in metadata tests and available official skill validators for structural checks.
Keep docs synchronized with actual commands. A scenario document alone is not proof that
an agent follows it; measured forward runs are described in evaluation.md.
