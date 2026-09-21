# JEV

The client follows TypeSafe's [HTTP quickstart](https://docs.typesafe.ai/introduction/quickstart):
POST https://api.typesafe.ai/v1/systemone with bearer authentication, model, state and
questions. Noul returns probability; Choice selects named criteria; Score uses ordered
levels. Responses are type/range/distribution checked before use. No live API call is
part of the tests or bootstrap validation.

`TYPESAFE_API_KEY` is the only application-level secret input. Deliberately inject it
into the specific AgentTool process or narrowly scoped terminal session used for live JEV
work. Secret storage and injection are outside AgentTool and the toolkit. Do not place it
in arguments, files, JSON, `.env`, shell profiles, `environment.d`, keyrings, desktop
credential stores, or a desktop-session-wide environment. Optional non-secret overrides
are documented in [configuration](configuration.md). `doctor` reports only
`JEV credentials: configured` or `JEV credentials: unavailable`. auto and off always preserve normal Codex behavior;
required reports exit 3 on service failure while still marking the result REVIEW.
Uncertain valid judgments remain REVIEW. No retries can accidentally multiply billing.
Redirects are disabled to avoid forwarding credentials to another endpoint.

Create a small sanitized JSON input:

```json
{"state":"README describes build setup","instructions":"Is this relevant to build documentation?"}
```

Run dotnet tools/AgentTool.cs jev noul --input safe.json --dry-run.
To transmit this exact reviewed input, omit --dry-run and add --safe-input. This flag
asserts caller review of the payload, not a guarantee of automated secret detection.
Never send .env content, credentials, complete private repositories or oversized excerpts.
Choice adds a criteria object mapping labels to descriptions. Score adds an ordered
criteria array. Screen accepts query plus candidates with id and text and returns an
individual judgment for every candidate. Query, ids and text must be non-empty strings,
and ids must be unique; malformed screen input keeps every candidate for review. Narrow
candidates before screening.

Noul relevance >= .70 is INCLUDE, <= .10 is EXCLUDE, everything else REVIEW.
Choice/Score require confidence >= .80. Open INCLUDE and REVIEW items. These heuristics
need task-specific evaluation; never use them for security authorization or exact facts.

Cache hashes include canonical request and endpoint; files contain responses only,
never requests or keys. Default TTL is 24 hours. Set cacheHours to 0 to disable; remove
with jev cache-clear. Cache state is ignored and best-effort. Model aliases can move, so
pin a provider model when repeatability matters. Private answers can still be sensitive.

Normal GitHub CI is keyless and uses fake HTTP responses. The optional `Live JEV integration`
workflow is isolated in the dedicated `jev-integration` GitHub Environment. Configure its
`TYPESAFE_API_KEY` Environment secret in GitHub; the workflow does not create, populate or
read the value. It is available only to the one synthetic smoke-call step, runs by manual
dispatch or a weekly trusted schedule, and never runs for pull requests (including forks).
