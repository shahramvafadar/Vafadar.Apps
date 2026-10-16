# Local Android billing and cross-store service authority (D-295)


## Current delivery status

The substantial local Android/service delivery is qualified together: native catalog/intent/replacement source,
original translated comparison/identity UI, private signed current cache/recovery and actual PostgreSQL/HTTP
purchase authority/lifecycle. The canonical BIL-01..05 rows remain In progress. Native StoreKit2 implementation and
actual Mac builds, genuine store sandbox/provider credentials/public callbacks, SDK traffic, physical devices and
commercial/publication acceptance are not complete. Web checkout and commercial Windows remain excluded.

The initial plan below intended to hold both native platforms in one change. The actual execution boundary now
retains the complete locally qualified Android/service batch while the unavailable native Mac/store environments
stay explicit in the current backlog. This is consistent with Android-first delivery and the canonical plan's
instruction to continue independent approved work across external gates; approved product scope is unchanged.
Earlier in-progress/uncommitted/checkpoint wording below is retained history and superseded by this current status.
Do not treat source compilation, local provider fixtures or this delivery as genuine store or whole commercial
wave acceptance. The subsequent approved Germany accountant-export batch is locally qualified as D-296; see
[13-accountant-export-delivery.md](13-accountant-export-delivery.md). Bank-file import is the next independent ready work.

This is one complete approved batch after local household D-294, not a sequence of helper deliveries. Android/iOS
store adapters, server verification/lifecycle, protected offline cache, original plan UI, trials/offers and the
future identity-bound web issuer prerequisites belong together. No real product, account, price, payment or
commercial quota activation is authorized by a successful local fixture. Existing test builds remain unrestricted.

## Execution and authority

1. Keep issuer, store environment, distribution, logical product, verified owner and local financial profile separate.
   Free/local store Plus requires no Zanance account. Pro and future web grants require the actual confirmed identity.
   A client price, receipt header, backup, local success flag or browser callback never grants a paid capability.
2. Persist provider-verified observations and protected receipt identifiers independently of financial data. Enforce
   global purchase ownership, subscription replacements, notification deduplication, serial refresh and durable
   acknowledgement/finish continuation. Requery the store after notifications; arrival order is not authority.
3. Use current official Play Billing and StoreKit2 APIs. Query the actual store catalog/eligibility, retain pending
   payments, verify before final acknowledgement/finish, and expose restore/manage without pretending a purchase
   canceled a different subscription. Keep production/sandbox products, verification and signing roots distinct.
4. Cache a service-signed, device-bound verified snapshot in private platform storage, never financial backups.
   Use server time plus native monotonic elapsed time for subscription/offline windows. A wall-clock change cannot
   create time or a paid grant. A verified Lifetime base survives outages; a fresh installation restores via store.
   Store billing grace and bounded service-outage allowance are distinct; revocation/hold cannot gain outage grace.
5. Preserve the original comparison page/forms and six translations. Show only actual store prices, complete renewal
   terms, eligibility and failure/pending states. Fourteen-day Pro trial and the25%-first-annual-year Lifetime offer
   require matching verified eligibility/store phases, standard renewal and no stacking. No fake sale countdowns.
6. Qualify the completed local composition through actual database/HTTP/native boundaries and failure continuations.
   Do not repeat successful household/security/UI baselines. Document exact gaps when actual store accounts/products,
   native Mac/Xcode, sandbox transactions, public callbacks or provider credentials are unavailable. Commit/push only
   the completed verified local batch; physical/iOS/store/public activation stays an explicit separate evidence gate.

## Current source review

The existing Entitlement/FeaturePermission policy and inactive commercial registration are retained. The current
plan page is comparison-only. No store verification, purchase persistence, entitlement cache or billing adapter is
present at the start of this batch. D-294 is committed/pushed as15c6904f; its local baselines are not repeated.

Google's current Billing Library is9.1.0 (2026-06-18). Microsoft's binding9.1.0.2 is current, but its dependency
graph must be reviewed before selection: the published graph includes transport/location bindings. Do not import
an avoidable network/analytics/location SDK merely for convenience. Check the actual official artifact/binding
source and the final permission/dependency graph. A smaller direct official binding is preferable if necessary;
no obsolete library or third-party payment service is selected.

Apple's client uses verified StoreKit2 transactions and appAccountToken; server notifications use V2 signed payloads
and the App Store Server API. The real Mac/native/store environment is not available merely because this Windows
clone can compile common policy. Preserve that distinction throughout qualification.

