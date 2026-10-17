# Original verified purchase lifecycle presentation (D-310)

## Missing requirement and complete local behavior

The current original ProductPlansPage shows BillingPlanText and actual offers, but prior source has no
owned-purchase validity/renewal/lifecycle rows. ENT-04 explicitly requires current plan and renewal facts.
The original D-163/165/166/167 writer/editor/import/restore outcomes and D-295 purchase/manage/restore UI
are already implemented. Repeating those deliveries would not fill this specific missing presentation.

The original plan screen now shows each already verified purchase's product/cadence and issuer, observed
state, last-verification date, paid-through end, provider-confirmed grace end, renewal enabled/disabled,
Lifetime and scheduled future product/effective date. Current regional calendar/language formatting is
used. A paid-through date is not labeled as the next debit. A scheduled change does not grant early rights.
Lifetime never receives a renewal/expiry date. Historic active state is explicitly labeled as observed
at last verification. Saved verification/outage allowance requires refresh; no new payment is inferred.

Local and identity snapshots retain their separate original histories and verification horizons; no
cross-snapshot state/date merge occurs. The existing cache decision still owns current access. Showing a
historic purchase cannot grant rights, activate quotas, launch a payment, restore a purchase or cancel a
subscription. No snapshot means no invented history. Page retirement clears it, and live regional/language
refresh rebuilds captions from the same originals without another store read. No receipt, account hash,
purchase reference or provider diagnostic reaches labels. Actual new-offer and Continue commands remain.

The new paragraphs/title rows grow independently at native font scale inside the original scroll area.
No fixed caption height, global scaling override, new control or hard-coded palette color is added.
Seventeen new resource keys have real en/fa/de/es/fr/it translations. No schema, migration, native SDK,
permission, cryptographic parameter, network provider, price or product policy changes.

## Focused actual-source tests

Seven new AT-310 cases qualify all six languages and absent-state behavior with actual source, SQLite,
localization and regional date formatting. Fictional metadata covers all seven lifecycle states, renewal
on/off, paid/grace ends, Lifetime, future scheduled changes, separate local/identity views and cache decisions.
Underlying lifecycle facts validate. Current paid access remains separate, retired history clears, no
reference leaks, original financial packets remain unchanged and live retranslation does not query a store.
The effect port throws if payment/restore/continue/manage is attempted; no real provider is invoked.

Initial seven cases pass. A strengthened scheduled-change assertion then selects the first renewal-off row,
which is the pending purchase rather than the original scheduled purchase; six theory cases fail. Only the
assertion selection is corrected to the original scheduled input. Those six cases pass, including the new
scheduled-product/date and populated live-translation checks. The unchanged absent-state case retains its
initial pass. Seven distinct cases are qualified across retained runs, not thirteen/new full-suite results.
No executable production correction follows this fixture/assertion failure.

Evidence: artifacts/billing-owned-state-d310/tests-owned.xml, tests-owned-final.xml (negative),
tests-original-row.xml, their logs and strict build logs. No unchanged Core/Data/backend/billing-verifier/
sync/security/upstream-crypto/full-suite run substitutes for changed original presentation checks.

## Actual hidden Windows layout

The maintained original billing layout route adds a dedicated billing-owned-layout mode with an explicitly
owned independent identity/path. Eighteen en/fa/de, light/dark, 360/412/1280 contexts at process-local 200%
text stress complete successfully. Six actual title/detail captions per context retain native scalable
text, full realized glyph allocations and growing rows. Original unrelated comparison/payment/retry matrices
are not replayed. Existing original actions keep their native complete names and at least 44-unit targets.
No payment/restore/store call occurs; the complete fixture financial fingerprint is unchanged.

The original own AppWindow remains hidden and is rendered directly; no desktop input, focus activation or
full-screen screenshot is used. Representative German light/360 and Persian dark/412 images are visually
reviewed. Receipt: artifacts/billing-owned-layout-d310/billing-layout.complete.json, eighteen per-case proofs
and own-window captures. The retained fixture hash is cfd8ebe0757deeece4abc1752d6de5baf27b7b17180b62be10a509623c30c34d.
The strict Windows Debug build has zero warnings/errors. This is native layout stress, not actual OS font
scale, spoken-reader, Windows commercial or real store acceptance.

## Remaining external acceptance

ENT-04 local implementation includes the prior comparison/readiness/retained outcomes and current verified
purchase lifecycle presentation. Genuine store status/SDK callbacks, sandbox transactions and replacement,
Mac/native StoreKit, phone/iOS, signing and owner production activation remain separate evidence gates.
No actual paid service, web checkout or Windows commercial delivery is added. Commercial limits remain
inactive. Neither layout fixtures nor a signed local APK are evidence for genuine purchases or publication.

## Actual Android and complete main package

The independent original-source Release/AOT caller completes all six en/fa/de, light/dark contexts with six
complete native title/detail paragraphs per context. It invokes the original ProductPlansPage/view model
with fictional lifecycle metadata, checks real TextView layouts for line ellipsis and horizontal/vertical
clipping, and compares original settings/accounts/entries. No main data, system setting, store, purchase or
financial write is touched. This native packet uses the local-history representation; separate identity
history semantics and live translation are qualified by the original-source flow tests. It is not genuine
provider/snapshot authentication or native payment acceptance.

Original run 883b0948ddef48579f11e4dc20a43a93, comparison 8b69680f82be42f798716093d0d83a38,
process 12770. Observation timeouts retain the same live process; the terminal report is read without
restarting or reseeding it. The independent process is stopped only after its positive report. Evidence:
artifacts/billing-owned-state-d310/android-native.json and android-identity.json. Native qualifier build
passes without warnings/errors. No unchanged earlier native payment/recovery/crypto fixture is replayed.

The complete ordinary main Release package builds without warnings/errors. Signature v2/v3 verifies with
the existing local test certificate. Actual package pro.vafadar.zanance, min24/target36 and embedded App/Data
AOT libraries for arm64-v8a/x86_64 are verified; no profiler is included. The configured connected package
declares INTERNET; it is not an offline or store-signed build. Path:
artifacts/android-billing-owned-d310-qualified/pro.vafadar.zanance-Signed.apk; 95,809,439 bytes;
SHA256 9fc3d9e1438f3fa0bc6178a228ea5756d550e55d8cc8bc2a14123dc1d96891f8.
Package/signature receipts are main-package-proof.json, main-signature.txt and main-badging.txt. This ordinary
edition does not enable the separate billing adapter; original paid-state page rendering is qualified by
the independent actual-source native caller, not a fabricated provider on the main installation.
The Release test filter output is cleaned once after terminal focused verification.

The original main installation upgrades successfully. Process 13148 first shows the existing Loading
cover, then reaches unchanged Home and its original zero-EUR readout (140 own-package nodes), without
reset, account creation or financial Save. The original first and ready hierarchies remain separate;
Activity timing is not Home speed or paid-state acceptance. Receipt: main-native-proof.json and
main-home-first.xml/main-home-ready.xml. The actual paid-state labels are qualified by the original
independent page above; the ordinary default edition's Home does not pretend to verify purchases.
