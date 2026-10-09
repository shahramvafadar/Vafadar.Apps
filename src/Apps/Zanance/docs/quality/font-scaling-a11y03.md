# Large-text review (D-77 / ZCR-A11Y-03)

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

## Remaining work and acceptance

- Home recent-entry and transaction/account rows still give long identity/subtitle text too little width.
- A large Home currency token can split across lines at 412 px; preserve readable token grouping.
- Bottom navigation at native 200%, fixed Settings actions, long picker selections and other screens/dialogs remain.
- Floating Add can overlap scroll content until scrolling; custom-drawn controls need independent text review.
- The blank editor's native Back behavior needs investigation separately from the confirmed visible Cancel path.
- Real OS text settings, native Android RTL at large text, iOS, physical ARM64, keyboard and screen-reader review remain
  independent acceptance gates. Windows process-local stress and build/CI success do not close them.

Continue ready layout findings under D-69 without introducing commercial restrictions or changing approved
financial, security, language or product rules. Device and provider acceptance remain owner gates.