Primary sources reviewed:

- https://developer.android.com/google/play/billing/release-notes
- https://developer.android.com/google/play/billing/security
- https://www.nuget.org/packages/Xamarin.Android.Google.BillingClient/9.1.0.2
- https://learn.microsoft.com/dotnet/android/binding-libs/advanced-concepts/resolving-java-dependencies
- https://developer.apple.com/documentation/appstoreserverapi
- https://developer.apple.com/documentation/storekit/transaction
- https://developer.apple.com/documentation/appstoreservernotifications/receiving-app-store-server-notifications

## Current implementation checkpoint

D-295 remains one unfinished, uncommitted whole batch. Commercial quotas, real purchases, actual store sandbox,
iOS, public callbacks and release acceptance are not claimed. The earlier checkpoint below is historical.

The current service has the additive BillingPurchaseAuthority migration and regenerated compiled model;
Google/Apple verification ports, authenticated notification scheduling, durable acknowledgement/reconciliation,
canonical ownership/aliases, sold-term preservation and restore-only legacy mappings. Events received during HTTP
retain their scheduling revision. Optional provider fields cannot invent renewal or known nontrial outage grace.
Expired requests never accepted by the service receive immutable no-op tombstones, without payment cancellation
or native finish permission. Exact original completion permission now has its own signature and native intent id;
only a fresh nonce-bound Current snapshot can replace paid cache state.

Apple code uses standard JOSE/system PKIX, fixed API hosts, exact application/environment/account bindings,
explicit private operator mounts and online certificate revocation. The bundled public root came from
https://www.apple.com/certificateauthority/AppleRootCA-G3.cer with SHA256
63343abfb89a6a03ebb57e9b3f5fa7be7c4f5c756f3017b3a8c488c3653e9179.
Negative certificate/wire cases are not positive acceptance of a real Apple receipt.

The private client retains original keys/requests, signed cache floors, native boot/elapsed anchors and completion
receipts outside financial/portable storage. Anonymous Plus and actual identity Pro use separate principals;
sign-out does not remove the anonymous Plus cache. The selected Android SDK adapter retains its original native
task across foreground loss. The original comparison page now has six-language store terms, actual provider price
packets, pending/unavailable/configuration feedback, explicit restore and fixed official management. It does not
activate quotas or invent prices. SDK transport/location dependencies still require final privacy/permission review.

Focused evidence, without replaying successful unrelated baselines:

- The original17 Core cases passed once. D-294's3753-case union and delivered native/crypto/household checks were not rerun.
- Provider series:12 of13 cases initially passed. Only the failed terminal scheduling case was rerun after its fix,
  producing a13-case successful union. Two later optional-field cases and five Apple refusal/wire cases passed separately.
- billing-persistence-cases-d295 completed nine retained real PostgreSQL stages, including original continuation,
  deferred acknowledgement, event-during-query, refund/replay, account-free restore and missing-authority refusal.
  This initial series preceded the later signed native-completion fields and is not replayed.
- billing-private-client-cases-d295 completed eight separate stages for the current signed protocol: lost committed
  response without another successful query, matching native completion, protected cold state, unknown boot,
  refund/nonce replay refusal, changed identity and an expired never-accepted request. Provider/private-platform
  effects are explicitly fictional; this is not actual store sandbox or native SecureStorage acceptance.
- billing-screen-cases-d295 passed eight actual application-flow cases: six-language price/renewal Cancel with
  unchanged financial rows, retired completion and native foreground continuation. No fake MAUI controls are used.
- Strict API/Data, selected Android binding/composition, application-flow and independent Windows layout compilation
  passed with0 warnings/errors. Test runners used shared10.0.12; final serviced self-contained/AOT runtime remains separate.

The own-window Windows review completed72 original plan-page cases under independent package
pro.vafadar.zanance.billinglayout. The later18 comparison-note/loading cases and90 new identity-form cases
completed separately; none replayed the72 successful cases. The actual Persian/English/German controls were
checked in light/dark at360/412/1280 widths. Empty native Submit displayed every relevant field error; actual
native sign-out Cancel made no identity request. No identity or purchase was created. Representative Persian
images were inspected. One initial diagnostic failure selected both ActivityIndicators; its original evidence
was retained and the diagnostic was narrowed to the running indicator before continuing the same fixture.

