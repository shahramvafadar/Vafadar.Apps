# Optional market reference rates and asset prices (D-299 local delivery)

## Approved complete batch

OD-14 approves the provider boundary, cache, source/timestamps, offline/manual fallback and local qualification.
The actual provider, rights/licence/redistribution, coverage, prices/costs and public activation remain separate
gates. No key input, guessed live source, new SDK or external account. No live data entitlement is inferred from
Lifetime. Current test commercial enforcement remains inactive. Deliver one whole app/data/service batch, not an
isolated helper; preserve completed unrelated qualification and the original financial/sync authority boundaries.

## Plan

1. Define bounded provider catalog/request/quote contracts: fixed approved public symbols, exact currency/unit,
   reference date, original observation/retrieval/freshness/retention, source/licence/attribution and provider revision.
   No account names, balances, quantities, holdings ids or arbitrary request URL. A reference is not an actual
   payment, dealer bid/ask, tax value or automatic transaction conversion. Unsupported mapping remains manual.
2. Add a closed default provider, original-source service gateway and bounded cache/single original in-flight
   lookup. Only explicit fetch may make one provider request; timeout/caller cancellation must not start a parallel
   request or replace the last good cached reference with corrupt/future/foreign data. Retain exact source policy.
3. Connect actual app scopes, device-private bounded cache and visible loading/empty/error/stale/unavailable states.
   Explicitly preview selected currency pair or exact supported asset/unit. Never infer a coin premium, purity,
   weight, country market or Iran's exchange-rate policy from name/language. Keep manual input fully functional.
4. Preserve reviewed provenance with saved rates/prices and portable restoration; audit the immutable delivered
   personal v1/household v2 contracts before choosing an additive persistence/sync evolution. Do not silently
   change an existing schema digest or create financial authority from source metadata. Source numbers, unit,
   date and original target must be rechecked inside the actual writer after native confirmation. Fetching/cache
   reads never rewrite historical rates, ledger money, quantities, budgets or goals.
5. Finish source/persistence/protocol tests, changed native en/fa/de light/dark360/412/wide review, actual main
   Android fallback/review, strict builds, complete signed APK, documentation and push. Repeat only changed or
   failed related cases. Actual provider/physical/Mac/performance/release acceptance stays distinct.

Current state: the approved local app/data/service prerequisite batch is qualified. No selected live source,
public activation, hosted CI success or whole-product/release acceptance is inferred.

## Current source and focused evidence

Core/Rates/MarketReference defines finite approved public catalogs/symbols, exact currency/gram/fine-gram/piece
units, date coverage, source/licence/attribution, original observation, retrieval, refresh and retention. A quote
retains only its selected symbol, not a repeated full catalog. Current-date coverage controls new lookups;
an already obtained latest-only reference remains readable after midnight until its original retention expires.
References never become payment rates or financial authority through validation alone.

Api/Rates has an unavailable default, explicit Market:LocalQualification gate and authenticated bounded typed
metadata/quote endpoints. Public hosting cannot turn this flag into a live provider. One original task per exact
source/revision/symbol/date survives caller cancellation/observation timeout; an actually still-live adapter is
not retired and dispatched twice. Source changes/future or malformed observations are refused and do not erase
the last good stale packet. Catalog policy, cache count and in-flight count are bounded. Real adapters must
honor cancellation, original revision and actual licence/fee terms before any future approval.

Data/Rates/MarketQuoteClient uses the existing approved/pinned identity transport and exact peer/file private
cache scope. Metadata/fetch is explicit, never on cache read. Cache reads retain exact original units/times/source,
refuse corrupt/unknown/oversized state, respect cover/file retirement and omit expired references without
silently replacing state. A late old response cannot overwrite a newer cached reference. Native adapter composition
is described below; no existing manual financial writer dispatches a network lookup.

