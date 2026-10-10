# Complete bulk-selection reload (D-105 / AT-110)

## Change and scope

Previously a fresh bulk snapshot scanned the complete ledger once for every selected id. With all 100,000 rows
selected this meant approximately five billion comparisons; Toggle also scanned the source and Select all rebuilt
its membership set. The bulk flow now indexes every id once per fresh snapshot, intersects retained selection with
that set, and reuses it for the same snapshot's Toggle/Select all. No cap, pagination, truncated financial scope or
database/schema/permission change is introduced.

Removed rows lose selection; known rows keep it across fresh objects/order changes; newly loaded rows are selectable
but are not implicitly selected. Unknown ids stay excluded, pending dialogs freeze selection, and actual financial
commands use copies of the current source, preserving validation, cancellation, transfer/refund semantics and Undo.

## Actual bound Windows measurements

Each build uses three independent hidden own-app processes per workload, one complete selected reload per process.
The fixed QA-06 fixtures contain 10,000/100,000 complete entries, 20/200 accounts and 1,000 day groups. The actual
TransactionsPage BindingContext is measured after its first read and native Select all; every entry remains selected
after the fresh LoadAsync. Native Cancel clears selection, complete ledger JSON stays equal, and preserved development
database/WAL/SHM files return with matching hashes. No owner ledger or device/system setting is used as benchmark data.

Milliseconds, medians of three independent samples per shape/build:

| Entries | Measured stage | Before | After |
|---|---|---:|---:|
| 10,000 | Membership/pruning | 668.13 | 2.28 |
| 10,000 | Selection publication | 1.09 | 0.78 |
| 10,000 | Bound LoadAsync | 880.42 | 201.47 |
| 100,000 | Membership/pruning | 23854.31 | 5.56 |
| 100,000 | Selection publication | 7.96 | 8.80 |
| 100,000 | Bound LoadAsync | 25304.83 | 1421.56 |

The tenfold LoadAsync median falls from 25,304.83 to 1,421.56 ms, about 94.4%. Membership itself falls from
23,854.31 to 5.56 ms. The timer ends when LoadAsync returns: it includes reads and synchronous bound publication,
but not subsequent native arrangement/painting. Immediate native transaction-row count is zero under that frame;
the source binding and actual native selection actions are verified separately. Do not call this a completed-frame,
cold-start, Android timing, two-second target or historical-ANR acceptance. Database materialization and native list
publication remain separate QA-06 work.

## Final verification

Three new application cases cover complete 10,000/100,000 selection, fresh reordered snapshots, removed/unknown/new
ids, no financial writes or Undo offer, and a retained selection reviewing fresh money/tags without mutating its
snapshot. The existing pending-dialog case also checks Select all refusal. Main suite: 1,491 passed, zero failed/
skipped; App.Tests 255. The first new test fixture attempted to assign a read-only Entity.Id; only that fixture was
corrected to create independent entities, then the complete suite passed. Main outputs are cleaned afterwards.

Final strict Windows and complete canonical Android Debug/Release builds pass with zero warnings/errors; temporary
timing probes are absent. Windows en/fa/de x light/dark x 360x800/412x892/1280x820 at process-local 200% text,
plus en/fa/de normal-text/360/light: 21 contexts, 609 own-app renders and 21 complete selection-reload proofs.
Native Select all and Cancel reach their actual commands; all refreshed selected rows and complete stored ledger
remain correct. Original development database/WAL/SHM files are restored with exact hashes.

Complete Release on the owned emulator: six en/fa/de x light/dark reference contexts, plus English/System tenfold.
The first driver's Cancel assertion matched the non-clickable period caption All as if it were the bulk button;
the native hierarchy already showed Select/Add with no bulk dock, and both selections had survived the return read.
Only the typed clickable-button assertion was corrected. Its failed folder is excluded. An initial restoration
also lacked a fresh hierarchy after three attempts; a separate fresh own-app observation then succeeded, with the
process present and no owned ANR marker. These driver findings do not close historical cold/ANR acceptance.
The second driver ended before its scenarios when Settings did not yield a fresh hierarchy in three observations;
its original English/System/Advanced profile restoration succeeded. The final driver permits eight fresh bounded
observations while retaining the actual loading cover and enabled-input check; stale XML is never accepted.
In each, two distinct known rows are selected through separate filters; the first hidden selection stays selected,
the native count advances to two, a real Home -> Transactions return/reload keeps both selected, and Cancel clears
the selection. Only emulator-5570 is used; FLAG_SECURE and
system settings stay intact. No financial Save or actual 100,000-row native bulk-selection timing is claimed.
Complete original tables are compared with D-104 for all three fictitious profiles; entries remain 10,001/100,001/3.
Settings values stay unchanged. Audit-timestamp differences are recorded separately:
reference=false, tenfold=false, prior=false.
The original QA03 Native English/System/Advanced sample is restored; final full Release, native English Home/three
transaction rows, translated Back, secure foreground flag, density 420/font scale 1.0 and Home-stopped handoff pass.

Installable local-test APK: `artifacts/android/zanance-d105-release.apk`, 81,356,885 bytes, SHA-256
`3eaf603dc052516396dbfb0913162d1d314722540c7d84ff8d9aab1161bdc470`. Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 assembly
stores and app AOT, ZIP integrity, non-debuggable package and v2/v3 signatures verified. Certificate SHA-256
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is the local test certificate, not production approval.

QA-06 remains in progress for native publication, cold/ANR/device/platform measurements. Physical phone/iOS, real
OS scaling/readers, provider/owner and release acceptance are separate. CI is reported by one delayed exact-commit
check after push; no result is inferred from local builds.