Seven additional application identity-flow cases passed: six-language invalid inputs, registration/sign-out
Cancel, unchanged financial rows, request-only secret clearing and retired-response suppression. The actual
HTTP current-session logout passed against retained PostgreSQL data: old access/refresh were refused while the
other device remained authorized. TestServer HTTPS scheme is not a real TLS handshake. No previous tables or
fixtures were reset; background workers were disabled only in this session-specific test host.

The original native intent now retains the reviewed logical product, prelaunch owned-receipt commitments and
the first selected new receipt in protected storage. An older owned purchase or substituted receipt cannot
finish it, including after process loss. A separate actual PostgreSQL/client case passed this boundary without
another query for refused references. Fresh provider facts for a different base plan are refused before a native
completion is issued; its separate case preserved the original pending request and granted no paid capability.
Cold SDK queries do not identify subscription base plans, so the original product is rechecked by the service.
Refresh cannot overwrite unresolved payment completion. Only synchronous SDK launch refusal or genuine native
Cancel releases an unverified intent; later callback/transport errors remain uncertain. No automatic new charge
or payment retry is introduced. Actual store sandbox/native SDK callback acceptance remains open.

The first real billing HTTP composition found an actual DI failure: BillingCatalog's internal constructor was
not selectable by the default container. Its production registration now uses an explicit factory, preserving
the constructor's internal scope. A later test-host-only failure correctly refused an installation marker whose
overridden protection ring was outside the host's expected keys directory. No guard, key or marker was bypassed
or reset: the host now keeps its normal installation/key ring together in its own subdirectory, while billing
uses the original separately retained fixture authority. Both original failure logs and private fixtures remain.
The same original client then completed the previously failed actual HTTP scenario: a paid observation followed
by service device retirement returned410, persisted its private refusal barrier, and a cold read plus outage
allowance could not recover old paid rights. A subsequent Observe refused locally without another HTTP request.
Original keys, pending work and old signed evidence are retained, rather than silently replaced or erased.

The selected Android Release billing/sandbox variant completed trimming and both native AOT compilations with
zero warnings/errors in3m22.71s. The merged manifest contains INTERNET and BILLING and no location permission;
official transport scheduling components are present. This checkpoint preceded the later client410-barrier change;
final package/native checks remain necessary after the remaining implementation. No complete new APK handoff or
native store purchase acceptance is claimed from this compile.

The checkpoint APK signature and permanent package were verified and it was upgraded once on the exact
Zanance_Storage_Review emulator, without uninstall/data reset or a physical device. Initial UI observation returned
no root, followed by the system not-responding dialog. The same PID21015 was retained; native Wait allowed it to
reach Home/More without restart. The service's last-ANR report contained no stored trace and the live process had
sleeping threads with the emulator using substantial swap. This does not establish an ANR root cause, acceptable
startup time or absence of a startup defect. The original binary/UI/error observations remain in local artifacts.

Actual Android plan/identity rendering then exposed a separate definite composition defect: lifecycle Resume,
Suspend and navigation retirement were selected only by PERSONAL_SYNC, leaving the independent billing edition
without foreground authority. Those existing transitions now cover either selected edition, while the automatic
sync worker remains sync-only. The earlier misleading Pending presentation came from an interrupted status read,
not an actual native payment. Reads now show verification-unavailable with explicit read-only Retry; payment and
restore continuations retain their original pending work. One new actual application-flow case passed this exact
read/refusal/retry distinction without replaying the earlier eight successful plan cases.

Build-AndroidApk.ps1 now explicitly controls Billing/BillingSandbox independently of PersonalSync, rejects an
Offline billing build and rejects Sandbox without Billing. Default/offline invocation cannot inherit billing
selection from ambient properties. The existing script encoding was preserved and its parser check passed.
The related full signed package rebuild completed through that script with no warning/error reported, and its
signature was verified. Its88.3MiB checkpoint remains local, not a final phone/release handoff. A same-installation
upgrade on the exact emulator retained its data; the new PID21387 reached Home without a process restart. Initial
covered preparation was still slow, so no startup-speed acceptance is implied. The observed plan read now showed
verification-unavailable, actual native Retry enabled and actual native Continue disabled, rather than inventing
an unresolved payment. The actual identity form then opened without the previous foreground refusal; no account
or password/code was entered. Native EditText hierarchy exposes its empty hints as text; the observation was
checked against those exact known placeholders, not misreported as submitted credentials.

