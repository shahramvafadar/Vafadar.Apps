# Visible settlement feedback and complete action (D-102 / AT-107)

## Reproduced behavior and correction

The real advance-settlement modal has an inverted period and an invalid bill, but reports neither field problem.
Its actual native Record the difference button is disabled. At process-local 200% in a 360 px Windows window its
caption also clips. Baseline own-window renders and native values reproduce these failures; no financial Save runs.
The baseline host also reported an encoding error. Its actual rendered/input evidence and restoration of original
development files are retained; successful final verification uses the corrected helper encoding.

Collect period and amount problems independently. Explain reversed dates next to the period, and a missing,
negative, invalid or overflowing bill next to its input. Initial empty input has no amount error until Save;
subsequent corrections clear stale problems without replacing the typed values. An actual period containing no
recorded advances has its own explanation. Do not present a reversed period as a valid zero-payment period.
Initialize both DateFields to valid local dates before handlers bind.

Keep the real action available while idle so an invalid attempt can explain all affected fields and reveal the
first input without moving desktop focus. Its growing caption preserves native scaling, complete spoken name and
44 px target. Validate before the unchanged financial continuation; zero is a valid bill, a matching bill creates
nothing, and refunds remain linked to the original advances rather than income. No schema, SDK, permission,
credential, security preference, portable data or financial calculation changes.

## Final evidence

- AT-107 adds 14 application cases for independent period/amount problems, inclusive same-day dates, regional
  formats and Persian/Arabic digit input, currency minor digits, immutable drafts and zero/partial/exact bills
  after an existing refund. The real parser and advance/refund engine are used. Main suite 1,470 passed,
  failed/skipped zero; App.Tests 234. Main test output cleaned. The first test build rejected the new fictitious
  Schedule fixture's missing required Name; correct that fixture and rerun the suite without repeating the already
  successful native product matrix. The failed compile log is retained separately.
- Windows en/fa/de, light/dark, Advanced 360x800/412x892/1280x820 and Simple 360x800, all at process-local 200%:
  24 contexts, 144 invalid/no-change native Saves, 72 valid native Saves, 1168 own-window renders. Check the
  first affected native field, complete realized error/action glyphs, native scaling, actual bill input and full
  native action name/target. Smoke and baseline cohorts are excluded from final matrix counts.
- Each context records an extra 50 EUR, a 50 EUR refund, and a zero bill refunding the remaining 950 EUR against
  the fictitious original 950 EUR advance. Read every original financial field/id plus Accounts, Settings and
  Schedules after each Save; only the single intended new difference/refund is created. Remove only identified
  scenario rows from the fictitious fixture before the next scenario, and verify the exact pre-save state.
  Invalid/no-change attempts write nothing. Original development files restore with matching hashes.
- Strict Windows and complete canonical Android Debug/Release builds finish with zero errors/warnings.
- Complete Release on owned emulator-5570: six en/fa/de × light/dark contexts and 18 actual invalid native Saves
  on QA06 Reference's existing plan with no advances. Type an inverted year and negative bill through native
  inputs; correct each independently, observe full translated field feedback and first-field visibility, clear
  the draft and Cancel. Restore English/System and the original QA03 Native profile through UI. This is normal
  native scale, without screenshot, OS/security setting change or valid Android financial Save. The initial native
  driver used year 3000, outside the DateField's 1900–2199 Gregorian bounds: the control correctly restored the old
  date, so that cohort could not demonstrate a reversed period and is excluded. The final driver uses the original
  displayed year plus two, inside the corresponding calendar range. A separate observation encountered a transient
  unavailable startup hierarchy; final bounded readiness checks continue past such observations without a product
  startup fix claim. The subsequent Persian correction attempt retained -1 in the actual input instead of typing
  the replacement, and is excluded. Correct the driver to clear on both cursor sides and assert the actual native
  amount before Save. Retain the two completed English cases on the same final APK; resume only the four remaining
  Persian/German cases. This is input-driver evidence, not a product fix or a false corrected-amount assertion.
- Read back all 24 original tables/columns in the reference 10,001-entry, tenfold 100,001-entry and prior three-entry
  fictitious profiles. All 24 tables in each profile, including complete Settings rows and audit timestamps, are exact.
  The comparison permits only a growing Settings.UpdatedAt timestamp for saved display choices, but no such exception
  is needed in this final readback. Schema and integrity pass. Debug is never launched during readback; reinstall the
  full Release afterward.
  Final English Home/three Transactions, System/Advanced, native Back, secure window and unchanged density 420/
  font scale 1.0 pass; return Home and stop.
- APK `artifacts/android/zanance-d102-release.apk`, 81,336,405 bytes; SHA-256 `44be9169d89941902260c5d5908878b1e04f04e515525270b534d360bbc24354`.
  pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, complete ARM64/x86_64 assembly stores and app AOT,
  non-debuggable, ZIP integrity and v2/v3 signatures pass. Local test certificate `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`;
  this is installable phone-test signing, not production signing or publication.

Physical-phone, iOS, real OS large text/readers, valid Android settlement acceptance, other A11Y-03 controls and
owner/provider/encryption/licence/release gates remain open. CI is recorded separately and never implies those gates.
