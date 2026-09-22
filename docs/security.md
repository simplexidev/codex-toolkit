# Security boundaries

The utility passes argument arrays without a shell and bounds execution time. Git mutations
are limited to clean, attached, operation-free issue branch creation. PR prepare/status
commands never commit, push, publish or merge. gh uses the user's existing authentication.

Installer ownership and exact symlink targets protect user replacements. State writes
refuse symlink traversal. They do not defend against a malicious process running as the
same user racing filesystem writes. Keep the checkout and home private and trusted.

Full build logs, traces and dumps may contain sensitive data even when terminal summaries
are redacted. .agent-tool is ignored; do not publish its contents without review. The
heuristic JEV secret check is defense in depth, not a DLP guarantee. Use only sanitized,
minimal data. Test doubles prevent live billing. Dependency and action updates need review.

The TypeSafe boundary accepts one application secret: `TYPESAFE_API_KEY` from the
AgentTool environment. AgentTool redacts it at structured terminal and overflow-artifact output boundaries and never logs, caches, describes, or
passes it in command arguments, and all child processes receive an environment with that
variable removed. Only the JEV HTTP request path can apply it as bearer authentication.
Inject it deliberately into the one process or narrow session doing live JEV work;
credential storage and injection are outside the toolkit. Do not use repository files,
`.env`, JSON, shell profiles, `environment.d`, OS credential stores, or desktop-wide
session variables for this purpose.

GitHub CI remains keyless and mocked. Any optional live check belongs in a dedicated,
protected environment (for example `jev-integration`) with `TYPESAFE_API_KEY` scoped only
to that job, triggered manually and/or on a low-frequency trusted schedule, and disabled
for untrusted fork pull requests.
