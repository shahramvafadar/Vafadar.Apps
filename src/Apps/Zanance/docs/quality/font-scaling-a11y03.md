# Large-text review (D-77 / D-78 / ZCR-A11Y-03)

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
- Investigate ignored Settings language selections and blank editor native Back independently of layout.
- Review bottom navigation, custom-drawn controls, keyboard and screen readers, actual OS 200% text, native Android
  RTL/large text, physical ARM64 and iOS. Windows local stress and normal-scale emulator checks do not close them.

A11Y-03 stays in progress. D-69 authorizes continuing the ready findings; unresolved owner/product/provider and
release gates remain independent.
