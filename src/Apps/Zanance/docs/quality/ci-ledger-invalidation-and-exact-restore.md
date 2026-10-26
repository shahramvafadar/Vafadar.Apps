# D-562 - Financial writer conflict guards and exact restore evidence

## Actual failed CI and current-source reproduction

Read the failed Libraries/tests job121422229286 from CI run40277086089, head1fff06a2.
The Windows and Android jobs already succeeded and were not rerun unchanged. The original job
contains83 failing cases:31 Data and52 application-flow cases. Reproduce the original
DataReview delete/Undo failure on current source before changing it. The original case fails;
seven of eight new focused conflict cases also fail. Preserve those reports separately.

SQLite applies an outer statement's conflict policy to writes inside triggers; a foreign-key
SET NULL action also exposes this boundary. Consequently the old trigger's INSERT OR IGNORE
can still reject a duplicate HomeLedgerDirtyBlocks primary key and abort the original financial
command. See the official [SQLite trigger conflict-policy documentation](https://www.sqlite.org/lang_createtrigger.html).
This is a real product writer defect, independent of observation-only snapshot differences.

## Additive correction and narrowly bounded comparison changes

Add migration20261026104025_HomeLedgerDirtyBlockConflictGuards. Do not edit the published
HomeLedgerReadBlocks migration or rebuild financial tables. Recreate only its three entry
triggers: insert each old/new rowid bucket only when absent, in the same SQLite writer, while
preserving the revision update. Down restores the exact historical definitions. The production
reader requires the corrected frozen definitions before trusting derived blocks. Entity/model
and compiled-model metadata are unchanged. Independent migration/retirement tools accept only
the explicit three-migration suffix, never an arbitrary future schema.

Two additional failure families belong to evidence oracles. Portable VACUUM/restore can change
SQLite's local schema cookie without changing source rows. Only actual portable-round-trip
assertions normalize that numeric cookie after validating the original transport shape/digest;
all remaining tables, values, payloads and revisions stay exact. Initial no-write comparisons
remain exact. For explicitly accepted financial round trips, the workload comparator normalizes
the two validated opaque ledger revision tokens in its disposable memory copy alongside the
existing financial revision token. It still compares every financial row, schema, count, bucket
and compressed payload. Neither original source is rewritten for comparison.

## Focused managed evidence

Use actual test host10.0.14 and strict builds. All original83 failing cases now pass locally.
Do not present this as a new full-suite run or a hosted CI result. Eighteen genuinely new cases
comprise eight trigger-policy cases, seven portable-cookie cases and three workload-token cases.

| Preserved selected report | Passed / total |
| --- | --- |
| Corrected new conflict regressions | 8/8 |
| Original shared writer reproduction after correction | 1/1 |
| Existing affected trigger/reader/retirement guards | 5/5 |
| Portable-cookie new cases and original recovery cases | 11/11 |
| Workload comparison existing and new boundaries | 11/11 |
| Remaining originally failed Data method families | 25/25 |
| Originally failed application cases and affected migration boundaries | 55/55 |
| Two affected authenticated three-migration repair cases | 2/2 |

Reports overlap where their names explicitly cover the same behavior; do not sum them into a
distinct total. Preserve the original one-case failure and new seven-case failure before fix.
The selected checks leave previously successful unrelated suites and financial operations alone.

## Changed native boundary and complete signed adoption

Strict Windows and complete shipping Android builds finish with zero warnings/errors. Shipping
SHA256 is8fd525b3842e94739938b154c3865e5972697b6d22468caf724cb2d6d385c000:
384 own-AOT images,408 ELF files,1608 statically16KiB-aligned LOAD segments, two ABIs,
offline/non-debuggable/non-profileable manifest, ZIP CRC/alignment and v2/v3 verification.
Static alignment is not physical16KiB runtime acceptance.

A new distinct package pro.vafadar.zanance.ledgerconflictreview uses the actual protected
factory, original store and derived reader. Its signed SHA256 is
4f1e9b4f663f80346e20c941744a6d51b08c5a114781fda25c54f186c6c355e2.
Decoded Data/Core/Brotli PEs and Mono/Brotli/cipher engines match the new shipping package;
do not claim equality of every per-app linked BCL image. Actual Samsung API36/ARM64/4096,
UID10397/PID5519, run a561ea82d6d149d8b7ebdaf5342a4790, executes one new purchase1000 and
linked refund400, one original deletion and the actual returned file-bound Undo receipt.
Original financial fields and links survive; UpdatedAt remains monotonic. Final two-row effect
is-600 and one actual derived-block read passes. Durable request/accepted markers prevent replay.
This is native protected-store evidence, not form/UI or whole-device acceptance.

Remove only that completed new package and prove absence. One authorized no-reset shipping
upgrade succeeds; do not repeat it after the initial16-node Preparing observation refuses
acceptance. Continue normal startup and inspect the saved ready141-node packet. An initial
host baseline-key mismatch is corrected only in the observer. Final Main7028 has the new8fd5
bytes, Home979/all141 public states, original UID10380/CE155299/DE154603 and permission flags.
The initial Preparing frame did not persist a PID receipt: do not claim uninterrupted PID
survival from that frame. Original caller b1d5/UID10381/storage/stopped state, five settings,
size/density and three original profiles are preserved. No private Main key/database is read.
Three final owned XMLs are hash-cleaned; an earlier handoff XML was already cleaned separately.

Four supported standard Clean commands remove17589 recorded build files totaling1861611432
logical bytes from the owned D-562 test/Windows/shipping/native build trees. Used signed APKs,
original rollback package and local evidence remain. This is not a measured host free-space gain.

## Remaining gates

Push this completed correction and obtain the configured exact-head hosted CI result once after
the required delay. Local83-case success is not hosted success. Preserve later failures and
continue only their relevant diagnosis. General native crash/ANR, two-second Home performance,
accessibility, hardware16KiB, iOS, provider/authentication, Store and publication remain separate.
No successful historical financial cohort, codec/OS matrix or unrelated suite was replayed.

### D-562 hosted follow-through - original CI green, remaining protected workload correction

The exact original correction head785b151b passes all required CI jobs in run40316474338:
Libraries/tests, Windows and Android are successful; iOS is skipped, not qualified. CodeQL,
identity and retained service workflows also succeed. The one-shot monitor is consumed/deleted.
A separate native-cipher workflow40316473545 fails only its Windows protected workload snapshot
comparison; native reproducibility, protected persistence and official Android slices already pass.
Read only that failed job's log. A new focused compaction case reproduces the same comparison error.
The physical protected-file copy can leave a stale derived schema cookie until the first accepted
writer refreshes it. Keep every row, schema and compressed payload exact; normalize that one
cached cookie only when before/after independently captured actual protected-source cookies are
unchanged and the resulting cache cookie matches them. Missing evidence, changed source cookie,
wrong cache cookie and changed payload are refused. Five new cases plus the11 affected original
oracle cases pass16/16 on actual runtime14; strict original workload entrypoint build passes.
This adds five cases to D-562's original18; it is not a new full-suite or native financial run.
No production DLL, cipher, APK, UI, permission, financial algorithm or successful phone cohort
changes/repeats in this follow-through. Actual corrected hosted workload success remains pending.
[Required CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40316474338),
[preserved native workload failure](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40316473545).

D-562 final hosted acceptance on5e2e9384: required CI40319587370, Native cipher candidates
40319587614 and CodeQL40319587303 all complete successfully. The corrected Windows protected
workload now passes on the actual hosted job; earlier native producer/persistence and Android
checks remain successful. Both one-shot monitors are consumed/deleted. No failure is hidden by
rerunning an unchanged job. General crash/ANR, two-second performance, accessibility, broader
platform/provider/Store and release gates remain open.
[Required CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40319587370),
[native qualification](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40319587614).
