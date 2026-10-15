# Complete local personal sync batch (owner approved 2026-10-15)

Status: implementation in progress. This document does not constitute completed application-feature acceptance.
The previous complete delivery is D-291. Do not commit isolated helpers from this batch as delivery.

## Cohesive implementation plan

1. Use one versioned allowlisted relational contract derived from the actual model. Track committed
   inserts/updates/deletes through profile-local SQLite triggers and durable outbox/known versions.
   Capture an immutable whole-transaction-visible cutoff under one reader, coalescing rows through
   that cutoff. Do not modify each editor or perform network work while saving locally.
2. On the existing authenticated local PostgreSQL service, bind spaces/devices/staged rounds/events
   to the actual owner/session. Stage bounded immutable parts and private blob objects; publish the
   complete validated round atomically. Retain tombstones and authentic version history. Serialize
   commits per space, check actual foreign keys/uniqueness and original financial invariants. The
   same occurrence cannot become a second server entry. Different retry content under the same
   operation/part identity is refused. Encrypt stored financial payloads with maintained protection.
3. Three-way merge only disjoint authentic-baseline fields; competing money, edits/deletes and
   resurrection require explicit review. An approval is bound to the current version and must be
   refreshed after another device edits it. Conflict rounds publish no partial finance. Apply feed
   changes atomically with outbox suppression and fresh captured-profile authority. Preserve local
   edits made while requests were in flight.
4. Strip sync state from portable/device safety exports. A restored generation cannot reactivate
   stale device/profile authority; keep it local until explicit reconnection and review. Never
   remember a backup password or put service credentials/security state into portable preferences.
5. Connect optional sign-in/space selection, complete status/queue/error/conflict and device
   revocation surfaces through original access/lifetime gates. Store tokens only in platform secure
   storage. Local recording survives outages. Keep offline manifests without INTERNET; qualify an
   explicitly network-enabled local review build. Require authenticated TLS, without a global trust
   bypass or device/OS settings change. No real mail, paid hosting, sharing or commercial activation.
6. After implementing the complete batch, run focused actual PostgreSQL + two isolated SQLite
   clients: concurrent settle/amount conflicts, identical and altered replay, interrupted staging/
   commit/ack, edits during flight, foreign owner/revoked device, restart and old-backup reconnection.
   Regenerate required migrations/compiled models, perform strict Windows/Android builds, native
   changed-screen fa/en/de/theme/width checks and a complete signed Release APK. Reuse unrelated
   successful evidence. Update current privacy/spec/backlog/acceptance/docs and push one completed
   verified delivery. Hosted CI is checked once afterward, separately from device/product acceptance.

External gates remain physical device, iOS/Mac/signing, real provider acceptance and publication.
The approved local personal scope does not imply household permissions, subscription lifecycle,
paid resources, commercial limits or public release.

## Implementation contracts under construction

- Core now contains protocol DTOs/AOT JSON metadata and a conservative authentic-baseline field
  merge. These are unfinished feature work, not an independently delivered helper step. No feature
  acceptance tests have run yet; final checks follow the complete service/client/UI composition.
- The schema exporter reads the actual EF design model without opening a data connection. It
  reuses the already qualified Windows native package; no legacy native bundle is added to the
  service. The first provider-free metadata attempt was refused before reading any financial data;
  the qualified provider succeeds. Initial contract: 21 tables, 294 columns, digest
  c521518276effefe5c7b52b38113a9f4ed520025ba6628f3ed670aa38bddda71.
- This reviewed Core manifest is the server authority, not a schema supplied by a client. ImportLinks
  (local guarded Undo journal), ResourceSelections (local commercial selections) and private Sync*
  metadata are excluded. Settings has a virtual singleton key; keep its actual device-local Id and
  security/notification choices. Only the explicit financial/display allowlist travels. UpdatedAt
  is local technical audit state; financial dates never derive from a sync timestamp.
- Pure SQLite triggers must capture complete after-values/tombstones in the same local write
  transaction, using built-in JSON/hex functions only, with Enabled/Applying guards. No custom SQL
  function or per-editor writes are necessary. Under one reader capture the maximum committed
  outbox sequence and coalesce all rows through that immutable cutoff. A committed SQLite
  transaction is wholly visible at this cutoff; stage the whole round before server publication.
  New local writes append after it and must survive acknowledgement. Keep known server bodies as
  well as versions so incoming disjoint edits can rebase pending local work rather than silently
  treating the new server baseline as the user's original baseline.
- Blob hex in a captured local row is converted to standard SHA-256 content references and bounded
  private object parts before transport. Validate ownership, length and concatenated digest, with
  immutable staged parts and resumable cursors; no plaintext staging file or credentials in rows.
- Retain authentic encrypted server event/history bodies and tombstones. Parent/child completeness,
  original unique rules (including non-partial settlement), scalar bounds and original Core
  financial validation must hold over the final graph under the same serialized PostgreSQL commit.
  Do not add an unreviewed SQLite runtime to the server merely to validate a schema.
- A whole-round server conflict publishes nothing. Whole-row owner choices are bound to the
  reviewed current version; a newer remote edit invalidates that approval. Local feed application
  also needs cross-row conflicts (for example a newly edited child of a remotely deleted parent),
  not just field comparison. Never auto-resurrect a tombstone or silently drop an unpublished entry.
- SQLite incoming application must preserve original FK/cascade/unique semantics. Deferred FK
  checks alone do not defer unique indexes and INSERT OR REPLACE can cascade-delete unrelated
  children. If unique-value swaps require temporarily recreating known explicit unique indexes,
  all drops/recreates must remain in the same writer transaction, with final original constraints
  checked before commit and rollback proven; no other writer can observe an absent index.
- Local network acceptance needs explicit TLS and a per-connection verified public certificate pin
  for loopback/emulator alias, never an all-certificates callback or OS trust change. Keep tokens
  in SecureStorage, independent of portable backup passwords. A restored/new generation must fail
  stale binding checks and stay detached until explicit review/reconnection. No periodic worker
  may activate an old restored outbox by discovering old metadata.

Next: finish Core schema/graph validation, both EF metadata/migrations/compiled models, PostgreSQL
staging/history/device routes, the transactional local client and export/restore policy, then the
original optional UI/secure TLS runtime composition before final focused acceptance.

Current implementation checkpoint: the model now has eight separate Sync* metadata tables and
the additive PersonalSyncJournal migration, with regenerated compiled model. Frozen capture SQL
owns 63 triggers across the 21 allowed tables; migration replay never reads a future mutable model.
The exporter now refuses drift in those frozen SQL resources instead of rewriting them. No opt-in,
network client or financial endpoint is activated yet. Core structural validation covers complete
scalars/keys/FKs/unique rules, including full versus partial settlement; semantic original Core
financial validation and the service/client/UI are still incomplete. The canonical typed schema
digest is 4c71696475cef5e2b18c7de99d436c48ee9a38d4ea33ed8e97aea60e366a16fc, independent of
Git LF/CRLF/indentation. Boolean, UTC-tick, decimal and enum/flags constraints are explicit. Prior
raw file hashes above are generator history, not the network compatibility digest.

Microsoft documents RETURNING limitations with some SQLite triggers:
https://learn.microsoft.com/en-us/ef/core/providers/sqlite/misc . Capture triggers do not mutate
the source row or its concurrency values. Do not disable optimized writes speculatively; the
final focused real EF/native checks must cover generated ids, affected counts and repeated Save
under these actual logging-only triggers, with rollback and concurrent occurrence uniqueness.

Implementation compilation checkpoint: strict Data build succeeds without warnings/errors
(artifacts/sync-data-implementation-build.log, 1.63 seconds), including Core protocol/validation,
metadata, frozen migration resources and regenerated model. This is not a migrated/runtime/round/
two-device acceptance result. No old full suite, native corpus, UI matrix or main APK was repeated.

## Service composition checkpoint (unfinished)

The actual API model now includes personal spaces/devices, immutable parts/round identities,
current records, authentic encrypted event history and private blob manifests/pieces. The fresh
unpublished migration was regenerated as its design stabilized; it has never been applied to a
live service or published as a separate step. Both compiled models were regenerated. Expiring
identity sessions must not cascade-delete financial round history: retained device references are
rechecked against a live session on every request, and maintenance revokes inactive devices before
removing expired sessions. Device revocation also revokes its actual session, preventing an old
bearer from registering a new label to bypass it. Labels are framework-protected as well as rows.

The round store stages bounded immutable proposals, verifies ordered complete-part digests,
loads authentic historical baselines, merges only disjoint fields and binds whole-row owner choices
to the reviewed current version. It validates the complete projected structural/ledger graph before
assigning any financial rows/events. Receipt-part manifests must already be verified. Commit and
acknowledgement metadata share the same owner-serialized transaction; acknowledged proposal bodies
are cleared while replay hashes/counts remain. Abandoned operations retain their nonce tombstones.
Feed uses authentic immutable sequence history, rejects missing events and bounds each page by
both rows and bytes. No financial date is derived from an event timestamp. Required wire fields
prevent an omitted nullable value or omitted review boolean from silently becoming deletion/choice.

The Core validator now distinguishes stored primitive JSON collections from arbitrary strings,
rejects malformed keys/scalars, compares GUID identities semantically for duplicate detection and
reuses the original LedgerValidator with complete linked-refund totals. Account/category retained
identity constructors support that projection without changing default construction. Full-settlement
uniqueness still excludes partial payments. Other financial domain validation and duplicate-settlement
review/rekey behavior remain to be completed; structural/ledger code is not whole-product validation.
The current typed compatibility digest is
85ecb37cf2e3813239013c49c6c7938ade1bce3d5d52e7de40b1a55bbb1e4df6.

Catalog, round and blob routes are now registered under the common TLS-only/authenticated boundary.
The local service has a loopback HTTPS listener with a protected self-signed certificate and a public
reviewed fingerprint. This is implementation only: no new worker or migrated live service has run.
The optional native UI and final complete two-device/native acceptance remain unfinished.
Strict API implementation builds pass without warnings/errors, last 1.44 seconds in
artifacts/sync-api-bounded-feed-build.log. No backend was started or database migrated. EF tooling
reports its existing 10.0.12 versus API runtime 10.0.13 patch-version notice during generation;
this is retained separately from the warning-free compiler result. No tool was downloaded/upgraded,
and no old successful feature/UI/native/full-suite acceptance was repeated.