The new read-only Retry surface completed36 separate native own-window cases across fa/en/de, light/dark and
360/412/1280 widths, for unavailable and unconfigured states. Real native Invoke performed one new status read
with zero purchase effects and unchanged complete fixture financial fingerprint. Original72 plan,18 note/loading
and90 identity cases were not replayed. A representative German narrow/dark image was inspected. The final
Windows Debug diagnostic build had zero warnings/errors; it does not add Android Release behavior.

One additional default-registration test resolved both actual official provider adapters plus the complete
command/reconciliation graph under container validation, with no credential access, database opening, directory
creation or HTTP. It complements the HTTP test's explicit fictional provider rather than claiming a real store
transaction. Native store lifecycle/sandbox, subscription replacement, private-slot-loss restoration and iOS/Mac
work remain unfinished inside D-295; no helper-only commit or push has been made.

Remaining inside this same batch: Android/AOT and actual native store continuation qualification;
native identity transport/storage qualification; original subscription changes/replacement; explicit private-slot-loss restore; actual optional
session/callback HTTP and private backup/restore reconciliation; StoreKit2 bridge and real Mac qualification;
privacy/notices and canonical docs; complete signed APK; then one whole-batch commit/push and delayed CI check.
Actual store products/credentials and sandbox matrices, public callbacks, Mac/iOS/physical devices and production
activation remain separate evidence gates. No owner accounts, credentials or spending were created/entered.

## Explicit private purchase recovery composition

The client now routes each optional identity's private state through protected metadata, with a separate namespace
for explicit restoration. Legacy slots stay at their original keys. An unreadable/corrupt original key or a lost
initialized platform slot never silently creates a replacement on Load. Explicit confirmed Restore retains a
candidate routing intent before creating its new key; it confirms that candidate's complete protected state before
publishing the route. A cut resumes the same candidate. Original keys, signed requests, caches and loss markers
are not overwritten or erased. The route contains no signing key and stays in protected platform storage, outside
financial tables and portable preferences. Current private generation and optional identity are checked on every
write. Client instances for the same peer/distribution/environment share their serialization gate.

A new context is restore-only: it may register/observe and verify existing owned receipts, but cannot begin native
payment. Only explicit current native owned-purchase discovery plus fresh service verification can release that
entry barrier; pending provider/native work retains it. Existing SDK pending purchases also refuse another launch.
Unreadable optional identity credentials do not force an account for anonymous Plus restoration, grant identity
authority or erase the old credential. Pro still needs its actual original identity. Six-language recovery status
and the full original Restore explanation describe this behavior and preserve the genuine confirmation/Cancel.

The focused actual PostgreSQL/private-client case passed: an original paid operation's lost acknowledgement and
private request/key bytes were retained; local loss refused Load; a cut after new candidate persistence resumed
that same customer/key; no paid grant was copied; restore-only refused new payment and pending release; actual
verification restored the one existing globally owned purchase to the new context. Cold private state retained it,
the old encrypted bytes were identical and the original immutable command reply remained. Provider/platform loss
effects in this case are explicitly fictional; no real store or hardware-key acceptance is claimed.

Six additional application-flow cases passed across all translations: recovery offered explicit Restore without
Buy/Continue, Cancel had no effect, accepted restoration called only its original port, and financial rows stayed
unchanged. No EF financial schema changed and no successful unrelated baseline was replayed.

The independently compiled signed Release AOT/trim Android package
pro.vafadar.zanance.billingrecoveryreview passed three retained processes on the exact existing emulator. The first
saved its candidate through actual MAUI SecureStorage, injected the cut before active routing and rendered the
original recovery page; its complete English native confirmation was dismissed with genuine Cancel. The second
process published the same saved customer/public fingerprint, retained restore-only while pending and refused
new payment; explicit empty fixture restoration released the entry barrier. The third process read that published
routing and snapshot cold. All stages checked the old native private record unchanged. No installation/data reset,
key deletion, unrelated package or system setting was used. This caller's authority port is explicitly fictional
and can return only empty grants/observations: it implements no store purchase or paid capability. Its native
storage/process evidence does not accept real provider transactions, hardware-backed keys, power loss or release.
Native build finished with0 warnings/errors in2m08.36s; the package is x64 and has no INTERNET permission.

Eighteen original own-window recovery geometry/Cancel cases completed, but subsequent image inspection found the
German confirmation action caption clipped despite IsTextTrimmed being false. Those original images are negative
caption evidence, not complete rendering acceptance. WindowsDialogTargets now wraps the original native button's
TextBlock and lets the footer grow while retaining its name/result/command and44-unit minimum. The scoped popup
observer still detaches after dismissal. A separate focused caption review now measures every realized character
against both its allocated text slot and original button bounds; the successful main page/body baseline is not
replayed.

