# Reopening the exact reviewed planned payment (D-135 / AT-137)

## Actual writer and tests

Three baseline failures reproduce stale caller identity deleting another occurrence's generated money, changed
actual markers being unlinked anyway, and a second state write throwing after the complete generated deletion
had already committed and Changed retired scope. Bind actual state/EntryId/entry markers to one writer and
reuse the private complete ledger deletion pipeline. Manual/imported money remains untouched except its link;
generated deletion retains exact group/refund/receipt/paid-total and file-bound Undo behavior. There is no second
state writer after commit. Prior partial money and complete due/amount/note overrides remain, with suppression.

15 new cases/main 2,172 passed, zero failures/skips; App.Tests318. State-update failure, membership rejection and
actual post-delete/update SQL access retirement compare all 24 complete tables and zero events. Retry succeeds
once; normal store/plan Changed occur after commit. Four Free/expired-host operations need no selected accounts.
Returned deletion journal restores exact refund links and complete receipt bytes/ownership. Undo deliberately
retains AutoPostSuppressed: compare every original column except that flag, asserted separately as true.

Negative test preparation is retained: attachment-list metadata has no Data member; use the actual single-attachment
read for bytes. One expanded expectation incorrectly required Undo to clear suppression, and two manual metadata
expectations used the unlinked seed object rather than the actual linked database entry. Correct expectations,
without changing established product behavior. Logs occurrence-reopen-before-tests.log, occurrence-reopen-tests.log,
occurrence-reopen-all-tests.log, occurrence-reopen-all-tests-final.log and occurrence-reopen-all-tests-verified.log;
initial attachment compile rejection remains in review history. Complete test cleanup follows actual completion.

## Normal installed app

Full signed Release on owned emulator5570, verified local TCP5571/qemu through dedicated ADB port5038; no physical
USB device or shared-server change. English/Persian/German light/manual and dark/generated fixtures use actual
Plans All, plan history and existing explicit native Reopen confirmation. Verify manual 950-minor payment kept
with only its markers released, or generated payment deleted; original state opened/suppressed. Preserve exact
1,100-minor occurrence override/note, original audit identity and receipt bytes/ownership, every other row/column
in all 24 tables. No replacement or estimated entry, native entitlement activation or security-flag change.

Restore exact original owned sample database/sidecar bytes and English/System preferences, compare all original
tables after display restoration and leave the final full Release installed/stopped. Native successful delivery
is separate from SQL/access rollback and does not prove exception feedback. No layout/caption change or Windows
native invocation claim; strict Windows/Android and canonical complete Release have no errors/warnings.
Retain 18 transient unavailable native hierarchy reads as negative capture evidence. The bounded reader
reacquires the current hierarchy without repeating a successful Reopen action; these misses are not acceptance
or a product defect. Stop only the independently created local ADB server after final restoration.

## Package and remaining boundaries

APK artifacts/android/zanance-d135-release.apk; 81,049,515 bytes.
SHA-256 ef2ab4d152e35544e81d824930fc64c5749f942e7ed15eb93a32cd277f3744ce.
Package pro.vafadar.zanance, min24/target36, non-debuggable, complete arm64-v8a/x86_64 stores/App AOT,
ZIP integrity, v2/v3 signature and local test certificate 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b verified.
No production signing claim. Proofs artifacts/occurrence-reopen-apk-proof.json and
artifacts/occurrence-reopen-native/final-proof.json; clean/windows-build/android-build/release logs use prefix
occurrence-reopen-. No schema, migration, SDK, permission, portable field, caption/layout or activation change.
ENT-02/03 remain in progress: selected-plan generation/automation, choice persistence/UI, occurrence exception
feedback and all existing provider/device/iOS/security/billing/publication gates remain unfinished.