## Client composition checkpoint (unfinished)

The actual Data layer now contains the pinned transport, platform token-storage port, profile-bound
connection review, durable outgoing cutoff/parts, incoming page/piece staging, explicit review snapshots
and the whole-round coordinator. Passwords are request-only; rotating framework credentials are not
financial rows or portable preferences. Per-peer request serialization spans multiple transports so
two profile windows cannot race the same one-use renewal. Sign-out shares that order.

The client must validate the captured generation, actual identity, peer/certificate and device between
network requests and local effects. Local writer transactions end before network waits. A restored or
changed-owner binding cannot silently replay its old outbox. Same-generation device replacement keeps
known baselines and offline tombstones while discarding only the old operation/review/feed staging.
Paused resumption captures current rows plus missing-known-live tombstones; new edits stay beyond the
frozen cutoff. These behaviors are implemented but have not yet passed final two-client acceptance.

Incoming application merges newer local changes against the submitted snapshot or received baseline,
not an advanced server version with an old local body. Explicit reviews bind the payload digest,
operation, phase, local sequence and remote version. Even a proposal that selects an absent server
record and produces no event participates in local acknowledgement. A complete graph refusal writes
only review metadata. Successful financial rows, rebased outbox, known versions and cursor share one
SQLite writer commit. Exact original explicit unique indexes may be dropped/recreated only inside
that transaction to support legal swaps; no REPLACE cascades or disabled foreign keys are used.
Native readback of the complete projection precedes commit. Final runtime proof is still required.

The current unpublished Data migration is `20261015141712_PersonalSyncJournal`; it replaces only the
earlier untracked/unapplied candidate. The compiled model was regenerated, and frozen capture SQL is
still version-owned. The API migration remains `20261015133719_PersonalSyncService`. Neither has been
applied to a live service/application as personal sync acceptance.

The typed contract now includes actual CLR integer/collection bounds. GUID keys use the actual native
uppercase text representation so original parameterized EF callers can find received rows. Current
compatibility SHA-256 is `7a1ec01b41bddf51d896b49ea050a5c7bf85f3293140797ba608b4eee9549a1e`;
the earlier digests above describe unpublished design checkpoints. The metadata exporter still refuses
any drift in frozen capture SQL. Shared graph checks reuse LedgerValidator, Recurrence.Validate,
HoldingsLedger.FindConflict and AccountCurrencyLock. Other original domain constraints and complete
duplicate-settlement review/related-row handling still need qualification before feature acceptance.

Service commit now reopens newly referenced encrypted attachment pieces and verifies their complete
digest. Existing unrelated images are not all decrypted again for a text edit. Identity login, renewal,
reset and revocation use the same owner transaction order as financial publication; a waiting stale
request cannot create a new usable session after reset or publish after session revocation.

Portable sources now have an application-owned copy-only sanitization port. Zanance clears all eight
Sync* tables and its outbox sequence in the isolated copy. The protected exporter creates fresh logical
memory pages and uses schema-scoped secure_delete before clearing ordinary metadata; it creates no
plaintext staging file. The existing plaintext adapter rebuilds only its owned temporary copy because
the online backup API also copies historic free pages. Restoring through that adapter also clears sync
authority. Protected generation restore still requires explicit reconnection under its new actual path.
Final export byte/cursor/queue and restored-profile acceptance has not run yet.

Strict implementation builds for Data/client and API are warning/error-free, latest evidence in
`artifacts/sync-client-financial-graph-build.log` and `artifacts/sync-api-authority-order-build.log`.
These are compilation checkpoints, not feature, service, phone, AOT, UI or release acceptance. No
successful unrelated full application suite, native cipher suite, UI matrix or signed main APK has
been repeated. The incomplete batch remains uncommitted and unpushed.

Next complete the optional original app UI and SecureStorage/lifecycle composition, bootstrap/session
recovery, complete original-domain validation and settlement/restore choices. Then run the focused
actual PostgreSQL/two-SQLite-client failure/replay/conflict/attachment/export/restore scenarios and
native Windows/Release Android acceptance, build/verify the complete signed APK, update canonical
status/decisions and push the one complete verified batch. Do not publish another isolated helper.

Implementation references:
- https://www.sqlite.org/pragma.html#pragma_defer_foreign_keys
- https://www.sqlite.org/lang_conflict.html
- https://www.sqlite.org/pragma.html#pragma_secure_delete
- https://www.sqlite.org/lang_vacuum.html
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions
- https://raw.githubusercontent.com/dotnet/efcore/v10.0.13/src/Microsoft.Data.Sqlite.Core/SqliteValueBinder.cs

## Original application-flow checkpoint (unpublished, 2026-10-15)

The optional original page now exposes reviewed local peer/pin sign-in, retained-session discovery,
separate-copy creation, explicit connection, manual synchronization, pause/sign-out and actual device
revocation. More includes its route only in the explicitly enabled personal-sync edition. Controls
use compiled bindings, semantic wrapping actions and the existing palette. All 54 new resource keys
have English, Persian, German, Spanish, French and Italian translations. This is source integration,
not running-app acceptance: complete financial conflict presentation, first-run recovery and the
foreground scheduler remain unfinished and the page is not a product handoff.

The initial connection snapshot starts covered; failed access leaves only a content-free error and
local-read retry, without opening HTTP or secure credentials. Creation/connection consent now carries
the exact local binding captured before its dialog into the transactional connection check. Pause
checks and writes within one local transaction. Attachment/empty responses recheck foreground access,
and queued native publication rechecks cancellation/access on the actual dispatcher.

`artifacts/sync-native-page-build.log`: original Windows app with
`ZanancePersonalSyncEnabled=true`, strict build, zero warnings/errors. This does not prove layout,
native session storage, network access, Android, first-run restore or two-device behavior.
After the transaction/transport/dispatcher lifetime changes, the original Windows app was compiled
again with zero warnings/errors (`artifacts/sync-page-lifetime-build.log`, 23.97 seconds). The test
solution's build output was cleaned successfully after the focused run.

`artifacts/sync-app-flow-tests.log`: five original application-flow regressions passed. After the
transaction/lifetime changes and two new portable scenarios,
`artifacts/sync-app-flow-portable-tests.log`: seven passed, zero failed/skipped under strict compilation.
The class uses actual application source and isolated migrated SQLite, with only native effects behind
explicit ports. It covers constructor silence/cover, failed initial access and local-only retry,
stale confirmation refusal, retired access during confirmation, and pause preserving finances/queue.
Portable export additionally preserves money while removing all eight replay tables and the private
identity from serialized bytes, without changing the source binding/queue. Restoring an older raw
copy containing sync metadata leaves finances intact and sync disconnected with an empty queue.
Those two tests qualify the plaintext SQLite portable adapter only; protected memory export/native
restore still require their own real-byte evidence. No full-suite count or feature acceptance is inferred.

The batch remains incomplete, uncommitted and unpushed. No unchanged APK, unrelated successful UI
matrix, full application suite or native cipher corpus was repeated at this checkpoint.

## Foreground orchestration checkpoint (unpublished, 2026-10-15)

The original native composition now starts its optional scheduler only after covered database
preparation. Constructors perform no database/network work. A public device interest hint only
avoids work for never-connected installations; the actual enabled binding, generation, identity,
certificate peer and current foreground/unlocked authority still control every attempt.

Native timer/store/access signals coalesce into one original current-profile round. Thirty-second
scope deadlines are checked before another financial read, and outages back off to five minutes.
Unauthorized or connection-review feedback is retained as fixed codes in that exact binding;
automatic requests remain blocked across process restart until explicit reconnection clears it.
There is no response/exception payload in feedback. A different saved public peer receives no request.
Visible status refreshes on the presentation dispatcher without discarding input drafts.

Automatic execution is limited to the original Home, More and personal-sync surfaces without a
modal, and skips a busy manual sync screen. Navigation/modal entry retires captured access before
an editor can open. Native authority is captured before queuing the worker, so a delayed task cannot
recapture a different page as permission. Lock/background/profile changes also retire it. Original
manual sync actions reset scheduling only after their own flow completes; automatic publication
does not reset its deadline and create an empty-round feedback loop.

A newly signed-in session may have a different actual service device. Reviews now carry that
device identity through connect/pause/conflict/failure transactions, and manual sync is disabled
until the current session matches the profile's reviewed device/owner/peer. An old device's delayed
failure cannot overwrite a replacement connection, even when the remote space did not change.

`artifacts/sync-foreground-tests.log`: nine new focused regressions passed. After the device-binding
and durable authorization-wait changes, `artifacts/sync-foreground-authority-tests.log`: ten passed,
zero failed/skipped. These execute original orchestration/flow source against migrated isolated
SQLite and explicit native ports; they cover no-interest/offline refusal, an unconnected profile,
restored metadata refusal, a mismatched peer, bounded outage retries, authorization waiting,
access retired after queuing, replacement device/space feedback refusal and translated visible
feedback. No mock financial algorithm or synthetic MAUI control is used. The transport port refuses
network in these tests: this is not hosted PostgreSQL, real HTTP/TLS, two-device, native navigation,
protected-key or Android acceptance.

Strict original Windows compilation passed before and after the device-binding change in
`artifacts/sync-foreground-native-build.log` and `artifacts/sync-foreground-device-build.log`.
Final wiring also compiles with zero warnings/errors in
`artifacts/sync-foreground-wiring-build.log` (16.04 seconds); no full feature or release acceptance follows.
The complete financial conflict presentation, safe first-run restore, initialization takeover,
remaining original-domain checks and final real-service/two-client/native scenarios remain open.
Do not commit or push this incomplete batch as another isolated scheduler/helper delivery.

## Actual two-client service checkpoint (AT-292, unpublished)