MarketGatewayTests:12 distinct focused cases qualified (initial10, one explicit historical-coverage refusal,
one midnight cache regression). The last change reruns only the three directly affected date/source cases.
MarketCacheReadTests:8 cases qualified against the actual Data reader with explicit native effects, no network.
The first eight cache cases failed before reading because the fixture used an arbitrary hostname. Correct the
fixture to two pinned loopback origins; never loosen the existing approved-origin policy. Retain that failure;
only the failed class was rerun. Core/API/Data and test builds have zero warnings/errors. No full-suite or
unrelated previously successful verification was repeated.

## Unfinished integration and immutable data contracts

Existing ExchangeRate stores a user-entered rate/date/estimate, and AssetValuation stores manual/purchase source plus an optional note.
The actual schema exporter freezes delivered personal v1 and household v2 columns/triggers. Do not add a hidden
payload to unrelated fields, silently mutate an immutable digest, drop saved-source history or call a local cache
a durable accepted provenance record. A new additive MarketReferenceReceipts table now retains complete source
and exact target mapping separately; migration20261017010332 and the regenerated compiled model are included.
SQLite rejects UPDATE and oversized source documents. The target fingerprint includes the original row/value,
date/currency and asset definition; a later manual value/definition cannot silently inherit attribution.
Review/revalidation and receipt/value insertion share the actual CommercialWriteTransaction. Existing price
source remains Manual rather than inventing a new value inside immutable delivered enum contracts.

