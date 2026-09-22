# Releases

Run full tests, validate, offline scenario checks and git diff --check. Verify plugin manifests and
config/toolkit.json share a version. Publish a reviewed vMAJOR.MINOR.PATCH tag only when
authorized; the release workflow packages the source/configuration users need and writes
a SHA-256 checksum. It does not include tests, caches or developer tooling.

GitHub CLI creates a draft release for human review. The workflow has write permission
only for that job. CodeQL remains a separate security gate where GitHub code scanning is
available; the workflow reports the private-repository limitation without pretending that
an upload occurred. Configure repository branch protection and maintainers after hosting.