The dedicated `test/Apps/Zanance/Vafadar.Zanance.Sync.Tests` project is outside the SQLite solution
filter. It requires an explicitly named `zanance_identity_sync_*` PostgreSQL database and a separate
absolute `ZANANCE_SYNC_TEST_ROOT`; it refuses arbitrary development/user databases. Run staged
methods explicitly in order, retaining the same database/root between them, rather than relying on
test-runner ordering:

```powershell
dotnet test --project test/Apps/Zanance/Vafadar.Zanance.Sync.Tests/Vafadar.Zanance.Sync.Tests.csproj -p:ContinuousIntegrationBuild=true -- --filter-method '*Two_actual_clients*' --minimum-expected-tests 1
dotnet test --project test/Apps/Zanance/Vafadar.Zanance.Sync.Tests/Vafadar.Zanance.Sync.Tests.csproj -p:ContinuousIntegrationBuild=true -- --filter-method '*Duplicate_full_settlements*' --minimum-expected-tests 1
dotnet test --project test/Apps/Zanance/Vafadar.Zanance.Sync.Tests/Vafadar.Zanance.Sync.Tests.csproj -p:ContinuousIntegrationBuild=true -- --filter-method '*Changed_related_payment*' --minimum-expected-tests 1
```

The actual owned fixture is `artifacts/personal-sync-acceptance/run-a`, with PostgreSQL container
`zanance-backend-sync-acceptance-20261015a`, database `zanance_identity_sync_acceptance_a`, loopback
port 55479 and the existing `postgres:18.4-alpine` image. No image/tool was downloaded. The migration
operator grants the separate non-superuser `zanance_runtime` role only normal table/sequence use.
Only these fictional resources were used; the test preserves successful and failed files.

