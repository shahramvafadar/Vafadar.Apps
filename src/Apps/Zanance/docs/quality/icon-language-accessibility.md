# Live original icon names (D-179 / AT-178)

Status: implemented and locally verified; spoken/platform/release acceptance remains open.

## Observed defect and correction

The original native Windows buttons correctly expose English names initially. After a transient
Translator change to Persian, the default target still reports Default icon instead of the original
Persian resource. This retained negative JSON is not accepted evidence. The review exits with its
assertion and a secondary WinUI title-bar exception; both are preserved.

IconPicker now resolves the original key each time, including the selected suffix. Loaded subscribes
once, Unloaded removes the subscription, and reattachment refreshes current names. Dispatcher updates
are ignored after unload. The same original buttons/drawings and two-way selection remain.
No new strings, policy, formulas, financial writes, schema, SDK, permission or OS/device setting.

## Verification scope

Main suite: 3,179 passed, zero failures/skips, 3m 04.824s. Strict Windows build has zero warnings/errors.
Actual native en/fa/de names/targets pass in both themes at 412 px: 188 current-name checks
per theme, eight native Invoke operations per theme, plus 47 reattachment catch-up checks per theme.
The same 47 original buttons, selected key and original draft remain; complete accounts/entries/settings
JSON stays unchanged. Original Windows database/sidecars restored byte-for-byte.
The first accepted name review did not retain the intended scroll viewport in its captures; final
review uses the existing native Scroll pattern and retains own-window captures at the icon group.
Transient Translator-only probes validate icon names, not whole-app language or glyph rendering. Six normal Release Android Goal-editor cases pass, en/fa/de and light/dark: actual default/Home
names, original selection round trip and Cancel without Save. The same shared IconPicker is used by
entry/account/goal forms; Android live language changes were not simulated on a retained modal.
Two bounded empty-entry gesture attempts did not reach its icon group; those hierarchies remain
negative evidence and the cause is not claimed fixed. Goal-editor native scrolling and selection
provide the accepted Android control evidence. One top-tab helper initially searched its nonclickable
caption; final helper invokes the observed clickable parent.
All 23 original Main tables remain unchanged. Exact database/sidecars restored; readback Debug was
never launched; final installed APK matches the complete Release file. English/System restored.
Spoken TalkBack/Narrator, system text scaling, physical phone, iOS and release acceptance remain open.


## Complete APK and records

`artifacts/android/zanance-d179-release.apk`, 82,921,557 bytes; SHA-256
`0d35f9911d24498d3a34e46825aefc6a057fc9e600363a97a9fe8b6aa0d4db56`.
Package `pro.vafadar.zanance`, API 24/36, v2/v3 signatures verified, intact ZIP CRCs and both
arm64-v8a/x86_64 assembly stores/app AOT libraries. Canonical Release script rejects warnings;
strict Windows build has zero errors/warnings. Test output cleaned. Existing local test certificate
and emulator installation do not establish production signing or physical-device acceptance.

Records: `artifacts/icon-language-all-tests.log`, `icon-language-clean.log`,
`icon-language-windows.log`, `icon-language-android.log`, `icon-language-apk-proof.json`,
`icon-language-baseline/`, `icon-language-light-final/`, `icon-language-dark-final/`,
`icon-language-native/`, `icon-language-data-proof/proof.json`,
`icon-language-windows-restoration.json`.
