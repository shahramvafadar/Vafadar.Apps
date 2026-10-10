# Immutable ledger Undo and later financial changes - D-129 / AT-131

## Reproduced defects and actual changes

Five focused real SQLite cases fail before the change: old Undo reattaches a retained refund after amount, kind or
receiving-account changes; restoring into a now-empty account's changed currency relabels minor units; mutating a
returned deleted row/tag list changes what Undo saves. Evidence: artifacts/ledger-undo-conflict-before-tests.log.
Every fixture uses actual production DI/store/native SQLite, with complete stored-row snapshots.

Delete captures complete expected unlinked refund rows and original source/destination account currencies under
its existing actual-file writer. The file-bound private container owns cloned ledger rows/tag lists and returns
fresh copies through indexing/enumeration. No receipt bytes enter it. Failed EF attempts can modify their working
copies without modifying the original offer. Restore materializes existing/missing ids, returns immediately when
already complete, and otherwise checks snapshot dependencies under that same writer before inserting entries or
changing occurrences. Partially restored siblings must match complete original semantics/creation identities.
Retained refunds must exist and match complete expected unlinked state and CreatedAt; ignore only UpdatedAt audits.
Explicit relinking or missing/recreated identities require review, with no guessed relationship or partial write.
Account currencies must still match, including transfer destinations. Name/archival changes retain owned recovery.
Use the existing source-generated SameEntry comparison plus explicit CreatedAt; no reflection metadata or schema
is added. Plain row enumerables retain their established trusted row-only recovery path without batch provenance.
This short container is not a durable journal or portable package. Current commercial registration stays inactive.

## Tests and platforms

17 added Data cases compare complete columns in all 24 tables: the five regressions; note/tags/review/foreign
refund metadata; deleted/recreated refund; explicit relinking; transfer destination currency; changed/unchanged
partial-group recovery; no-op after later edits following complete Undo; account rename/archival recovery.
One added actual command case compiles TransactionUndoViewModel/UndoService through the existing native-effect
port with isolated SQLite: report stale refund, preserve every ledger row, no refresh on failure, keep six remaining
seconds of the original offer, and expire without another write/failure. No fake control/application algorithm.
Focused 17 pass; main 1,972 pass, zero failures/skips; App.Tests 318. Existing SQL/access rollback/retry, independent
writer, receipt and derived occurrence tests also pass. Cleanup/strict Windows/Android produce zero warnings/errors;
canonical Build-AndroidApk.ps1 completes signed full Release. Source hashes remain unchanged. Logs:
artifacts/ledger-undo-conflict-focused-tests.log, ledger-undo-conflict-all-tests.log,
ledger-undo-conflict-tests-clean.log, ledger-undo-conflict-windows-final-build.log,
ledger-undo-conflict-android-final-build.log, ledger-undo-conflict-android-final-release.log and
ledger-undo-conflict-final-source-hash.json.

## Actual installed normal and conflicted Release

Only independently owned emulator-5570 and exact sample database zanance-43b6ec71edfe4a62a8c75a355adf9bcd.db are used.
Preserve original database/sidecar bytes. Normal full Release invokes real Delete, confirmation and Undo from
Transactions; the original purchase returns with its refund link, full metadata and unchanged receipt bytes/type/
filename/ownership. Compare all 24 tables, allowing only expected UpdatedAt of the exact restored rows. Restore
the original bytes and full Release Home/stopped state. Evidence: artifacts/ledger-undo-conflict-release-native/
proof.json, native-undo-proof.json and retained fresh hierarchies.

Independently prepare a second exact fictitious fixture. Its after-delete SQLite trigger edits only its retained
refund amount/note to 2000 and a fictitious later note. The deletion captured the original amount 300. Real installed
Delete/confirmation/Undo now reaches the actual stale metadata guard, not an INSERT-abort simulation, and the
existing native translated error dialog. Dismiss and navigate More to prove responsiveness. Read back all 24 tables:
the purchase remains absent, the newer refund amount/note/unlinked state remains, all receipt bytes/metadata/ownership
and unrelated rows match the committed deletion plus that exact fixture edit. This simulates the stale state
deterministically; it does not claim a user edited the refund through the native editor during the eight seconds.
Actual separate later saves are covered by real store/application tests. Restore exact original bytes, original
English/theme choices through UI and full Release Home/stopped state. Evidence:
artifacts/ledger-undo-stale-release-native/en-light/proof.json and restored-display-proof.json.

Keep connection-negative.json and the interrupted theme-configuration hierarchy: the existing adb daemon
connection failed before Delete/Undo. Reacquire the current hierarchy and resume the same prepared fixture,
without reseeding or repeating financial operations. That interrupted attempt is not acceptance.

Visible controls/captions/layout/error adapter are unchanged. Retain D-128's en/fa/de light/dark installed native
error-dialog matrix and established 360/412/wide layout evidence. No new large-text, device/iOS, native editor
timing, provider or store acceptance is inferred. Visible ledger Save feedback and remaining commercial, security,
billing, provider/device/release gates remain open. No SDK, permission, portable data or commercial activation change.

Phone-test artifact: artifacts/android/zanance-d129-release.apk, 81,459,285 bytes; SHA-256:
`f2a255906a8282725703a33868a1db85a843a253cf19b399058ce9a94e343317`. ZIP/full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permissions and v2/v3 signature pass. Existing local test signing is not production signing.
Evidence: artifacts/ledger-undo-conflict-apk-proof.json. CI, physical phone and release acceptance remain separate.