Initial real TLS failed before enrollment. `artifacts/sync-real-service-run-a.log` and
`artifacts/sync-real-service-original-tls.log` retain the failed client calls;
`run-a/tls-failures.txt` identifies Windows Schannel's refusal of ephemeral private keys. The test
then used the original protected service certificate/listener, not a permissive validation callback.
Windows now imports its wrapped PFX with UserKeySet, without PersistKeySet; the runtime owns the
temporary current-user key container's disposal. The authoritative certificate stays wrapped by the
service key ring, and the same public fingerprint reopens across subsequent host instances. No
system trust change, plaintext PFX, custom cryptography or certificate replacement was used.
Non-Windows retains EphemeralKeySet and is not qualified by this Windows run. Sources:
[key-storage flags](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509keystorageflags)
and [runtime v10.0.13 import/container lifetime](https://raw.githubusercontent.com/dotnet/runtime/v10.0.13/src/libraries/System.Security.Cryptography/src/System/Security/Cryptography/X509Certificates/X509CertificateLoader.Windows.cs).

`artifacts/sync-real-service-schannel-fix.log`: first staged test passed. Eight retained checkpoints
prove original pinned TLS/enrollment, idempotent space creation, initial financial publication,
second-client pull, disjoint edits preserving both values, competing money held for an explicit
remote choice, an actual server commit whose response was deliberately dropped and safely resumed
with identical receipt bytes, tombstone propagation and revoked-session refusal. The dropped-response
port suppresses only the response after the real transaction commits; it replaces no server algorithm.

`artifacts/sync-real-service-settlement.log`: the separate retained-fixture continuation passed.
Both original financial files and the same protected TLS/key ring reopen. New actual sessions are
explicitly reconnected, two independent offline full payments are submitted, the duplicate remains
unpublished, and related existing payment/state records are returned for review. Explicit existing-
row choices leave exactly one payment and the matching settled occurrence. Returning related unique
rows fixes the previous empty counterpart presentation. An initial xUnit2031 assertion-style compiler
error was corrected before this case executed; it was not an application/runtime failure.

`artifacts/sync-real-service-related-review.log`: third continuation passed, without replaying either
prior successful scenario. Choices now carry the exact related record/version as well as their own
row version. The server requires the current unique counterpart even if an untrusted caller omits
its review. A concurrent change from 500 to 600 invalidates the old existing-payment choice without
mutating the waiting client's 500. Fresh approval applies the actual 600, with one canonical payment.
The local journal preserves other resolved choices when only one review becomes stale; deleting all
choices previously could alternate conflicts forever. The real case proves the unchanged occurrence-
state choice survives and the next acknowledgement completes with an empty outbox.

All three staged tests passed under strict compilation. Exact phase traces are
`completed-stages.txt`, `completed-settlement-stages.txt` and `completed-related-review-stages.txt`
under the fixture root. These use plaintext fictional SQLite clients and the real least-privilege
PostgreSQL service/HTTPS transport; they do not prove original encrypted native clients, full UI,
first-run restore, initialization takeover, all domain/partial-payment cases, commit/lifecycle crash
boundaries, Linux persistence/CI, Android/iOS/phone or release acceptance. The broader batch remains
in progress and must not be pushed as a completed feature.

## Original receipt delete/Undo checkpoint (unpublished)

The next real-service case uses original `DeleteEntryAsync` and `RestoreEntriesAsync`, without
manually deleting the receipt first. It reproduced a genuine mismatch:
`artifacts/sync-real-service-undo-before-fix.log` fails because the other client still retains the
live receipt after the entry disappears. The original sender deliberately keeps physical receipt
bytes for its existing Undo lifetime; sending those bytes as an active orphan is incorrect.

The runtime now derives paired receipt changes from the already durable entry outbox under the
same writer before sealing a round. It also derives changes after the cutoff before incoming
publication, so newer local Undo/deletion is not erased by an older receipt acknowledgement.
Logical financial reads exclude retained orphan bytes, and the service refuses live wire receipts
without a live parent. Local physical storage/Undo, portable backup ownership and startup orphan
cleanup keep their original rules. No table, column, trigger or frozen migration was rewritten;
this is snapshot/outbox reconciliation using existing transactional metadata.

An explicit local Undo against the exact authentic current deletion may restore its entry/receipt.
Only the actual historical/current deletion version proves this on the server; a missing or stale
deleted baseline still requires review. An acknowledged own deletion also allows newer local Undo
to survive rebase. `artifacts/sync-restore-authority-core-tests.log`: all four focused Core cases
pass, covering unknown deletion refusal, exact-known deletion restoration, a live historical
baseline not granting deleted authority, and active wire-orphan refusal.

`artifacts/sync-real-service-undo-after-fix.log` retains the next terminal failure during initial
reconnection: the previous failed prototype had already published a live service orphan, and the
new graph refusal correctly did not silently delete unknown remote data. That exact fictional
orphan was retired through an explicit normal protocol review in the next continuation, with both
financial files/key ring and retained sender bytes preserved. No raw SQL cleanup, reset or bypass
of financial conflicts was used. Only the failed receipt scenario was retried with a separate
entry/receipt inside the existing files; the three previously successful staged cases were not rerun.

`artifacts/sync-real-service-undo-continuation.log`: the original delete/Undo case passes. Its actual
source retains the same receipt bytes, the receiver loses the logical receipt with its entry, and
Undo republishes exactly one original entry and identical receipt. Both queues finish empty.
Retained failed trace: `undo-attachment-4907c6445ba5456d83526f9d8d3f22e0.txt`; successful trace:
`undo-attachment-b4ecef31368f40349167cfe632621ef1.txt`, under the same owned fixture root. Harness
reopening is not native application startup/power-loss/Undo-across-restart acceptance.

`artifacts/sync-attachment-contract-check.log` verifies the same 21-table/294-column typed hash and
the unchanged frozen capture SQL. The review-data reader also now prepares bounded original local
relationship snapshots and refuses a choice after those displayed records change. The original
page now renders local/current/related versions using translated names, original date/money
formatters and exact account currency. Missing currency, stale local rows and unsupported changed
relationships keep choices disabled. Forty-eight labels have real translations in all six languages.
`artifacts/sync-review-presentation-tests.log`: six original App/SQLite tests pass, including KWD
three-decimal amounts, missing currency, group refusal, related payment visibility and consent
reference guards. `artifacts/sync-conflict-page-build.log`: Windows compilation has zero warnings
and errors. Full-domain comparison and native layout acceptance remain unfinished; compilation
and these focused tests are not UI acceptance.
No unchanged APK, Windows UI matrix, complete suite or native cipher corpus was repeated.


### Acknowledged local review refresh

A concrete phase-2 defect left Restart review inert after a later local edit invalidated consent.
The coordinator now recomputes review against the complete retained acknowledged feed without
abandoning its operation or applying financial changes. Old decisions are invalidated during this
explicit recalculation. A conflict-free recalculation also retains the queue and acknowledgement
until the next explicit synchronization. Phase-1 abandonment still requires the actual service
receipt and preserves lost-acknowledgement recovery. The confirmation explains both cases in all
six languages.

`artifacts/sync-real-service-committed-review.log` first proves the competing-edit case;
`artifacts/sync-real-service-committed-review-final.log` adds the conflict-disappears boundary and
passes the one targeted staged acceptance test. The original PostgreSQL service, pinned TLS and
the same two retained fictional SQLite files are used, with a new isolated financial row. A real
local edit is injected after the original service transaction commits and before client feed
publication. An outdated decision is refused; refresh retains the same phase-2 operation and
current 1500 minor units without consuming the queue; fresh explicit remote choice applies 1300.
A second conflict is removed by an actual subsequent edit matching the remote 1600; refreshing
removes only the obsolete review, and only explicit synchronization consumes the pending queue.
No previous staged settlement/receipt scenario, full suite, UI matrix or APK was rerun. This is
real service/data evidence, not native UI, device, encrypted-client or product acceptance.


### Budget comparison and reference consent

The original conflict presenter now covers budget period/calendar/year/month, method, rollover,
alerts, own-currency overall limit and named account scope. Empty scope explicitly means all
eligible accounts in that budget currency, rather than no accounts. Category limits retain the
parent budget currency, calendar/period identity and actual category name, including translated
default categories. Zero is displayed as a zero monetary packet, not an absent limit. Six new
labels have real translations in every supported language.

`artifacts/sync-budget-review-tests.log` retains an actual failing test: unchanged account-id
collections could hide unavailable account names while choices remained enabled. Collection
reference failures now prevent consent even when unchanged between versions. The three failed
method cases alone were retried successfully in `sync-budget-review-reference-fix.log`; the
previously passing zero/all-accounts case was not rerun. `sync-budget-category-review-tests.log`
retains the next actual failure: category limits intentionally lack a physical category FK, so
presentation did not resolve that logical relationship. The reader/presenter now resolve the
explicit logical CategoryId reference as well as account/category collections. That failed case
passed in `sync-budget-category-reference-fix.log`; the subsequent explicit/default category-name
variants passed in `sync-budget-category-names-tests.log` (two cases).

`sync-budget-array-consent-tests.log`: one additional original SQLite case proves that prepared
review reads account-array references and rejects an old choice after an actual displayed account
rename. All finance is written through the original store; only fictional received journal facts
are seeded explicitly. `sync-budget-review-windows-build.log` records a strict Windows build with
zero warnings/errors in 21.34 seconds before the final category/calendar caption refinement;
those final refinements compile in the focused App test host. Native layout, final native builds,
full-domain comparison and release acceptance remain open. These are seven new budget behaviors
across targeted runs, not a rerun of the full baseline suite or a finished synchronization batch.


### Plan recurrence and transfer comparison

The original presenter now consumes the actual localization service and original PlanText rule
formatter. Schedule comparison shows both transfer accounts/currencies, amount mode, full stored
recurrence settings, rule summary, calendar/start/end, ownership slice, pause/continuation,
automatic posting, reminders with actual time, fixed weekend-day names and contract information.
Individual recurrence fields remain visible alongside the summary so inactive stored ending/count
choices cannot disappear behind a whole-record choice. Twelve new labels are translated in all
six languages. The existing immutable relationship consent checks remain in force.

`artifacts/sync-plan-review-tests.log`: three targeted original SQLite plan cases pass, covering
monthly Gregorian/count end, yearly Persian/date end and Once/no end. The source amount is
3456 KWD minor units (3.456 KWD); destination values 500/600 JPY remain zero-decimal values.
The expected recurrence is produced by the original PlanText formatter, not a copied recurrence
algorithm. Original store writes preserve reminder/automatic-post settings, and missing destination
context prevents both choices. Existing passing unrelated scenarios were not repeated.
`artifacts/sync-plan-review-windows-build.log`: actual original page and new localization injection
compile with zero warnings/errors in 19.83 seconds. Native layout, encrypted-client acceptance,
asset/goal/other-domain comparison and the remaining whole-batch delivery gates are still open.
No APK, complete test suite or previously successful native matrix was repeated.


### Holding units and exact price comparison

The original presenter now covers asset-type dimension/kind/metal, purity, unit weight, count unit,
divisibility and price currency; holding events show named type/location, original quantity units,
event kind, direction, reason, basis and proceeds in the actual type currency. Valuations show the
actual currency and price per gram/unit. Original HoldingText supplies quantity, purity and unit
captions. Overview price formatting intentionally rounds to minor units; consent instead formats
the exact stored minor-unit-times-1000 value with all additional three decimal places, direction
marks and the original per-unit caption. It does not round two competing stored prices into an
identical label. Non-divisible count types refuse fractional quantity display rather than silently
rounding a malformed value. One additional basis caption has translations in all six languages.

`artifacts/sync-holding-review-tests.log` retains an initial fixture-only failure: the test context
contained its root AssetTypes record twice. Production prepared context uses a dictionary and
already supplies unique identities; the test fixture now deduplicates that context without changing
production consent checks. Both failed cases alone passed in
`artifacts/sync-holding-review-fixture-fix.log`: original store writes for mass and divisible-count
holdings, original quantity/purity formatting, named locations and KWD basis, exact competing prices
3.456123 versus 3.456124 KWD per original unit, and refusal without asset-type context. No previously
successful plan/budget scenario, full suite, APK or Windows native matrix was repeated. The actual
App test host compiles the original changed source strictly; final native builds and layout review
remain part of the unfinished whole-batch delivery. Linked purchase/sale groups, other remaining
record types, remote reference completeness and native encrypted-client acceptance are still open.


### Authenticated relationship snapshots and service consent

New conflict results carry bounded relationship snapshots from the same owner-locked service
graph as the competing financial rows, including explicit absent references. The client retains
that context inside the exact conflict payload; submitted captions use local context and received
captions use authenticated remote context. Missing remote references cannot silently borrow the
local account currency/type. Retained older prototype conflicts without the optional field still
use the previous presentation path; the service will refuse their omitted context versions and
return a fresh review. This prototype compatibility path is not a completed release contract.

Explicit choices include the exact context identities/versions as well as row/counterpart version.
The service recalculates required relationships and refuses omitted, added, duplicate, invalid or
changed context before selecting financial content. Successful completion hashes include the full
typed choices. Each context is bounded to 128 records/64 KiB; complete conflict responses are capped
to 2 MiB. Phase-2 local conflicts capture their reference snapshot from the immutable incoming feed
plus known authenticated rows, rather than current local edits. Shared SyncReviewRelationships
walks original physical/explicit logical/account-category collections on service and local reader;
no schema migration or financial algorithm is duplicated for captions.

`artifacts/sync-real-service-reference-consent.log`: one new staged test passes against the same
retained real PostgreSQL/TLS service and two original SQLite files. A later actual remote account
rename changes the required relationship version while leaving the contested entry version
unchanged. The old recorded choice receives ReviewStale; local 2200 remains untouched. Fresh
consent applies remote 2100 and the renamed account with an empty pending queue. The prior staged
receipt/settlement cases were not rerun. `sync-remote-context-presentation-tests.log`: one original
App/SQLite case passes with distinct local/remote captions and no currency fallback for an explicit
missing remote context. The service and App test hosts compile the original changed sources
strictly. This is not native layout/AOT/encrypted-client/final release acceptance. The owned Docker
fixture was stopped cleanly and retained. Broader release verification remains deferred until the
whole authorized personal-sync batch is ready.


### Original native Android page, protected fixture and semantic action

The independent `eng/benchmarks/Zanance.AndroidSyncReview` caller compiles original production
sources/XAML/resources and uses original covered startup, protected data, local flow and native
foreground adapter. Its package and canonical data path are checked before fixture preparation;
production never references this caller. No service credentials or HTTP request is used. The
existing `Zanance_Storage_Review` AVD was verified at adb port 5038 / emulator-5570. The production
installation was not launched or modified and FLAG_SECURE remained enabled.

Initial native assertions failed in the caller's button-name observation, not in the original
command: `sync-native-review-result.json`, `sync-native-review-continuation.json`,
`sync-native-review-final.json`, `sync-native-window-review-result.json`,
`sync-native-java-traversal-result.json` and `sync-native-command-handler-result.json` retain those
failures. General window traversal/class changes did not resolve that observation. Debug builds
were used only to narrow the caller failure; they are not Release acceptance. The final observer
starts from the original WrappingAction/command parameter and its actual Android button handler,
then reads CreateAccessibilityNodeInfo. MAUI's semantic name is supplied through its accessibility
delegate, so ordinary View.ContentDescription was an incorrect check. An intermediate reader
correctly refused conflicting diagnostic/final chunks; diagnostics now have a distinct suffix.

The same fictional rows, protected generations/keys and journal were preserved through every
relevant package update. Continuation used the original run plus explicit review_resume, never
fixture reseeding or restarting solely after an observation timeout. UI automation captured the
actual English choice action, native confirmation and Cancel in
`sync-native-real-confirmation.xml` / `sync-native-cancelled-confirmation.xml`. The subsequent
retained conflict and unchanged financial value also survived that cancelled native action.

Final `artifacts/sync-android-accessible-node-build.log`: Release/AOT/trim compilation has zero
warnings/errors in 63.59 seconds. The APK identity and v2/v3 signature were verified. Final
`artifacts/sync-native-release-accessibility-result.json` passes all six en/fa/de x light/dark
cases: native width 412, content height 720, density 1; covered construction, both original money
packets, actual native choice handler/parameter/semantic name, retained 1400 minor units and secure
flag. Main package untouched is enforced by caller identity/path guards, not inferred from CI.
Public proof and signed independent APK are retained under
`artifacts/android-sync-review/db2bab6a1b8140d5b3bc8acdee77a108/`;
APK SHA-256: `5676bc775f6b1e20af3efe105529242165c16270a0869a3e15efbf34d421b910`.
The caller was stopped after the successful report; its private fixture remains intact.

This is actual original native page/flow evidence on the independent protected Android install,
not all layout glyph boundaries, 360/wide Windows acceptance, TalkBack audio, phone/arm64/API24/
16-KiB OS, whole native service synchronization, iOS or product release. The main APK and last
full-suite baseline were not replaced or rerun. The whole personal-sync batch remains unfinished.


### Goal/contribution units and transitive reference consent

The original presenter now distinguishes monetary goals from mass/count holding goals in both
targets and contribution amounts. Quantity goals use original HoldingText units; monetary goals
use their exact currency; signed goal allocations use the actual account currency and retain the
named goal. Contribution rules use original PlanText, with explicit method/category/percentage,
reminder and assumed-price fields. Assumed prices retain all three subminor digits and are
labelled as plan assumptions, never silently presented as valuations. Lifecycle timestamps show
the chosen date calendar and explicit UTC time. Four new captions have six real translations.

The bounded local review reader now walks transitive references through the shared original
relationship helper: contribution -> goal -> holding type/location, rather than a special second
pass limited to account currencies. Logical goal/allocation/asset references are explicit even
where the original schema intentionally has no physical FK. Every captured local relationship
remains in the unchanged consent guard; existing record/byte bounds still apply.

`artifacts/sync-goal-review-tests.log`: four original SQLite goal/contribution cases pass (earmark,
account balance, holding mass, divisible count), including original units, exact assumed price and
missing type refusal. `sync-goal-reference-allocation-tests.log` records an invalid test filter
that ran zero tests; it is not acceptance. The filters were run separately. Initial
`sync-goal-allocation-tests.log` retained two assertion failures because the expected goal caption
omitted its already-correct displayed currency. `sync-goal-allocation-caption-tests.log` verifies
the meaningful named-goal/currency caption and both signed monetary packets; two cases pass.
`sync-goal-nested-reference-tests.log`: one original SQLite case proves the transitive type read
and refusal after its actual later rename. Seven new targeted behaviors pass across these runs;
this is not a new full-suite count. No successful unrelated native/App baseline, Android matrix,
APK or Windows build was repeated. Native review of these additional record presentations and the
remaining whole-batch release gates are still pending.


### Explicit continuation of unpublished initialization

A revoked/lost original session could previously strand a space before its first publication,
because all financial endpoints correctly restricted initialization to its original device. The
profile catalogue now identifies that initial device while pending. The authenticated owner can
explicitly POST `/sync/profiles/{profile}/initialization` from another actual current device,
bound to the observed initial device. Under the same owner transaction, the service requires
Initializing, cursor zero, matching schema and no financial records/events. It marks prior
uncommitted rounds abandoned, retains their protected evidence, and changes initial authority.
A same-device retry preserves new staging after a lost claim response. It cannot reset an already
published space or impersonate another owned device through a supplied device id.

The original profile list offers a separate pending-only action and a confirmation in all six
languages. It clearly names the current local profile as the new source; this is not recovery of
lost original data and portable Zanance backup remains necessary for that case. No account is
created by this action. Native foreground/profile authority and reviewed actual session/device/
peer are rechecked after confirmation. Explicit local reconnection can discard only an
unacknowledged round; queued rows/tombstones and financial records remain. An acknowledged phase-2
round cannot be discarded by this recovery option. Final native layout/flow acceptance of the
new action remains pending; the App test host compiles its actual command source strictly.

`artifacts/sync-real-initialization-recovery-tests.log`: one new real PostgreSQL/pinned-TLS case
passes. An original financial Account proposal is staged in a separate new fictional space; the
actual original device is revoked; another device explicitly claims initialization. The old
round is abandoned while its protected part remains; wrong-device impersonation is refused and
the revoked session remains unauthorized. A lost-response retry returns the same claimed state.
The new device publishes the exact account, then published-space reset receives Conflict and the
financial row is unchanged. The extra fictional space remains in the retained fixture catalogue;
future staged cases must select the original `Fictitious shared personal copy` by identity/name,
not assume the owner has only one space. Previous successful staged cases were not repeated.

`sync-initialization-local-boundary-tests.log` records one passing acknowledged-round refusal and
one failed test expectation: legitimate reconnection appends a full reconciliation, so queue row
count need not stay equal. `sync-initialization-queue-preservation-tests.log` retries only that
case and proves every previous sequence/table/key/body/tombstone survives byte-for-byte, financial
1400 remains unchanged and only the unacknowledged round/review metadata disappears. These are
three new targeted behaviors, not a full-suite rerun or native end-to-end recovery acceptance.
The owned Docker fixture was stopped cleanly, preserving files/keys; no production deployment or
release claim is made. Complete first-run synced-profile adoption, remaining record/group review,
native Windows/remaining UI acceptance and other whole-batch gates remain open.


### First-run received-profile adoption

The original wizard now offers optional personal sync only in the explicit edition, at both
restore alternatives. A native modal keeps the wizard and its unsaved draft attached. It uses the
same original sync page/flow, with explicit Back and Use these accounts actions in a fixed growing
footer. The first-run mode selects an existing service space; it does not manufacture a financial
account or create another service space. Six new captions/confirmations have real translations
in all six languages. Existing portable backup restore remains independent.

PersonalSyncStore reads completion readiness from the exact enabled binding/generation with an
actual applied success, accounts/settings and no pending round/review. Explicit completion checks
those conditions again in its transaction and changes only device-local OnboardingCompleted.
Preferences and all received accounts stay unchanged. Cancellation/back preserve the wizard;
connection retirement during confirmation cannot complete another binding. Native completion
pops only the wizard's modal then queues the original ShowCurrentProfile path so saved display
units and profile choices are applied without copying unsaved setup defaults.

A separately confirmed receive-first-run connection treats only an empty, unfinished destination
as reception: saved setup Settings are not submitted as edits to the remote profile. Every other
financial table is checked, including independent goals/holdings without accounts. Existing data
refuses this exception. Original local rows are not deleted by the connection. A same-binding
interrupted round keeps its frozen nonce and does not recapture setup preferences; acknowledgement
publication and regular first-run eligibility still require the complete normal sync protocol.
This is not a general overwrite option for an existing profile.

`artifacts/sync-first-run-flow-tests.log`: five original flow/SQLite cases pass, covering explicit
completion preserving received accounts/preferences, empty/unacknowledged refusal, Cancel/Back
and connection change during confirmation. `sync-onboarding-entry-tests.log`: two additional
original wizard cases pass, covering optional entry/no writes and offline-edition refusal.
`sync-first-run-receive-boundaries.log`: three cases pass, covering existing accounts/independent
goals refusal with unchanged binding/outbox and exact frozen empty-round continuation.

`sync-real-first-run-receive-tests.log` retains a compiler-only duplicate-local-name failure; no
scenario ran. After that fixture variable correction,
`sync-real-first-run-receive-after-compile.log` passes one real PostgreSQL/pinned-TLS case: a fresh
original SQLite installation receives actual existing accounts and completes first run without
creating another account, preserving the complete received account list.
`sync-real-first-run-preference-reception.log` passes an additional separate case: the original
source first obtains its actual current baseline, then explicitly saves/publishes JPY report
currency, KWD default currency and Advanced mode. A fresh receive-first-run destination receives
those preferences without proposing empty wizard defaults, completes first run and retains all
received accounts. The retained primary fictional source now has those nondefault preferences;
future cases must not assume its earlier defaults. Both new destination files are retained under
the same owned service fixture. The previous successful staged cases were not rerun.

`sync-first-run-native-windows-build.log`: original Windows app/XAML/native host integration
compiles strictly with zero warnings/errors in 26.15 seconds. Twelve new targeted behaviors pass
across these runs, not a fresh full-suite baseline. Native runtime review of the new wizard/modal/
footer/initialization controls remains open, as do other record/group presentation, whole native
network synchronization, final platform builds/APK and remaining complete-batch gates. No new
main APK or release acceptance is claimed. The owned Docker fixture was stopped cleanly.

## Unpublished linked financial group consent

An observed real PostgreSQL/pinned-TLS failure is retained in
`artifacts/sync-real-group-choice-before-fix.log`: independent event/payment choices committed
an inconsistent holding purchase. The failed fictional scope and its client files are preserved;
no financial reset or deletion was used to turn it into a success.

Original entry/asset-event GroupId now binds conflict consent. Capture retains unchanged group
members and explicit deletions before freezing a round. The service returns a complete bounded
group, rejects partial/mixed choices and binds every member to authenticated relationship versions.
Concurrent disjoint group edits also require one version choice instead of independent row merges.
A newly added remote member is included in the review context; a local choice cannot silently retain
that unseen member. Review pages never split a group; an oversized group refuses explicitly.

The local journal records an explicit group choice atomically after checking each original snapshot,
sequence, displayed local relationship and current connection. The post-acknowledgement path retains
the same operation and invalidates the whole group after any member changes. Other completed choices
are retained when a bounded review page is replaced. No financial row is written by recording consent.

`sync-real-group-choice-atomic-fix.log` passes the original HTTP adversarial mixed-choice case and
one complete local choice. `sync-real-group-post-acknowledgement.log` extends that same behavior with
a real original write after service acknowledgement, a later fee edit invalidating all approvals,
and a fresh remote choice applying exact quantity/payment/fee without duplicates.
`sync-real-group-disjoint-and-membership.log` passes the final expanded case, additionally covering
disjoint concurrent edits and a new remote fee, including explicit refusal of a local hybrid.
These are successive focused runs of one evolving acceptance scenario, not three new full-suite totals.

The original presenter displays one group card with every member's local/current versions, actual
financial units and no raw group identifier. A remote-only member is shown and disables the local
choice. Three English/Persian/German original presenter cases pass in
`sync-group-review-complete-remote-member.log`. Earlier logs retain an unseeded fictional fee-category
failure and an incomplete remote-only member presentation failure; both causes were corrected.
Native group rendering and complete-batch qualification are not established by these managed tests.

### Native group rendering and explicit complete-group actions

The independent `ReviewLinkedGroup=true` caller has its own compiled/verified permanent identity
`pro.vafadar.zanance.androidsyncgroupreview`; the earlier single-expense installation is preserved.
Original HoldingStore creates the event/payment/fee in protected native storage. Only fictional
received journal snapshots are seeded. `sync-group-native-android-build.log` retains a caller-only
CS0162 constant-branch compilation failure. Replacing the branch flag with a readonly field produced
a strict successful signed Release/AOT/trim build (`sync-group-native-android-readonly-build.log`).

`sync-native-group-result-initial.json` passes six actual original page cases at native width 412:
English/Persian/German, light/dark, one group card, six version sections, four original monetary
entry packets, exact command parameter/accessibility node and unchanged original amount, with
FLAG_SECURE intact. The first observation boundary expired; the same still-running PID 29585 then
reported success without restart or reseeding. The caller originally deactivated the final page,
so that result alone did not prove a later invokable action. The correction leaves only the final
page active and asserts actual managed/native enablement. The same run/data/key/journal continued
with an update of this package, not fresh fixture preparation. `sync-native-group-result-active.json`
passes those checks; an actual native service-choice confirmation opened and Cancel was invoked.

That native confirmation exposed generic singular-record wording. Production group actions and
confirmations now explicitly choose the complete event/payment/fee group in all six languages;
single-record wording remains unchanged. Three final presenter cases pass in
`sync-group-complete-action-tests.log`. The final strict original Windows build passes with zero
warnings/errors in 23.61 seconds (`sync-group-complete-action-windows-build.log`). The final strict
independent Android Release/AOT build passes with zero warnings/errors in 67.84 seconds
(`sync-group-native-complete-action-build.log`).

`sync-native-group-result-complete-action.json` passes the six cases again after the relevant
caption/confirmation changes, on the exact retained protected fixture with enabled native group
actions. UI Automation of this package verifies the exact translated German complete-group
confirmation (`sync-native-group-complete-confirm.xml`), invokes Cancel, and observes the retained
enabled complete-group action (`sync-native-group-after-cancel.xml`). This is cancellation/navigation
evidence, not a post-cancel database comparison or a native network roundtrip. The prior disabled
page capture is retained as negative harness evidence.

The final independent APK is retained under
`artifacts/android-sync-group-review/89919e073be246b190af2facf6687022/complete-action/`;
SHA-256 `bccc70c4e87085f42ab0beadf5a844c0af1d85f18a6e51bea066a14ca7d1192c`.
Its package and signature were verified before installation. Only this independent process was
stopped afterwards; its data/keys and all previous evidence remain. The exact owned PostgreSQL
fixture was stopped cleanly. No production APK, phone/iOS/Windows runtime, 360/wide glyph layout,
full native service sync or complete-batch acceptance follows from these checks. Other supported
record presentation and whole-batch delivery remain open; no incomplete sync batch is committed.

## Unpublished remaining portable record comparison

Original typed comparison now covers templates, category/parent/default-category/spending rules,
named saved filters, exact directional exchange rates, portable settings and complete saved daily
forecast paths. Templates use their actual source account currency and preserve a transfer's named
destination. Rates display source/target currencies and every stored decimal. Settings show the
original effective HomeLayout order/visibility, validated display-unit factors, review month/steps,
calendar, estimate currency/period, explicit zero and default account. Device security remains outside
the portable schema. Forecasts show named CSV scope through original references, each original
ForecastSnapshot.Points day/balance, signed minimum and assumptions; no raw JSON/GUID scope is shown.
The authenticated relationship reader now includes settings' default account and CSV forecast scope.

`artifacts/sync-portable-review-original-records.log` passes three English/Persian/German original
SQLite/presenter cases, each covering all seven record types with actual production store writes.
The forecast includes Int64.MinValue and preserves every daily value; rate decimals, KWD template
units, named JPY transfer destination, named categories and zero estimate are checked explicitly.
`sync-portable-review-malformed-settings.log` retains four fictional fixture failures: the test had
not saved any Settings row, so there was no actual capture to inspect. After saving through the
original store, only those failed cases were rerun; all four pass in
`sync-portable-review-malformed-settings-source-fix.log`, refusing invalid/duplicate display units,
unknown HomeLayout sections and unknown review steps. These seven targeted behaviors are not a new
full-suite baseline. Native runtime qualification of these additions remains pending at this point.

### Portable comparison final targeted and native evidence

`sync-portable-empty-forecast-review.log` adds one original-store case: an empty saved forecast
retains empty scope/path instead of claiming all accounts. `sync-portable-reference-consent-tests.log`
adds two actual SQLite/journal cases: both Settings.DefaultAccountId and CSV ForecastSnapshots scope
load the real named account into the bounded consent context; renaming that account after review
refuses the old choice and leaves the original settings/snapshot unchanged. Ten new managed behaviors
pass across these focused runs, not a newly executed full-suite total.

The independent native variant is `pro.vafadar.zanance.androidsyncportablereview`, with compiled
identity and signature verified before installation. Its own original stores create all seven record
types in protected storage, including an unrelated expense whose amount must stay unchanged. The
first strict Android build passes with zero warnings/errors (`sync-portable-native-android-build.log`);
its final empty-scope correction also compiles strictly (`sync-portable-native-complete-build.log`,
63.24 seconds). The original Windows app/XAML compiles strictly with zero warnings/errors in 28.57
seconds (`sync-portable-native-windows-build.log`).

`sync-native-portable-result.json` passes six English/Persian/German light/dark original-page cases
at native width 412. All seven items have both choices enabled, original template monetary packets,
command parameter/native accessibility node, unchanged fictional expense and FLAG_SECURE. The first
bounded observation returned no result; the same live PID 30276 then completed without restart.
This report verifies its selected native monetary controls, not every field's glyph geometry.

The expanded check compares every nonempty field with actual native TextViews and checks explicit
forecast/settings line counts. `sync-native-portable-full-values-result.json` retains a failed Persian
observation: native display-digit conversion changed ordinary text such as color digits, while the
caller compared raw stored text. `sync-native-portable-lines-before-fix.xml` preserves the native
Persian conversion. No production layout change was made for this caller mismatch. The original
English loops had advanced before the Persian failure, but no separate final expanded English report
was retained. After correcting the comparison to the same NativeDigits presentation, only Persian
and German light/dark were continued with explicit `review_resume` and `review_from_language fa` on
the exact retained data/keys/journal. `sync-native-portable-digit-aware-result.json` passes these four
cases, including every native field and explicit multiline count. The final strict caller build has
zero warnings/errors in 87.50 seconds (`sync-portable-native-digit-aware-build.log`).

The final independent signed APK and positive/negative reports are retained under
`artifacts/android-sync-portable-review/52c884a3794f4f1fad07bec9e4eee14a/`;
APK SHA-256 `1e2ac643d9fd2a473c5db35b8868f1e9401392f1622fbdb22fd70bec633d622b`.
Only this independent process was stopped; its protected fixture and all earlier evidence remain.
Production installation/data were not used. Native field presence/line counts are distinct from
360/wide glyph bounds, Windows runtime, phone/iOS and full native network synchronization.
Attachment content preview/consent, remaining transport/validation/operations qualification and
complete-batch checks remain open. No completed personal-sync batch, production APK or release is
claimed by this evidence.

## Unpublished attachment content review and inline native preview

The original attachment path now reads its actual logical live metadata and computes a bounded
content digest under the same snapshot, instead of reporting every blob-backed local row as absent.
It never loads or exposes a physical orphan kept only for the original Undo lifetime. The reviewed
journal, operation/phase, connection/generation, local sequence, complete payload digest and displayed
relationships are checked before returning an exact version. Staged content is verified against its
whole length/SHA-256; transient input buffers are disposable and cleared. Preview neither selects a
conflict nor writes the original attachment or ledger. The protocol now enforces the original
EntryAttachment bound of 1..5 MiB, in addition to the technical profile bound.

An uncommitted refused round downloads only authenticated current/related attachment references into
its protected private staging. A retained unresolved round resumes that download before returning
review feedback; no financial connection spans HTTP waits. Phase-one staging rejects content outside
the actual retained authenticated review. Normal committed-feed download remains separate.

`sync-real-attachment-review-content.log` retains a compiler-only wrong ValidateRow test signature;
no scenario ran. After that fixture-call correction, one real PostgreSQL/pinned-TLS/two-client scenario
passes in `sync-real-attachment-review-content-signature-fix.log`: exact local/service bytes are
available before a choice or commit, a disposed transient buffer is zeroed, original file/amount stay
unchanged, zero/oversized wire files refuse, deliberately corrupted private staging refuses and a
later original-model file edit invalidates the old preview. Its isolated negative staging is retained
without repair/reset; this is refusal evidence, not automatic corruption recovery or PDF rendering.

The original page offers an inline image/PDF preview with real page counts, explicit previous/next,
zoom and pan, and Close. Each native image is bounded; a PDF renders only the requested page, not an
OCR sample or every page at once. Native memory-only image sources replace retired frames; input and
rendered bytes are cleared on close/retirement/failure. The existing Android system PDF renderer uses
one private cache copy removed by its normal finally path; this does not claim process-kill cleanup
or elimination of every native/graphics memory copy. No external viewer or additional permission/SDK
is introduced. Lock/profile/visibility cancellation and a fresh post-render original snapshot check
prevent late or changed content from being published.

Preview is optional. Valid locked/unsupported files retain an explicit complete-file choice based
on exact version-bound metadata; renderer availability never decides financial authority. No image
recognition, guessed amount or ledger update follows from viewing/selecting a file. The unpublished
initial all-pages-before-choice experiment was removed before delivery because it blocked legitimate
locked documents. Its earlier passing logs remain historical, not the final product rule.

Four final original flow/SQLite behaviors pass in `sync-attachment-optional-preview-flow.log`,
`sync-attachment-optional-preview-retirement-flow.log`, `sync-attachment-optional-preview-late-edit-flow.log`
and `sync-attachment-unsupported-preview-choice-flow.log`: actual paging/zoom/close clears transient
images without changing or selecting the file; late completion after retirement cannot publish;
a file edit during native rendering invalidates preview/old consent and remains intact; and an
unsupported renderer preserves explicit confirmation/whole-file choice without writing file/money
early. Native rendering is an explicit effect in those tests, not a simulated MAUI control.
Together with the real-service case these are five new targeted behaviors, not a new full-suite run.

The independent `ReviewAttachments=true` engineering package is
`pro.vafadar.zanance.androidsyncattachmentreview`. Original stores create a fictional PNG in its
protected installation; two-page PDF bytes are an explicit fictional received counterpart. The
original native adapters decode the PNG and render both actual PDF pages. The initial strict native
build passes with zero warnings/errors in 87.09 seconds; initial six language/theme cases pass in
`sync-native-attachment-result.json`. Native UI Automation invokes Preview service file and Next,
then observes actual German page 2/2 and next/previous enablement in
`sync-native-attachment-preview-one.xml` / `sync-native-attachment-preview-two.xml`.
The initial reporter's generic money-packet flag referred to attachment size captions in this
variant; it is not evidence of a native financial readout. The final reporter names that check
AttachmentMetadataValues instead.

After the relevant optional-preview change, final strict Windows and signed Release/AOT Android
builds pass with zero warnings/errors in 19.40 and 61.41 seconds respectively
(`sync-attachment-optional-preview-windows-build.log`, `sync-attachment-optional-preview-android-build.log`).
`sync-native-attachment-optional-result.json` passes all six English/Persian/German light/dark cases
at width 412 on the same retained fixture, without reseeding. It verifies native decoded images,
real two-page count/navigation, zoom, exact command/parameter/accessibility node, cleared retired
image buffers, no normally completed renderer PDF cache residue, unchanged original entry amount
and intact FLAG_SECURE. The final independent signed APK/report are retained under
`artifacts/android-sync-attachment-review/598cdfc1a8134438b3339211aaa09889/optional-preview/`;
SHA-256 `f022751b8afd6c403243d19e241c23e659c0e6a4f9b6cd3e4decdcf2f50248e3`.
The package/signature were verified before installation. Only the owned independent process and
exact labeled PostgreSQL fixture were stopped; their data/keys and all evidence remain.

Full native HTTP synchronization, protocol/domain/operations hardening, 360/wide/Windows runtime
geometry, final complete-batch tests/builds/production APK and documentation/commit/push remain open.
No phone/iOS/graphics-memory/abrupt-kill/Store/product-release acceptance is claimed here.

## Unpublished original protected Windows HTTP and cold-process qualification

The earlier real-service acceptance clients use ordinary SQLite. Their TLS/PostgreSQL evidence is
not evidence that the native protected runtime completed network synchronization. The independent
`eng/sync/ProtectedRoundtrip` caller closes that specific Windows gap using the original SQLCipher
provider, Windows device-bound key store/durability, bootstrap/current-generation factory and all
original personal-sync stores/coordinator/transport. Production never references the caller.

Two distinct fictional roots are migrated from empty originals; both published financial headers
are non-plaintext and initial originals are absent. The retained actual service/private key ring and
certificate are reused; only a generated `.invalid` account is registered/confirmed. No real mail,
owner credentials, database reset, unrelated container or paid resource is involved.

`sync-protected-http-first.log` passes lost original commit acknowledgement/retry without duplicate
money; a three-MiB attachment with download part one refused after part zero, no partial ledger
publication and missing-piece-only retry with full-byte comparison; explicit competing-amount
resolution; and original attachment/entry tombstones. The opaque fictional PDF-named bytes qualify
transport, not PDF validity or rendering. A distinct OS process then reopens the same protected
generations, device key envelopes and DPAPI-wrapped fictional sessions at the same service port and
certificate. `sync-protected-http-cold.log` confirms retained account/deletion state, authenticated
requests and original synchronization, with no new sign-in/password or regenerated database.

Strict `sync-protected-http-build-windows.log` and final `sync-protected-http-build-final.log` have
zero warnings/errors. The final build removes an unused qualification-only scalar helper and adds
internal XML documentation; successful scenarios were not repeated for those changes. Initial compiler-only
namespace/platform-contract failures are retained, and no scenario ran before their correction.
The original files/keys/checkpoints remain under `artifacts/protected-sync/run-a`. This qualification
is not a new full-suite test count, Android/iOS native HTTP, abrupt-power-loss/session durability,
all-domain/operations qualification, complete-batch delivery or product-release acceptance. Remaining
native Android network, operational/domain and final complete-batch gates above remain open.

## Unpublished original protected Android HTTP and cold-process qualification

The independent `ReviewNetwork=true` package compiles original production sources with the native
source-built cipher package, original protected startup/factory and platform SecureStorage adapter.
It uses actual pinned HTTPS through the emulator host alias to the original PostgreSQL/service
fixture and an original protected Windows counterpart. No production installation, real mail/user,
owner secret, additional service finance endpoint or paid resource is used. This directly closes
the previous basic Android protected network roundtrip gap, rather than inferring it from page-only
native review or ordinary SQLite service tests.

The final strict native Release/AOT/trimmed build in `sync-native-network-first-run-build.log` has
zero warnings/errors (60.46 seconds); the strict Windows counterpart build also passes. Actual
package/signature/INTERNET were checked before installation. Final independent APK:
`artifacts/android-sync-network/094bb950f44a4024945b21afc395b010/first-run-reception/`, SHA-256
`1da663d7b2db73e9210173515fa607606a4f75d29e4c69d83ad1354d1e50e9d6`.

Negative evidence is preserved, not hidden by reseeding. Initial manifest inspection found the
caller still removing INTERNET unconditionally; production's edition condition was already correct.
The first launched caller incorrectly compared canonical and aliased Android file paths. Its
continuation canonicalizes both and confirms an original protected publication/non-plaintext
header. The next run retained a Settings-only conflict because the caller omitted the original UI's
receiveFirstRun flag. Recovery of that exact empty destination requires only that uncommitted
Settings review, abandons it through the original coordinator and connects through original
receive-only first-run behavior; it does not discard financial work, reset a database or replace
device keys. These are caller corrections, not invented production defect fixes.

`sync-native-network-first-run-result.json` confirms same-fixture continuation, exact three-MiB
attachment content, original receive and amount upload, with a later unsent edit retained. After
terminal success only that process was stopped. The original Windows client received the edit and
published a competing amount. A second Android OS process reopens the same generation/key files and
actual SecureStorage session without another sign-in; the unsent amount stays until explicit exact
original conflict resolution. Original attachment/entry deletions then reach both service and
Windows counterpart. `sync-native-network-cold-result.json` and `sync-native-network-host.log`
confirm cold success and counterpart tombstone reception. Only this exact owned package process
and labeled PostgreSQL fixture were stopped. Their original files/keys and earlier failures remain.

This is independent native engine/transport qualification, not first-run UI rendering, every Android
network interruption, process-kill transaction boundary, 360/wide geometry, physical arm64/API24/
16-KiB hardware, iOS, server operations/retention, complete-batch delivery or release acceptance.
Successful page-only/native Windows checks were not replayed. No full-suite test count changes.

## Unpublished bounded staging and original storage maintenance

The original per-round staging bound left aggregate active proposals unbounded across one owner's
rounds/spaces, and rollback after writing a private ciphertext piece left no collection path. The
service now applies finite operator-configurable owner-wide pending-round, serialized-proposal and
declared-blob bounds under its original PostgreSQL owner lock. A space also retains its active-round
and aggregate-byte bounds. Exact retained operation/part/blob retries precede capacity refusal;
replay cannot reserve bytes twice. Each attachment declaration now shares the original 1..5-MiB
file bound. These are technical logical-payload limits, not commercial plan activation or physical
database/filesystem size guarantees; defaults/ranges are in the API README.

Only explicit abandonment or a genuinely expired/revoked device retires its unpublished parts.
Revocation does so in the original owner transaction. The worker takes that same owner lock before
rechecking live session authority; it cannot discard parts after an intervening valid session-row
renewal. Committed receipts/digests, financial rows, event history and operation tombstones stay.
This introduces no time-based deletion of active pending work or committed financial history.

The piece collector rotates batches of at most 100 candidates and unlinks only server-GUID-named
regular files older than one day with no committed manifest. It skips fresh/known/redirected or
unknown-name files and never decrypts their bytes. Original piece publication and collection share
a separate two-integer PostgreSQL transaction advisory namespace, retained through manifest commit
or rollback. Collection takes no owner lock afterward; publication orders owner then storage.
This prevents deleting a real uncommitted publication even if its filesystem timestamp is old.

`sync-real-operations.log` initially passes the real PostgreSQL/pinned-TLS quota/orphan case.
After adding expiry/revocation reclamation, `sync-real-operations-expired-and-revoked.log` passes
that expanded scenario. The final `sync-real-operations-renewal-race.log` passes the added actual
owner-lock wait/session-row renewal race as well (one evolving test method, not three new tests).
Original service/coordinator transports and actual original account capture create the fixture;
only its synthetic session expiration/renewal and owned ciphertext timestamps are fault inputs.
The worker's session-row race isolates authority rechecking, not framework refresh-token acceptance.

Final checkpoint file `operations-2d469072dbb24648b7d1673b63204b9c.txt` is retained in the existing
fictional service fixture. It proves owner budgets spanning spaces, immutable retries at capacity,
abandonment reclaim with non-revivable nonce, original complete readable ciphertext preservation,
actual blocked PostgreSQL publication gate followed by commit/rollback, renewed pending work kept,
expiry reclamation with original committed record/event/receipt intact, and atomic explicit device
revocation reclamation. Native/device/UI tests were not rerun for these server-only changes.
Strict `sync-operations-build.log`, `sync-operations-expired-build.log` and final API Release build
`sync-operations-api-release-build.log` have zero warnings/errors. No model change/migration,
financial-history expiry, new permission, external processor or fresh full-suite count is involved.

All successful and negative owned fixtures remain. Whole-batch domain/protocol/operations review,
final app/UI/build/test/APK/documentation/commit/push and platform/provider/release gates remain;
these targeted passes do not declare the personal-sync batch or persistent development goal done.

## Unpublished planning validation and existing fixture compatibility

Final graph validation now also checks the existing goal target/name/unit rules, duplicate active/paused balance
goals, earmark account currency, contribution amounts/percentages, budget limits, settings month start/estimate,
known ISO currencies, positive exchange rates and nonnegative stored holding prices. Negative earmark releases,
zero budget limits and null contribution scaffolding remain valid. Holding prices may round to zero through the
original total-value converter; this is deliberately not rejected as a new product rule. No financial value is
normalised, guessed or rewritten to make a graph pass.

`sync-real-planning-run.log` passes one actual PostgreSQL/pinned-TLS case with fourteen invalid complete proposals:
no records/events/cursor are published on refusal, the original local rows stay unchanged, and a valid complete
original SQLite capture reaches a separate actual client through the original coordinator. The source fixture
is retained alongside its checkpoint. `sync-planning-service-final-build.log` is strict, zero warnings/errors.
The earlier compiler-only missing creation nonce and unsupported runner-option logs remain; no test scenario
ran in either attempt.

Eleven existing no-write snapshot helpers now include all 33 migrated tables, including eight sync metadata tables.
Their original complete-data comparisons were retained rather than weakened or excluding the new tables.
`sync-data-snapshots.log` passes all 421 cases in those affected classes. This is a targeted run, not the new full
suite count. `sync-data-snapshot-build.log` is strict and warning-free.

Identity TestServer clients now explicitly use HTTPS. The first ConfigureClient override was superseded by the
factory's final client options, so four scenarios still returned 426 before enrollment. That negative evidence
is retained in `sync-identity-https.log`; the corrected explicit factory-client options pass all four scenarios
in `sync-identity-explicit-https.log`. The initial case also asserts that a real HTTP-scheme request remains refused.
This exercises middleware, not a TLS handshake; real socket/pinning evidence is separate. The isolated database
`zanance_identity_d292_https` is distinct from retained sync evidence; no old sync database was truncated.

## Unpublished Linux persistent protection and CI composition

The Linux qualification runs the original service and SQLite clients under UID/GID 1654:1654 on an owned Docker
volume, with a self-contained .NET 10.0.13 publication. The official SDK 10.0.402-noble image is pinned to observed
MCR digest `sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa`; it supplies Linux/OpenSSL
and does not install or change the host SDK. Official source/maintenance reference:
https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md.

`Run-ServiceQualification.sh` generates a fictional certificate in a mode-0700 private fixture directory (files
mode 0600) and configures the original certificate-protected persisted Data Protection provider. It does not use
the Integration ephemeral fallback. This follows the existing explicit certificate boundary, not a new crypto
algorithm: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview.
Private wrapping/TLS keys, key-ring files, databases and payloads are never uploaded with XML results.

`sync-linux-protocol.log` passes the original two-client initial roundtrip and then a distinct OS process reopening
the same retained key ring/TLS identity, decrypting financial state and resolving one duplicate full settlement.
The two cases pass in 8.267 and 3.323 seconds. `sync-linux-planning.log` and `sync-linux-operations.log` pass their
separate actual planning and storage-maintenance cases in 5.850 and 4.799 seconds. All three containers exit zero
as UID/GID 1654:1654. Their four XML reports are retained under `artifacts/sync-linux-results`; the independently
owned volume and three PostgreSQL databases preserve the original files and keys. Standalone fixture preparation
performs additive migration and existing runtime-role grants only, never reset/truncation of retained finance.

The existing backend workflow now includes three isolated Linux qualification jobs (protocol, planning, operations),
with database/key roots independent per job, strict patched publication and bounded XML-only artifacts. The protocol
job executes its two cases in ordered independent processes; unrelated previously successful scenarios are not
replayed. Hosted execution has not yet occurred: local Linux success is not a hosted CI success claim. This is
qualification infrastructure, not public Docker deployment, public TLS/certificate renewal, hardware/device/iOS,
whole-app E2EE or product release acceptance. Final integrated app/build/UI/APK and complete-batch publication gates
remain open.

## Final managed solution qualification with selective continuation (unpublished)

The strict full solution attempt is retained as `sync-final-solution-tests.log` and its thirteen TRX reports.
It exposed six further application snapshot helpers still expecting 25 tables, plus a source-policy scan that
mistook the immutable wire Mode caption formatter for feature gating. After 493 repetitions of the shared snapshot
failure, only the verified application-test child process was stopped; the other projects completed normally.
The original unsuccessful attempt is not reported as passing or deleted.

All six helpers now compare all 33 tables, retaining the complete-data comparisons. The architecture scan exempts
only the exact stored-value caption expression in the one comparison formatter; any other mode reference in that
same file remains checked. `sync-app-snapshot-build.log` and `sync-mode-architecture-build.log` are strict with no
warnings/errors. `sync-mode-architecture.log` passes only the one previously failed architecture case.

A fresh pre-enumerated discovery lists 1,258 application cases. The first attempt had 648 passing cases. Its TRX
names were matched to discovery, and 32 fully passed classes plus 101 fully passed methods were excluded from the
continuation. The remaining 610 cases contain none of those 648 successes. All 610 pass in
`sync-app-remainder.log` (128.859 seconds), including the 493 prior failures and 117 previously unexecuted cases.
No successful application case or unrelated project was repeated to obtain that result.

`sync-final-coverage.json` reconciles the original reports and selective continuations: 3,690 covered managed test
executions across thirteen projects, no unresolved recorded outcome, all 1,258 application cases covered. Report
identity matching normalizes the standalone runner's repeated quote escaping and explicitly checks uniqueness and
no overlap in application continuation. The Core report retains its actual 915 execution count rather than
collapsing a duplicate display name. This is complete managed coverage through a failed initial attempt and focused
continuation, not a claim that a new complete-suite invocation passed. Separate PostgreSQL/native/platform checks
are not added to that 3,690 total.

`sync-final-windows-build.log` qualifies the integrated personal-sync-enabled Windows Debug build: zero errors and
zero warnings, 34.86 seconds. The complete signed Android personal-sync edition is being built separately; final
UI/geometry/APK and complete-batch documentation/publication remain open. The exact labeled PostgreSQL test
container is stopped; successful/negative database, non-root Linux volume and report evidence remain intact.

## Complete personal-sync APK artifact (unpublished)

`eng/scripts/Build-AndroidApk.ps1 -PersonalSync -Output artifacts/android-personal-sync-d292` produces the complete
Release APK, 85.6 MiB, using the script's strict no-warning/error gate. The edition selector is explicit; combining
`-Offline` and `-PersonalSync` fails before any build. Ambient sync environment properties cannot override the
selected offline/default edition. The script preserves its original encoding; no owner credential was inspected.

Artifact: `artifacts/android-personal-sync-d292/pro.vafadar.zanance-Signed.apk`.
SHA-256: `27e07a83540a2b811178664f617791757e0a676ce33aea2b9df2f6b0ebe07c7b`.
Actual package inspection confirms `pro.vafadar.zanance`, INTERNET for this opt-in network edition, embedded
original app AOT assemblies and `libsqlcipher.so` for arm64-v8a and x86_64. Signature verification passes v2/v3.
Exact public artifact proof is in `sync-final-android-apk-proof.json`; package/signature/permission logs are retained.
This is an installable artifact, not physical-device, Store signing, arm64/API24/16-KiB, first-run UI or release
acceptance. The personal-sync-enabled Android Debug build is qualified separately.

Remaining complete-batch closure: final original-domain/protocol review; Windows/native first-run and comparison
geometry at 360/412/wide without touching production profiles; add new screens to the Debug route inventory;
retain relevant existing Android native proofs; finish current status/privacy/spec/backlog documentation and
review all staged content/message before the one complete verified commit/push. Hosted CI is checked once after
that push. Public Docker deployment remains the following approved batch, not an accomplishment of this artifact.

Integrated Android personal-sync Debug also passes strictly in `sync-final-android-debug-build.log`: zero warnings,
zero errors, 62.35 seconds. The signed Release artifact and integrated Windows Debug are separate successful build
results. Test output was cleaned after managed qualification; retained proof/private fixtures are untouched. No
D-292 delivery commit/push or hosted CI result exists yet: final native geometry/first-run and whole-batch review
remain the next closure work, followed by the approved isolated deployment batch.

## Budget boundary closure (unpublished)

Final original-domain review found a concrete missing semantic check: typed budget year/month/start fields could
still fail the original calendar calculation. Complete graph validation now calls `BudgetPeriods.Range` using the
received financial month start, refusing unsupported monthly boundaries and overflowing weekly endpoints without
publishing data. Weekly budgets legitimately keep year/month zero. No independent calendar arithmetic was added.
`sync-planning-period-build.log` is strict, zero warnings/errors. `sync-planning-period-run.log` passes the one
changed actual PostgreSQL/pinned-TLS planning case in 6.483 seconds: nineteen invalid complete proposals leave
records/events/cursor empty; the valid graph reaches the original receiving coordinator with source values intact.
Prior successful unrelated cases were not repeated. Previous binary artifacts predate this Core change and need
only the final affected integrated rebuild after native closure; their old hashes remain historical evidence.

## Complete native Windows sync review (unpublished)

The final original page now has an independent Debug route, `personal-sync`, guarded before startup by the actual
compiled `pro.vafadar.zanance.syncreview` identity. Its fresh marked protected native installation contains only
fictional data. All 36 contexts pass: en/fa/de, light/dark and 360/412/1280 widths, each for first-run and complete
version comparison. The original native caption/glyph checks retain 71,796 observations; actual first-run Back uses
the UI Automation Invoke pattern, and unreceived accounts cannot finish onboarding. Comparison choices retain their
original reviewed parameter, complete spoken name and native target at least 44 pixels. Financial fingerprints
match before/after each phase. No credential entry, service HTTP, desktop focus/input or full-screen capture occurs.
`sync-window-d292/personal-sync-proof.json` and 36 own-window PNGs retain the evidence. Persian narrow and English
wide renders were also visually inspected. The independent process exited normally without a recorded error.
`sync-window-review-final-build.log` is strict, zero warnings/errors. This route is now in the Debug inventory.
Existing independent Android native HTTP/page/group/portable/attachment evidence is retained without repetition.
This is not physical-device, iOS, native OS text-scaling or spoken screen-reader acceptance.

## Final local batch delivery

The complete personal-sync implementation is locally qualified for this commit. Main Windows Debug identity is
restored (`pro.vafadar.zanance`), strict build zero errors/warnings. The final affected complete Android Release APK
also passes the strict build gate after the budget Core change. This supersedes the earlier APK as current artifact:
`artifacts/android-personal-sync-d292-final/pro.vafadar.zanance-Signed.apk`, 89,332,038 bytes,
SHA-256 `255141fe1430af8172caff866512e23c62299f021d81ab2d66ac3c91663a56fc`.
Actual package is `pro.vafadar.zanance`; v2/v3 signatures verify; original App AOT and SQLCipher native items exist
for arm64-v8a and x86_64. The opt-in edition declares INTERNET. Public artifact inspection logs/proof are retained.
The final planning service case and native Windows closure supplement the previous original protected Windows/
Android HTTP, conflict/group/portable/attachment, restore/revocation, Linux process persistence and managed coverage.
No already successful unrelated test was replayed for this closure. Earlier failed attempts and historical artifact
hashes remain evidence, not current binaries. The original labeled PostgreSQL fixture is stopped and its data/keys
are preserved. New standalone test output is cleaned after qualification; fixture and signed artifacts are retained.

This commit is the local ZCR-SYNC-01..03 delivery. Hosted CI is pending until its one delayed check after push.
Current-machine container/public-origin deployment is the next approved batch. Household E2EE, billing, providers,
phone/iOS/Store acceptance and release are separate remaining work; this personal protocol is not E2EE.
