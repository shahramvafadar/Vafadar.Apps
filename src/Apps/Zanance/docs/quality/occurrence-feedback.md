# Complete occurrence actions and input feedback (D-103 / AT-108)

## Problem and resulting behavior

The real due-item page clips Record as complete, Record partial payment and Edit this occurrence at 200% in a
360 px Windows window. Invalid override amounts write their explanation into the payment section, away from the
field the user edited. Baseline en/fa/de own-window renders reproduce clipping. The baseline host also reported a
helper encoding error after rendering; all original development files restored with matching hashes. Correct the
helper's extra BOM before successful smoke/final runs; do not count its host exit as successful verification.

Use growing real actions with full native spoken names and at least 44 px targets. Give payment and optional
override their own inline amount errors. Completion and partial payment share the actual payment requirement;
change-this-occurrence has the optional override requirement. Correct each independently and reveal the actual
attempted input without moving desktop focus or replacing drafts. Initialize both DateFields before binding.
Keep the original distinction: an empty override retains the plan amount; a payment must be positive. Changes
remain occurrence metadata, partial payments stay open, and completion retains the unique settlement and original
ledger continuation. Guard the change Save while busy. No new strings, schema, permission, SDK or security changes.

## Verified local evidence

- AT-108 adds 14 application cases over the actual regional input validator: required/optional invalid input,
  omitted override, overflow, currency minor digits, Persian/Arabic digits and independent corrections.
  Main suite: 1,484 passed, zero failed/skipped; App.Tests 248. Test output cleaned.
- Complete Windows financial cohort before the final invalid-message correction: en/fa/de × light/dark at
  process-local 200%, Advanced 360x800/412x892/1280x820 and Simple 360x800.
  24 contexts, 96 invalid native Saves and 96 valid native Saves, 1192 own-window renders. All seven actions
  retain complete native scaled glyphs, full spoken names and targets. Invalid completion, partial payment and
  override attempts reveal the correct native input and preserve its exact draft plus complete stored values.
- Each context saves a positive override and an empty override through the actual action; neither posts entries.
  The new isolated occurrence state has the intended amount/date/note. Restore only the exact new fictitious state
  before the next scenario. Actual native partial payment of 25 EUR and completion of the remaining 925 EUR create
  exactly two intended plan entries and one unique settled state, preserving every original financial field/id.
  Remove only the identified scenario rows/state from the fictitious database and compare Entries, Accounts,
  Settings, Schedules and all original occurrence states exactly. Original development files restore with matching
  hashes. Smoke and baseline renders are excluded from the final counts.
- Strict Windows and canonical complete Android Debug/Release builds: zero errors/warnings.
- Complete Release on owned emulator-5570: six normal-scale en/fa/de × light/dark cases, 18 actual invalid native
  Saves, independent native input corrections and Back. No valid Android financial or metadata Save. Restore the
  original QA03 Native profile and English/System; retain screenshot protection and unchanged system settings.
  Exclude the initial native cohort: its first input was correctly revealed at the actual viewport top, 283 px,
  but the driver incorrectly required a fixed 300 px minimum. Correct the driver to use the actual native scroll
  viewport and exclude partially visible controls before acting. This driver correction requires no product source
  or APK change.
- Native inspection also identified a real payment-message problem: the former error asks users to choose income
  or expense, although this form has no such choice. Use the existing six-language Plan_AmountMustBePositive
  message. Interrupt and exclude the former native message cohort, then explicitly restore QA03/English/System
  through native UI. After this final message-only source change, rerun all 1,484 tests, strict builds and the full
  24-context Windows invalid-message path: 96 invalid Saves, zero valid Saves, 1144 own-window renders,
  complete captions/names/targets for six checked native actions per context, independent corrections and exact store
  retention. These follow-up renders prove the final wording; retain the distinct earlier cohort's 96 valid
  metadata/financial Saves as evidence of unchanged financial continuations. The six final Release native cases
  and complete signed APK use the corrected message.
  Retain the two completed final English native contexts after the regional driver failed to match an already
  visible Persian error: the raw resource contains 12.50, while the app correctly renders the example as ۱۲٫۵۰.
  Normalize only numeral glyphs and decimal/group separators when matching the actual native error TextView.
  Resume only the remaining four Persian/German contexts over the same SHA-256-verified APK; do not count the
  interrupted Persian case. No product source, financial write or repeated build is needed for this driver fix.
  After both Persian contexts pass, the uncompressed hierarchy runner fails three fresh Settings observations and
  its restoration attempt before either German case. An independent fresh compressed native observation shows the
  real German Settings controls and no owned ANR marker. Retain four completed same-APK English/Persian contexts;
  resume only the two German cases using fresh compressed hierarchies with the same bounded attempts and actual
  controls/viewport assertions. Finally restore QA03/English/System through UI. This observation does not close
  the separate historical QA-06 ANR or performance findings.
  The first German resume tries the existing English-only profile helper while the current UI is German and fails
  before a German occurrence action. Its finally block restores English/System/QA03. Begin the corrected resume
  from that restored English context, select the fictitious reference profile, then select German through UI.
  The finish runner initially selected no proof files because its directory wildcard was combined with the file
  filter. Use explicit directory enumeration; no product build or native run had started at that failed gate.
- Read back all 24 original tables/columns in reference (10,001 entries), tenfold (100,001) and prior (3) fictitious
  profiles against the verified D-102 baseline. Every complete original table is exact, including Settings and its
  UpdatedAt audit; no allowed audit difference is needed. Integrity and schema checks pass. Do not launch Debug
  during readback; reinstall complete Release and verify cold English Home,
  three native Transactions, System/Advanced, translated Back, secure window and unchanged density/font scale.
- Phone-test APK: `artifacts/android/zanance-d103-release.apk`, 81,356,885 bytes,
  SHA-256 `8bbbff98ef85ff431360991075a63f0c6264eaa336ba830423213d6978f289ac`. Package pro.vafadar.zanance, complete ARM64/x86_64 stores and app AOT, non-debuggable,
  ZIP integrity and v2/v3 signatures pass. Local test certificate `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`; this is not
  production signing or publication.

Physical phone, iOS, real OS large text/readers, valid Android occurrence acceptance, other A11Y-03 controls and
unresolved owner/provider/encryption/licence/release gates remain open. CI is separate evidence.
