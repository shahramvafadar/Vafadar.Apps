# State-matched transaction details (D-90 / AT-96)

The D-89 native review observation is explained by retained current hierarchies: Payee and Tags already appear in
the pre-tap frame, because Advanced opens details by default. Tapping the unchanged "More details" control correctly
closes that panel. The earlier helper assumed it was collapsed; this is not proof of a missed or broken tap.
The real UX finding is that the same caption describes opening even when the actual action is hiding the fields.

The independent Windows negative review retains an expanded panel with the actual caption "More details", expected
"Hide details", and the unchanged ToggleDetails command. Its draft and complete stored rows restore without Invoke
or Save. The initial diagnostic compilation lacked the Settings extension namespace; that failure is not runtime
acceptance and original development files still match their preserved hashes.

Use mutually exclusive translated actions for the actual ShowDetails state. Expanded says "Hide details" in all six
languages; collapsed keeps the established "More details" wording. Both use the existing growing Text action and
unchanged native command, within the real form width, with scalable 15 px captions and at least 44 px targets.
Advanced/Simple defaults, imported or existing-entry visibility, receipt expansion and financial Save stay unchanged.
Hiding changes visibility only: all entered values stay in the unsaved draft.

AT-96 invokes both actual native actions, checks complete captions/spoken names, exactly one visible action,
initial experience-policy visibility, bounded target geometry and the original draft fingerprint from the actual
editor. Fictitious payee, note and tag text remain in the real bound inputs through hide/show. Restore exact original
fields, suggestions, visibility and dirty state; compare complete stored Accounts/Entries/Settings without Save.
All captures render only the owned app window; no desktop input/focus or device/system-setting change.

## Verification

- Final Windows matrix: en/fa/de, light/dark at 360x800, 412x892 and 1280x820 with process-local
  200% text, plus all three languages at 360x800/light/100% and at 360x800/light/200% in Simple mode. All eight
  cohorts pass: 384 owned-window/caption renders, 24 proof files and 48 actual native Invoke operations. These
  are observations, not new unit cases. Advanced starts expanded; Simple starts collapsed, matching the existing
  feature policy. Exactly one complete caption/name is visible; targets are 44 to 63.33 px high within their group.
  All 24 complete original-draft/stored-row restorations pass. The outer guard restores original development
  database/WAL/SHM files with matching hashes. Reviewed Persian narrow/light, English narrow/dark, German wide/light
  and other matrix captures; no duplicate action or incomplete caption was observed.
- Main suite: 1,369 passed, zero failed/skipped; App.Tests remains 140. Output cleaned. Final strict Windows build:
  zero warnings/errors. AT-96 contributes real native runtime checks, not an invented increase in unit-test count.
- The canonical Add-Strings.ps1, Run-Snapshots.ps1, Build-AndroidApk.ps1 and Test-AndroidPrivacy.ps1 scripts completed
  using the existing installed PowerShell engine. The earlier console-host startup limitation did not require any
  system/security setting change. The canonical Release build has zero warnings/errors; shipped Cloud permissions
  pass the canonical allowlist. The dated D-89 console-host failures remain historical negative evidence.
- Normal complete signed Release on owned API 36 x86_64 emulator-5570 passes German, Persian and English. Advanced
  initially exposes the translated Hide details action with a >=44 dp target. Native entry of fictitious payee,
  tag and note values survives actual hide/show and Keep editing. Discard returns Home without Save. Fresh native
  hierarchies, rather than assumed initial visibility or stale coordinates, identify the current action.
- An unlaunched independently owned Debug package only copies the exact fictitious database for comparison;
  Release is reinstalled. Full Accounts/Entries/Schedules/Settings/Budgets/BudgetCategoryLimits equal the pristine
  baseline: three entries, zero budgets, SQLite integrity pass, no financial or preference write. Final cold
  English Home, three visible Transactions, System theme/Advanced mode, English Back, secure window, density 420/
  font scale 1.0, return Home and force-stop pass. No other device was accessed and no system setting was changed.
- Installable APK: `artifacts/android/zanance-d90-release.apk`, 80,750,507 bytes, SHA-256
  `655d43cdaf4148a7074c11fc214e01b3679a7b3abd04b3d467e786af111b9274`. Package `pro.vafadar.zanance` 0.1.0/code 1, min 24/target 36; complete ARM64/x86_64 assembly stores
  and application AOT libraries, ZIP integrity and v2/v3 signature verified with the existing local debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`. This is local test signing; it establishes no production signing or Store release.

Physical OS large text, TalkBack/Narrator, ARM64 phone, iOS, provider and Store/product acceptance remain independent.
This slice does not complete A11Y-03, QA-06 or unresolved encryption/OS-backup/product decisions.
