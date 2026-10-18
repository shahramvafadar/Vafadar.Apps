# Real Android OS text scaling and TalkBack qualification (D-345)

## Scope and owner authority

On 2026-10-18 the owner authorizes temporary accessibility changes only on the independent
Zanance emulator, with exact prior settings restored. Samsung, Windows settings, accounts and
actual authentication remain outside the exception. The verified target is emulator-5570,
AVD Zanance_Storage_Review, Android 16 x64, physical 412x800 / density 160. No display-size or
density setting changes. This does not establish 360px, ARM64 phone, other APIs/page sizes or iOS.

The shipped app and complete signed APK remain D-344/37fdf487. Reuse its already built independent
Release/AOT caller and the retained fictitious run 6d7425c5777045b4a6f2806a9cb69dbe. No prepare,
reseed, reset, financial Save, source change, domain suite, Windows rebuild or APK rebuild is needed.

## Actual OS 200% result

Capture exact prior settings before mutation: system font_scale=1.0, secure accessibility_enabled=0,
enabled_accessibility_services absent/null. Set only font_scale=2.0 for this phase. The original
native Activity reports mCurrentConfig 2.0 and 412dp/160dpi; retain that direct configuration evidence.
This is actual OS configuration, without a Debug font override. Android's nonlinear scaling means
configuration 2.0 does not assert a doubled pixel size for every text size.

Comparison 97b2389a77274984ae3385604fe8f2a5, original PID 7868, completes all six en/fa/de xLight/Dark
cases: 36 original Home recent/account, transaction item/day-net and account item/group monetary
viewports. Original full text allocation, horizontal scroll ends, semantic colors, accessible names,
commands/44-unit targets and native selection/deselection pass. The retained accounts, entries,
budgets and settings compare unchanged. ResultNoFinancialSaveOrSystemSettings describes the caller
itself; this external authorized experiment did change OS font_scale and must not be described as
having no system changes.

Activity launch observation times out. An observer also transiently cannot confirm PID 7868;
authoritative pidof and original Activity confirm the same live process. An actual startup ANR is
retained. Select Wait on its exact own-package dialog; the same PID reaches startup-ready and the
complete result. Only after that final receipt is the independent package stopped. Do not restart,
repeat successful contexts or reinterpret these component checks as ANR-free/cold-speed acceptance.

Restore font_scale=1.0 and verify the exact value before TalkBack preparation.

## TalkBack attempt is not accepted speech/navigation evidence

The preinstalled product TalkBack 16.0.0.738667889 is enabled temporarily through its exact service;
dumpsys accessibility confirms the bound spoken/haptic/audible service. The preinstalled Google TTS
version is googletts.google-speech-apk_20241125.02_p2.702443970. Targeted TalkBack-PID logs repeatedly
show binding, connection setup and disconnection from that engine. This is observed connection
behavior, not a proven root cause or an application speech defect.

Capture emulator audio only with the already reviewed official portable recorder, no window,
video, control, desktop playback or microphone. The first WAV contains 4.267 seconds of zero-valued
samples. The next contains 22.507 seconds, peak 579. Native audio input cannot be evaluated directly
in the current execution environment. An existing offline Windows en-GB recognizer returns five
unreliable fragments with confidence 0.0086-0.1508; reject these as a transcript or speech acceptance.
No recognition service, credential, upload, new download or system installation is used. A local
script-file invocation is rejected by the existing execution policy; do not change that policy.
The same ordinary recognition calls execute inline without changing system configuration.

TalkBack's optional notification prompt repeatedly returns. Touch/keyboard attempts do not establish
successful TalkBack app navigation; native keyboard focus only is observed. Notification permission
remains denied, with USER_SET/USER_FIXED after denial. Its pre-experiment permission flags were not
captured, so do not claim their exact restoration. No permission is granted. After restoring the
three captured settings, stop this test TalkBack process to dismiss the repeated prompt and bring
the existing Zanance task to the foreground without restarting it. Bound/enabled services are empty.
The font setting is 1.0, accessibility_enabled 0 and enabled_accessibility_services absent/null, exactly
matching their captured prior values. This does not claim restoration of uncaptured TalkBack-internal
preferences or notification flags. Retain the failed speech attempts rather than rerunning them
unchanged or claiming that service binding, audio energy or native labels prove spoken acceptance.

All 14 own remote hierarchy files are removed only after matching their retained local SHA256.
No main data or key is read, reset or changed. No personal authentication or physical device input.

## Remaining acceptance and next action

A11Y-03 gains real OS 200% evidence for these six original monetary surfaces at 412dp and remains
In progress for other controls, 360px/platform combinations and broader device acceptance. A11Y-01
remains In progress: resolve the actual TalkBack/TTS/prompt delivery path before a complete spoken
navigation pass; preserve the current negative audio/connection evidence. No managed test count
increase, new APK, full-suite rerun or completion claim. QA-06/ANR, the separate Windows Canvas
root-change AV, Mac/iOS, providers, Store accounts and publication remain separate open gates.

Official references: [Android nonlinear text scaling](https://developer.android.com/about/versions/14/features#non-linear-font-scaling),
[TalkBack navigation](https://support.google.com/accessibility/android/answer/6006598?hl=en),
[TalkBack keyboard commands](https://support.google.com/accessibility/android/answer/6110948?hl=en-EN).

Final main-app hierarchy again shows Home, My account and0.00EUR after restoration; the existing
main task is preserved. Only its exact hash-matched temporary hierarchy is removed, bringing the
owned remote hierarchy cleanup total to14.
