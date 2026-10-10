# Complete visible plan validation (D-99 / AT-104)

## Observed problem and correction

Actual native Save in an unsaved German transfer plan with blank name, zero amount and missing destination
publishes only name/amount errors. Destination feedback is postponed until a later attempt, and scrolling to
the footer leaves even the existing errors out of view. The initial fixture fails with unchanged complete
Accounts/Entries/Settings/Schedules and retained typed values. Failed-baseline teardown also logs a WinUI title-bar
exception; this is negative diagnostic evidence, not a separate product crash repair claim.

The actual save continuation now uses PlanDraftValidation to collect every applicable name, source amount/account,
destination account/amount and recurrence-group problem before any entity mutation or persistence. Keep existing
MoneyText parsing, positive minor units, unknown/estimated/fixed modes, same-account rejection, cross-currency
requirements and Recurrence validation. Field captions sit beside their own inputs. The plan-specific positive
amount message is translated in all six languages and does not direct a transfer user to choose expense/income.
The visible page reveals the first affected input after publishing all problems without keyboard focus or typing.

A first queued-scroll candidate passes the initial name case but fails after correcting earlier inputs: the actual
destination picker is 134.33 px above the viewport although its own error is correct. Clearing error rows changes
the form height. Windows now resolves actual native coordinates after UpdateLayout and uses a bounded native
ChangeView, avoiding the known redundant-MAUI-scroll wait at reached/clamped offsets (D-81). Android waits for
its actual global-layout callback, removes the temporary observer in finally and bounds the wait to one second.
iOS requests its native layout; that platform remains unbuilt/unaccepted here. Requests are scoped to the visible
page lifetime and latest validation attempt; leaving or a newer attempt invalidates stale completions.

## Verification

- Twenty-four new App.Tests scenarios compile the real validation helper: independent errors together, zero/
  negative/invalid/overflowing amounts, source/destination absence, same-account transfer, hidden unknown amounts,
  cross-currency minor digits, regional parsing/digit shapes, retained draft/rule and fresh corrected results.
  Main suite 1,408 passed, zero failed/skipped (App.Tests 172); output cleaned.
- AT-104 invokes the actual native Save four times in each context, always with an independently invalid blocking
  input: first name/amount/missing destination together; corrected name/amount with missing destination; same
  source/destination; fictitious presentation-only JPY choice with zero required destination amount. No valid
  financial Save. Compare complete Accounts/Entries/Settings/Schedules before/after, retain notes/reminder values,
  clear stale errors and restore the original unsaved inputs/choices. First invalid field must be within the actual
  native viewport; initial name/amount/destination error glyphs retain scaling and fit their actual MAUI allocation.
- Final en/fa/de light/dark 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de 360x800/light at 100%:
  651 own-window renders, 21 proof files and 84 native invalid-Save invocations. Original development DB/WAL/SHM
  files restore with matching hashes. Failed baseline/queued-scroll cohorts are excluded from final counts.
- Strict Windows and canonical complete Android Debug/Release builds have zero warnings/errors. Signed Release
  on the owned API 36 x86_64 emulator checks a new transfer plan in en/fa/de and both themes: one actual invalid
  Save exposes name/amount/same-account feedback, brings the name input into view, retains the selected source,
  and actual Cancel/Discard returns to Plans. Six normal-scale native name/bounds cases; no typed name/amount,
  successful Save, OS or security changes. App language/theme return to English/System.
  An initial native driver stopped before Save because the field's caption-plus-value did not match the picker's
  plain value. It was corrected against the actual hierarchy; that failed cohort and restored settings are retained
  separately and excluded from the six accepted cases.
- Exact all-24-table/column/row readback and integrity pass for the original 10,001-entry, 100,001-entry and
  three-entry fictitious profiles. Unlaunched Debug is only for readback; complete Release is reinstalled.
  Final English Home/three Transactions, System/Advanced/native Back, secure window, density 420/font scale 1.0
  and Home/stop pass. No claim of Android glyph/200%, screen-reader, physical-phone or iOS acceptance.
- APK `artifacts/android/zanance-d99-release.apk`, 81,250,389 bytes; SHA-256 `c0a96d47a1145be475e39a4b974dd458d20eee5d1a6e16db81a8e610692bd1b1`.
  Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 stores/app AOT,
  non-debuggable manifest, ZIP integrity and v2/v3 signatures pass. Existing local test/debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` remains test signing, with no production or Store claim.

No schema, ledger calculation, reminder permission, new SDK, credential, portable preference or commercial limit.
A11Y-03 other controls/OS/readers/phone/iOS and owner/provider/encryption/licence/release gates remain open.