## Exact existing-subscription replacement and signed store possession

The current Android selection joins actual native owned-token commitments with fresh signed service grants and
exact configured store SKUs. It refuses unverified ownership, an existing subscription on another store account,
multiple/suspended/already scheduled subscriptions and a parallel subscription from another supported store.
Account-free local Plus remains separate. Plus-to-Pro replacement first requires explicit association of the
original purchase with the actual confirmed Zanance identity; merely signing in does not associate that purchase.
A separately purchased Lifetime base explicitly warns that it does not cancel an existing renewing subscription.

Native replacement uses Play9.1's product-level SubscriptionProductReplacementParams plus the retained original
purchase token in SubscriptionUpdateParams. Same-SKU cadence keeps the billing date; a higher cross-SKU tier uses
provider time credit; a lower cross-SKU tier/cadence is deferred. The store alone computes the payable amount and
credits. The original opaque purchase id is retained with the native intent and signed command. Fresh provider
linkedPurchaseToken must match it. A deferred callback may still identify the old SKU; only that original reviewed
intent may interpret the verified future target. Previously owned receipt commitments still cannot complete it.

The actual two-line deferred provider response keeps the old paid product distinct from its verified scheduled
target. Signed native completion names the original target, while the paid snapshot continues to grant only the
current product. At the known change date, neither old-tier outage extension nor an invented future-tier grant is
permitted. A fresh provider query must establish the effective product. Previously accepted future terms survive
later catalogue changes. Continue uses original completion receipts before querying again; only explicit Restore
may create a separate recovery context or associate local Plus with an identity. A synchronous already-owned
launch response does not silently perform restoration or identity association. Failed presentation reads preserve
original pending commands/intents without another provider query; only actual outage errors enable outage policy.

Public Apple verification/restoration now requires the StoreKit2 signed transaction JWS, bounded to16KiB, checked
for the original application/environment/account/transaction chain before an operator credential read or HTTP.
A numeric transaction id alone is not store possession. Only authorized internal Refresh/reconciliation may use
the already persisted canonical numeric id; even a resolved public alias must recheck its original signed JWS.
The existing JOSE/system-PKIX/pinned public Apple root and fresh Server API query remain required. No positive
real Apple receipt or native Mac build is claimed.

New focused evidence, without replaying earlier successful cases:

- Seven source-linked actual selection cases passed: replacement rather than parallel subscription, explicit
  identity association, native pending/unverified/missing/scheduled ownership and cross-store management.
- Six new actual view-model cases, one per translation, checked all three replacement timing explanations,
  current regional dates, the changed-offer re-review, Lifetime warning, genuine Cancel effects and unchanged
  complete financial rows. Initial fixture registration was missing its date formatter; only these failed cases
  were rerun with the real formatter. No application algorithm was duplicated.
- Four new Apple possession cases passed: numeric/unsigned/oversized public references performed zero credential
  reads and zero provider calls; the separate internal canonical path still required the actual provider query
  and honestly returned its simulated outage. This is refusal evidence, not accepted Apple interoperability.
- One five-stage PostgreSQL/protected-client replacement sequence passed: explicit original existing purchase,
  refusal of parallel entry, exact replacement intent, committed lost reply, cold same-command completion without
  another successful provider query, old purchase retirement and effective future product only after fresh
  verification. Signed completion bound the original target and native intent; accepted future terms survived.
- One actual HTTP-session/PostgreSQL/private-client sequence passed: account-free Plus, refusal before explicit
  identity association, retained anonymous provider binding through a verified Pro upgrade, same-identity Pro in
  the other supported distribution and refusal of another actual identity. Provider facts are a named fictional
  port; no real store, public TLS handshake or payment is claimed. Both completed fixture roots are guarded
  against replay and prior PostgreSQL data/keys were retained.
- The strict selected Android Debug build passed with zero warnings/errors after restoring its actual Android
  assets. The initial no-restore attempt found only Windows assets; that failure is retained. The current SDK9.1
  preferred replacement APIs compile; compilation is not actual store execution or sandbox purchase acceptance.
- The strict original Windows app passed with zero warnings/errors. Thirty-six new native own-window cases
  covered the deferred replacement/changed-offer/association text and ManageExisting state in English/Persian/
  German, light/dark,360/412/1280 widths. Original native Cancel started zero payment/restore effects and the
  complete financial fingerprint remained unchanged. Narrow Persian/German prompt images were inspected.
  These are native layout/effect-port cases, not actual store calls.
