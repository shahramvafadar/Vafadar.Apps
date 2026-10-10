# Growing native picker captions (D-107 / AT-112)

## Observed defects and change

The Persian Settings capture at 360 logical units and process-local 200% text clipped the selected Persian calendar
and German regional-format names. The native Windows ComboBox's default single-line presentation was the clipping
boundary. Picker items now use a wrapping native TextBlock template for both the closed selection and the popup.
The existing MAUI Picker, items, selection, semantic field name, inherited native font/direction/colors and native
expand/selection patterns remain. No extra caption or alternative picker is added.

Normal-text popup review then measured a selected language row at 42.6667 logical units, below the app's minimum
44-unit target. Its complete name, glyphs, font and scale were already correct. The item container retains its
existing style/native theme template, with a 44-unit minimum height. Growing rows still accommodate longer text.
No financial algorithm, saved selection, schema, data, SDK, permission, security or portable preference changes.

## Verification and retained negative evidence

The first compile attempts had ambiguous MAUI/WinUI DataTemplate and ScrollView names. Explicit aliases correct
them; the subsequent strict builds pass. Failed builds are not acceptance. The first normal-text popup attempt is
retained as negative evidence for its 42.6667-unit row. The corrected focused Persian run passes all nine actual
Settings choices with inherited fonts, complete glyphs/names and 44-unit targets. Its folder name ends in dark,
but the actual command used light theme and normal text; it is not dark/200% evidence.

The first full normal-text attempt timed out awaiting ShellRebuilt during German-to-Persian native Back. Windows
then recorded an IObjectReference finalizer/access violation at process shutdown; their causal relationship is not
established. The whole launcher failed and its dependent builds/native checks did not start. Original developer
files were restored with matching hashes. This attempt is retained and excluded from accepted counts.

The Back review now uses the actual control's associated FrameworkElementAutomationPeer instead of constructing
a separate peer. A bounded timeout records actual root/page/navigation/native load/dropdown state and still fails;
no extended wait or programmatic return substitutes for Back. A fresh complete normal-text repeat passes the same
29-route/three-language scope and native returns. This does not claim that the observed finalizer failure is repaired.
The canonical script now throws on timeout, unsuccessful app exit or error.txt. An actual deliberately invalid
native route proves rejection with retained error evidence; the harness has the expected failure exit. A separate
actual positive native picker/Back run completes successfully under the changed script. Both restore original files.

The final matrix covers en/fa/de, both themes, 360/412/wide windows at process-local 200% text, plus normal text at
360/light. Every cohort has all 29 matched base routes in all three languages; actual selected onboarding, entry,
plan, split, category/rule, goal and asset-event captions are included. Hidden/unselected picker states are not
represented by a selected-caption check. A render at each scroll position is a layout observation, not a unique
data record or acceptance of every unexercised choice/state.

| Cohort | Process-local scale | Own-window renders | Selected-picker geometry checks |
|---|---|---|---|
| screens-360x800-light | 2 | 329 | 357 |
| screens-360x800-dark | 2 | 329 | 357 |
| screens-412x892-light | 2 | 326 | 356 |
| screens-412x892-dark | 2 | 326 | 356 |
| screens-1280x820-light | 2 | 261 | 342 |
| screens-1280x820-dark | 2 | 261 | 342 |
| baseline-retry-360x800-light | 1 | 251 | 336 |

Across 21 contexts: 2083 own-window renders and 2446
selected-picker geometry checks. Nine actual selected/popup choices per context give 189 native SelectionItem
reselections, with raw estimate/period drafts and complete Settings/Accounts/Entries JSON unchanged, without Save.
Reopened review separately exercises 84 native language choices and 84 Back returns, retained live drafts and
frozen retired titles. Native character bounds, complete names, inherited fonts/scaling and 44-unit targets pass.
All original developer database/WAL/SHM files are restored with matching hashes after the complete matrix.

The deliberate transaction loading/failure captures are fictitious diagnostic states, not failed reads. No final
error file, missing matched route or negative picker geometry is accepted. A partial folder or an observation
timeout is not acceptance of a completed cohort.

## Builds, tests and Android regression

All 1,512 main tests pass, zero failed/skipped (App.Tests remains 276). Test output cleanup passes. Final strict
Windows and complete canonical Android Debug and Release builds pass with zero warnings/errors. The Windows-only
diagnostic caller and helper share their platform guard; Release alone cannot prove Android Debug compilation.

The complete signed Release installs on the existing independently owned emulator-5570. Actual cold English Home,
three original native transaction rows, System/Advanced choices and translated native Back pass; the app returns
Home and stops. The owned native main window remains secure, density 420 and system font scale 1.0 are unchanged.
Complete 24-table readbacks for each of three fictitious profiles (10,001/100,001/3 entries) equal the verified
D-106 baseline, including exact Settings audit values; SQLite integrity passes. An unlaunched Debug install copies
only those exact fictitious databases, followed by reinstalling the complete signed Release. No physical phone,
credential, security-storage or system-setting operation occurs.

## Complete phone-test handoff and limits

APK: artifacts/android/zanance-d107-release.apk, 81,360,981 bytes; SHA-256
ee213bc4ef21ae7d8cef6ea3b416f107cb1114047df1c2aae1745685432a4d34.
Package pro.vafadar.zanance, version 0.1.0/code 1, min SDK 24/target 36; ZIP integrity, embedded ARM64/x86_64 assembly
stores/application AOT and nondebuggable manifest pass. v2/v3 signature verifies with the existing local test
certificate, public SHA-256 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. Cloud permission policy remains unchanged.
This is an installable local test handoff, not production signing or publishing approval.

Windows process-local layout stress is not actual OS scaling, a keyboard/screen-reader pass, Android/iOS typography
or physical-device acceptance. A11Y-03 and independent cold/ANR/performance/provider/owner/release gates stay open.
Continue ready work under D-69. CI is one separate delayed check after push.
