# Complete first-run restore actions (D-91 / AT-97)

The actual German first-account screen at 360 px/process-local 200% clips "Ich habe bereits eine Sicherung" in its
native one-line button. The before app-window render retains this negative finding. The reviewed transaction editor
Save/Cancel/title already fit; this slice changes neither those controls nor the wizard footer.

Both existing onboarding restore alternatives now use the existing growing Secondary action. Keep their full
six-language captions, blue-independent secondary semantics, native scaling, real command target/spoken name and
48 px minimum. The existing RestoreBackup command and IsNotBusy binding are unchanged. No new text, VM, schema,
permission, SDK, backup policy, preference or account-creation behavior is introduced.

AT-97 measures both real actions on steps 1 and 3, invokes native Restore and the restore page's actual native Back,
and checks the same onboarding page/step, the actual AccountFormModel snapshot plus all selected regional/theme/mode/
currency/calendar draft choices and complete stored Accounts/Entries/Settings. Opening and returning restore must
not create an account, replace data or discard inputs. The focused `onboarding-actions` Debug route returns before
the general snapshot account/financial fixtures are created. All diagnostic code is absent from Release.

## Verified local evidence

- Windows en/fa/de, light/dark, 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de 360x800/light at 100%:
  seven cohorts, 273 owned-window renders, 42 proof files and 84 actual native Restore/Back invocations. Targets
  range from 48 to 94 px high; full captions/spoken names and exact original draft/stored-row comparisons pass.
  The outer guard restores the original development database/WAL/SHM with matching hashes. The first helper's
  duplicate script BOM caused a host error despite captured checks; that attempt is not final acceptance. Its
  header was corrected and the final guarded matrix completes with exit 0. No system/security setting changed.
- All 1,369 main tests pass with zero failures/skips; App.Tests remains 140. Test output cleaned. The final strict
  Windows build and canonical Build-AndroidApk.ps1 Release build have zero warnings/errors. No invented new unit
  count: AT-97 adds actual runtime navigation/geometry checks. Canonical Test-AndroidPrivacy.ps1 Cloud checks pass.
- Normal complete signed Release on owned API 36 x86_64 emulator-5570 checks both restore alternatives in German,
  Persian and English. All six full native captions and >=44 dp targets pass; actual Restore/Back returns to the
  same step without account creation. A typed fictitious account name survives the third-step round trip in each
  language. The initial new-profile hierarchy read timed out; the same already-created profile was reobserved on
  first-run onboarding without repeating creation. Retained failed observations are not a claim of a repaired app.
- Native review creates only disposable QA97 Native through the app. After the checked restore paths it explicitly
  finishes that fictitious account solely to leave its wizard, switches back to the existing QA03 Native profile,
  then removes QA97 through the two-confirmation UI. The four original profile names remain. An unlaunched owned
  Debug package copies only the exact independently owned QA03 database; Release is reinstalled afterwards.
  Full Accounts/Entries/Schedules/Settings/Budgets/BudgetCategoryLimits equal the pristine baseline: three entries,
  zero budgets, SQLite integrity pass. No original financial/preference change. Final cold English Home, three
  Transactions, System/Advanced, English Back, secure window, density 420/font scale 1.0 and Home/stop pass.
- Installable APK: `artifacts/android/zanance-d91-release.apk`, 81,188,949 bytes, SHA-256
  `1044583317b87099dc0d741def0da15902805513faa248e4a234af70114c864d`. Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36; complete ARM64/x86_64 assembly stores
  and app AOT libraries, ZIP integrity, non-debuggable manifest and v2/v3 signatures pass with existing local debug
  certificate `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`. This is local test signing, without a production/Store claim.

Physical OS text scaling, TalkBack/Narrator, ARM64 phone, iOS, real provider restoration and Store/product acceptance
remain independent gates. A11Y-03 and QA-06 remain partial; unresolved encryption/OS-backup/product choices stay open.
