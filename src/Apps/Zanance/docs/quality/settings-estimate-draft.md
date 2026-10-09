# Open Settings estimate retention (D-84 / AT-91)

D-83's real nested-return review exposes Settings.OnAppearing republishing a stored estimate over an unsaved draft.
The repair keeps the complete covered read, retry and availability/account refresh, and merges only the explicit-
Save estimate against its last published baseline. Exact incomplete/invalid text, period and entered currency stay
on the same open form. Clean fields take current saved values. Profile and settings-row identity bound the draft;
a restored copy may share row ids. No draft persistence, autosave, data model/migration or financial rule change.

Suggestions calculated in a changed default currency are hidden rather than relabeled. Explicit Save validates and
captures one submitted input, then accepts its baseline only after successful persistence. Later typing remains
dirty, and a late save in a retired context cannot replace the current baseline or claim that a changed draft is saved.

## Focused source and prototype evidence

Thirteen new AT-91 cases compile the real EstimateDraftState source. Real isolated SQLite checks prove reads retain
complete stored preferences and create no entries, including when a default currency changes. Cases also cover
partial/invalid/empty/native-digit text, period-only changes, clean external refresh, copied profile/settings ids,
successful Save, subsequent typing and late previous-profile completion. Final focused strict run: 13 passed,
zero failed/skipped and no warnings. Initial test cancellation warnings are corrected before the final run.

The real Windows Settings route now keeps its draft through nested navigation, rather than restoring it before
return. The preliminary 360/light/200% en/fa/de run passes all three retained-draft proofs and original data hashes.
Final focused matrix, suite/build and normal signed Release emulator/package evidence are recorded below.

## Final Windows, suite and strict build evidence

Seven focused cohorts cover en/fa/de, light/dark at 360x800, 412x892 and 1280x820 with process-local 200%, and
100% light/360x800. Only the changed headers/Settings draft route plus existing first-run captures are repeated:
420 rendered own-window/content images, 105 native header checks and 21 retained-draft/data proof files.
Each actual form retains its unsaved 17.25/month draft after nested navigation and own-window resizing; the same
body/bindings, one header, real native Back and complete stored settings/accounts/entries remain correct. Original
development files/sidecars/marker are restored with matching hashes. D-83's other child layouts are not repeated.
This is local layout stress, not real OS 200% or keyboard/screen-reader/device acceptance.

Final strict main suite: 1,358 passed, zero failed/skipped (App.Tests 140); output cleaned successfully. Final strict
Windows Debug build: zero warnings/errors. The 13 new cases and native runtime evidence have separate counts.

## Normal Android Release and unchanged data

The complete signed Release installs on the existing isolated API 36 x86_64 emulator. Actual native numeric input
enters the fictitious 17.25 draft and selects month. Open and close the existing PIN settings modal through real
buttons, without credential focus/input, Save/removal or a security change. English, Persian and German each retain
the exact amount and selected month after returning to Settings. Restore the unsaved draft without financial Save,
then restore English through the actual picker. The first helper stopped because the month chip was below the
visible input; its corrected bounded scrolling reaches the real control. That failed helper is not counted as a pass.

After stopping the app, install the known full Debug package without launching it solely to copy the exact owned
fictitious database via run-as. Compare complete Accounts, Entries, Schedules and Settings rows with the prior
baseline: all equal, including the same three entries. No other profile, private preferences, SecureStorage or
owner archive is read. The final full Release is reinstalled and cold-started in English: Home loads, the actual Transactions list shows
all three owned rows, and native selected captions confirm System theme and Advanced mode. The initial native
Back description is English. Return to Home and stop the app. The exact owned main window retains SECURE,
physical density 420 and font_scale 1.0; no system setting change.

## Installable APK

`artifacts/android/zanance-d84-release.apk`: 81,164,373 bytes, SHA-256
`4710807c5a7c11fe797f191dce9f1fe17fbb4ced4c88a5a509633fa7c08fe29d`.
Strict official Release build: zero errors/warnings. ZIP integrity, complete ARM64/x86_64 assembly stores and app
AOT libraries pass; independent research/measurement assemblies are absent. Package `pro.vafadar.zanance`, version
0.1.0/code 1, minimum SDK 24, target SDK 36. v2/v3 signatures verify with the local Android Debug certificate
SHA-256 `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`.
The shipped Cloud permission boundary passes. This is an installable phone-test package, not store signing or
physical ARM64, real OS 200%, TalkBack/Narrator, provider, iOS or product acceptance. No production credential,
security policy, schema, encryption provider or commercial limit changes.
