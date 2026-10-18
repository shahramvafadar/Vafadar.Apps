# Android Settings at actual large text (D-355)

## Observed cause and scoped change

On the owned Zanance_Storage_Review emulator at412dp and actual system font_scale2.0,
the original English Light Settings page exposes56 labels,9 selected pickers and4 entries.
The numeric-format choice and the long fictitious default-account caption both exceed their
single native line:346px wide,54px high,27px native text. Retain the exact settings-text-page
packet for run9c371116a4054255b445a584990faba8, comparison4165ddf3786e4dba9aad988152734d46,
PID29218, sourceMvid5e1c9a77-eaf0-4f4b-b953-63be767496d8. Stop the negative baseline after
this concrete failure rather than repeat its remaining contexts.

AndroidPickerText changes only the original native picker selected-caption presentation:
disable single-line/horizontal scrolling, allow growing wrapped text and remove ellipsis.
Keep inherited OS font size, native selection/dialog, keyboard restrictions, bindings and
semantic descriptions. No custom input, replacement dialog, font override or financial change.
The mapper is Android-only; Windows development behavior is unchanged.

## Retained setup failures

The initial PID28770 terminates with an actual startup ANR before managed test stages.
PID28949 reaches startup-ready but the initial helper incorrectly assumes native popup focus
after a fixed delay. Retain that failed result; it is not a product-dialog defect.
The helper now waits for actual focus and retains page geometry before entering any popup.
PID29218 initially remains cached/frozen behind the pre-existing system TTS Settings activity.
Bringing the existing task forward, without restarting or recreating data, allows startup and
the original page result above. No TTS setting or unrelated system surface is operated.

The initial independent fixture contains only fictitious accounts/settings and no entries;
its database, marker and identities are reused. Presentation-only LockAvailable exposes a
security caption; no authentication or security action is invoked. Compare complete stored
accounts/entries/settings across each completed case. Read-only geometry scopes to original
Labels, selected Pickers and Entries; it does not qualify every custom/native control.

## Final evidence

The corrected Release/AOT caller completes the original six contexts once in PID29697:
English/Persian/German, Light/Dark,412dp, actual font_scale2.0. Every context checks56 labels,
9 selected pickers,4 entries and3 original account-popup rows: no geometry failure. Original
Cancel preserves selection, and complete stored accounts/entries/settings remain equal.
SourceMvid77ebcb75-475f-4eb4-a5d2-59ab37f8384c; report
artifacts/settings-os-d355-fixed-complete.json retains the full observations and identities.
The final caller APK SHA256 isa9cc87df1fa5b65f6b96e2156888fcd3a1a3dd65449b6bbc9dd06796d20b2bb5.

PID29697 has an actual startup ANR before startup-ready; the existing system ANR dialog is
captured and its original Wait action continues the same PID, without reset/reseed. Preserve
that failure and the temporary observer PID-mismatch refusal; later authoritative PID readback
confirms the original process. A successful Settings matrix is not an ANR fix.

After the final native result, stop only the independent caller. Exact readbacks restore
font_scale1.0, accessibility_enabled0, absent enabled_accessibility_services and
touch_exploration_enabled0. Only font_scale was written. Both exact owned hierarchy files
are SHA256-matched before individual removal; local evidence remains. Samsung, desktop settings,
accounts and authentication are untouched.

Strict Windows build succeeds with0 warnings/errors in32.27s. Complete signed offline main
Release APK is artifacts/android-settings-os-d355/pro.vafadar.zanance-Signed.apk,
94,822,133bytes, SHA256b00637b5f2099f5fec0a9075c71ff49c8dcbb1b33a32e5452a5a099c0eb280af.
Packagepro.vafadar.zanance, min24/target36, ARM64/x64,384AOT images; no INTERNET/debuggable.
Signature verifies with certificate SHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
All eight original Mono/runtime/cipher native libraries match D-353 byte-for-byte. The first
broad filename comparison accidentally included managed AOT output; the corrected explicit
native-library category is retained in settings-os-d355-main-package.json. No native dependency
or runtime change is introduced. Unit-suite count is unchanged; unrelated passed tests are not rerun.

## Limits

This is scoped actual OS text qualification on the owned emulator, not spoken TalkBack,
360dp/OS display-size, Samsung, biometric, all-Settings, whole-app ANR/GC, iOS or release
acceptance. Main startup and runtime failures remain open. Never infer a whole-suite count
from native cases. No unchanged successful financial/backend/unit suite is repeated.

The main candidate upgrades the existing owned emulator without clearing data. MainPID30201
reaches original English Home/Recorded balance/My account with the complete0.00EUR MoneyText
packet; no Save or financial input is invoked. The first null native-tree observation is retained
as uncertainty, then the same PID supplies Home without restart. Hash-match/remove only its exact
owned hierarchy. Font_scale remains1.0 and no reader service is enabled. Main-return receipt:
artifacts/settings-os-d355-main-return.json.
