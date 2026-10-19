# D-392 - Import Undo and retained aggregate fields

## Actual defects and corrective boundaries

On the authorized Samsung SM-A176B, the original own-format one-row CSV was imported, its title
was edited through the original entry form, and Undo was confirmed once. The application reported
success and removed the edited row: balance returned from -187.00EUR to -180.00EUR. This is a real
failure, not a successful refusal. The original file, native observations and action receipts remain
retained. No actual owner financial data was used or extracted.

ImportAggregateStore previously wrote its immutable journal only when an aggregate was reduced.
Ordinary and explicit Keep both imports therefore had no original snapshot against which to detect
later edits. Every new ledger import now writes complete original row snapshots in the same transaction,
using the existing version-one ImportLinks format and optional empty adjustments. Undo compares
complete semantic metadata, refuses later attachments or extra batch rows, and requires an original
snapshot. Legacy unlinked batches without one remain for manual review; their current rows are never
promoted into invented historical evidence. Snapshot identity lookups use a set rather than a repeated
scan of every imported row. Existing aggregate ordering, refund/dependency and original-file guards remain.
No schema/model migration, CSV format, quota, runtime, permission or provider change is introduced.

A separate actual editor defect hid an existing aggregate in Simple mode: CanAggregate was evaluated
before LoadFrom loaded IsAggregated. Publish retained capability only after the complete original range
and categories are loaded, and expand details for an existing aggregate even without note/payee/tags.
The capability remains available during an unsaved off/on draft change. Ordinary Simple entries do not
gain new aggregation controls. No money is posted by opening or canceling this form.

## Managed qualification

Ten new real SQLite regressions cover ordinary/Keep both title and amount edits across reopening,
unchanged cold Undo, missing legacy snapshots, later attachment ownership, and backup/restore followed
by a later edit. Seven of the first nine fail against the original writer; two unchanged-Undo controls already pass.
The additional batch-identity test proves that an unsnapshotted later row sharing a batch id is not deleted.
An initial test compile mistake is retained separately and is not counted as a behavioral failure.

The corrected original ImportAggregateTests class passes all28 then-present cases (nine added), including linked
partial/full replacement, ordering, original metadata/attachment preservation, refunds and backups.
Six original bank-confirm/import/reimport/Undo application cases, the existing atomic/known-id case
and three recovery/commercial-write policy cases also pass. A final additional batch-identity case passes
separately, making39 distinct related cases (ten new). After replacing a repeated identity scan with
a set, only the nine then-new regressions repeat on the final source and pass. This is not a full-suite
rerun or a new overall executed-suite total. Native observations are not additional managed cases.

## Native editor qualification

Original actual MAUI forms load separate persisted Expense/Income aggregates, with and without other
metadata, and an ordinary Simple negative control. The fixture store is checked unchanged after each
case; the editor Save command is never invoked.

Windows:36 aggregate forms across en/fa/de, Light/Dark and360/412/1280 widths, plus ordinary-row guards.
Keep the first nine successful captures; after one incomplete native-toggle observation, a bounded
layout wait and continuation run inspect only the27 remaining cases. The continuation's launcher wait
expires, but its original complete receipt and captures are recovered without input or process replay;
the original process subsequently exits. The input desktop remains Default. All36 app-window-only
captures are reviewed. This is layout qualification, not general Windows/runtime acceptance.

Release/AOT Android:12 aggregate forms across the six en/fa/de Light/Dark contexts at412dp, plus six
ordinary-row guards, pass with the actual checked native switch, complete captions and original date
controls. Correct an observer that waited for the covered Activity while the original modal owned
focus; continue the same three-row SQLite fixture without reseeding. An initially unpublished PID is
observed from the same launch, without another start command. Original Main15560/Home0/account/OS
and the ordinary independent caller package are restored. No OS setting is changed.

## Physical financial sequence retained before the correction

The new D392_Fictitious_Overlap profile has one fictitious EUR account, opening2026-10-01 at zero.
The original three profiles and Main979 are captured beforehand. The same immutable27-column
format-two inputs are used throughout; these are Zanance own-format CSVs, not bank certification.
An opening-date precondition warning is corrected through the original account editor before the
first import; the same seed bytes receive a fresh actual preview. A mistaken observer warning and
a retained-tab navigation assumption are preserved and corrected without replaying a financial action.

