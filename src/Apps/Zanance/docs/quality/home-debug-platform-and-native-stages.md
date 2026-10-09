# Home Debug platform boundary and Android stage evidence (D-93 / AT-98 follow-up)

## Corrected build defect

D-92 called ReviewHomeSnapshotAsync outside a platform guard, although that method is inside the Windows-only
diagnostic block. Android Debug failed with CS0103. Android Release excluded the entire Debug diagnostic and was
unaffected. The call now uses the same WINDOWS boundary as its implementation. Actual Home loading, stored data,
financial calculations, layout and the existing Windows diagnostic are unchanged; no failure is swallowed.

The first canonical temporary Debug build reported failure without retaining the underlying error text. A direct
strict publish exposed CS0103. After the guard correction both the temporary measurement package and final Debug
package build successfully. Final source contains only the platform guard; all stage instrumentation is removed.

## Evidence that selects the next performance work

- An independent SQLite copy of each known fictitious profile compared five full ordered SELECT samples with and
  without a (Date, CreatedAt) index. Reference median 29.99 -> 25.47 ms; tenfold 516.94 -> 527.87 ms. Complete rows
  and order match. The temporary sort disappears from the query plan, but the tenfold read does not improve.
  No production index, migration or model change is adopted from this experiment.
- A complete temporary Debug Android package records only stage names/durations for the two exact independently
  owned reference/tenfold profiles. Three process-cold actual bound Home observations per shape preserve ledger
  totals. Median reference Home body 4,140.28 ms: entries read/materialization 2,125.60, initial account publication
  722.09. Median tenfold body 16,822.46 ms: entries 9,792.50 (range 9,367.61-11,953.63), first creation of all 200
  account rows 5,168.23 (4,346.81-6,746.29), financial sections 1,021.24 and entry indexing 48.16 ms.
  These are Debug operation boundaries, not Release first-paint timings. Main tests and Windows review overlap
  some samples; host contention and Debug/JIT effects prevent a controlled before/after speed claim. Metadata
  identifies the expensive paths, not attainment of Q-02. Some hierarchy observations time out while loading;
  later exact total/loaded-Home observations pass without restarting the measurement operation.
- The exact original sample is restored through native UI; the existing complete D-92 Release is restored after
  profiling. All temporary source instrumentation is deleted before final builds. Only the probe's exact owned
  cache JSON files/directories are removed after validating their metadata-only shape, with no financial writes.

## Final verification and installable package

- All 1,384 main tests pass (App.Tests 148), zero failures/skips; output cleaned. No new unit cases are invented:
  the regression is verified by compiling the previously failing Android Debug target and retained AT-98 runtime
  checks. Final strict Windows and canonical complete Android Debug/Release builds have zero warnings/errors.
- Final Windows en/fa/de, light, 412x892, native 100%: 57 owned-window renders, three actual Home row/context/one-Reset/
  complete stored-row proofs. Original development database/WAL/SHM files restore with matching hashes. No visible
  layout changes; D-92's full theme/width/200% matrix remains separate prior evidence, not a repeated claim here.
- Final normal signed Release installs/starts on API 36 x86_64 emulator-5570. All 24 original tables/columns/rows of
  the 10,001-entry, 100,001-entry and prior three-entry samples compare exactly, with SQLite integrity. The unlaunched
  temporary Debug package reads only those owned samples; Release is reinstalled. Final English Home/three native
  Transactions, System/Advanced, translated Back, secure window, unchanged density 420/font scale 1.0 and Home/stop
  pass. Cloud permission boundary passes. No phone, system/security setting, secret or unrelated owner file is changed.
- APK: `artifacts/android/zanance-d93-release.apk`, 81,193,045 bytes, SHA-256
  `ff1a9679979eff2aa11a1d7c3d9cfb720be710c33e71b1f1e95d481e62c260ec`; package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 stores/app AOT,
  non-debuggable, ZIP integrity and v2/v3 signatures pass. Existing local debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is test signing, not production/Store signing.

QA-06 remains partial. Next measure entry materialization and initial native account-row creation without dropping
stored metadata, capping accounts, hiding ledger rows or duplicating financial formulas. Release/device duration,
ANR, Q-02, ARM64 phone, iOS, real-provider, screen-reader and owner/Store acceptance remain independent gates.