MarketReferenceStoreTests qualified18 distinct cases: six unsupported unit/metal/item mappings, nine migrated
SQLite writer/portable-history cases, and three final native-cover/committed-observer/forged-mapping cases.
The initial nine storage cases failed only during fixture cleanup: the scoped pool helper discovers owned
*.db files, while this new fixture used *.sqlite. Retain that failure; change only the new owned fixture suffix
and rerun those nine failed cases, not the six successful unit cases. The three final cases ran separately.
Original completion survives reference-cache expiry; an expired uncommitted quote remains refused. Portable
restore retains the exact source/mapping and current financial binding after cache expiry. No ledger or
holding quantity is created by a price reference. HasPendingModelChanges is false against the regenerated model.
The source conversion retains fine troy-ounce units and the exact31.1034768g factor documented by
[NIST precious-metals conversions](https://www.nist.gov/system/files/documents/2017/05/09/Precious-Metals-Conversion-Information-march2015.pdf).
Explicit matching metal and known purity are mandatory; fine metal does not imply a coin or retail premium.

The read-only schema exporter --check passed without changing original manifests/triggers: personal21tables/
294columns, hash7a1ec01b41bddf51d896b49ea050a5c7bf85f3293140797ba608b4eee9549a1e;
household additional4tables/30columns, hash922685c738665fdfe011caa1d231e85fa0b63e97cd24b7040f435db7be590f96.
Separate bounded provenance transfer under actual original personal/household authority is still unfinished;
do not infer that frozen financial contracts already transport this new table. No market Apply UI is released.

## Personal original-source companion integration

Original mapping/document contracts and their bounded validator now live in Core/Rates/MarketReferenceDocument,
shared by local storage and service without an API dependency on the application's EF infrastructure. Explicit
.NET10 source-generated JSON contexts reject duplicate as well as unknown fields. A source document validates
its original quote, effective mapping, exact numeric/definition fingerprint, encoded bound and acceptance time;
this is original comparison/provenance, never a provider signature or financial write permission.

PersonalMarketSourceDocuments service migration20261017013539 adds a separate protected-at-rest original-id/
profile feed. The same actual owner/session/device lock as financial revocation guards every request. Source
rows have a profile foreign key with privacy-erasure cascade, finite owner bytes, immutable UPDATE refusal and
separate monotonic source positions. Whole bounded uploads validate before staging; an altered original id
fails409. No financial table/row/value or delivered manifest is accepted from this metadata endpoint.

MarketSourceCompanionState app migration20261017013925 adds a separate binding cursor and private literal send
state. Portable projection scrubs that state with the original device binding, while retaining source history.
The original source/value index becomes non-unique: separate devices can independently confirm the same value
under distinct immutable receipt ids, and transfer must preserve both histories rather than silently dropping
one. Known original-id replay remains idempotent. Compiled application/service models were regenerated; both
design models report no pending changes. One metadata-check command initially used the wrong dbcontext command
group; the correct migrations command performed only the intended model comparison, without a database reset.

The personal coordinator connects the actual pinned typed transport after financial application. It seals a
finite local append cutoff, acknowledges only exact literal original documents, imports bounded source pages
and cursor atomically, and never creates a financial row for an absent target. Later source confirmations wait
for another round. Unsupported companion transport fails visibly instead of silently omitting source history.
Source received from the same peer is marked acknowledged to prevent an upload echo. A separate original
profile/device/identity binding is revalidated in every local metadata transaction.

MarketSourceAcceptanceTests qualified one actual HTTP/PostgreSQL/SQLite scenario with five retained stages:
the original source PUT committed while its reply was deliberately lost; continuation retained exactly one
server document and rate; a second actual client received the identical original document beside the same
financial row; another sync produced no source PUT echo; unknown/duplicate upload fields, changed original
content, wrong profile and independent PostgreSQL UPDATE were refused; a later manual rate removed current
attribution without deleting original history; and actual device/session revocation denied the source feed.
The first fixture compile lacked the DI Extensions import; only that failed build was repaired. The completed
five-stage original fixture must never be replayed. Its complete/stage markers and protected original client/
service state remain under the separately owned market-local-d299 runtime. PostgreSQL18.6-alpine was already
cached; --pull=never created only labelled repository-owned loopback container/network/volume. No owner data,
other project's containers, real source/provider licence, physical device or public activation was involved.

The shared encrypted companion, pending-source status/continuation refinement, remaining HTTP-negative cases,
retained saved-source UI and native/final delivery qualification remain in this same unfinished batch. Do not
infer household plaintext permission, complete signed financial-history coverage, whole-product completion or
release acceptance from this personal scenario. Successful unrelated qualifications were not repeated.

Native SecureMarketCache and MauiMarketRuntime now compose actual SecureStorage bounds, fixed approved identity
transport, original foreground/lock/file lifetime and explicit metadata/fetch effects. The new MarketReferencePage
and actual view model have separate native confirmation, original cached selection, no automatic symbol/asset
choice, explicit existing-account navigation and source/date/unit/purity/target text. Rates keeps its unsaved
manual draft across this separate route; its Date is initialized before control construction. More exposes the
separate reference destination; Debug snapshots include it. Forty real resource translations cover all six
languages, with full source/licence/expiry and manual/fine-metal limitations rather than copied English.
Original source and converted positive amounts use complete direction-safe MoneyText reference packets without
rounding a fractional per-unit quote to a posted minor-unit payment.

MarketReferenceFlowTests qualified13 distinct actual-source/SQLite cases: six language/native-port Cancel then
explicit Save paths, covered access before the dialog, picker changes inside confirmation, retired metadata,
unconfigured source, one failed explicit fetch with cache/manual preservation, original Persian price/source
packet and explicit account navigation. The first fixture compile failed on an unassigned nullable Catalog
field; make that controllable native-effect property explicit, then run the previously unrun cases. Initial11
passed; only the two subsequently added source-packet/account cases ran afterward. No model copied into tests.
Actual native controls/rendering have not been checked yet. Resource completeness/literal usage23cases and the
updated runtime-unit enum keys6cases passed; unrelated full-suite/security/provider checks were not repeated.

The first strict Windows
composition build found nullable original access.Scope; reject a missing original file rather than asserting
non-null. The resulting secondary XAML warning is retained with that failed build. The corrected strict Windows
composition build passed with zero warnings/errors. This is compilation, not a native visual/Android acceptance;
the eventual complete-page final builds/review remain due. Known committed reference writes now publish through
both existing financial and holdings subscriber sets; notification failure retains the exact committed result.
The first new-page build hit the XAML generator's undeclared inline r namespace. Move binding namespace
declarations to the page root, retaining the failed log; the corrected full page strict Windows build passed
with zero warnings/errors. The subsequent explicit-account navigation addition is source-tested; final native
builds/review remain required. Source policy/notification final composition reran only its two directly affected
previous cases, one each, successfully.

Personal TLS/PostgreSQL qualification completed once through the real identity/session middleware and two actual
SQLite clients: the source PUT committed but its response was withheld, then the exact original document was
acknowledged without duplication; a second device received the original receipt beside the exact rate. Unknown
or duplicate fields, altered original identity, wrong profile, direct database UPDATE and revoked session were
refused. Manual correction removed current attribution while preserving historical source content. This test
did not repeat already completed synchronization scenarios or claim live market-provider acceptance.

Household source documents use explicit payload format2 companion parts inside the existing signed/encrypted
publication, not a second unauthenticated source channel. The immutable personal v1 and household v2 financial
schema hashes are unchanged. New readers support legacy format1 and typed source format2; older readers refuse
the new content before financial apply and require upgrade. This is not forward compatibility or public rollout.
Each original publication captures at most8 pending documents; further documents remain visible pending work for
the next explicit synchronization. The source cursor is not the financial row cursor. Actual signed membership
and its publication horizon reject new attribution forged with an old removed-member key. Six focused cases
qualified source-only publication, exact legacy serialization, original identity immutability and signed horizon
refusal. Initial fixture failures (invalid multi-device genesis and incomplete owner-root rotation) were repaired
in the fictitious setup without weakening production validation; only the failed cases ran again.

The independent MarketHousehold executable then qualified actual Windows device-bound SQLCipher clients with
the retained non-superuser PostgreSQL service and real pinned TLS. Process1 sealed the original attempt once;
process2 reopened the same keys/ciphertext, lost the actual commit response and resumed its original operation.
A real Viewer invitation/admission received the exact source and rate with no readable personal financial HTTP;
its attempted source-value review was refused before confirmation. Process3 reopened both actual protected
clients and compared the original documents/current attribution and rates. Completed stages are guarded against
replay. This is not physical-device, Android native sharing or live-provider acceptance. The first qualifier
compile used a nonexistent peer member; the corrected binding uses the production session key exactly.

Market source review now captures the real household boundary under the transaction-local write permission,
releases its read transaction before native consent, and rechecks the same space/device/membership/identity/peer
inside Apply. Viewer, expired or unpublished membership cannot reach a writable review. Household selected-source
creation still copies only its approved accounts/entries/categories/optional attachments; rates, asset definitions
and valuations are not selected by that contract. Consequently none of their private source documents are copied
or disclosed by that creation shortcut. Sources confirmed later inside the shared file follow its signed chain.

Saved-source UI reads finite20-document pages and explains exact-current versus changed/deleted historical
targets. History survives cache expiry, is read-only and cannot supply a writable cached reference. Original
provider/revision/quote identifiers and accepted conversion remain visible. Six actual-source flow cases covered
all six languages after manual correction and empty expired cache; one SQLite case qualified21 receipts across
two pages after target deletion, without recreating finance. A fixture compile initially called a nonexistent
clock method; the corrected fixture uses its existing explicit clock property. These7 new cases passed.

## Final native/package qualification and limits

The first native observation reached stale refusal but did not find the expected Cancel. The diagnostic and
native runtime used Shell directly rather than the established active-page interaction path. Compose the
existing IAppInteraction instead, preserving its original native result, popup sizing and current modal page.
The review now also uses the actual growing PageHeader instead of the fixture NavigationPage's clipped title.
Its first helper compile used an unavailable binding getter, and a later helper edit guessed a nonexistent
localization member; both failed builds are retained. Correct the observer using actual semantic descriptions
and IsRightToLeft. No production permission or source check was weakened.

All18 actual Windows en/fa/de, light/dark,360/412/1280 cases then passed. Each qualified original selected source
identifiers, saved history, stale refusal, native Toggle acknowledgment, native original confirmation/Cancel,
complete realized captions and44-unit actions. The financial fingerprint stayed
3731cccaec8e1584f2a0455ee8c4bbb94cf0edbdc7ceeb764e44c04f66c4f678. Completed cases are durably guarded and were
not replayed. Original own-window images include the popup body and wrapping native footer; no desktop input,
focus or full-screen capture was used. These are real controls with fictitious provider effects, not live HTTP.

The independent Android signed Release AOT/trimmed caller qualified SecureMarketCache with exactly65536 bytes,
another scope remaining absent, oversized/invalid-scope/canceled replacement refusal and preservation, then
read the identical private value in a different actual process. Its first launch command guessed the main
application's activity suffix; no fixture started. Resolve the installed independent activity and launch the
original run. The initial real process showed an ANR during covered preparation; preserve it and choose Wait.
It later emitted private-market-staged, without reset. Only the required cold process boundary followed, and
private-market-complete was observed. The exact independent package was then uninstalled; main data was untouched.

The main signed Release package upgraded the original owned emulator installation. Before/after Home retained
My account and0.00 EUR. More and the manual Rates form both open the actual reference page; initial cache is empty,
Save/review remain disabled without a selected source, and an explicit unavailable identity/source check shows
translated failure with no automatic retry. The scrollable history remains read-only. An unsaved manual
1.987654321 rate survived nested reference navigation and return, then the form was left without Save. The first
observer expected only raw input but native accessibility exposes the complete "Rate, 1.987654321" packet; the
same retained screen confirmed the original value without repeating navigation. Return Home retained the exact
account/balance. An early hierarchy read during Activity creation was unavailable; observe the same process.
Main startup also showed an ANR, preserved with Wait; Activity start reported27540ms, which is not Home-ready
timing or a performance acceptance. Startup/ANR remains open and is not claimed fixed by this batch.

Final permanent-identity strict Windows and Release Android package builds have zero warnings/errors. The first
Android package preceded the observed native-dialog composition correction; build the final affected package
afterward, retaining both logs. Final APK:
artifacts/android-market-d299-final/pro.vafadar.zanance-Signed.apk;93741613 bytes;
SHA25697e554dfded459e45a071527a4a31ae4aa379f4d6025d15e5cf5538d569ba31d.
Permanent id pro.vafadar.zanance, min24/target36, arm64-v8a+x86_64 and complete embedded package verified.
v2/v3 signature verifies against local test certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b; this is not store signing or a phone handoff acceptance.

64 distinct new focused unit/source cases and the separate real TLS personal acceptance fact passed; household
three-process and native qualifications are separate evidence, not more unit-test counts. Resource completeness/
usage23 passed after the six saved-history translations were added; runtime enum keys6 had already passed and
were unchanged. Historical whole-suite3753, unrelated billing/cipher/accountant/AI/bank-import checks were not
repeated or recounted. Both compiled models and frozen financial hashes were checked after their actual model
changes; subsequent presentation-only changes did not justify repeating those checks.

After adding device-private companion acknowledgment state, extend only the existing portable-source scenario:
seed an actual acknowledged original source, export/restore, verify the accepted source remains exact while
SyncMarketSourcesSent is empty in the restored file and unchanged in the source. That one directly affected
case passed after the table/scrubber change; no successful unrelated backup suite was repeated.

The original fictitious PostgreSQL archive is retained with its service key ring and protected client roots under
artifacts/market-local-d299/runtime; archive202486 bytes, SHA256
38398b866284a7903424ff226cf0adc8b53fcbbf740b23144022cb5969a039f0. None is portable source/configuration or committed.
Live provider/coverage/redistribution/cost, genuine Android arm64/API24/16KiB, physical device, Mac/iOS, store
signing, performance and public release remain open. Do not reinterpret this local prerequisite as those gates.


## D-388 - Concrete next source decision

The provider-neutral delivery remains unchanged. [Reviewed ECB proposal](25-ecb-reference-source-decision.md)
adds actual public-feed and complete six-currency pair evidence, zero-upstream-fee terms and explicit
paid-context free-original obligations. It identifies missing date-precision/derived-source metadata
that must be implemented compatibly before a selected source can be activated. The unavailable default,
original financial writers and current D-387 app/APK remain unchanged; no new managed tests or repeated
successful native/build checks. Owner OD-14 source/terms approval is still required.