| Actual operation | Observed result |
|---|---|
| Import Food100 covering1-7Oct and Housing80 covering8-14Oct | Two rows; Home-180EUR |
| Review Food20/Food30 and Housing10 while both overlap choices are unset | Import disabled |
| Choose Food Link and replace; leave Housing unset | Import still disabled |
| Choose Housing Keep both | Food remainder50; explicit double-count warning; import enabled |
| Import the three details once | Five complete native packets50/80/20/30/10; Home-190EUR |
| Request Undo of original two-row seed before dependent details | Actual conflict; both batches and balance retained |
| Planned cold start8338->23570 after the refusal | Same five packets and-190EUR; no input reimport |
| Undo only the dependent three-row batch once after cold start | Original two aggregates100/80 restored; Home-180EUR |
| Read restored Food original public detail | Title/date/account/category/payee/multiline note/tag and100EUR match |
| Inspect aggregation fields in original Simple editor, then cancel | Actual hidden-control defect; no draft edit or Save |
| Import the independent7EUR row, edit only its title, then confirm Undo once | Actual unsafe deletion failure described above |

## Final package and corrected physical continuation

The complete offline signed final package is artifacts/android-import-undo-d392/pro.vafadar.zanance-Signed.apk,
94912245bytes, SHA256c57a88c52b404efcab1702733328a8fce1646d83844183a2ac3a33d5c8c54220.
It contains384 AOT libraries for ARM64/x64, minAPI24/targetAPI36, no INTERNET/debug/profile declaration,
and the same eight engine libraries/default GC policy as D-389. Local signing certificate SHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b is verified; this is not store signing.
Canonical final Android and strict final Windows builds have zero warnings/errors. Managed output is
cleaned after the focused runs. The earlier aggregate-only package is retained as an intermediate,
not installed as the final financial correction.

Upgrade the original phone installation without reset23570->32753. The same D392 profile opens at
-180EUR. Its original Food aggregate now exposes a checked aggregation switch and all six original
From/To month/day/year fields,1-7October2026, alongside the same title/date/account/category/amount/
payee/multiline note/tag. Cancel without editing or Save. The earlier hidden-control evidence remains.

Repeat only the actual failed one-row case with the same immutable zanance-d392-unsafe.csv bytes and
original ID in the same profile. Original native preview is one new/zero known/zero invalid. Import
once, edit only the same title once, and confirm that batch's Undo once. The original conflict message
is now displayed; both history batches remain and Home stays-187EUR. Reject an observer's invented170px
minimum: the complete original fixed-footer message has its actual141px allocation. Recover the first
saved message instead of replaying Undo; retain the premature wrong-surface Home assertion separately.

Also confirm Undo of the original two-row legacy seed once. Its unavailable snapshot is refused
without invented backfill. Final All-time/All-types native packets are exactly the retained Food100,
Housing80 and edited7EUR row, total187EUR. Previously successful aggregate choices/imports, planned cold
restart and dependent Undo are not repeated after the correction. No additional cold ordinary-Undo,
raw main database equality or all-provider/device claim is inferred from the managed cold cases.

Return to Main through its original action sheet. Retain the initially empty native transition tree;
observe the same process until actual Home979 appears, without opening again or restarting. Only the
inactive D392_Fictitious_Overlap profile is retired through both original confirmations. Correct a
menu observer that expected Button while the original native action-sheet choice is a clickable TextView;
continue that already open sheet without replaying an accepted deletion.

The original Main/D316_Fictitious_Work/D319_Fictitious_Loan list is restored, Main is explicitly current,
and the five known native Main packets remain: income1000, expense25.50, refund5.50, internal transfer100,
and fee1. Current-month/all-type filter and original meaningful visible Home are unchanged at979EUR.
All three exact owned public inputs are hash/identity-matched before removal; rmdir removes only their
empty original folder. Exact four secure/font settings, physical size/density and final installed APK
are checked. The phone turn is released to the authorized peer; no further phone input follows.
No private main database/key is extracted, and full private/ID equality is not claimed.

## Privacy and remaining limits

Snapshots remain profile-local financial metadata, covered by the existing protected database and
explicit portable backup protection choice. They include original imported notes/tags until the batch
is undone or its profile/database is removed; later row edits do not rewrite the immutable original.
No raw selected CSV cache, attachment bytes in the journal, new SDK, network call or permission is added.
Native derived records are scoped to known fictitious data; no private main database or key is extracted.
This bounded sequence does not establish real bank formats, hostile-file corpus, all device workflows,
random crash/ANR resolution, TalkBack, iOS, provider/store acceptance or publication. D-386 ordering remains.