- The earlier focused native dialog action-caption review also completed all18 cases, including every realized
  glyph against the original text slot/button bounds. Its earlier clipped German-caption images remain negative
  evidence, separate from the corrected successful images.

Cold owned-purchase routing additionally checks the exact old/new SKU from the original verified replacement
context. An unrelated Lifetime/unknown/null-SKU receipt cannot pin or finish that subscription intent, and an
unresolved identity payment prevents implicit association of another local purchase. All five new routing cases
passed; the earlier seven selection and six translated timing cases were not replayed.

Current snapshots now prioritize every nonterminal purchase, then fill remaining bounded slots with the newest
terminal facts. More than32 genuinely nonterminal purchases still refuse explicitly. A complete fresh nonce-bound
snapshot may safely omit old terminal history; it never merges old paid cache entries. The database retains the
whole history. One new actual PostgreSQL/protected-client case seeded40 explicitly fictional terminal records
alongside one provider-port-verified active purchase: the signed current snapshot stayed at32, retained actual paid
access, then removed all paid access after fresh verified revocation, while all41 service records remained.
The fresh verified replacement also dates its original source's revocation observation so recent source metadata
stays available for cold continuation. The service/test projects compiled strictly without rebuilding shared
projects during the independent mobile package operation.

Primary replacement/possession references:

- https://developer.android.com/google/play/billing/subscriptions
- https://codelabs.developers.google.com/codelabs/play-billing-subs-replacement
- https://developer.apple.com/documentation/storekit/verificationresult/jwsrepresentation-21vgo

The signed current Release APK/native continuation, StoreKit2 bridge/Mac build, actual store account/product and
provider acceptance, service deployment restore and final canonical whole-batch documentation are still separate
unfinished checks. The earlier successful household, cipher, full-suite and native private-storage baselines were
not replayed. D-295 is not yet a completed or committed whole delivery.

## Current signed mobile and service packaging checkpoint

The current complete product APK is
artifacts/android-billing-cold-continuation-d295/pro.vafadar.zanance-Signed.apk:92,697,133 bytes,
SHA2566de7ff6012b1dda81250131f5a85582483b6d7abb036bc362d965e9f9d4e1820.
Build-AndroidApk.ps1 selected the explicit billing sandbox edition and embedded Release AOT/trimmed application.
Package/signature verification passed: permanent pro.vafadar.zanance identity, arm64-v8a/x86_64, API36 target,
v2/v3 signing with the existing local debug certificate. INTERNET/BILLING are explicit; there is no location
permission. This is a complete installable test APK, not a store-signed release or physical/arm64/API24/16KiB
acceptance. The earlier replacement APK predates the cold-SKU routing correction and is retained only as a build
checkpoint, not the current package.

The same existing emulator installation was upgraded with -r without data reset. PID22478 stayed the same from
covered preparation through actual Home and native More/Plans-and-access navigation. The current original page
honestly showed verification unavailable, no invented paid result or payment, disabled Continue-original-purchase
and an enabled read-only retry. No credentials, purchase, authentication setting or system setting was entered.
One intermediate UI hierarchy capture returned null during navigation; its stale prior-file pull is explicitly
negative evidence, and only the subsequent successful fresh hierarchy supports the page result. There was no
process reset to make this pass. The app was returned to its own Home tab. Cold storage recovery/translated
layouts already completed earlier were not replayed.

The final related strict Android Debug and Windows development builds passed with zero warnings/errors. The new
focused test output was cleaned once after completion. No unrelated full suite, cryptographic probe or household
scenario was repeated. The product's commercial limits remain inactive.

The changed Linux service source also published successfully in the pinned Docker build, running as1654:1654 in
image sha256:a07ff49dd4ce8ffe911f19c7666f67312ea51d88fabbc5b1bbc450d55469710d.
Only the explicit source allowlist entered the build context; host secrets, the owner archive and unrelated
projects were excluded. This image has been built, not deployed or accepted by a new runtime backup/restore.
The retained D-294 service, database, identities and snapshots were not changed during this packaging check.

D-295 remains uncommitted and incomplete as one whole approved batch. Native StoreKit2/Mac work, actual store
provider/sandbox/credential/public-callback acceptance and final service composition/documentation remain open;
current Windows common-code or Android qualification cannot substitute for those results.

