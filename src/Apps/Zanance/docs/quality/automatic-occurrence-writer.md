# Actual automatic payment writer (D-138 / AT-140)

Automatic payment discovery is only a candidate list. Recheck the stored active rule/slice, actual open state,
linked partial/full money, current amount/due date and accounts inside the same SQLite writer as ledger Save.
Manual partial money, including a legacy incorrect paid cache, prevents another automatic full payment. Removed,
paused, skipped, moved-future or already settled candidates post nothing. Use current overrides/account routing;
transfers remain one entry and automatic money stays unreviewed. Exact cached membership, selected plan and
AdvancedPlans rights apply when enforcement is enabled; choices grant no paid rights. Current registration stays
inactive. Snapshot plan choices are immutable, scoped and in memory only; no selection persistence/UI, schema,
backup format, SDK, permission or visible layout change.
31 added AT-140 actual SQLite cases; main 2,241 / App.Tests 348 pass with normal parallel execution (67.124 s).
Strict Windows/Android builds: zero warnings/errors. Complete signed Release APK verified. Six normal Release
emulator foreground flows cover en/fa/de, light/dark, actual posting/override/transfer and partial/paused/disabled
blocking. All 24 unrelated complete tables match; exact original sample files/sidecars and English/System theme
are restored, then the original Main profile is reopened. The first native assertion inspected an inactive sample
while Main was open; its fixture runs through actual services successfully. Selecting the exact owned sample
through native UI resolves the review setup. Transient hierarchy misses resume the same fixture; a local helper
syntax error executes no native action and is corrected. Preserve these negative attempts; they are not acceptance.
The original source reproduces automatic full money after independent manual partial payment; the final writer
prevents it. One expanded test initially expected a result instead of the existing missing-membership exception;
the corrected test retains the rejection. No Windows native, physical-phone, iOS, provider, performance or release
acceptance claim. Other resource bindings, generation/reminders, choice UI/persistence and external gates remain open.

## APK

D:\_Projects\Vafadar.Apps\artifacts\android\zanance-d138-release.apk

Bytes: 81516629

SHA256: d73c09b76d9e24c6d1f937f7a0ed67d74f41bad153146f97085598008daf91b3

Package pro.vafadar.zanance, minSdk 24, targetSdk 36; non-debuggable complete arm64-v8a/x86_64 stores/application AOT. ZIP integrity and v2/v3 signatures verified with the existing local test certificate. Sideload only, no Store publication.

## Retained evidence

- automatic-occurrence-baseline.log: old automatic full payment after independent partial money fails the regression.
- automatic-occurrence-focused-expanded.log: initial missing-membership test expectation failure retained; final rejection verified.
- automatic-occurrence-all-tests.log / automatic-occurrence-clean.log: 2,241 actual passed, zero failed/skipped; output cleanup.
- automatic-occurrence-windows-build.log / automatic-occurrence-android-build.log / automatic-occurrence-release.log: strict builds and canonical APK.
- automatic-occurrence-native-fixture-inspect.log: actual service run against the same complete copied fictitious SQLite fixture.
- automatic-occurrence-native/en-light-normal/observed: inactive sample from the initial wrong-profile native assertion, not acceptance.
- automatic-occurrence-native-en-dark.log / automatic-occurrence-native-en-dark-resumed.log: bounded hierarchy miss and helper syntax failure; same fixture resumed.
- automatic-occurrence-native/final-proof.json / automatic-occurrence-apk-proof.json: final six native cases, restoration and APK verification.
