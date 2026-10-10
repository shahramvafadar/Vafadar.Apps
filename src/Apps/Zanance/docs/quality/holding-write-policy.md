# Holding write rights and atomic purchase price - D-122 / AT-124

ENT-02 remains in progress. HoldingStore direct writes, retained corrections/delete/Undo and import data rights
now use the D-118 actual-file cached-access gate. Current registration remains inactive without a paid grant;
test builds stay unrestricted. No model/schema/migration/compiled model, SDK, permission, UI string/layout or
portable entitlement change. Explicit selected read-only classification and other ENT-02/03 paths remain open.

## Enabled operation rights

Direct new types, explicitly added locations, new holding events, standalone prices and confirmed account-to-
holding conversion require ManageHoldings. Existing stored-id corrections, delete and Undo preserve Free rights,
including an expired shared host, but personal Pro never replaces exact accepted shared membership. Default-location
scaffolding supports retained quantity corrections; it does not grant new types/events/prices. A new reasoned
quantity correction references an existing type and has no linked entries, group, basis or proceeds. Import and
Undo preserve owned history rather than rejecting data after downgrade; selected read-only new-work classification
remains unfinished. A Purchase source label alone never authorizes a new standalone price.

Capture access before acquiring the actual SQLite writer. Existing event/delete/import/conversion/Undo atomic
transactions now encompass history and existing-id reads. Independent providers cannot both validate the same
quantity and commit sales leaving negative history; identical simultaneous imports recheck actual stored ids and
create each row once. An explicit requireTransaction retains the existing operation transaction when checks are
inactive; it does not add a separate quota transaction. Retained dimension-lock/conflict/ledger rules stay intact.

## Complete purchase editor Save

Previously, the editor committed event/payment/fee and then separately saved its purchase-derived price. It now
requests the store's atomic derived-price overload. The store reads the actual type currency and derives the price
with the existing calculation, retaining the latest stored same-type/date Purchase price id, note and creation
metadata. Existing non-editor callers keep the original no-price overload, so imports and established Data calls
do not gain automatic valuation rows. Earlier dates and other prices are preserved.

Recheck cached access after SQL before commit. Failed price insert/update restores event, money, deleted old fee and
price rows as one transaction. Retired access after SQL also rolls everything back. Changed fires once only after
the complete commit. Manual new prices remain an independent management operation; a retained purchase correction
may derive its corrected price after expiry without losing essential correction rights.

## Actual SQLite acceptance

46 new cases are tagged AT-124. Full main suite passes 1,767, zero failures/skips; App.Tests 299 unchanged. Cases
cover ten new-work rejections with complete all-table equality/no Changed; paid/exact shared/inactive purchases;
payment/fee/derived price and actual ledger income/spending classification; latest same-date price metadata;
standalone Purchase-label rejection; retained Free/expired-host metadata/manual-price/purchase corrections;
reasoned no-money quantity correction and six disguised-work rejections; ten missing-membership boundaries;
default scaffolding; Free idempotent imports with no revived management right; complete delete/Undo; and conversion
failure/rollback/retry preserving the legacy account and ledger.

Real SQLite triggers reject derived-price INSERT and existing-price UPDATE. Complete stored rows remain identical,
including an old fee already deleted within the failed correction. A native SQLite function invoked by an AFTER
INSERT trigger retires cached access before commit; the complete SQL Save rolls back and no Changed fires. The
test decorates the production context factory's actual SQLite connections, rather than duplicating app algorithms.
Independent providers rendezvous before writer acquisition: enabled and inactive sales commit exactly one group,
reject the second conflicting history and leave 3,000 quantity units; simultaneous identical imports add four
objects once and skip all four in the second transaction. Counts/history are not mocked or protected by a store
semaphore.

Initial focused compilation used a nonexistent Spending method and two analyzer-disallowed filtered Single calls;
fix only test assertions and exercise actual LedgerCalculator.Totals. The first runnable suite passes 40 of 43:
an unrelated EF options callback did not attach to the production factory, and the sale conflict's Available field
means quantity before the whole date, not after the first same-day event. Use an actual factory/connection decorator
and preserve the established conflict semantics. The corrected focused 43-case suite passes. Three final cases and
latest-price ordering run in the full final suite, all 46 pass. Keep negative evidence separate from acceptance.
Logs: artifacts/holding-policy-focused-tests.log, holding-policy-focused-tests-final.log,
holding-policy-focused-tests-corrected.log and holding-policy-all-tests.log. Test output cleanup passes without
warnings/errors: artifacts/holding-policy-tests-clean.log.

## Platforms, normal installed Release and APK

Strict Windows and Android builds plus canonical complete signed Release pass with zero warnings/errors. Source
hashes confirm HoldingStore.cs, CommercialWriteTransaction.cs and AssetEventEditorViewModel.cs stayed unchanged
during final builds. Logs: artifacts/holding-policy-windows-final-build.log, holding-policy-android-final-build.log
and holding-policy-android-final-release.log. No visible controls changed; completed language/theme/width layout
matrices are retained. SQL failure proof does not claim an injected native error-dialog review.

Prepare only one fictitious empty asset type and location in the exact independently owned emulator-5570 sample
database. The actual normal Release More/Holdings/details purchase editor records 5 g, 25 EUR and a 1 EUR fee,
then displays 5.000 g, the purchase-based 25.00 EUR value and 5.00 EUR/g average cost. Read back every column/row in
all 24 tables: only the exact purchase, its AssetPurchase/Expense group and marked derived price differ. Original
type/location metadata, other financial/settings rows and original ledger match. No profile/security storage or
physical device is touched. Restore and verify exact original database/sidecar bytes, reinstall full Release and
return it to stopped Home. Evidence: artifacts/holding-policy-release-native/proof.json and fresh native hierarchies.

Phone-test artifact: artifacts/android/zanance-d122-release.apk, 81,000,363 bytes. SHA-256:
`351b78174f2add62eaab66467d6a02511331126210fb29ed2fdb20931a403157`.
ZIP integrity, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permission boundary and v2/v3 signature pass. The existing local test certificate is not production
signing. Evidence: artifacts/holding-policy-apk-proof.json. Emulator/build evidence is not physical-phone, iOS,
OAuth, store or product-release acceptance.

Remaining ENT-02/03: explicit active/read-only selection and imported/restored new-work classification, future
budget activation; contributions/allocations/occurrence automation, profiles and other automatic/native entry
points. Translated limit UI, purchases, encrypted storage and owner/external release decisions remain unfinished.
