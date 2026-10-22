# Android quick widget native accessibility (D-174 / AT-174)

## Observed defects and implementation

The actual original launcher widget exposed three clickable TextViews rather than Button roles.
Its logo target was 40 dp. After the app selected Persian (including Android's persisted per-app
locale), the actual widget still had English captions. These are observed native findings, not
an inferred TalkBack pass.

Use real Button/ImageButton controls with the existing resource IDs and immutable PendingIntents.
Give the logo a 44 dp target with 2 dp padding, preserving its original 40 dp brand image. Preserve
existing button colours, captions and geometry with explicit native casing/minimum/padding settings.
Resolve existing six-language widget strings in the original app-language configuration and send
actual text/content descriptions and layout direction in RemoteViews. Android 13+ persisted app
locales take precedence over the localization service's not-yet-published startup defaults.
No database, schema, permission, SDK, security policy, commercial switch or new translation keys.

## Checks and scope

- Ordinary main: 3,169 passed, zero failed/skipped, 2m 34.496s; output cleaned. This run precedes
  the final Android-only caption correction, which is outside the non-MAUI test compilation.
  No unchanged main algorithms/tests were rerun merely for that native correction.
- Strict Windows: zero errors/warnings (20.34s). Windows production UI is unchanged; its old
  complete matrices were not repeated for Android widget resources/native adapter code.
- Final canonical complete Android Release build: no warnings/errors. Native compilation and
  final actual receiver checks cover the corrected platform source, rather than copied logic.
- Twelve actual widget instances/cases: en/fa/de x app light/dark x two simultaneously hosted
  widths (approximately 266 and 360 dp) in the existing approximately 412 dp phone viewport.
  All four native roles are Button/ImageButton, named, focusable/clickable, at least 44 dp;
  exact translated native captions and Persian RTL/English-German LTR positions are asserted.
- Actual original expense/income/transfer PendingIntents and logo in each language: open the
  correct unsaved editor or original app; cancel every draft without invoking Save.
- Cold Persian receiver: force-stop the app and add a second independently owned widget through
  the real launcher. Both original/new widgets publish Persian captions and RTL positions.
  The cold expense intent stays covered initially; the same live process later opens its exact
  original draft. No duplicate launch is used to replace an observation timeout.
- Complete original 24-table owned fixture matches including schema/metadata/rows. Its byte
  representation differed without logical changes; restore only its recorded bytes/sidecars.
  Remove only the two added widget IDs through the launcher; restore English/System; final
  complete signed Release installed/stopped. No global OS settings or security storage changes.

App theme choices are checked without changing the launcher's system theme. This is native
role/name/target/intent evidence, not global dark-mode, 200% system font or spoken TalkBack acceptance.
TalkBack is installed but not enabled; the repository's no-device-settings-change rule is retained.
Other screens/dialogs, announcements, chart alternatives, keyboard/navigation and physical-phone/
older Android/iOS acceptance remain open. A11Y-01 is in progress, not completed.

## Negative evidence retained

Initial null/covered native roots and the cold editor's first covered frame are observations, not
terminal app failures. A picker uses native CheckedTextView selection even when its own clickable
flag is false; the review helper initially rejected that row. A native Locale type ambiguity fails
compilation and is corrected with an explicit Android alias. The first caption implementation can
use an unpublished startup language; the persisted per-app locale correction is checked cold.
A manually requested protected widget-update broadcast is rejected by Android; no permission bypass.
Use the actual launcher Add/receiver instead. Launcher removal retires widget IDs after its Undo
window; current service metadata confirms both removals. An EOF-dependent fixture stream copy times
out; recover only the owned fixture with the established push/copy method and verify exact bytes.
No financial/profile production data is replaced and no original backup archive is opened.

## Evidence and package

artifacts/widget-accessibility-tests.log, widget-accessibility-clean.log,
widget-accessibility-windows.log, widget-accessibility-android-cold-fixed.log;
artifacts/android-accessibility-review/*-proof.json, actual native hierarchies, cold/new-widget
and native picker evidence; final-proof.json; widget-accessibility-apk-proof.json,
widget-accessibility-final-signature.log and widget-accessibility-final-package.log.

Complete APK: artifacts/android/zanance-d174-release.apk, 82,913,365 bytes; SHA-256
24eb3be6e2edb74c890708951b15099985900c34232d9d76ed16a37b82fe9c74.
Non-debuggable pro.vafadar.zanance, API 24/36, intact ZIP, full arm64-v8a/x86_64 assembly stores
and app AOT, v2/v3 verified with existing local test certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
This is not production/Store signing, physical-device, OAuth, iOS, performance or release acceptance.

## D-479 physical continuation without repeating this cohort

[Original physical widget actions](physical-widget-pending-intents.md) adds Samsung default-scale
native provider/RemoteViews actions and one actual cold origin through a consented independent
host. All four original actions pass; three drafts are cancelled, Home979/public tuples and OS/APK
match, and only the newly owned widget/helper are removed. This does not replace D-174's original
emulator launcher/language/theme evidence or accept actual Samsung launcher placement, large-text,
spoken TalkBack, all lock variants or general stability. No new managed cases or shipping changes.
