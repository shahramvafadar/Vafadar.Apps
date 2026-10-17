# D-304 billing HTTP qualifier isolation

## Actual failure and cause

The D-303 Local identity backend run 39012610813 fails job 117338141114 in the protected original-client step.
Seven client cases pass; Real_service_retirement_cannot_regain_paid_cache_after_cold_restart_or_outage throws
the transport's generic refusal during purchase verification. The initial CI log does not expose its status.
The one-use follow-up was deleted and no second workflow-runs status request was made.

Standalone exact-case Windows and Linux 10.0.13 observations pass. One fresh, isolated reproduction of the actual
persistence-then-client sequence reproduces the same case's failure, with bounded diagnostics exposing HTTP429.
The service correctly partitions its durable RequestWindows by lane and remote address. In-memory TestServer
hosts had a null address and shared one billing partition; their aggregate requests crossed 30 in one minute.
Order/minute boundaries explained the apparently unrelated backend failure after a Home-only change.

## Repair and invariant

A test-only IStartupFilter assigns connection metadata from an explicit fixture peer name. Addresses are
stable, unrouted 2001:db8::/32 documentation values; headers cannot select them. Independent fixtures get separate
peers, while explicitly shared peers keep one budget. Product middleware, 30-request policy, Retry-After,
PostgreSQL counting, authentication, purchase authority and cryptography are unchanged.

The previously rate-refused private client resolves its same retained pending command before any new explicit
restore attempt. Successful original work is not queried again; a definitive non-finish receipt can permit the
fixture's explicit restore retry. No financial or actual store payment is made. HTTP diagnostics report only
numeric status, not response bodies, receipts, references, credentials or key material.

The anonymous HTTP fixture uses another explicit Integration worker when continuing. Its existing billing
signer, protected client state, customer and PostgreSQL schema stay unchanged. A missing ephemeral HTTP
installation key is never recreated or ignored; its old installation is retained. This is client/billing
continuation, not cold acceptance of that unrelated service installation.

## Verification scope

- The exact failed retirement case succeeds on the original retained Linux state after the repair.
- New AT-304 regression: requests 1..30 succeed, 31 returns 429 with Retry-After 60; another peer succeeds; another
  worker with the original peer remains limited; advancing only the injected test clock by one window permits
  the next request. No sleep, counter deletion, raised quota or production bypass makes the test pass.
- Original seven successful client cases are not replayed after the repair. The preceding full sequence was
  necessary to reproduce the state-dependent failure after standalone cases passed.
- Strict test compilation and the Linux self-contained 10.0.13 publish have zero warnings/errors. The local
  temporary connection guards are restored to their original 55475 before commit. The isolated reproduction uses
  an already cached exact CI PostgreSQL image, a separate container/schema and the cached noble 10.0.13 runtime.
- The original password-bearing fixture is untouched; no password was read. Its attempted additional schema
  stayed empty after authentication refusal. The minimal PostgreSQL image could not execute a glibc app host;
  no case ran there. The existing noble image supplies the correct runtime without new downloads.
- A first continuation correctly refused the old ephemeral service installation with no persistent keys.
  All original files remained retained; another explicit HTTP worker subsequently continued the same billing
  authority/client. The original missing-key guard remains unchanged.
- The minimal noble runtime reports an optional missing Kerberos library while these explicit fictional trust
  connections still succeed. This is not new integrated-authentication, real TLS, production-hosting or Store evidence.

Exact logs, retained container/schema identities and XML receipts: artifacts/billing-ci-repair-d304.
The failed original sequence, isolated standalone results, key-guard refusal and final retirement/rate XML
remain distinct. Temporary test ports were source-only local guards and were restored; no owner container,
credential, original successful fixture, app screen or APK is reset. The new fixture data is entirely fictional.

No unrelated application/UI, cipher, synchronization, whole-suite or APK check is repeated. Production bytes
remain the D-303 main package; no user-visible behavior, schema, dependency, public purchase or release gate is
changed. Hosted completion after the repair must be reported from its own actual execution.


## Actual hosted repair result

The one-use check of commit 534c883522dfdb919cbc75b36ea7f46e3bdf8d54 confirms completed/success for
[Local identity backend](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39016047872),
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39016047828) and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39016047850). The backend qualifier therefore
passes on its actual hosted runner after the isolated-peer repair. Its one-use follow-up is deleted;
ci-once.json preserves the exact receipt. No unrelated workflow execution or release acceptance is inferred.
