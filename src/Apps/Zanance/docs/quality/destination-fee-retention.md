# Existing destination fee retention in Simple (D-101 / AT-106)

## Reproduced loss and correction

Actual native Save of a fictitious two-fee transfer in Simple changes only its note but removes the destination
expense: its loaded input is 0.75 EUR, initially hidden, and the stored row count falls from 11 to 10. The source
fee and single transfer remain. This is a successful-edit regression, separate from D-100's invalid-Save checks.

The editor now shows a stored destination fee in both modes and refreshes the net-effect summary when fee visibility changes.
The initial candidate retained the fee but showed the gross destination amount on opening; native image review exposed this
ordering problem. That interrupted cohort is excluded. Final native checks compare exact localized net effects before/after edits.
Existing validation, effect summary and save continuation consume that visible value. Simple still hides the destination-fee input on a new or reopened transfer
without a fee; Advanced continues to offer creation. Explicitly clearing or setting the displayed fee to zero
removes only that fee. Existing amount parsing, currency/context-change rules, ledger grouping and calculations
remain unchanged. There is no schema, data type, permission, SDK, credential or portable/security preference change.

## Final local evidence

- AT-106 invokes real native Save three times per context: retain the fee while changing a note; change 75 to 125
  minor units with the same expense id; explicitly remove it with blank input (en/de) or zero (fa). Compare all
  serialized financial fields/ids and unrelated entries, plus Accounts/Settings. Check exact localized net effects
  before/after each edit; allow only the chosen note/fee and normal audit timestamp advances. The transfer stays one transfer and the original source fee stays 150 minor units.
- Reopen the fee-free transfer, then open a new transfer with a destination. Simple keeps creation hidden, Advanced
  shows it, removed text does not return, and native Cancel/Discard writes no financial rows. An earlier smoke driver
  waited for Cancel without handling its ordinary discard prompt; that incomplete cohort is excluded. The final
  driver invokes the actual confirmation and bounds completion. Original development files restore with matching hashes.
- Simple: en/fa/de, light/dark, 360x800/412x892/1280x820 at process-local 200%. Advanced regression: en/fa/de,
  light/dark, 360x800 at 100%. The final matrix has 24 proofs, 72 valid native Saves and 792 own-window renders.
  Check actual input/value, minimum native target, viewport reachability and full realized caption glyphs/native scaling.
  Baseline and smoke cohorts are not included in matrix counts. This is not OS text-scale or screen-reader acceptance.
- Main suite 1,456 passed, failed/skipped zero (App.Tests 220); test output cleaned. Strict Windows and complete
  canonical Android Debug/Release builds have zero warnings/errors. The regression is the actual native editor/store
  route, rather than a unit test that repeats the visibility predicate.
- Complete signed Release on owned emulator-5570: 12 en/fa/de × light/dark × Simple/Advanced new-transfer cases.
  Native destination-fee input is absent/present according to creation policy after selecting another account; source
  fee remains available, normal-scale native bounds are valid and actual Cancel/Discard returns Home. The earlier
  native driver waited for a hidden Simple note and an offscreen selected Mode chip;
  that failed cohort is excluded. The final driver follows the visible details disclosure and rediscovers actual
  selected chips after inserted rows. No typed amount or successful Android financial Save. English/System/Advanced
  are restored through UI. Existing-fee successful edits above are Windows evidence; do not label this Android device acceptance of stored-fee retention.
- All financial rows and preference values remain exact across 24 original tables in all three fictitious Android
  profiles. All rows of all 24 tables are exact in the 10,001-entry and 100,001-entry profiles. In the active three-entry
  profile, 23 tables are exact; only Settings.UpdatedAt advances normally after actual mode-choice saves. All Settings
  columns/values except that single audit timestamp remain exact. The initial strict whole-row assertion detected this
  legitimate audit change and is retained separately; it is not a financial-loss fix or an exact 24-table byte claim.
  Schema/columns and integrity pass. Unlaunched Debug is used only for readback, then complete Release is reinstalled.
  Final English Home/three Transactions, System/Advanced/native Back, secure window, density 420/font scale 1.0 and stop pass.
- APK `artifacts/android/zanance-d101-release.apk`, 80,869,291 bytes; SHA-256 `f00891d4b0f1f892485a3f63a5e10141ff553b60e8944ec06a56bc5701459997`.
  pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 assembly stores/app AOT,
  non-debuggable, ZIP integrity and v2/v3 signatures pass. Local test certificate `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`;
  this is phone-test signing, not production signing or Store release.

Physical-phone, iOS, OS large text/readers, other A11Y-03 controls and owner/provider/encryption/licence/release
gates remain open. This closes the specific runtime concern recorded after D-100.
