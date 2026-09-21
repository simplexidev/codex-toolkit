# Configuration

Edit central config/*.json; the schemas describe allowed values. AgentTool finds its
checkout from its source path; override with --toolkit or CODEX_TOOLKIT_ROOT. --root
selects the target repository. Configuration is loaded from the toolkit, not untrusted
project files. Config/model-routing.json is advisory; native agent TOML selects models.
Enabled integrations are descriptive preferences, not an automatic plugin installer.

JEV environment overrides: TYPESAFE_API_URL (complete HTTPS endpoint), JEV_MODEL,
JEV_MODE, JEV_TIMEOUT_SECONDS. `TYPESAFE_API_KEY` is the only secret input: its value is
confined to the credential boundary and applied only to JEV HTTP authorization. It is
removed from every child-process environment and is never accepted as a CLI option or
persisted. Inject it only into the specific live-JEV process or narrowly scoped session;
storage and injection are external.
Thresholds, cache lifetime and input limits live in jev.json. Output limits control
summary lines/items. repo-health.json controls evaluated MSBuild policy.

Install --home uses an isolated profile and deliberately ignores ambient CODEX_HOME;
--codex-home explicitly overrides its Codex directory. Without --home the user profile
and CODEX_HOME are honored. Existing config.toml is never edited.
