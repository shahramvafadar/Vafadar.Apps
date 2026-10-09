# Hidden Home account views (D-95 / AT-100)

## Measured problem and correction

The default Home layout hides Accounts. Its BindableLayout still eagerly constructed 20 or 200 actual native account
rows in all six baseline Android observations. Every named independently owned sample had the default empty layout
string; hiding the parent therefore did not avoid row construction. Home now starts that section hidden and attaches
the complete account collection only while the selected section actually displays content. Hiding detaches the
source and removes its views; showing attaches the current complete snapshot. Visible reloads keep the same source,
native rows and fresh binding contexts (D-92). All accounts, ordering, values, calculations and existing actions remain.
No account cap, ledger filter, cache, model/migration, SDK, permission, security or portable preference change.

## Native before and after

Three process-cold Debug observations per reference (20 accounts/10,001 entries) and tenfold (200/100,001) shape on
the independently owned API 36 x86_64 emulator use the actual bound Home page. Temporary stage instrumentation checks
the complete account snapshot, section visibility and every realized row/handler, recording counts and timings only.
No main test/build overlaps either timing cohort. All twelve observations retain the exact complete financial-total
oracle. All baseline hidden sections have 20/200 realized native rows; all candidates have zero and retain 20/200
complete account values. Median account-publication time changes from 430.69 to 0.57 ms (reference) and 5,339.89 to
10.69 ms (tenfold). Tenfold stage ranges are 4,255.23-5,875.56 before and 9.50-13.16 ms after.

Body medians are 2,856.11 -> 2,228.53 ms and 17,395.46 -> 11,040.62 ms. The baseline first tenfold body is 36,119.37 ms;
OS caches, JIT/emulator/host variability and native hierarchy observation overhead prevent an exact first-paint or
controlled total cold-start claim. Entry reading still takes roughly 9-11 seconds in the candidate large sample.
Requested visible Accounts still constructs every row; this slice removes hidden work rather than claiming that
visible creation is free. Q-02, cold duration/ANR and physical-device acceptance remain open.

## Final verification

- Windows en/fa/de, light/dark, 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de 360x800/light at 100%:
  615 own-window renders. Twenty-one AT-100 proof files record 294 actual native Invoke/Toggle operations.
  Hide/show/hide, warm visible retention and fresh contexts, opening the requested account details, and the complete
  Accounts list from hidden Home pass. Every snapshot remains complete; hidden native rows are absent. Pure reloads
  compare complete Accounts/Entries/Settings/Budgets with no Save. Customization tests change only fictitious layout/
  auditing time, restore the exact original layout and compare every other preference and all financial data.
  Original development database/WAL/SHM files restore with matching hashes. An initial capture rejected newly rebuilt
  recent rows before arrangement (width/height -1); the final helper waits for real native arrangement, retaining the
  original geometry assertions. A later wide-window review tried Back before its animated native header was ready;
  the final helper waits for that actual handler before invoking it. Failed cohorts are excluded from final acceptance.
- All 1,384 main tests pass with zero failures/skips; output cleaned. No new unit count is invented for view lifetime.
  Strict Windows and canonical complete Android Debug/Release builds have zero warnings/errors. Windows-only helper
  and caller share their platform guard. All temporary stage instrumentation is removed before final builds.
- Normal signed Release runs on the emulator: the initially hidden Accounts section can be shown with every original
  account name, the actual row opens its details, native Back returns Home, and hiding works again. Only the exact
  prior fictitious profile's HomeLayout/auditing time changes during this UI check. Both fields restore from its
  original baseline after comparing all other columns of every original table. Readback then compares all 24 tables,
  columns and rows of the reference, tenfold and prior three-entry profiles; integrity passes. The Debug readback
  package is never launched, complete Release is reinstalled, and 14 validated count/timing-only metadata files
  are removed from the exact temporary probe cache. English Home/three Transactions, System/Advanced, native Back,
  secure window, unchanged density 420/font scale 1.0 and Home/stop pass. No OS/security setting is changed.
- APK: `artifacts/android/zanance-d95-release.apk`, 81,193,045 bytes; SHA-256 `942cdd99dffb24b1bd9b0f0e738839e40081ad8e69b18b958f9f96af3a523988`.
  Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 assembly stores/app AOT,
  non-debuggable manifest, ZIP integrity and v2/v3 signatures pass. Existing local debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` provides test signing, without a production/Store claim.

QA-06 remains partial. Real OS scaling, screen readers, ARM64 phone, iOS, OAuth/provider, owner, encryption/licence,
commercial and Store acceptance remain separate gates.
