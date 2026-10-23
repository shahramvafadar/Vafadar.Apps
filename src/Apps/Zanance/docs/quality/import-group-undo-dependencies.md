# D-490 original import-group Undo dependencies

## Reproduced financial defect and correction

D-392 protects complete imported-row snapshots, attachments, refunds and linked aggregate imports.
An imported transfer can nevertheless keep identical semantic fields when a later original editor
adds a fee to its GroupId. The old Undo removes that transfer and leaves the later fee. Reproduce
this on isolated real SQLite before correction: the unsafe-case test fails while the unchanged
pre-existing-transfer positive control passes. Do not repeat D-392 physical overlap/Keep both,
dependent aggregate or title-edit cohorts already accepted.

Capture external ledger group members and associated AssetEvents under the same original writer
transaction as the accepted import. Exclude the batch's own imported ids, order source records,
exclude only audit timestamps and retain a SHA256 equality digest in the existing ImportLinks JSON.
Grouped batches use internal journal version2; ordinary ungrouped batches keep version1 and skip
group dependency reads. Before any Undo mutation, compare the current external group to that
original digest. New/edited/removed peers or holding context require review and retain all rows
and the journal. Unchanged pre-existing groups remain undoable; this is not a blanket group ban.

Version1 closed groups with no external context remain supported. Version1 groups with external
members have no original comparison proof and are refused without guessing/backfilling it.
Unsupported versions or malformed version2 digests fail before mutation. This internal journal
version is independent of the public CSV format. No EF entity/model/schema/migration or compiled
model change, financial calculation, public CSV projection, permission, SDK or new UI string occurs.
Database backups carry the existing journal, now including its non-secret dependency digest.

## Focused managed evidence

Ten distinct new AT-79 cases pass: later fee/cold refusal; valid pre-existing transfer; complete
imported group; reverse grouped-import dependency order without consuming a refused journal;
changed original parent; legacy closed/external controls; portable restored dependency proof;
original/edited holding context. The first two pass after correction; run only the eight new
controls subsequently. Five selected existing aggregate/refund/idempotency regressions pass.
This is15 selected successes/10 new cases, not a full-suite rerun or a new suite-total claim.
Strict Data.Tests and Windows Release builds have zero warnings/errors.

## Actual original Android page and retained continuation

A complete signed Offline ARM64 Release/AOT caller executes six original ImportExportViewModel /
ImportExportPage history Undo buttons. Each case owns one isolated fictional SQLite source and
persists source.json before Undo, outcome.json immediately after the original command, and a
complete.json receipt after native feedback/full typed-row comparison. These secondary fixture
databases are plain under the original SQLCipher engine; this is not encrypted-Main acceptance.
The existing independent protected base remains exactly equal in its complete typed-row view.

| Original case | Actual Undo outcome | Complete source result |
|---|---|---|
| Later fee after imported transfer | Conflict | All later/original rows retained |
| Imported fee, unchanged pre-existing transfer | Success | Only imported fee removed |
| Complete imported transfer/fee | Success | Both imported rows removed |
| Unchanged original holding group | Success | Imported fee removed; cash/event retained |
| Later holding note change | Conflict | Complete later source retained |
| Legacy journal with external transfer | Conflict | All rows/journal retained |

Run85efc38f1a8b40c6b09d14d8ba13f26a/comparison68c141bb646b433d9f91d1347daaa267 first
fails in PID29319 after the first original Undo: the observer incorrectly ScrollToAsync targets a
fixed result dock outside the scroll viewport. Preserve its failed report and installed APK.
Correct only that observer, install-r the caller without clearing its fixture and explicitly
resume the same run/comparison in PID30533. The saved first command outcome supplies read-only
feedback; NativeActionsThisPass=0 for that case,1 for each of the five unexecuted cases.
No accepted import/setup/Undo is repeated. Six cumulative original Undo actions, all full typed
source comparisons and the unchanged original base pass. Successful reports refuse replay.

Consent is the declared fictional interaction port, not native system-dialog or picker acceptance.
The final caller has56,058,204bytes,192 AOT, valid v2/v3, no Internet/debuggable and five unchanged
native runtime/cipher engines against D-487. SHA256:
`c02dd0ea91390f58f102f854cf8f99eaac0037ab440a0f3b4a71ee3fc7b0cfd1`.
The preserved first failed caller is56,496,646bytes/SHA256
`b4bb7fd6d5517bc8cac3fe6742e7343fe62f230fb6968a9fb3d5ff779a936841`.

## Shipping package and exact public restoration

Canonical Build-AndroidApk.ps1 -Offline completes without warnings/errors. Complete signed Main
APK: `E:/Zanance.ReviewBuilds/D490-shipping-package/pro.vafadar.zanance-Signed.apk`,
96,980,895bytes,384 AOT, ARM64/x64, valid v2/v3, non-debuggable, no INTERNET/network-state permission;
ten runtime/cipher engines match D-487. SHA256:
`5e2f6c8bd2843b78b1369d4a6b0b10b8f8fc1e9073931ac6275b548ea900cd79`.

After terminal native success, restore the pre-existing independent APK with install-r and leave
it stopped, retaining its data. Original Main13827/all141 public Home tuples/979EUR/old APK and
OS match before the Main upgrade. Install-r final Main without reset; actual WARM Activity1645ms
is not cold/whole-Home timing. Main31885 retains all141 original public tuples/Home979 and exact
font1/1080x2340/density450/accessibility0/empty services. Remove only three owned hash-matched XMLs.
No Main private-store/key inspection, owner authentication or OS change occurs. This public
preservation proof is not complete Main database/key equality. The phone remains project-reserved.

## CI and remaining gates

The prior exact ecde9366 head returns CI39947429715 and CodeQL39947429490 success in its one
allowed delayed request. Its consumed ci-physical-period-reminders-d489 schedule is deleted;
no second check. The new D-490 pushed head receives its own single delayed check.

This closes the reproduced external-group Undo gap, not every possible financial relationship,
malicious-file, aggregate, native confirmation, full-device/cipher/security, general ANR/crash,
performance, spoken accessibility, iOS, provider, Store or release gate. Preserve successful old
cohorts; continue only genuinely uncovered planned work. D-386 priorities remain unchanged.

Private local receipts retain the first failure, original source/outcome/checkpoints, managed
reproduction/final logs, native result, package proofs, restoration/final proofs and cleanup list.
No private fixture data is uploaded.
