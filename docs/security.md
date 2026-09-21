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
