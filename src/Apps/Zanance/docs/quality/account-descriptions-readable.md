# Complete account descriptions (D-96 / AT-101)

## Observed problem and correction

The shared AccountRow shows the account type and existing default, excluded-from-totals and incomplete-opening
descriptions above its balance. In the actual German 360x800 dark Home view at process-local 200% native text,
the excluded caption wraps to two lines after FlexLayout shrinks it. The MAUI label remains 47 px tall while its
realized native glyphs require roughly 63 px. The second line is clipped. IsTextTrimmed is false and the native
TextBlock's own allocation is taller than the enclosing MAUI label, so checking either alone misses the defect.

Each description now bounds its maximum width to the actual description group before measuring wrapped height,
disables flex shrink and aligns at the start of its flex line. Compact descriptions still share a line when they fit;
long ones wrap within their own growing allocation. Keep the same complete text, native scaling, 13 px typography,
semantic colors, visibility flags and ordering. The amount remains below the whole identity group. The complete real
row button, spoken name/hint and account details command are unchanged. No financial/default-account logic changes.

## Verification

- The initial slot-only diagnostic incorrectly passed. A tightened check compares realized caret/glyph rectangles
  against both native allocation and the actual MAUI label width/height. That check rejects the unchanged baseline
  with the 47/63 px mismatch above. Keep this negative evidence; it is excluded from final acceptance.
- AT-101 exercises all eight combinations of the three existing flags on one fictitious AccountItem in the actual
  bound Home collection. Every case retains the complete collection, native row instances, original account identity
  and amount. Each complete type/status caption is counted and measured. Restore the exact original presentation
  snapshot and compare full stored Accounts/Entries/Settings/Budgets; no Save or financial calculation occurs.
- Final Windows en/fa/de, light/dark, 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de 360x800/light at
  100%: 1083 own-window renders, 21 proof files and 168 flag cases. Actual Accounts page captures verify the
  same shared row with its genuine default-account caption. Native glyph/label bounds pass, as do existing native
  Home visibility/detail/list/Back checks. Original development database/WAL/SHM files restore with matching hashes.
- All 1,384 main tests pass with zero failures/skips; test output cleaned. Strict Windows and canonical complete
  Android Debug/Release builds have zero warnings/errors. Windows-only caller/helper retain the same platform guard.
  No new unit count is invented for this native view geometry fixture.
- Normal signed Release on the independently owned API 36 x86_64 emulator opens the complete existing two-account
  list from Home, reads actual type/default/excluded captions, opens the requested asset account and uses native Back
  through Accounts to Home. It never changes stored preferences or financial values. Subsequent exact readback
  compares all 24 tables, columns and rows of the reference 10,001-entry, tenfold 100,001-entry and prior three-entry
  fictitious profiles; integrity passes. Debug readback is never launched, complete Release is reinstalled, and final
  English Home/three Transactions, System/Advanced, native Back, secure window, density 420/font scale 1.0 and
  Home/stop pass. No device/OS/security setting is changed.
- APK: `artifacts/android/zanance-d96-release.apk`, 80,754,603 bytes; SHA-256 `01dd7d4a8ead129104b8039c608384423eba43af79c38c62cde297b0e8313928`.
  Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, full ARM64/x86_64 assembly stores/app AOT,
  non-debuggable manifest, ZIP integrity and v2/v3 signatures pass. Existing local test/debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is test signing, without a production/Store claim.

ZCR-A11Y-03 remains partial: other controls/modals, actual OS text scaling, screen readers, ARM64 phone and iOS
acceptance remain open. Provider, owner, encryption/licence, commercial and release gates are separate.
The actual narrow Accounts capture separately shows its Add debt/receivable native button caption clipped at 200%.
That existing action is the next ready layout finding; this row-description slice does not change it.
