# Complete debt entry action (D-97 / AT-102)

## Observed problem and correction

The actual German Accounts page at 360x800/dark with process-local 200% native text clips the existing
"Schuld oder Forderung erfassen" button caption. Native realized glyphs extend to 332.88 px within a 313.33 px
target. The tightened native-caption check rejects the unchanged baseline; keep this negative evidence separate.

Replace that existing SecondaryButton with the existing WrappingAction in Secondary appearance. Keep the same
complete Debt_Add translation, AddDebtCommand, native text scaling, semantic colors and real native button.
The caption wraps and its target grows; the German candidate target is 94 px tall. No new wording, product
direction, account type/default logic, debt estimate, repayment or ledger calculation is introduced.

## Verification

- AT-102 measures realized native caret/glyph bounds against the actual native button and enclosing MAUI label,
  complete spoken name, scaling, enabled command and minimum 44 px target. Invoke the actual button through
  UI Automation, verify the existing new unsaved Loan draft, then invoke Cancel and return to the same Accounts
  page. Compare full stored Accounts/Entries/Settings/Budgets/Schedules before and after; no Save or principal posting.
- The first full matrix stopped on a Persian modal date input with MAUI size -1/native size zero. This was an
  unarranged input, not evidence of digit clipping. A bounded wait now requires positive actual MAUI/native date
  input dimensions before capture, then scrolls the reference date through the native Scroll pattern. Existing
  digit-width/target assertions remain unchanged. The production DateField is unchanged. Failed diagnostic cohorts
  are excluded from final acceptance; original development database/WAL/SHM hashes were restored.
- Final en/fa/de light/dark 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de 360x800/light at 100%:
  573 own-window renders, 21 AT-102 proof files and 42 native open/cancel invocations. Both initial modal view
  and scrolled reference-date view pass their existing layout checks. Development data files restore identically.
- All 1,384 main tests pass, zero failed/skipped; output cleaned. Strict Windows and canonical full Android
  Debug/Release builds have zero warnings/errors. The native fixture remains Windows-only; no invented unit count.
- Normal signed Release on the owned API 36 x86_64 emulator opens Accounts from Home, invokes the complete
  debt action, reads the actual new debt draft, cancels and uses native Back to Home without input or Save.
  Exact readback of all 24 tables/columns/rows of the existing 10,001-entry, 100,001-entry and three-entry fictitious
  profiles and integrity pass. Unlaunched Debug is used only for readback; complete Release is reinstalled.
  Final English Home/three Transactions, System/Advanced/native Back, secure window, density 420/font scale 1.0
  and Home/stop pass. No device/system/security setting changes.
- APK: `artifacts/android/zanance-d97-release.apk`, 81,197,141 bytes; SHA-256 `4505452aa6d3cb6ec9c9ac9c48962ff810f23b11448ae0fefd5bcec55b5563b4`.
  Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 stores/app AOT,
  non-debuggable manifest, ZIP integrity and v2/v3 signatures pass. Existing local test/debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is test signing, with no production/Store claim.

A11Y-03 remains partial: other controls/modals, real OS scaling, screen readers, ARM64 phone and iOS acceptance
remain open. Owner, provider, encryption/licence, commercial and release gates remain separate.
The actual Windows modal captures also show a duplicated title: the form's own Cancel/title row plus the generic
child PageHeader. That separately observed layout defect is the next ready slice; it is not changed by D-97.