## Selected SDK privacy boundary, still under qualification

The explicit Android billing variant uses Microsoft's9.1.0.2 binding of official Play Billing9.1.0. Its official
Maven POM itself requires transport-api/runtime/backend-cct and Play services base/basement/location/tasks;
these are not an optional application analytics feature or an avoidable binding-only addition. A direct binding
would not remove the upstream dependencies. The original official POM is retained in the local qualification
artifacts. Presence of the location dependency is not proof of location access; inspect the final merged manifest
and native runtime behavior rather than inferring either collection or absence of collection from package names.
The offline variant does not select this SDK. The billing variant necessarily permits network access and cannot
claim the offline build's no-INTERNET boundary. SEC-09's existing ML Kit diagnostic caveat for network-capable
variants also remains relevant. Do not claim zero SDK diagnostic traffic or release privacy acceptance from a
successful build. DF-12 has been updated to distinguish this unfinished optional variant from Phase1/offline.

## Earlier checkpoint (superseded)

D-295 remains an uncommitted, unfinished whole batch. No new product APK, complete-suite count, real purchase,
store sandbox, native UI, production quota or release acceptance is claimed.

Written so far: logical products/issuer/environment and explicit identity-versus-local-Plus scope; normalized
provider lifecycle, independent billing-grace/outage rules and monotonic-clock policy; actual catalog-phase offer
eligibility; strict source-generated signed protocol; separate service entities for device/customer, identity
account binding, canonical purchase/transaction aliases, device claims, original command queue/reply and
notification work. Purchase metadata is outside the financial database and its portable exports.

The API now has explicit localhost/Integration-only billing qualification composition, HTTPS/content-free refusal
boundaries, nonce-bound public time, original device-key registration and signed commands. Registration replies
are immutable and empty of paid grants. Only a fresh Current-kind snapshot for the exact live request nonce may
replace cache state; Operation-kind lost-response replies are completion receipts, not current entitlement proof.
Principal revision tracks only identity/retirement changes. Purchase freshness is separate: exact fresh nonce,
nondecreasing signed server time, full replacement state and original active-request/profile ownership. Purchase
updates never rewrite another customer's principal counter. The native consumer still must enforce these rules.

Google verification uses the fixed application and official Developer API endpoints, current1.77.0 official auth,
explicit service-account-only credentials under this service's private billing-credentials directory, a fixed OAuth
token endpoint and the exact same validated credential bytes. No ambient machine account, owner credential read
by a review, interactive sign-in, raw-token logging or arbitrary HTTP destination is introduced. The actual official
9.1.0 POM confirms the binding's transport/location dependencies; a direct binding would not remove upstream
requirements. The9.1.0.2 SDK restores only with ZananceBillingEnabled=true. Offline graphs/manifests omit it.
Actual merged permission, SDK traffic/privacy and native purchase behavior remain unqualified; do not infer absence
of billing diagnostics from the lack of product analytics or from a source/manifest inspection.

Provider work retains its literal signed request and finite queue before HTTP, closes database scopes during HTTP,
leases one original step, revalidates actual session/device and ownership afterward, converges verified transaction
aliases and advances only its completed prefix. An awaited terminal failure releases only its own visible lease;
uncertain work/commits and another worker's lease are preserved. Provider acknowledgement follows committed grant
persistence; durable pending acknowledgement still needs the worker below. Actual explicit restore can associate
an account-free store Plus purchase, while Pro and future web rights require the actual identity. Client-selected
registration ids are separate from server-created store-account UUIDs; Google installation/identity namespaces are
also domain-separated. A previously verified purchase cannot be reassigned to a different identity.

Focused results only: the17 new Core billing policy cases passed once; original initial/registration/current API
strict compilation and native package restore had0 warnings/errors. These are not provider interoperability or
running-app evidence. Logs: billing-policy-cases-d295.log/xml, billing-current-api-compile-d295.log and
billing-native-package-restore-d295.log. The policy runner used the installed10.0.12 shared test host; that is not
qualification of the final serviced self-contained runtime. Test output was cleaned. D-294's3753 successful-case
union and its native/device/privacy/crypto scenarios were not repeated.

Next work inside this same batch:

1. Finish actual Apple API/JWS chain verification, authenticated Google/Apple notification handling, durable
   acknowledgement/reverification workers, provider catalog/legacy-term preservation and provider-failure/replay
   qualification. Settle the complete service model, then generate its one additive migration/compiled model and
   exercise actual isolated PostgreSQL/HTTP cases. Do not migrate or replace a retained D-294 fixture prematurely.
