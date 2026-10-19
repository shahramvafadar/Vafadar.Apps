# Covered Android startup responsiveness (D-300)

## Original observed problem and complete batch

D-299 retained main Android PID29535 after a failed-to-complete-process-startup ANR, followed the same process
through Wait to actual Home and kept the original account/balance. Activity start27540ms did not measure Home
readiness. Earlier retained events include an input-dispatch timeout in PID24201 and process-startup failures
in PID26893 and independent analysis/market callers. Do not conflate their reasons or call this a database defect
without evidence. The original app is currently ready, with its original protected data/keys and no pending work.

1. Read scoped existing ANR reasons, then measure the actual Release/AOT process phases: application bootstrap,
   framework/DI/initializers, window, covered database preparation, access gate, Shell and loaded Home. Measure
   actual main-thread latency independently from worker completion. Temporary phase markers carry only elapsed
   time/thread/stage and must be removed before the finished commit; no token, key, path or financial values.
2. Correct the measured blocking cause in the real composition. Keep the startup cover until storage and access
   are verified; no moved database/key work into Application.onCreate, skipped integrity, guessed entitlement,
   plaintext fallback, invented data or reset/rebuilt fixture. Keep actual profile/recovery/lock semantics.
3. Test only changed execution/ownership boundaries. Re-measure the same retained native data after the change,
   including responsive cover, actual loaded Home, original data and basic navigation/Cancel. Do not repeat
   successful unrelated cipher/sync/provider/market/layout/whole-suite checks. Any new or failed concern may
   justify its directly affected check, with original failures preserved.
4. Finish strict Windows/Android builds, complete signed APK/signature/package, current documentation, commit
   and push. Keep emulator evidence separate from the actual reference phone, iOS, two-second target and release.

## Current evidence

The first read-only activity lastanr observation reported no retained entry. The existing events buffer is the
stronger source: it records actual main and independent process-startup timeout reasons. Package-scoped exit
history also retains an earlier main ANR trace. No fresh startup or fixture regeneration was used to recover
this existing evidence. Startup source already dispatches protected storage through Task.Run after window
creation; preserve that ownership while measuring earlier bootstrap and later native composition.

The actual first temporary phase package retained main PID30350 and reached Home with the original0.00 EUR
account after its ANR was acknowledged through Wait. Android's process event shows the cold process was first
requested for Plugin.LocalNotification.ScheduledAlarmReceiver, not only the subsequent Activity. Managed
Application.OnCreate occupied12404ms, including registration5015ms and builder.Build2836ms; the App constructor
itself was142ms in this sample. There was also substantial runtime time before the first managed phase marker.
Storage preparation19650ms ran on worker threads, followed by settings5237ms and Home publication7981ms.
Sparse native dispatcher observations mostly remained near250ms while storage worked; one later composition
gap was1809ms. These callbacks are phase/latency evidence, not a GPU-ready, phone or two-second measurement.

A second narrower registration sample retained PID30598. It measured telemetry disable2370ms and key
registration1259ms, with the mandatory original order preserved. Both samples kept actual finance/keys and their
own original process after ANR. An early instrumentation preflight found two matching source anchors before
writing any changes; the unintended unmodified publish was canceled. A shell-quoting failure in a later probe
also caused no build; use the explicit retained script instead. All seven temporary phase source files were
subsequently restored byte-for-byte and the exact ignored helper removed before recording an unmodified app.

