# File-bound ledger deletion and retry-safe Undo - D-127 / AT-129

ENT-02 remains in progress. Actual single/bulk deletion and Undo use the guarded production store. Registration
stays inactive without a paid grant. No schema/model, migration/compiled model, SDK, permission, caption/layout,
portable format or entitlement activation change. Receipt bytes are not loaded into a new in-memory snapshot.

## Bound owned-data writes and explicit deletion snapshots

DeleteEntriesAsync checks DeleteData and RestoreEntriesAsync checks Corrections under the actual SQLite writer
before groups, existing ids, refund links and occurrence reads, including inactive deployment. Free, paid, exact
guest and expired-host owned-data operations remain available; personal payment never replaces membership.
Cached access is captured before the writer wait and rechecked after SQL before commit. Profile movement during
access capture cannot choose another database for the already-bound operation.

The returned committed deletion batch carries its actual original file, entries and explicit stored refund links.
It retains the existing IReadOnlyList shape used by the actual app's Undo offer. Reject this batch in a different
selected profile before writes. Replace the session-global refund-id map: copied profiles can share imported ids,
failed deletion must leave no phantom relationship, and failed Undo must not consume links required by retry.
Restore only missing entry ids; relink only newly restored purchases. Repeated/no-op Undo emits no Changed.
Plain row enumerables retain row-only recovery without inferred refund relationships. The short batch is not a
durable journal or portable backup format. Rows, refund links, occurrence state and paid totals commit together
or fully roll back with no Changed.

## Actual application Undo service

UndoService preserves the original eight-second deadline after failure; retry is available only while that same
deadline remains valid. One action runs at a time. A successful action clears only its original offer version;
replacement and dismissal during an await remain authoritative. An old failure cannot revive a dismissed offer.
Changed-observer failure cannot leave the service stuck in progress. No clock/OS settings are changed.

## Tests and negative evidence

29 added Data cases and nine actual app-source cases pass; main passes 1,945 with zero failures/skips, App.Tests
308. Data covers three reproduced refund defects; receipt bytes/metadata/ownership retained through Undo; five
data-right contexts; four membership paths; two wrong cached files; returned-batch wrong profile; two initial-file
moves; four SQL/access retirement failures/retries; two independent writer races; complete split deletion/Undo;
and four derived occurrence failures. Compare complete columns of all 24 tables. Use production DI/native SQLite
and actual factory connection callbacks, with independent cached fixture facts rather than fake storage.

App tests compile actual UndoService through existing native-effect ports and isolated SQLite. Cover original
deadline/retry, two replacement and two dismissal completion/failure paths, double taps, late failure, observer
failure and a real failed ledger Undo followed by complete refund retry. Native rendering/error adapters are
separate evidence. Logs: artifacts/ledger-undo-before-tests.log (three failures), ledger-undo-app-before-tests.log
(one failure), ledger-undo-after-tests.log, ledger-undo-retained-receipt-tests.log, ledger-undo-focused-tests.log,
ledger-undo-app-focused-tests.log, ledger-undo-policy-all-tests.log and ledger-undo-policy-tests-clean.log.

Preserve the negative receipt fixture: ledger-undo-receipt-before-tests.log and ledger-undo-snapshot-after-tests.log
fail at an incorrect assumption that deleting an entry removes its receipt. Existing code intentionally retains
orphan attachments for short Undo, with startup purge and durable-import protection. Correct the assumption and
remove the unnecessary receipt-byte snapshot prototype; the finished source retains this policy unchanged.
Neither failed fixture is evidence of receipt loss.

## Platforms and actual normal installed Release

Strict Windows/Android and test cleanup pass with zero warnings/errors. Canonical Build-AndroidApk.ps1 produces a
complete signed Release with warning/error guards; both production source hashes remain unchanged during final
checks. Logs: artifacts/ledger-undo-policy-windows-final-build.log, ledger-undo-policy-android-final-build.log,
ledger-undo-policy-android-final-release.log and ledger-undo-policy-final-source-hash.json. Captions/layout did not
change; retain completed language/theme/width matrices rather than repeat them without a new layout concern.

Only independently owned emulator-5570 and its exact sample database are used. Preserve original database/sidecar
bytes; seed one fictitious purchase, explicit refund and retained text receipt with complete original metadata.
Normal signed Release opens the real transaction details from Transactions, invokes Delete and confirms the
existing native confirmation. The actual list
shows the refund, removes the purchase and offers Undo. Invoke that real button within its existing deadline;
the original purchase returns and the successful offer disappears. Read back all columns of all 24 tables:
original amounts, group/date/title/payee/note/tags and creation metadata match; the refund links to the original
purchase; receipt bytes, filename/type/date and ownership remain unchanged. Only UpdatedAt of the two exact
reviewed rows is normalized for expected Undo auditing. No unrelated account/entry/settings/table changes.
Restore exact original database/sidecar bytes, reinstall full Release, return to Home and stop. No security/profile
storage, desktop input, screenshots, system settings or physical device is touched. Evidence:
artifacts/ledger-undo-policy-release-native/proof.json, native-undo-proof.json and retained fresh hierarchies.

Retain the initial native review failure: actual-delete-offer and delete-observed-state show the existing
confirmation dialog, before deletion or any Undo deadline. The helper incorrectly expected immediate list
navigation; handle the actual confirmation and then invoke Undo successfully. This is a review-helper correction,
not a product defect. Before financial actions, correct only the fictitious audit timestamps to the database
INTEGER UTC-tick format; timestamp-fixture-correction.json proves unchanged complete rows before correction.
Neither attempt is acceptance. Original database bytes are preserved throughout.

Faulted native Undo/error-dialog delivery was not injected or accepted in this review. Source review also finds
TransactionsViewModel.UndoDeleteAsync awaits Undo without a failure guard; investigate that real native caller
next, alongside D-126's bottom-of-form SaveError visibility. Later edits to retained refunds during an outstanding
Undo also require a focused safety review. These observations are not covered by the normal success flow or a
claim that all ENT-02/03/04 entry points are finished.

Phone-test artifact: artifacts/android/zanance-d127-release.apk, 81,455,189 bytes; SHA-256:
`cbfc1ea71c140004370dcbc7d62f85c3fb6673412950fc527d828a89103fabff`.
ZIP integrity, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permissions and v2/v3 signature pass. Existing local test signing is not production signing.
Evidence: artifacts/ledger-undo-policy-apk-proof.json. Build/emulator evidence is not physical-phone, iOS, OAuth,
store or product-release acceptance. Selected read-only data, automation, commercial feedback, other ENT-02/03/04,
billing/encryption and external decisions remain unfinished.
