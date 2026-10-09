# Home snapshot publication and goal evaluation (D-92 / AT-98)

## Measured problem and change

The actual bound Home view rebuilt all native account rows on every reload. Its empty-goal path also calculated every
account balance by rescanning the entire ledger. D-92 constructs the complete account presentation snapshot first and
publishes one Reset through SnapshotCollection, retaining the collection and native template rows. Each row receives
a fresh binding context, including when record values compare equal: computed theme colors must refresh. Enumeration
failure retains the original snapshot without notifications. Counts, order, money packets and account actions remain.

GoalProgress skips ledger work when no Active/Paused goal remains. Otherwise AccountEntryIndex routes the original
ledger once to the unchanged balance calculator. Priority funding, account/currency/holding boundaries, refund and
transfer rules, future dates and checked overflow remain. No account or ledger row is hidden or capped; no cache,
schema, SDK, permission, security, portable preference or provider policy is introduced. GoalPresenter.Balances and
the Accounts page remain separate work; this slice does not claim their scans were optimized.

## Local evidence

- Three controlled actual-bound Windows Debug processes per shape, initial load, warm-up and measured reload:
  reference (20 accounts/10,001 entries) median 257.56 -> 170.19 ms; tenfold (200 accounts/100,001 entries)
  median 2,727.70 -> 1,228.28 ms, ranges 2,717.87-2,780.37 -> 1,223.07-1,236.32 ms. Tenfold account publication
  median 751.51 -> 17.51 ms and goal/attention/holdings stage 707.18 -> 4.91 ms. Before: Reset plus 200 Adds and
  replaced native rows; after: one Reset and every existing row retained with the exact fresh values. Temporary stage
  instrumentation is removed. The original development database/WAL/SHM were restored with matching hashes.
- The first temporary diagnostic build had a local-name conflict and was corrected. An initial timing run resolved
  a different transient VM instead of the actual page BindingContext and ended with a host error; its timings/row
  claims are excluded. Only the corrected actual-bound before/complete-candidate cohorts above are authoritative.
- Final canonical Windows en/fa/de, light/dark, 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de
  360x800/light at 100%: 994 own-window renders and 21 Home proof files. Actual row instances, fresh contexts,
  exact values, one Reset and complete Accounts/Entries/Settings/Budgets comparisons pass. The data guard restores
  original development files with matching hashes. Debug checks use the current page, never a newly resolved VM.
- All 1,384 main tests pass without failures/skips (App.Tests 148), including eight collection and seven goal cases.
  Test output cleaned. Strict Windows and canonical Android Release builds have zero warnings/errors. Cloud APK
  privacy checks pass; no temporary timing route or independent probe/performance project enters Release.
- Normal signed Release is installed on the independently owned API 36 x86_64 emulator. Three process-cold Home
  observations per existing reference/tenfold profile preserve ledger-oracle totals; timings include accessibility
  observation overhead and warmed OS caches. Raw baseline/candidate measurements remain local engineering evidence,
  not exact first-paint benchmarks:
  reference loaded-Home observation median 12,222.61 -> 9,991.44 ms.
  tenfold loaded-Home observation median 15,231.02 -> 15,201.33 ms.
  The tenfold cold observation is essentially unchanged; this is not evidence that cold startup meets Q-02.
  All original tables/columns/rows of both large samples and the prior three-entry
  sample compare exactly; SQLite integrity passes. The temporary Debug package used to read only those independently
  owned databases is never launched; complete Release is reinstalled. Final English Home/three Transactions,
  System/Advanced, secure window, density 420/font scale 1.0 and Home/stop pass.
- Installable APK: `artifacts/android/zanance-d92-release.apk`, 80,750,507 bytes; SHA-256
  `3f84f0ec49088e5bb5360e28980d3371f41308fbd974d1e77cdea7caf359c8a7`. Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 stores/app AOT,
  non-debuggable manifest, ZIP integrity and v2/v3 signatures pass. Existing local debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is test signing, without a production/Store claim.

Large entry materialization, first creation of 200 native rows, cold-start duration/ANR and the physical-device Q-02
objective remain open. QA-06 is partial. ARM64 phone, iOS, screen-reader, real provider, owner and Store acceptance
remain independent gates; this optimization does not resolve those decisions or encrypt the database.
The current Customize Home page still truncates some existing section captions at 200% (observed narrow en/fa/de
renders); it is outside this performance change and remains an A11Y-03 follow-up, not a claimed layout pass.
