# Complete prepared Brevo identity flow - D1227

## Owner scope and actual completion

The owner directed completion of the Brevo code without waiting for credentials:
only the sender address and key entry should remain. This completes the prepared
mail path after [D1226](brevo-identity-delivery-d1226.md); it does not activate real
mail, public routing, paid service, a Store, household production or any account.
[The operational guide](../../../../../eng/hosting/BREVO.md) and its two-input
settings template are ready. No Brevo template ID or further source modification
is required for the selected sender.

## Complete original paths

- The actual client source-generated identity JSON captures the UI language at
  request entry. The service normalizes only en/fa/de/es/fr/it and stores that
  language inside the original protected challenge. Later UI/worker culture cannot
  relabel the submitted message. No region, financial data or new permission is sent.
- Format2 has complete confirmation/recovery instructions in all six languages,
  escaped HTML/plain text and RTL-safe Latin codes. Published versioned resources
  remain immutable. Historical format1 keeps its exact earlier body; unknown
  versions refuse rather than borrowing a newer template.
- A correct original unconfirmed account can explicitly recover an expired
  confirmation through registration again. The existing account lock serializes
  requests; an active code is retained. Incorrect passwords/confirmed/deleted
  owners do not get a replacement or password overwrite. Expired old codes remain
  unusable, and anonymous responses do not reveal account existence.
- Runtime key input accepts ordinary UTF-8 editor BOM/line endings. Each new
  request reads the original private file; credential/request/envelope diagnostic
  formatting is redacted. A stalled body after response headers still hits the
  complete20second deadline without provider acceptance or private exception text.
- The original register/confirm/recover/reset endpoints, protected outbox, provider
  receipt and consumed challenge are exercised together. No mail code bypasses
  Identity's token/password validation, session retirement or original user locks.

There is no database-model/migration change. Device settings, Main/private source,
financial ledger, quotas and accepted financial/authentication checkpoints are untouched.

## Actual focused evidence

31 distinct new managed conditions pass on .NET10.0.15, zero errors/failures/skips:
the initial29 cover six-language/purpose output, actual client/server JSON,
historical body, diagnostics, BOM/rotation, stalled-body expiry and the complete
fictitious identity flow; only two later new format/expired-retry conditions run
after the retry addition. These are explicit source lineages, not31 cases all
rerun on the last test binary or a full-suite acceptance.

The original fictional registration's unknown provider return retains exact bytes
and language across retry. Its actual confirmation then succeeds. Original recovery
issues a different purpose/code and the original endpoint changes that fictional
password; two challenges are accepted/consumed, one user remains, sessions/objects0.
This exercises real framework identity logic with a fictional account and an
in-memory provider, not owner authentication, real email or a live purchase.

A separate fictional source covers initial missing key, expiry, refusal of the
expired code and wrong password, two concurrent correct callers, one new envelope,
preserved Italian language despite a later French request, one submission after
the key fixture appears, and no new code after confirmation. No previous setup,
successful payment, real credential or financial action is replayed.

The first combined class/method selector selected zero and was rejected as
evidence despite exit0; its logs are retained. Only those two unstarted methods
then execute separately, each with nonzero XML counts.

Strict Windows app and complete dual-ABI Android builds pass with zero warnings/
errors. The signed offline APK retains384AOT/408ELF/1608static16KiBLOAD, original
serviced Mono hashes, v2/v3, ZIP CRC/alignment and nondebug status. It is not
installed on Main or used to claim live online identity/whole-PE/device acceptance.

The current Linux source image passes strict compilation. Twelve actual packaged
language/purpose renderer conditions pass in that image without network, credentials,
database or financial input. After the real retry source change, the final immutable
image is rebuilt and its packaging rechecked; old managed/purchase suites are not
replayed. Exact source manifests/images/used artifacts remain local under
`artifacts/d1227-complete-brevo-identity-flow`.

## Original CI failures corrected without hiding them

One prior exact-head observation found failed local-identity run43971780274. Only
its two failed job logs are fetched. Other job results come from returned metadata;
no successful job log or rerun is requested.

1. Identity job133665478042 fails because the two dedicated-mail database tests
   dynamically skip with missing configuration while `-failSkips` is required.
   They and the new full-flow cases now have a separate `IdentityMailLocal` fixture
   job provisioning all four exact owned databases. General identity selection
   excludes only that fixture trait; actual runner discovery and parsed workflow
   confirm the split. The dedicated job keeps `-failSkips`; coverage is not removed.
2. Purchase job133665478605 fails at the existing retirement test's expectedPlus/
   actualFree. Its fictional term ends16November2026 at00:00UTC while that HTTP
   host uses wall time. The original fixed fixture clock is now injected, matching
   the original October/November facts. Production expiry policy and provider facts
   are unchanged. Only this failed method is rechecked on a fresh disposable source,
   without rerunning the nine successful service stages; its one verification and
   retirement pass. This is fictional service qualification, not a real Store action.

The workflow test-runtime selection also matches the existing central10.0.15
servicing choice. The validator is MIT PyYAML6.0.3 from official PyPI, installed
only beneath this review directory after the initial missing-module refusal;
`safe_load` is used, no global environment/package change. The initial too-long
log-archive command is preserved as an OS206 refusal; the original decoded logs
are retained without re-requesting them. These observation errors are not test passes.

Five incoming commits affect Dastan and its solution declarations only. They are
fast-forwarded with this dirty, disjoint work preserved. Current explicit Zanance
project inputs remain unchanged by that merge. The unrelated untracked benchmark
file, frozen cleanup scopes, one-shot crash classifications and native runtime/GC
remain untouched. Further performance/quality remains deferred by the owner.

## Owned output retirement

The final signed APK, selected API/test/Data assemblies and packaged language
resources are copied and hash-checked before five scoped standard Cleans. All
five finish successfully; used images, exact sources, logs and test evidence
remain. No supplementary deletion or frozen scope is attempted.

The two fresh fictional PostgreSQL containers are stopped and removed by their
exact recorded IDs/labels, after proving their databases use tmpfs without a
persistent volume. Every prior container ID remains present. The initial cleanup
guard expects tmpfs in `Mounts` and refuses before any stop; the same Docker
inspection proves `HostConfig.Tmpfs` instead, so only that observer is corrected.
No successful test or fixture preparation is replayed.

## Remaining external acceptance

For the Brevo code, only verified sender/runtime-key input remains. No real Brevo
request, owner credential, email, DNS or public deployment is performed here.
Provider receipt cannot prove inbox delivery. Actual account/domain/processor and
recipient acceptance are checked only when the owner supplies the real inputs;
this record does not pre-claim them or overall project/release completion.
