# Atomic new planned payments (D-134 / AT-136)

## Implementation and negative evidence

Three initial SQLite tests reproduced two defects: full entry committed before a failed state insert, and final
partial payment classified using a stale form balance. Existing partial state failure already rolled back.
Reuse the established private complete ledger writer pipeline instead of copying validation or granting arbitrary
correction rights. Bind actual stored rule/slice, kind/accounts/destination, state and entry identity checks before
money. Read actual linked partial entries, apply Core effective amount/date/outstanding rules, then persist full/
partial/final money, paid total and status together. Keep explicit amount/date and prior partial totals. Reject
stolen identities, skipped or changed occurrences; duplicate settlement retains the existing concurrency contract.
Legacy RepairSettlements remains for older data. Changed follows complete commit only.

16 added cases: full/partial SQL state failure, stale balance final payment, six changed relationship/identity
rejections, two actual post-entry-SQL access retirements and retries, two invalid input rollbacks, moved/changed
amount and complete metadata, plus two independent-provider full/partial writer races. Rejected writes compare
all columns of every row in 24 tables. Preserve zero notifications before commit and two successful store/plan
notifications. Existing account selection/retained payment and automatic-posting regression tests still pass.

Initial test compilation attempted to assign a read-only entry identity; corrected to the actual original row.
The first expanded run had two fixture failures: Free's four accounts required selection before the intended
post-SQL retirement point. Use Pro's unlimited fixture scope for this test, without changing production rules.
Retain negative logs; these are test preparation failures, not claimed passing evidence.
Main suite: 2,157 passed, zero failures/skips; actual non-UI App.Tests 318. Strict Windows/Android builds and
canonical complete Release: zero errors/warnings. Cleanup follows actual test completion.

## Normal installed app

Only owned emulator-5570, dedicated ADB port5038/local TCP5571 verified qemu; shared server and physical USB
phone untouched. Full normal signed Release with fictitious owned sample fixtures, English/Persian/German:
light Confirm saves one 1,000 minor-unit payment and settled state; dark native Pay part saves 600 then 400,
leaving the first open and finally one settled state with earlier PaidAmount600 and exactly two ledger entries.
Preserve actual default past due payment date, explicit confirmed/source markers and every other original row
across 24 complete tables. No debug route, entitlement activation or forced screen/security setting.

Restore exact original owned sample database/sidecar bytes and English/System preferences; compare all24 rows
after display restoration. Final complete Release installed/stopped. No desktop input, full-screen capture,
physical-device/iOS or Windows-native invocation claim. No layout/caption change requires a new layout matrix.
Native normal success is separate from SQL/access failure proof; command exception feedback remains unfinished.
Transient unavailable native hierarchy reads are retained as negative captures. Reacquire the fresh hierarchy
within the bounded reader; never repeat a successful payment action to obtain a capture.
German dark had a dedicated-server connection failure during initial amount input, before the first Save.
Reconnect the same owned local transport, verify qemu and reacquire the original unsaved form with amount10.00.
Resume only input/payment in the already prepared case, without another fixture or any repeated successful Save.
Retain that interruption as negative infrastructure evidence, not a product defect or failed payment acceptance.

## Package and follow-up

APK artifacts/android/zanance-d134-release.apk; 81,487,957 bytes.
SHA-256 cef1458896e516f4fc58fcd65390d050135aecc1e294097b69cbbe940d74d784.
Package pro.vafadar.zanance, min24/target36, non-debuggable, complete arm64-v8a/x86_64 assembly stores/AppAOT,
ZIP integrity, v2/v3 signature and local test certificate 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b verified.
No production signing claim. Proofs artifacts/occurrence-payment-apk-proof.json and
artifacts/occurrence-payment-native/final-proof.json. Logs occurrence-payment-before-tests.log,
occurrence-payment-all-tests.log (negative fixture run), occurrence-payment-all-tests-final.log,
occurrence-payment-clean.log, occurrence-payment-windows-build.log, occurrence-payment-android-build.log,
occurrence-payment-release.log. Initial read-only-identity compilation error is retained in review history.

No schema/SDK/permission/backup-field/UI change; normal commercial source remains inactive. ENT-02/03 remain in
progress: generated-entry reopening, selected-plan generation/automation, choice persistence/UI and occurrence
command exception feedback are unfinished. Physical device/iOS/OAuth/encryption/billing/publication gates remain.
