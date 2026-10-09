# Large-text review (D-77 through D-83 / ZCR-A11Y-03)

Engineering review on 2026-10-09. A11Y-03 is in progress. This first slice improves Home quick actions and plan
rows; it does not establish whole-app, operating-system, screen-reader or physical-device accessibility acceptance.

## Changes and preserved behavior

Home wraps complete quick actions according to measured label width instead of forcing four equal columns.
Existing commands, accessible descriptions and reading direction remain. Plan identity and amount/status use
separate rows, so a large amount no longer consumes the title column. Titles wrap; date tiles and template pills
grow with text; section links have a minimum 44 px height. Money/date formatting and calculations remain.

Readable text keeps native font scaling. Only decorative symbols opt out. The current icon component measures its
formatted span's unscaled square; scaling that span clips the glyph. The attached property uses the component's
public child layout, disabling scaling for both its label and spans. The named floating Add action retains its
semantic description and touch target while its plus glyph keeps its explicit size.

No new financial formula, model, migration, portable preference, security behavior, permission, SDK or translation.

## Windows rendered evidence

The explicit Debug snapshot font manager applies process-local stress for fictitious data, compensating the
read-only system text factor exactly once. It honors text-scaling opt-outs. It never changes an OS setting and is
absent from Release. Without an explicit `-FontScale`, snapshots keep the normal system font path.

| Requested scale | Window | Theme | Languages | Final app-window PNGs |
|---|---|---|---|---:|
| 200% | 360x800 | Dark | en, fa, de | 33 |
| 200% | 360x800 | Light | en, fa, de | 33 |
| 200% | 412x892 | Light | en, fa, de | 31 |
| 200% | 412x892 | Dark | en, fa, de | 31 |
| 200% | 1280x820 | Light | en, fa, de | 31 |
| 200% | 1280x820 | Dark | en, fa, de | 31 |
| 100% | 360x800 | Light | en, fa, de | 30 |
| **Total** | | | | **220** |

Review includes actual Home, plan list, entry editor, Settings and onboarding. The 162 initial diagnostic captures
are separate from the final matrix. Initial 200% observations showed truncated quick names, clipped icon/date
content and plan identity compressed by its amount. Final captures demonstrate the repaired Home/plan layouts.

Twenty-one actual native Home checks measure 84 labels/targets: four native labels per view, no reported text
trimming, horizontal containment within the wrapping layout, and targets at least 44x44 px. These assertions run
after native layout; they do not prove every glyph, scroll occlusion, keyboard or screen-reader behavior.
Other screenshots are visual review, not new unit cases. Original development database files were preserved and
restored with matching hashes after every walkthrough. No desktop input, focus request or full-screen capture.

Two final English smoke walkthroughs add 30 captures outside that matrix: 21 first-run/no-transaction views at
200% and nine normal-font-path views. Both complete without a walkthrough error and restore the original data hashes.
The normal path produces no font-scale override metadata. Their two additional Home target checks also pass.
The no-transaction view includes an onboarding-created account; it is not a no-account fixture. At 200%, the floating
Add overlaps part of the quick-account/change row in the unscrolled view; this is retained as an open layout finding.

## Existing Android emulator evidence and failed probes

The already installed Visual Studio API 36 Google APIs x86_64 emulator was explicitly selected for every command.
No physical phone, owner profile, SecureStorage value, SDK download or device/system-setting change was involved.
Screenshot protection remained intact; native review used fresh accessibility hierarchies.

A temporary activity-context override recorded native 200% text conversion for fixed English/German content but
pinned locale: a later Persian selection mixed old-language content. A font-only configuration delta left startup
covered with no exposed native text. Those probes were removed from source before delivery. They are failed
experiments, not supported review features or native RTL/200% acceptance. Early unavailable hierarchies were retained
and bounded retries used fresh paths; an unavailable hierarchy by itself was not classified as an app crash.

The final complete Release has no font-scale override or collector. At the unchanged normal native scale it starts,
shows four Home names/icons, changes language through Settings, and exposes Persian RTL Home/Settings and the
empty Plans state. The real Persian Expense action opens the editor; visible Cancel closes the unsaved draft.
Hardware Back did not close that blank editor in the checked sequence and remains a separate navigation finding.
No amount was entered or Save invoked. Native populated plan rows were not demonstrated in this three-entry fixture.

Before Release GUI review, only the owned fictitious QA03 Native database was copied through an unlaunched Debug
package: complete Accounts, Entries and Schedules match D-76 read-back, with three entries. That checkpoint does
not claim a database read-back after all Release navigation. Final preferences were restored to English and Use
device setting through the actual UI, followed by a normal restart and force-stop; final Release remains installed.

## Build, tests and APK

Strict final Windows Debug and complete Android Release builds finish with zero warnings/errors. The unchanged
main suite passes 1,335 tests, zero failed/skipped (App.Tests: 117); test output is cleaned afterwards. AT-84 records
this runtime/layout review separately; no new unit count is claimed for XAML changes.