The maintained Android SDK settings show RunAOTCompilation=true, AndroidEnableProfiledAot=true and marshal
methods enabled. Investigate an application-specific AOT profile before changing license/privacy initialization,
weakening derivation/integrity, enabling global key-bearing pooling or repeating rejected asynchronous-root
candidates. Installed SDK targets expose profile capture, but their default capture target writes a global
device property and the collector is not installed. That default target is not run. The documented alternative
uses an app-scoped AndroidEnvironment file, so no emulator setting or global property changes. Current official
references: [Mono AOT capture](https://github.com/dotnet/android/blob/main/Documentation/guides/profiling.md),
[AndroidEnvironment](https://learn.microsoft.com/en-us/dotnet/android/building-apps/build-items#androidenvironment),
[per-app execution properties](https://learn.microsoft.com/en-us/previous-versions/xamarin/android/deploy-test/environment)
and [telemetry ordering](https://help.syncfusion.com/maui/telemetry).

Only the local recording package temporarily restores Mono.AotProfiler.Android10.0.0-preview1, the listed
Mono10 development recorder, through normal build restore with a central temporary version. It is not a
production dependency/runtime update, security certification or shipping profiler. The package's prerelease
status is explicit; its capture/consumer compatibility must pass before using any produced method profile.
Only names/types/method metadata on the existing fictitious installation are intended, no values/heap export.
Remove the recording package, central temporary version and app environment before final qualification. Final
candidate behavior/size/startup and source cleanup remain unproven at this point.

## Production compilation and retained native candidate

Capture completed only after real loaded Home in the original recorder process30799. The collected method file
has83 modules,5559 types,24170 methods and3104542 bytes; SHA256
08fbdfe3e358a3bed5dabaebdbf25e9ecc03f6ff0f678ab27befb8160fc2e951. The dedicated9911 forward was removed and the
global profiling property remained empty before/after. No system setting, screenshot policy, heap or secret
was changed/exported. The ordinary production project contains only an additional Release Android
AndroidAotProfile input and retains SDK defaults; the central temporary recorder package was restored out.
All seven temporary phase source files are byte-for-byte restored and their helper is absent.

The .NET10 compiler reader discards captured MVIDs and resolves assembly/type/method signatures; no unsafe token
or identity rewriting is used. The current ordinary compiler successfully consumes the original method profile.
The APK retains the full existing app AOT library; framework/license compilation changes visibly: x64 licensing
AOT increases from9440 to51920 bytes, telemetry6504 to8160, MAUI Controls1955512 to2134808. The corresponding
arm64 libraries also change. Thus compilation is qualified on both shipped architectures, while runtime is
qualified only on the actual x64 emulator. No native recorder library remains in the production candidate.

The strict production candidate is98443821 bytes versus93741613 in D-299:4702208 extra bytes, about5.0 percent.
This explicit package tradeoff is retained instead of weakening secure initialization. Activity start in main
PID31031 is COLD/ok10624ms; the first actual native observation by16728ms still shows the covered preparing page.
Loaded Home with Recorded balance0.00 EUR/My account is observed by33485ms from the original Activity request.
This is an upper bound at a real native observation, not an exact rendering timestamp, two-second objective,
reference phone or statistical performance result. No builds ran during those startup observations. Earlier
recording samples started for a broadcast and have different instrumentation; do not present these as a clean
same-trigger statistical speed ratio. No new ANR appears for this candidate process in the retained events.

The same process opens the actual New expense form and native Cancel returns to unchanged Home without Save.
The first form observer reports null-root; only the read is retried on the same original live process, which then
shows the complete real form. No repeated navigation, fixture, keys, financial writes or source rebuilding.
Windows strict Release also succeeds with zero warnings/errors. No algorithm/schema/UI string changes require
unit/full-suite, localization, layout-matrix, cipher, sync, provider or market test repetition. Historical test
counts remain unchanged. The final ordinary script package, background-start behavior and digest follow below.

## Final ordinary package and background-triggered process

Build-AndroidApk.ps1 -Billing -BillingSandbox produces the final complete ordinary Release package with zero
warnings/errors. Final file: artifacts/android-startup-d300-final/pro.vafadar.zanance-Signed.apk;
98001140 bytes; SHA256625555e6af1546d59f6ddc4b5810adb51071badbb125af4d85072222cc76a5fc.
Its size increase over D-299 is4259527 bytes, about4.5 percent. The earlier candidate also contained478 extra
Kotlin/META-INF build metadata entries, absent in the ordinary script package; the final runtime library sizes
retain the qualified profile compilation. Do not infer deterministic AOT bytes from equal method/library sizes.
Final package identity is pro.vafadar.zanance, min24/target36, arm64-v8a+x86_64. v2/v3 verification succeeds with
the unchanged local test certificate SHA25692cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
This is not store signing. The final APK has no native profiler, recording port environment or packaged.aprof
asset; method metadata is a compiler input only. The ordinary dependency graph has no recorder package.

Upgrade preserves the original installation. It naturally starts PID31252 for
plugin.LocalNotification.ScheduledAlarmReceiver at05:22:25.262, while no Activity is requested. The initial
immediate pid observation is empty; the later events identify the same actual original background process.
Keep it alive: no force-stop, restart, extra broadcast, fixture recreation or key change. After more than the
original startup-timeout horizon, the Activity opens in that same PID with WARM/ok1709ms. First observation
still shows the cover; subsequent actual native hierarchy shows loaded Home/Recorded balance0.00 EUR and the
same account. Thus1709ms is not Home readiness. At05:23:48 the retained events contain zero ANRs for31252 or
the earlier cold candidate31031. These two concrete samples address the observed startup concern, not every
possible lifetime/input-dispatch race, statistical target, phone or iOS acceptance.

Seven temporary phase sources compare byte-for-byte equal to their originals, the helper is absent, central
packages are restored, the9911 forward is removed and the global profiling property is still empty. An attempted
read of a still-writing observation file was incomplete; the completed retained observation is used instead.
No prior successful unit/UI/cipher/provider/sync fixture, whole-suite run or physical-device claim is repeated.
The coherent compilation/native/documentation/package batch is complete locally. General performance, actual
arm64/API24/16KiB, physical device, iOS, upstream native warning review and release remain separate open work.

## D-390 - New bridge does not qualify as a responsiveness fix

An isolated exact-runtime-supported new bridge passes complete original Home correctness, but all
twelve historical corresponding dispatcher gaps are longer. It is rejected; ordinary caller and
same Main/Home/OS are restored, without phone extension or unrelated successful verification replay.
Main/APK remain D-389; runtime/ANR and speed acceptance remain open.
[Complete trial](../../quality/android-new-bridge-trial.md).