2. Implement the original private native billing client/cache and current-snapshot consumer, Android adapter,
   StoreKit2 bridge/Mac build prerequisites, original plan page, complete six-language states and explicit
   store/identity readiness. Never enable checkout merely because the local verifier can issue an empty snapshot.
3. Complete changed-scope native UI/AOT/Release, service backup/restore/provider reconciliation, privacy/notices,
   current signed APK and canonical docs, then one whole-batch commit/push. Store accounts/products/credentials,
   actual sandbox, Mac/iOS, physical devices/public callbacks and production activation remain explicit gates.

Do not commit these foundations separately, rerun unchanged17 policy cases or repeat delivered household/crypto/UI
checks as a substitute for the unfinished billing flow. Continue through the actual end-to-end local batch.

## Final local Android/service artifact and runtime scope

The current complete test APK, including actual bundled SDK notices, is
artifacts/android-billing-local-d295/pro.vafadar.zanance-Signed.apk; 92262644 bytes;
SHA256ed860c6d6bc90df4a1fe1adc938ee76976c6457ee7b0f5cb3561bbdfc8943270. v2/v3 local debug signature and the permanent identity pass; the explicit Billing permission
guard passes and the bundled notices exactly match source bytes. The earlier cold-continuation APK remains a
qualified code checkpoint before the notices-only asset update, not the latest artifact. No store signature,
physical/arm64/API24/16KiB acceptance, real transaction or commercial activation is claimed.

The existing isolated stack zanance-selfhost-privacy-restore-d294 received the additive migration after a complete
drained snapshot, zanance-hosting-snapshot-billing-pre-upgrade-d295. Actual PostgreSQL reports the new migration
and seven Billing*Row tables. The initial metadata query used incorrect plural table names and returned0; the
correct mapped-name query returned7 without any reset or repair write. API/database are healthy, API runs as
1654:1654 with a read-only root, and its scoped actual CA/hostname-validated HTTPS readiness probe passes.
The final image with service notices is
sha256:1c0f2608650103060ba0207d30754f063e99ee2a509eb1961d75ffc9d6efebbf.
Only the API container needed the notices-only final replacement; unchanged migration/dependencies were not
rerun. Existing identities/private data/latest privacy markers and household opt-in were retained. Other projects
were untouched. Public billing remains disabled; the genuine provider/public/Mac/store gates above are unchanged.
The earlier whole historical-restore drill was not repeated as a substitute for the changed migration check.

Nine new permission-policy cases passed, including exact Billing network/store permissions, refusal of missing
permissions/location/advertising-id/network-state/debug packaging and unchanged Offline/Cloud refusal of billing.
The previous successful privacy cases were not replayed. Actual package inspection was repeated only for the
changed handoff binary. Google SDK/MIT and service-side Google authentication Apache notices are bundled; their
terms grant no rights to Zanance source. Current SDK disclosure is docs/privacy/zanance-sdk-review.md.


## Hosted regression boundary

The changed CI definition now compiles the explicitly selected Android billing sandbox adapter as well as the
original default Release graph. The identity job still fails unexpected skips, but excludes only the10 named
BillingLocal qualification cases. Those cases have their own ephemeral PostgreSQL job: original nine service
stages first, then original private-client cases, then HTTP session qualification. A fresh exact fixture root,
marker, dedicated loopback55475/database/user and nonuploaded private files preserve the same actual contract;
only bounded test result XML is uploaded. No product/provider credentials or real payments are needed.

The actual local runner discovery confirmed all10 fixture cases and that the normal identity selection excludes
only their explicit trait. Test bodies/earlier completed roots were not replayed. The source/test compilation
passed strictly; workflow YAML was manually reviewed. No local YAML parser was installed solely for this change,
and hosted success is not claimed until the one real post-push CI check. The actual full hosted matrix may expose
platform/ordering issues that local qualified cases alone cannot establish.


The final notices APK upgraded the same installation and opened its genuine native About/notices page in
PID22824. The native legal text includes the new actual billing notice and the page title is correct; the package
asset is independently byte-identical to source. This is loading/content evidence, not a new complete legal-text
glyph-layout, physical-device or store acceptance claim. The app returned to its own Home without credential,
purchase, security-setting or financial changes. Its unchanged successful billing/recovery UI matrices were not
replayed for the notices-only update.