Phone handoff: `artifacts/android/zanance-d77-release.apk`, 81,107,029 bytes; SHA-256
`15f09bc889572b9fa0d1ab45138cf79d9c9055c2967e5aac063ed4d0d75871d0`.
Package `pro.vafadar.zanance`, version 0.1.0/code 1, min SDK 24/target 36. ZIP integrity, ARM64/x86_64 assembly stores
and application AOT libraries, and v2/v3 local Debug-certificate signature verified. Public certificate SHA-256:
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`.
This is an installable local-test APK, not Store signing, physical ARM64 or release acceptance. CI is separate.

## Findings after D-77

- Home recent-entry and transaction/account rows still give long identity/subtitle text too little width.
- A large Home currency token can split across lines at 412 px; preserve readable token grouping.
- Bottom navigation at native 200%, fixed Settings actions, long picker selections and other screens/dialogs remain.
- Floating Add can overlap scroll content until scrolling; custom-drawn controls need independent text review.
- The blank editor's native Back behavior needs investigation separately from the confirmed visible Cancel path.
- Real OS text settings, native Android RTL at large text, iOS, physical ARM64, keyboard and screen-reader review remain
  independent acceptance gates. Windows process-local stress and build/CI success do not close them.

Continue ready layout findings under D-69 without introducing commercial restrictions or changing approved
financial, security, language or product rules. Device and provider acceptance remain owner gates.

## D-78 follow-up: actions and financial rows

Seven pages now reserve space below the scroll viewport for Add: Home, Transactions, Accounts, Plans, Holdings,
Savings goals and Categories. Existing empty-goal and Simple-holding visibility remains. Transactions bulk actions
use the Add row when selecting; Undo has a further row and its button below its message. Financial identities and
amount/status use separate growing rows on Home/Transactions, Accounts and Holdings. Dates/group names and totals
also use separate rows. Commands, accessible names, money/date formatting, stored data and financial rules remain.

### Running Windows review

| Process-local scale | Window | Theme | Languages | Captures |
|---|---|---|---|---:|
| 200% | 360x800 | Light | en, fa, de | 75 |
| 200% | 360x800 | Dark | en, fa, de | 126 |
| 200% | 412x892 | Light | en, fa, de | 73 |
| 200% | 412x892 | Dark | en, fa, de | 73 |
| 200% | 1280x820 | Light | en, fa, de | 124 |
| 200% | 1280x820 | Dark | en, fa, de | 124 |
| 100% | 360x800 | Light | en, fa, de | 119 |
| 200%, Simple | 360x800 | Dark | en, fa, de | 126 |
| **Layout matrix** | | | | **840** |
| Undo preview, 200% | All three widths | Both | en, fa, de | 360 |
| Undo preview, 100% | 360x800 | Light | en, fa, de | 60 |
| **D-78 total** | | | | **1,260** |

Counts include page captures and, after the own-window renderer was added, supplemental *-window.png; they are
not 1,260 distinct screens or test cases. The renderer includes navigation and action rows that page captures can
omit. The 860 actual layout JSON records verify 412 Add docks, 45 bulk docks and 21 Undo docks against their measured
viewport, plus 1,576 realized entry rows, 192 account rows and 92 holding rows. Financial titles are not natively
trimmed, identity has useful width and amounts begin below the identity. These are repeated realized observations,
not unique data records, all glyphs or offscreen-row acceptance. Viewport boundary clipping remains scrollable.

Undo previews set and restore only visibility, never delete or invoke Undo. They demonstrate the message/button
layout in all matrix combinations, not the timed deletion/recovery workflow. Every original development database,
sidecar and marker was restored with matching hashes after each guard; no desktop input/focus/full-screen capture.

Two early diagnostic cohorts (18/6 captures) selected hidden Home account handlers with unmeasured negative sizes;
the checker now requires a visible ancestor chain. This was a diagnostic selection error. An initial successful
75-capture narrow-dark cohort is duplicate coverage and excluded from the final total. First small cohorts predate
the supplemental window renderer; the supplemental narrow-dark and Undo matrix include own-window views. No
financial geometry failure remained in the formal cohorts. Fixed debt/bulk labels still need their own repair.

### Existing emulator and data proof

Normal-scale API 36 x86_64 Release review used only emulator-5570 and the owned QA03 Native three-entry fixture,
retaining screenshot protection and the unchanged 420 dpi/font_scale 1.0. English Home/Transactions/Accounts/Plans/
Categories/Holdings expose a 147x147 px Add target, at least 44 dp; actual content ends at y=1938 before Add begins
at y=1991. Drafts open through their real Add buttons and visible Cancel closes them without Save. Empty Savings
goals retains its primary Add goal instead of the persistent plus; that draft also opens/cancels. No entry/account/
plan/holding/goal was saved, deleted or undone. Native populated plan/holding/goal rows are not demonstrated here.

Actual German and Persian Home/Transactions/Accounts confirm translated layout, separated names/amounts and no
Add/viewport overlap. RTL puts the Add target at [52,1991,199,2138]; LTR at [881,1991,1028,2138]. The first native
measurement mistook an outer Shell container for the content viewport; exclude ancestors containing Add. A German
Home tab tap retained Accounts navigation, so that record was renamed and actual Home rechecked through Navigate
up. A tab tap alone never proves the active page. Negative/inverted offscreen hierarchy bounds are not usable taps.

A language selection on apparently available Settings changed the picker label but not translated content or the
persisted language; restart retained the prior language. Later retries from a fresh Settings visit succeeded,
including restoring actual English/Gregorian/Latin defaults. This normal Release finding is separate from D-77's
removed font prototypes. Theme/mode chips also retained old-language captions while nearby labels changed;
their System selection remained. Two theme-search helpers missed those retained captions; no theme choice was
changed. Its cause and remediation remain unverified; stable-looking labels do not prove asynchronous loading
is complete, and this slice does not repair language changes.

After native navigation, an unlaunched Debug package copied only the known owned fictitious QA03 database and
sidecars. Complete Accounts, Entries and Schedules still equal D-76 baseline, with three entries. The final complete
Release was reinstalled afterwards. English was restored through the UI and the original System theme confirmed
selected, then normal Release Home/Transactions were checked again and the app force-stopped. No physical phone, owner database or
SecureStorage content was inspected, and no system setting was changed.

### Final D-78 build and handoff

Strict Windows Debug and complete signed Android Release builds finish with zero warnings/errors. Main suite:
1,335 passed, zero failed/skipped (App.Tests 117); test output cleaned afterwards. No new unit case count for XAML.
AT-85 records runtime checks separately. Final APK: artifacts/android/zanance-d78-release.apk, 81,123,413 bytes;
SHA-256 03892da524ac5d43eeda27561f4f6e71ae4f427dd220bb91e5c0566fe3803a22. Package pro.vafadar.zanance,
0.1.0/code 1, min SDK 24/target 36; ZIP integrity, ARM64/x86_64 assembly stores/app AOT and v2/v3 local
Debug-certificate signature verified. Public certificate digest remains the D-77 value above. The intermediate APK
built before the Undo layout was replaced by this final complete APK. This is a local-test package, not Store signing
or physical-device acceptance. CI is a separate single delayed check after push.

### Current remaining work

- Keep large money/currency tokens readable at 412 px without changing formatting or shrinking readable text.
- Repair fixed debt/Settings actions and transaction bulk labels at large text; review other forms/dialogs/pickers.
- Settings input/caption remediation is verified in D-79 below; investigate blank editor native Back and retained navigation captions independently.
- Review bottom navigation, custom-drawn controls, keyboard and screen readers, actual OS 200% text, native Android
  RTL/large text, physical ARM64 and iOS. Windows local stress and normal-scale emulator checks do not close them.

A11Y-03 stays in progress. D-69 authorizes continuing the ready findings; unresolved owner/product/provider and
release gates remain independent.

## D-79 follow-up: complete Settings and live choice captions

Settings now reads preferences, accounts/entries, the existing spending suggestion and device availability before
publishing editable fields synchronously. SnapshotLoadState keeps the form covered through initial/reload/failure;
retry is a translated growing button. The earlier async write-suppression boundary could ignore input on an exposed
form. Live display refresh rebuilds all choice captions and restores indexes under the nested synchronous guard,
without replacing unsaved estimate/reminder text. No financial formula, model, security policy, permission or SDK.

### Running Windows evidence

| Process-local text | Window | Theme | Languages | Captures |
|---|---|---|---|---:|
| 200% | 360x800 | Light/Dark | en, fa, de | 132 |
| 200% | 412x892 | Light/Dark | en, fa, de | 132 |
| 200% | 1280x820 | Light/Dark | en, fa, de | 132 |
| 100% | 360x800 | Light | en, fa, de | 66 |
| **Total** | | | | **462** |

Counts include 126 existing onboarding captures and 336 Settings page/own-window captures, not distinct screens
or unit tests. All 21 language/cohort proof files pass. The separate settings-display route holds a read, injects
a handled read failure and invokes the actual native RetrySettingsButton/page handler. Actual SettingsViewModel
bindings keep content hidden/disabled until publication, refresh de/fa/en choice captions, preserve selected indexes
and the unsaved 17.25 estimate/period, and retain identical stored settings/entry JSON. No estimate Save, PIN or
permission action is invoked. Original development database files/sidecars/marker are restored with matching hashes.
Only the app's own window is rendered; no desktop input, focus request, global text setting or full-screen capture.

Loading/retry text fits at the reviewed sizes/themes. Theme/mode choices grow at 200% and reflect the current language.
The German page header truncates at 360 px/200%; open Shell tab/back captions can retain a prior language during
in-place display changes. These are retained findings under the existing D-67 deferred navigation rebuild; this
slice does not claim complete large-text or navigation-caption acceptance. Windows stress is process-local, not
real OS 200% acceptance. The failure fixture proves the form/retry wiring, not a naturally failing native provider.

### Normal Release emulator evidence

Only emulator-5570, API 36 x86_64 and the owned QA03 Native fixture are used. The final full Release starts normally.
One actual language-picker selection each applies Deutsch, فارسی and English. Cold restarts confirm each persisted
language. On the open Settings page, native theme/mode chips show current German, Persian RTL and English captions;
System theme and Advanced selections remain. System density 420/font_scale 1.0 and screenshot protection are unchanged.
The native toolbar Back description retains an earlier language until reconstruction; record it separately from
the now-current Settings choices. The loading frame can complete before hierarchy capture, so no captured native
initial loading frame or native fault/retry result is claimed.

After navigation, an unlaunched Debug installation copies only the known fictitious QA03 database/sidecars.
Complete Accounts, Entries and Schedules remain equal to the D-76 baseline, with three entries. The final Release
is reinstalled; cold English Home and all three Transactions rows are checked, Home restored and the app stopped.
No financial Save/delete/Undo, owner database, private preferences or SecureStorage read, physical phone or system
setting change. Native checks use accessibility XML, never removal of FLAG_SECURE.

### Tests, builds and APK

Ten AT-86 cases compile the actual snapshot/label sources with real isolated SQLite/localization and availability
answers. They do not compile SettingsViewModel or substitute fake native controls; running-app checks above prove
the binding/selection behavior separately. Main suite: 1,345 passed, zero failed/skipped; App.Tests 127. Test output
cleaned. Final strict Windows Debug and complete Android Release builds have zero warnings/errors. The shipped
Cloud permission boundary passes; this is static package validation, not provider or network-traffic acceptance.

Full signed APK: artifacts/android/zanance-d79-release.apk, 80,713,643 bytes; SHA-256 989227ed5f8bee52c96cab755a53dfc737eb409f52976adf116a58e9364db242.
Package pro.vafadar.zanance, 0.1.0/code 1, min SDK 24/target 36; ZIP integrity, embedded ARM64/x86_64 assembly stores
and app AOT verified. v2/v3 local Debug-certificate signature verified; public certificate SHA-256 remains
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. This is the local phone-test package, not Store
signing, product release or physical-device acceptance. CI remains one independent delayed check after push.

A11Y-03 remains in progress: fixed actions/bulk labels, currency tokens, page headers/navigation captions, other
forms/dialogs, custom-drawn controls, keyboard/screen readers, actual OS 200%, physical ARM64 and iOS remain.
Continue these real findings under D-69; unresolved product/licence/provider/release decisions stay owner gates.

## D-80 follow-up: complete action captions and selection targets

Settings/account/debt commands use a wrapping, natively scaled caption above a real last-child transparent native
Button. Existing commands/arguments, CanExecute and full translated spoken names remain. Long related actions stack
vertically; transaction selection count, All/Cancel and two rows of bulk actions no longer compete for one strip.
Compact bulk captions retain their original 12-point font with smaller side padding; the preliminary 15-point
prototype split words at 200% and was replaced before the formal review. No financial formula/model or security change.

### Running Windows evidence

| Process-local text | Window | Themes | Languages | Main captures | Intermediate-action captures |
|---|---|---|---|---:|---:|
| 200% | 360x800 | Light/Dark | en, fa, de | 276 | 216 |
| 200% | 412x892 | Light/Dark | en, fa, de | 276 | 216 |
| 200% | 1280x820 | Light/Dark | en, fa, de | 276 | 216 |
| 100% | 360x800 | Light | en, fa, de | 138 | 108 |
| **Total** | | | | **966** | **756** |

The 1,722 images are captures, not distinct screens or tests; they include existing onboarding and supplemental
own-window renders. Actual native geometry yields 4,223 layout records, including 3,129 growing-action records.
Every checked caption is untrimmed, has a complete spoken name, an unchanged command/argument and a target at least
44 px. All 21 no-selection states have four disabled bulk commands and enabled All/Cancel; actual native UI Automation
invokes All/Cancel and checks selection changes. Complete entry JSON remains unchanged. The 21 D-79 Settings proof
files still pass. Separate loan-actions/settings-actions scroll each currently visible command into view without
invoking repayment, estimate Save, PIN, permissions, conversion, reconciliation or deletion. Eight visible loan
commands and three visible Settings commands are captured per language/cohort. Original development files/sidecars
and marker are restored with matching hashes after both formal runs.

An initial local review helper put the first snapshot inside its stop routine before preservation; it failed on
stale selection geometry. The original files moved by that snapshot were verified against the prior D-79 preservation
hashes and restored before work continued. The helper was corrected and the capture now waits for the actual native
layout pass. Preliminary failed/prototype captures are excluded from the formal counts above. This was a review
helper correction, not application data recovery or a financial behavior change.

The reviewed action captions fit in Persian RTL, English and German, both themes and all three widths. Remaining
findings are retained: large balance/currency tokens split at narrow 200%, movement labels compete with amounts,
the fixed year box clips four digits at 200%, and the German 360 px header truncates. These are separate work, as are
retained Shell captions, other forms/dialogs, keyboard/screen readers, actual OS 200%, physical ARM64 and iOS.
Windows stress is process-local. A11Y-03 remains in progress.

### Normal Release emulator and financial evidence

Only the existing emulator-5570, API 36 x86_64 and known owned QA03 fixture are used. Normal Release Transactions
opens selection with four disabled native bulk Buttons; the actual lower All button selects all three rows and enables
them, then Cancel closes selection. No Reviewed/Category/Tag/Delete command is invoked. Account detail's actual Edit
and Add transaction here buttons open the expected drafts; visible Cancel closes each without Save. Native toolbar
Navigate up restores the actual More page after retained tab-stack navigation; tapping a tab alone does not prove its
root page. The first Settings lookup on that retained Accounts stack failed and was corrected by visible Navigate up.

Actual language-picker selections apply Deutsch, فارسی and English. PIN/manage and delete-profile actions are real
native Buttons with complete current translated descriptions and usable bounds in all three languages; neither is
invoked. Original System theme and Advanced mode remain selected. The QA03 profile has no loan account, so the
intermediate repayment actions have Windows runtime evidence only. Density 420/font_scale 1.0 and FLAG_SECURE remain;
no native 200%/screen-reader or physical-device result is claimed. Hierarchy reads sometimes take longer to finish;
wait for the same bounded request rather than launching parallel UI operations.

After navigation, install the known Debug package without launching it to copy only the fictitious QA03 database
and sidecars. Full Accounts, Entries and Schedules still match the D-76 baseline, including three entries. Reinstall
the final full Release, check cold English Home/Transactions, return Home and force-stop. No owner database/ZIP,
private preferences, credentials or SecureStorage read, physical-phone action or system-setting change.

### Tests, builds and full APK

Main suite: 1,345 passed, zero failed/skipped (App.Tests 127); test output cleaned. AT-87 adds runtime assertions,
not a new unit count. Final strict Windows Debug and full signed Android Release builds have zero warnings/errors.
Cloud permission boundary passes as static package validation; no provider/network acceptance claim.

APK: artifacts/android/zanance-d80-release.apk, 81,152,085 bytes;
SHA-256 a0df14c3b11d95a62d24ac089c96994d8f122930caf58302edf7f02607856338.
Package pro.vafadar.zanance, 0.1.0/code 1, min SDK 24/target 36, ARM64/x86_64 full assembly stores/app AOT and ZIP
integrity verified. v2/v3 local Debug-certificate signature verified; public certificate SHA-256 remains
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. This is a local phone-test package, not Store
signing, publishing or physical-device acceptance. CI remains a separate single delayed check after push.
Continue the retained A11Y-03 findings under D-69; unresolved owner decisions remain independent.

## D-81 follow-up: growing date inputs and complete large amounts

DateField retains real inputs and their validation, focus progression, calendar/culture order and DateOnly binding.
Invisible digit reserves include native entry chrome; complete input/separator groups reflow rather than clipping a
fixed year box. AmountReadout keeps the original scalable AmountLarge style, full MoneyText packet and spoken name.
An actual horizontal viewport exposes oversized sign, decimal digits and currency, with a six-language overflow hint.
Empty values hide their viewport and hint. Account balance and movement values have their own rows. Home's account
navigation target is on its visible heading, leaving the amount viewport interactive. No financial/schema/security change.

### Final running Windows evidence

| Process-local text | Window | Theme | Languages | Main captures | Supplemental captures |
|---|---|---|---|---:|---:|
| 200% | 360x800 | Light | en, fa, de | 560 | 82 |
| 200% | 360x800 | Dark | en, fa, de | 560 | 82 |
| 200% | 412x892 | Light | en, fa, de | 522 | 78 |
| 200% | 412x892 | Dark | en, fa, de | 522 | 78 |
| 200% | 1280x820 | Light | en, fa, de | 384 | 54 |
| 200% | 1280x820 | Dark | en, fa, de | 384 | 54 |
| 100% | 360x800 | Light | en, fa, de | 365 | 48 |
| **Total** | | | | **3,297** | **476** |

The 3,773 images are rendered own-window captures, not distinct screens or tests. The 6,062 actual native geometry
records include 2,811 date inputs, 1,138 large amounts and 31 empty readouts, plus 1,296 existing growing actions,
196 action docks and 590 financial rows. Native date TextBox chrome plus full digit width and calendar target geometry
pass. Complete amount TextBlock packets/spoken names are untrimmed and occupy one line. Realized character/caret
boundaries fit inside actual content. The 170 native horizontal-scroll proof files reach both ends and return to the
sign; no await depends on a new event at an already clamped scroll boundary.

Twenty-one draft proofs cover Gregorian/Persian/Hijri actual parts, a valid year/month/day update and a partial year
that preserves the last valid date; complete stored entry JSON is unchanged. These programmatic Entry inputs are
separate from a preliminary real external native ValuePattern English 360 px/dark/200% date check. No keyboard or
screen-reader acceptance is inferred. Twenty-one supplemental split proofs use the actual eligible income modal,
check the full total and parts, and preserve complete entry JSON. The ordinary refunded expense fixture deliberately
rejects splitting; the modal review follows the actual top modal, not the covered Shell page. Supplemental rejected
split and report Headroom captures also verify empty readouts without a blank viewport or stale hint.

Both formal runs restore original development files, sidecars and marker with matching hashes. A discarded unit-gap
caption prototype allowed a decimal number itself to split and is not shipped. A detached measurement with the bundled
Persian font reported a different width from actual rendering; the first failed matrix is retained as negative evidence
and excluded from the totals. Final assertions measure realized native boundaries. Earlier review scroll awaits and
the background-page modal lookup were corrected before the final review. No temporary native font/configuration or
security override enters the product.

A final focused Home review repeats all seven language/theme/width/scale cohorts after enlarging the account
heading target and removing its negative vertical margin. It adds 331 captures and 66 measured heading records;
every actual heading is at least 44 px, retains the command/spoken name and ends before the amount viewport. Other
Home native readout/action checks still pass. Together with the main/supplemental reviews there are 4104 captures.
An attempted focused run failed before launching the app because a simultaneous Android restore changed shared
Windows assets (NETSDK1047); its original data was restored with matching hashes. After the Android build finished,
the single sequential retry passed and restored original files/sidecars/marker with matching hashes. Earlier
heading-only prototype captures and that failed build are excluded from the final focused count.

### Normal full Release emulator evidence

Only emulator-5570, API 36 x86_64 and the known owned QA03 fictitious profile are used. Actual numeric-keyboard input
updates 2027/02/28, while a focused partial year retains that valid preview; the original 2026/10/09 draft is restored
without Save. An initial review helper tapped stale coordinates behind the open keyboard and typed into the unsaved
reconciliation amount; it failed its assertion. The draft was discarded by real navigation, not saved. The corrected
helper hides the existing keyboard before finding the next input and verifies actual native focus before typing.

Fresh real Settings pickers select Deutsch, فارسی and English; cold Home/Accounts/account detail show complete
signed EUR packets with matching spoken descriptions for large values. Date parts retain 2026/10/09 for English/
German and 1405/07/17 for Persian's automatic calendar, representing the same date. Native editable digits remain
Latin. A preliminary helper incorrectly expected Gregorian parts after the automatic Persian-calendar selection;
its assertion was corrected against the actual calendar without changing application behavior.

Repeated Settings navigation exposed an independent remaining failure: after a language change and Shell rebuild,
the reopened native picker can change its displayed choice while application captions remain in the previous
language. A cold restart followed by a fresh Settings page applies the choice. This behavior is recorded as a real
follow-up, not claimed fixed by D-81. A bounded hierarchy read also failed during review; its failed case is retained,
and the remaining English fresh-page check completed independently. No polling or duplicated active UI operation.

The final actual Home heading target is 907x116 native pixels at density 420, ends exactly where the full amount
starts, and opens Accounts through the existing command; real Navigate up restores Home. The extra negative vertical
margin was removed so the target does not cover the value viewport. Density 420/font_scale 1.0 and FLAG_SECURE
remain. Native checks are normal text, not OS 200%, screen-reader, physical ARM64 or iOS acceptance.

After the draft/navigation review, the known Debug package is installed without launch to copy only the owned
fictitious QA03 database/sidecars. Complete Accounts, Entries and Schedules match the earlier baseline, including
three entries. The final full Release is reinstalled; cold English Home/Transactions show all three rows, the
heading command is checked, Home restored and the app force-stopped. No owner database/archive, private preferences,
credentials or SecureStorage read, physical-phone action or system-setting change.

### Tests, strict builds and complete APK

Main suite: 1,345 passed, zero failed/skipped (App.Tests 127); test output cleaned. AT-88 adds native runtime assertions,
not copied-control unit cases or a new unit count. Final strict Windows Debug and complete Android Release builds
have zero warnings/errors. The Cloud permission boundary passes as static package validation; real provider and
network-traffic acceptance remain separate. Installed full Release starts successfully on the existing emulator.

APK: artifacts/android/zanance-d81-release.apk, 80,709,547 bytes;
SHA-256 74e3fef28976855adfb9123fce0bf3abe32273ca50d664d4d4c881245aa8480f.
Package pro.vafadar.zanance, 0.1.0/code 1, min SDK 24/target 36; ZIP integrity and full ARM64/x86_64 assembly stores
and app AOT verified. v2/v3 local Debug-certificate signature verified; public certificate SHA-256 remains
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. This is a local phone-test package, not Store
signing, publishing or physical-device acceptance. CI remains a separate single delayed check after push.

A11Y-03 remains in progress. The reopened native Settings language failure is the next ready finding; German narrow/large page-header
truncation and retained navigation captions remain queued. Other forms/dialogs, smaller monetary rows/custom controls, keyboard/screen readers,
real OS 200%, physical ARM64, iOS and QA-06 performance/ANR acceptance remain independent. Continue ready work under
D-69; unresolved product, licence, provider, spending and release decisions remain owner gates.

## D-82 follow-up: repeated Settings language changes

The normal D-81 Release reproduces a real native failure: choose Deutsch, return, reopen Settings and choose فارسی.
The actual picker changes but captions remain German. A temporary public-state observation shows the view-model
and Translator already holding fa, followed by NullReferenceException from the retired ShellItemRenderer's
get_MauiContext / SetupMenu title callback. The synchronous exception interrupts remaining translation and native
locale notifications. Cold restarts do not prove this repeated-navigation path.

Before replacing a Shell root, remove only its translated item/section/content Title bindings and its application
navigation subscription. Create the replacement first; preserve current forms, deferred rebuilding and the lock
gate. A separate actual regression finds initial ReminderDaysText saving during Settings construction. Suppress
only synchronous constructor/publication callbacks. The negative stored-data comparison identifies UpdatedAt
alone; actual preference values/accounts/entries match. Require complete equality after the repair.

### Final running Windows evidence

Each of seven cohorts has 72 rendered captures: en/fa/de, light and dark at 360x800, 412x892 and 1280x820 with
process-local 200% text, plus light/360x800 at 100%. Total 504 own-window/own-content images, not distinct screens
or unit tests. Twenty-one proof files exercise four actual native language selections and real header Back actions
per cohort/language. Keep at least three retired shells strongly alive, so collection cannot conceal stale title
subscriptions. Their item/section/content titles remain frozen; current titles and choice captions translate. The
current form, unsaved estimate draft and other choice values remain intact until Back. Complete settings JSON,
including UpdatedAt, accounts and entries remain unchanged. Original development files/sidecars/marker are restored
with matching hashes after both formal runs.

Initial review failures are retained and excluded from the final count. WinUI selection belongs to the ComboBox's
native data peer, not the visual container peer; a valid virtualized choice need not have a realized popup container.
Replacement navigation completion also precedes Home's async read/native arrangement. The final baseline waits
for the actual read and positive realized bounds, then still applies ordinary target/overlap/packet assertions;
it does not skip a geometry failure. No real OS setting is changed or performance/ANR acceptance inferred from the
review wait. All temporary public-state observations and diagnostic build flags are removed from product source;
normal Release checks below use the complete uninstrumented APK.

Main suite: 1,345 passed, zero failed/skipped (App.Tests 127); test output cleaned. AT-89 adds actual runtime
regressions rather than fake native controls or copied view-model cases. Final strict Windows Debug has zero
warnings/errors. The remaining strict Android/native/package evidence follows below.

### Normal complete Release emulator and APK

Only emulator-5570, API 36 x86_64 and the known owned fictitious QA03 profile are used. Actual Settings pickers choose
Deutsch, فارسی and English with Back/reopen between choices and no cold restart between them. Selected picker/body
captions and returned navigation match; the bottom tabs mirror correctly on the replacement Persian root. A final
cold English launch verifies persistence. A preliminary helper compared the top More heading with a bottom tab;
its false mirroring assertion is retained and corrected to compare actual bottom labels. It also assumed the System
chip's literal English text; final checks use the real translated resource caption, "Use device setting, selected".
No production change is made to satisfy either helper mistake.

The owned main window retains SECURE; density 420/font_scale 1.0 stay unchanged. The known Debug APK is installed
without launch solely to copy owned fictitious QA03 database/sidecars. Complete Accounts, Entries and Schedules equal
the D-76 baseline, with three entries. Reinstall the final complete Release, check cold English Home and three actual
native Transactions row buttons, confirm selected System/Advanced via UI, return Home and force-stop. No financial
Save, PIN/permission operation, owner database/archive, private preferences/credentials/SecureStorage inspection,
physical-phone action or system-setting change. Normal native text is separate from OS 200%/screen-reader acceptance.

Final strict Windows Debug and complete Android Release builds have zero warnings/errors. Cloud permission boundary
passes as static package validation, not real OAuth/network acceptance. Full APK: artifacts/android/zanance-d82-release.apk,
81,156,181 bytes; SHA-256 eefff4a0388f658dd59e160cd88622bad327506b079c9ce7ce7b6fb65961dc5c.
Package pro.vafadar.zanance, 0.1.0/code 1, min SDK 24/target 36; ZIP integrity and complete ARM64/x86_64 assembly stores
and app AOT verified. v2/v3 local Debug-certificate signature verified; public certificate SHA-256 remains
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. This is a local phone-test handoff, not Store signing,
publishing or physical-device acceptance. CI remains one separate delayed check after push.

D-81's reopened-language failure is resolved by this slice. A11Y-03 stays in progress: growing Windows headers,
retained native navigation descriptions, other controls/monetary rows/dialogs, keyboard/screen readers, real OS 200%,
physical ARM64, iOS and QA-06 duration/ANR acceptance remain independent. Continue ready work under D-69.

## D-83 follow-up: complete child titles and current Back descriptions

Windows Shell's fixed-height TitleView clips long scalable page titles at 360/200%. Child headers now occupy an
Auto row above the same original body, with complete WordWrap text and the existing 44 px native Back action.
Bind Back's spoken name and tooltip to live translations; keep body bindings stable during reparenting and attach
only once. ReadableWidth sizes current Content, so the growing header and body remain one centered 720 px column
when wide and share the available width when narrow. Modal editors and Insights tabs keep their existing paths.

Android's retained toolbar can hold the preceding per-app locale for its default Navigate up description. A scoped
tracker retains the framework arrow, enablement, drawer/navigation and back command; it uses live Common_Back
for the accessible description. Ignore retired/disconnected Shells and unsubscribe when disposed. No new SDK,
permission, schema, financial calculation, portable preference or security-policy change.

The normal complete Debug prototype repeats actual de/fa/en pickers with Back/reopen and no cold restart between
choices. Immediately after each choice the unique native arrow description is respectively the German and Persian Common_Back resources and English Back. Actual picker/body/returned navigation agrees,
RTL mirrors on the replacement root and cold English persistence passes. Owned main window remains SECURE at
density 420/font_scale 1.0. This is normal emulator text, separate from OS 200%/screen-reader/device acceptance.

Negative prototype evidence is retained separately. The initial header fixture incorrectly used a modal entry
editor that deliberately has its own header and no PageHeader; it is corrected to the actual Settings child page.
The actual Settings nested return then exposes its existing OnAppearing reload resetting an unsaved estimate.
Header resize itself retains the draft; returning keeps the same body/bindings and one header. The final header
scenario restores its draft before nested return and reports those boundaries explicitly. The independent reload
finding stays queued for the next repair; this slice does not claim to resolve it.

### Final Windows and test evidence

Seven final cohorts cover en/fa/de: light/dark at 360x800, 412x892 and 1280x820 with process-local 200% text, plus
100% light/360x800. Routes are the actual Settings header/resize scenario, entry detail, repayment schedule,
profiles and notices, with existing first-run captures. Total: 684 own-window/content images and 237 realized native
header checks, not unit cases or unique screens. Twenty-one proof files each verify retained unsaved estimate
input during real own-window resizing through 360/412/1280, shared header/body widths (720 px when wide), the
same page/body/bindings and one header after nested return, native Back and complete unchanged stored settings/
accounts/entries. Original development database files, sidecars and marker are restored with matching hashes.
Preliminary failed prototypes are excluded. Full real OS 200%/screen-reader/device accessibility is not inferred.

Main suite: 1,345 passed, zero failed/skipped; App.Tests remains 127. Test output cleaned successfully. AT-90 adds
actual runtime assertions, not fake native controls or copied algorithms. Final strict Windows Debug build has
zero errors/warnings. Normal complete Release emulator and package evidence follows below.

### Normal complete Release emulator and installable APK

The final complete Release repeats actual de/fa/en Settings picker/Back/reopen without a cold restart between
choices. Each unique native arrow immediately has the current Common_Back description; picker/body/navigation
agree, replacement RTL tabs mirror and cold English persistence passes. The owned main window remains SECURE
at density 420/font_scale 1.0. No OS setting, PIN/permission or financial Save operation.

Install the known complete Debug without launch solely to copy the owned QA03 fictitious database/sidecars.
Complete Accounts, Entries and Schedules equal the earlier baseline, including three entries. Reinstall final
Release and verify cold English Home, three actual native Transactions row buttons, selected System/Advanced
through their UI descriptions and the initial English Back description. Return Home and force-stop. An initial
handoff helper's long swipe jumped over the Appearance chips; retain its negative evidence and use smaller actual
swipes. No product change or guessed preference value is used to satisfy that helper assertion.
No owner archive/database, private preferences, credentials or SecureStorage inspection, physical-phone action,
real OS 200%, keyboard/screen-reader, provider or iOS acceptance.

Final strict Windows Debug and complete Android Release builds have zero errors/warnings. Cloud permission boundary
passes as static package validation, separate from real provider/network acceptance. Full phone-test APK:
artifacts/android/zanance-d83-release.apk, 80,721,835 bytes;
SHA-256 d70be2fa2270b3fb660513b4f6060a2b2c5354c6e7d6c6b9c660e07a95d7ec00.
Package pro.vafadar.zanance, 0.1.0/code 1, min SDK 24/target 36; ZIP integrity and complete ARM64/x86_64 assembly
stores/app AOT verified. v2/v3 local Debug-certificate signature verified; public certificate SHA-256 remains
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. Local handoff is not Store signing/publishing
or physical-device acceptance. CI is one separate delayed check after push.

A11Y-03 stays in progress: modal/Insights headers, custom controls, the independently observed Settings nested-return
estimate reload, real OS/keyboard/screen-reader/physical ARM64/iOS and QA-06 durations/ANR remain open. Continue
ready work under D-69; unresolved product/licence/provider/spending/release choices remain owner gates.
