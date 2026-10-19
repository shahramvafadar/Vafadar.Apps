# D-397 - Retain the committed advance-bill outcome (in progress)

## Original defect and complete batch

The original SettlementViewModel treats every writer exception as Common_SaveFailed. Its actual
advance-bill writer commits before calling OnChanged without a committed-result wrapper. A subscriber
failure therefore offers the accepted bill as a failed Save. Native return also occurs after commit
without retiring that form; retrying an extra payment can post the same difference again. Refunds
instead risk a stale-review refusal. Neither is an honest retained accepted outcome.

The initial focused regression fails on the original source: expected
CommittedWriteNotificationException<SaveResult>, actual ordinary InvalidOperationException from
OnChanged after Commit. Preserve artifacts/settlement-d397-original-regression.log. This is actual
SQLite/application evidence, not a claim of an observed duplicate physical-phone charge.

Deliver the whole original form lifetime, field validation, retained writer outcome and native
follow-through in one batch. Do not reopen completed D-396 reconciliation or unrelated successful
matrices. D-386's dedicated ANR/crash/accessibility execution order remains in force.

## Implemented correction

The advance-bill writer retains the committed result if a nonfatal subscriber fails. It checks the
original configured file/currency and current application draft before save/commit, preserving
existing correction, selected-account and household boundaries. The form reads its plan, account
and history in one coherent actual-file SQLite snapshot on its worker before enabling input.

Compile the actual non-UI SettlementViewModel in application tests, keeping only its native query
adapter in a separate partial file. Original IAppInteraction effects provide dialogs and return.
After acceptance, the form is read-only and its existing action becomes Back. The accepted state is
set before native follow-through; Save/Cancel/return retries cannot call the financial writer again.
Typed drafts survive pre-write refusal, invalid fields and failed/retried loading. Accepted state
cannot be reloaded or retargeted as another bill. Feedback remains beside the fixed original action,
with complete translations in the six existing languages. No model/migration/backup/permission change.

## Focused completed checks

Sixteen original application/SQLite behaviors pass under strict compilation, including positive and
refund bills, post-commit observer failure, native-return refusal, overlapping return actions,
changed advances, required discard, independent field errors, SQL rollback/retry, actual-file/currency
refusal, late draft retirement, initial failed-load retry and accepted-form retirement. Earlier ten
passing cases are development evidence, not additional distinct cases. Fifty-five related retained
account/write-policy regressions pass; no full or unrelated suite is repeated.

Independent Windows run cf2cfe71095d40e3976667cf233ece8c builds with zero warnings/errors. All six
en/fa/de light/dark original modal cases complete: actual native Save accepts one extra400-minor
expense in Light or one600-minor linked refund in Dark against the original1000-minor advance.
An owned actual Changed subscriber then refuses notification. Original advances/fields remain
unchanged; the accepted form is natively read-only, translated complete feedback and44px Back remain.
One original native Back per case closes the form without another writer call. All eighteen own-window
360/412/1280 images are inspected and complete; base original rows remain unchanged.

The global Windows runner still returns failure because it also retains a separate Home read failure:
The original quick-resource definitions, source or references changed; reload them. This occurs during
default Home refresh as the independent fixture changes, not in the completed settlement checks.
Keep error.txt and the six accepted receipts/complete.json/images; do not replay any accepted bill or
report a globally clean run. This observed Home refresh defect remains separate ready work.

## Android continuation and final candidate

The complete offline Main Android package is built, and strict Windows Release has zero warnings/errors.
The independent Release/AOT caller has a separate plain SQLite financial fixture; this is not Main
encryption qualification. Initial PID lookup races original launch; recover PID2311 without another
start. That run refuses a native button before the first financial action. Await the real attached,
shown, focused/enabled target and retain the same run27124c7392d543ccb334981c1575b258 and untouched
first account/plan/advance. Corrected PID2692 accepts the first en-Light400-minor expense, then its
caption observer incorrectly counts both the hidden loading-error label and the visible footer label.
This is a terminal observer failure after accepted money; never repeat the Save.

The same live PID2692 is read through app-only native hierarchy: accepted14.00 bill, read-only fields,
complete recorded/follow-up feedback and original48px Back are visible. Invoke only that Back once;
the original modal closes. Retain first-accepted-native-back-proof.json. The next caller correction
filters actual shown native labels and reads back that already accepted first case without another
Save/Back, then continues only the five not-yet-executed contexts on the same database. The final
Release/AOT caller succeeds with all six contexts retained, original base rows unchanged and no
financial replay. Final PID3187 uses comparison53a1f482ef784a0abfe00a014d23ce83. Preserve both
terminal reports, the first accepted native Back receipt and all original financial identities.
The independent caller uses its owned real modal return port; this does not qualify the Main native
route adapter or encrypted Main storage.

The Main candidate is94,973,685 bytes, SHA256 c401e10a49a309ba28d77d73ff413d1d86e0b8422f5d0e23122d6c5be584f4c8.
V2/v3 signature verifies with the existing local development certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. Its package/privacy report is
retained. Main is upgraded once without reset and restored to Home0 in PID3694; meaningful controls,
four original OS settings, size/density and installed package hash match the retained baseline.
The independent caller is stopped after terminal qualification. No phone input or Main financial
write occurs. Test output is cleaned with zero warnings/errors. Documentation accompanies the
finished correction; the separate Windows Home failure remains open. Hosted CI,
physical phone, all financial histories, iOS, generic stability/accessibility and publication remain
separate evidence gates. No new spending/provider/account decision is needed for this local correction.
