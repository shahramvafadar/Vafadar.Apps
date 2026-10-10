# Native transaction Undo failure feedback - D-128 / AT-130

## Reproduced command defect and actual implementation

The transaction notice previously bound an unguarded UndoDeleteAsync: a failed RestoreEntriesAsync or following
LoadAsync escaped its asynchronous command. Move that exact command into TransactionUndoViewModel, compose it
in TransactionsViewModel and retain the existing XAML command name and controls. A focused regression on the
extracted two-await command fails with the original IOException before the guard. Log:
artifacts/transaction-undo-command-before-tests.log. No duplicate algorithm or fake MAUI control is compiled.

Non-fatal action/refresh failure is reported once through IAppInteraction.ShowFailureAsync, delivering the existing
Failures.ShowAsync translated generic dialog. Include action, refresh and pending feedback in one execution guard.
The actual AsyncRelayCommand remains busy; explicit duplicate invocation returns without touching a newer offer.
UndoService retains ownership of the original eight-second deadline. Failed Undo never refreshes or renews it.
Successful Undo consumes its offer before refresh; a later reload failure never recreates it. OutOfMemoryException
propagates and the execution guard always releases. No production fault-injection switch is added.

## Tests and strict platform builds

Nine added AT-130 application tests compile the actual command and UndoService through explicit native-effect
ports. Cover action failure, success, duplicate calls during action/refresh, pending dialog with a replaced offer,
post-commit refresh failure, expiry, fatal memory recovery, and isolated real SQLite trigger failure followed by
complete retry. Compare every column/row in all 24 tables; receipts and explicit refund relationships return.
Successful restoration intentionally updates only known entry/refund audit timestamps: normalize only their
UpdatedAt, retaining creation/receipt timestamps and other values. The initial fixture incorrectly expected
successful Undo auditing not to change after advancing its clock; that failed comparison was corrected, not
interpreted as a financial-data defect.

Focused nine pass; main 1,954 pass with zero failed/skipped, App.Tests 317. Test cleanup, strict Windows/Android
builds complete with zero warnings/errors. Canonical Build-AndroidApk.ps1 completes signed full Release; production
source hashes remain unchanged. Logs: artifacts/transaction-undo-command-focused-tests.log,
transaction-undo-command-all-tests.log, transaction-undo-command-tests-clean.log,
transaction-undo-command-windows-final-build.log, transaction-undo-command-android-final-build.log,
transaction-undo-command-android-final-release.log and transaction-undo-command-final-source-hash.json.

## Actual installed Release failure dialogs

Use only emulator-5570 and independently owned fictitious database zanance-43b6ec71edfe4a62a8c75a355adf9bcd.db.
Preserve original database/sidecar bytes. Six cases independently start from that database and seed one fictitious
purchase, explicit refund and text receipt. A trigger blocks INSERT of only that exact purchase, installed in the
fixture before signed Release starts. Drive actual Settings choices for en/fa/de light/dark, the Transactions row,
full-width entry Delete, its confirmation and Undo. Fresh native hierarchies show the real translated title,
message and complete OK target. Dismiss that dialog and invoke More to verify responsive navigation.
No desktop input, screenshot, system setting or physical phone is touched; screenshot protection stays enabled.

Retain fa-theme-helper-negative.json and the failed fa-light-theme-changed hierarchy: the helper expected an
English comma in the selected-theme description, but the actual Persian caption uses its translated punctuation.
Correct the check to Common_ChipSelected and resume after configuration, before any financial action. This was a
review-helper mismatch, not a failed theme change or product defect; it does not count as acceptance.

The first German attempt lost the existing adb daemon connection during configuration, before any deletion.
An initial resume helper also mistook the Settings link on More for a child-page header. Preserve those failed
hierarchies and de-connection-negative.json; reacquire the actual current hierarchy and resume the same fixture
without reseeding or repeating financial actions. Neither interrupted check is acceptance.

Read back all 24 tables after failure and compare the exact committed deletion, normalizing only the retained
refund's expected unlinking UpdatedAt. The purchase remains absent, its refund unlinked at the original amount;
receipt bytes, metadata and ownership remain unchanged. All unrelated rows/columns match. Feedback neither commits
a partial restore nor guesses a relationship. Restore exact original database/sidecar bytes, original English/theme
choices through UI, full Release, Home and stopped state. Evidence:
artifacts/transaction-undo-command-release-native/{en,fa,de}-{light,dark}/proof.json, restored-display-proof.json,
original/restored exact files and retained fresh native hierarchies.

Native retry within the eight-second window is not claimed; actual command SQLite tests prove retry separately.
Captions/layout are unchanged; retain established language/theme/360/412/wide layout matrices. These checks are
not new Windows large-text, phone or iOS acceptance. No schema, SDK, permission, portable data, entitlement enforcement
or paid grant change. D-126 SaveError visibility, later retained refund edits during outstanding Undo, remaining
commercial paths and billing/encryption/provider/device/release gates remain open.

Phone-test artifact: artifacts/android/zanance-d128-release.apk, 81,016,747 bytes; SHA-256:
`6a1babf3a89d1db80b79f9ebb0bb37a3bdf785ae4e29cc95925454282c90c4dc`. ZIP, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permissions and v2/v3 signature verified. Existing local test signing is not production signing.
Evidence: artifacts/transaction-undo-command-apk-proof.json. CI/device/store acceptance remain separate.
