# Complete visible transaction-field validation (D-100 / AT-105)

## Observed problem and correction

Actual native Save in a fictitious German transfer with zero amount, missing destination and malformed source fee
shows only the amount problem. Destination/fee feedback is postponed to later attempts or placed in the general
footer. The old positive-amount wording also tells a transfer user to choose expense/income. The initial negative
fixture retains the complete draft and Accounts/Entries/Settings. Its focused amount already appears in the actual
viewport; do not claim that this initial field was offscreen. Failed diagnostic teardown logs a WinUI title-bar
exception, which is retained as negative evidence rather than a separate product-crash correction.

EntryDraftValidation now collects all independent applicable monetary problems before entity or fee mutation:
source/destination account, main/cross-currency amount, source/destination fee, original ISO currency/amount and
reimbursable part. Each caption belongs beside its field. Original-currency restrictions already enforced by the
ledger now appear before mutation. Blank reimbursable input still means the whole valid expense; optional blank/
zero fees remain zero. Existing MoneyText regional/display-unit parsing and MoneyAmount ISO-unit parsing remain.
Their sign rejection is unchanged. A preliminary negative-input test expected a positivity message incorrectly;
reading the real parser corrected the fixture to its existing invalid-number result. No new negative-fee repair
is claimed. The new positive-amount caption has actual translations in all six languages.

Collapsed invalid original-currency/reimbursement details reopen without clearing entered values. Invalid Save
reveals the first affected input, scoped to the visible page and latest attempt, without focus or text replacement.
Actual retries exposed a Windows-specific ordering failure: direct ChangeView reaches the focused amount instead
of the destination after layout/Save enablement changes. Native error values and target geometry are correct.
Queued layout and temporary focus-scroll/caret guards did not solve it; these failed cohorts are excluded.
The final Windows request uses the native target's StartBringIntoView after the earlier native updates, through a
bounded low-priority dispatch. It changes no persistent keyboard/scroll policy. Android waits for actual global
layout, bounds the wait to one second and removes its observer in finally. iOS requests native layout and remains
unbuilt/unaccepted here. Required valued-asset consent, receipt-unit protection, overlap review, explicit persistence,
fee synchronization and attachment/navigation continuations retain their existing ordering and successful behavior.

## Verification

- Forty-eight new AT-105 cases compile the actual validation helper, covering independent errors, missing account/
  destination, same-account transfer, malformed/zero/negative/overflow input, EUR/JPY minor digits, optional fees,
  original-currency restrictions, regional/digit parsing, blank/invalid reimbursements, hidden inapplicable values,
  immutable drafts and corrected results. A display-unit case proves original purchase amounts remain ISO units.
  Real EntryActions/LedgerValidator preserve one transfer plus separate fee expenses and their group identities.
  Main suite 1,456 passed, failed/skipped zero (App.Tests 220); test output cleaned.
- AT-105 invokes actual native Save eight times per context, always with independently invalid blocking input:
  simultaneous main/destination/fee errors; corrected main with missing destination; same account; presentation-only
  JPY destination with zero amount and malformed destination fee; corrected destination amount; collapsed invalid
  reimbursement/original fields; corrected reimbursement/currency with zero original amount; corrected original
  amount with identical source/original currency. No valid financial Save. Check every error's full realized glyphs
  against actual MAUI/native allocation and native scaling, automatic current-input visibility and stale-caption
  clearing. Restore all original presentation choices and compare complete Accounts/Entries/Settings before/after.
- Final en/fa/de, light/dark, 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de 360x800/light at 100%:
  1281 own-window renders, 21 proofs and 168 invalid native Save invocations. Original development DB/WAL/SHM
  restore with matching hashes. Baseline and failed candidate cohorts are excluded from these counts. Temporary
  native offset/phase instrumentation and reflection are removed before the final matrix/builds.
- Strict Windows and canonical complete Android Debug/Release builds have zero warnings/errors. Complete signed
  Release on the owned API 36 x86_64 emulator checks actual Home/new-transfer in en/fa/de and both themes. Blank
  amount and same-account destination produce both field problems in one Save; the first amount input is visible,
  source selection remains, and actual Cancel/Discard returns Home. Six normal-scale native name/bounds cases,
  no typed financial amount or successful Save. Only a currently visible keyboard is dismissed with owned-device
  Back; app language/theme return to English/System without OS/security changes.
  The first final cold hierarchy is read during the protected startup frame and contains no app text. The six
  form cases already passed; do not call this a product startup repair. Final review reads actual ready Home with
  bounded fresh hierarchy checks and verifies English/System/Advanced, native Back and the secure window before
  stopping. The initial driver failure remains separate negative evidence and is excluded from the six-case counts.
- All 24 original tables, columns and rows plus integrity match for the 10,001-entry, 100,001-entry and three-entry
  fictitious profiles. Unlaunched Debug is only for readback; complete Release is reinstalled. Final English Home/
  three Transactions, System/Advanced/native Back, secure window, density 420/font scale 1.0 and Home/stop pass.
  No Android glyph/200%, screen-reader, physical-phone or iOS acceptance claim.
- APK `artifacts/android/zanance-d100-release.apk`, 81,311,829 bytes; SHA-256 `bdca47520d8dd774c08a356233a88691e1d2507e83d5e184a0f60ca2110e356c`.
  Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 stores/app AOT, non-debuggable,
  ZIP integrity and v2/v3 signatures pass. Existing local test/debug certificate `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is
  test signing, with no production signing or Store claim.

- Final review routes deferred Windows callback failures back into the existing awaited GuardAsync and expires
  a queued request at its deadline, preventing a stale later scroll. The eight German 360/dark/200% invalid native
  Saves pass again, with 61 separate own-window renders and original development-file hashes restored. This final
  callback-only review is separate from the 21-context counts above; no simulated native-failure repair is claimed.
  Strict Windows and complete Android Debug/Release are rebuilt with zero warnings/errors. The final APK passes
  one additional English/light invalid transfer Save/error/cancel and English/System restoration. All original
  three-profile 24-table rows/integrity match again; final complete Release Home/Settings/Back/secure/stop pass.
  The six Android language/theme cases above used the pre-guard native candidate (SHA-256
  `7010f0c98eed369ecf57cd9958a9af11dfa030e606d8c67062c2483cdb09422d`); the final callback change is Windows-only.

No schema, ledger calculation, new data, permission, SDK, credential, portable preference or commercial limit.
Separate source concern: Simple hides an existing positive destination fee, while its current continuation applies
only visible destination fees. This slice preserves that policy; native valid-edit reproduction and a dedicated
retention correction remain pending. Do not infer that this separate possible loss is fixed from invalid-Save tests.
Other A11Y-03 controls/OS/readers/phone/iOS and owner/provider/encryption/licence/release gates remain open.
