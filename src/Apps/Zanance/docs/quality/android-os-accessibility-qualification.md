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

## D-346 native speech control and bounded follow-up

Exact prior TalkBack PID3076 reports TTS-not-ready; enginePID2162 logs TensorFlow Lite null-tensor
errors and synthesizeWithoutLoadingVoice failures. These are observed engine/client errors, not a
proven app defect or proof that every synthesis fails. Capture five absent secure TTS keys before
opening the original Android Text-to-speech output settings. That activity automatically persists
tts_default_synth=com.google.android.tts. No voice/rate/pitch slider, reset, download or account is
used. Its Play action produces a14.805-second48kHz stereo recording with peak818. The existing
offline en-GB recognizer initially gives a low-confidence near-match. An analysis-only gain31.785
on a derived mono WAV, without changing device volume, yields the exact native sample sentence
"This is an example of speech synthesis in English" at confidence0.2225. This is limited control
evidence, not an accepted full app speech pass. It contradicts a permanent all-synthesis-failure claim.

With the material native-settings/default-engine change, retry TalkBack once on the existing Main
Home. New TalkBackPID9847 binds spoken service, while Main remains foreground. The documented
read-from-top shortcut and one next-item swipe produce16.64seconds of exactly zero audio samples.
The same fresh TalkBack PID again reports TTS-not-ready and engine connection setup/disconnection.
An explicit default engine therefore does not fix this client path. Do not retry unchanged, edit
Zanance text for it, claim a definite root cause, or introduce another speech engine/network source.

Restore all five captured secure TTS keys to absent and the captured font/accessibility settings to
their exact prior values; readback verifies them. Stop TalkBack only after disabling the test service.
The two exact owned control/current hierarchy copies are deleted only after local/remote SHA256 match.
Native settings initial observation returns null root; observe the already launched activity again,
without recreating it. The subsequent hierarchy confirms its original controls. No app code/APK,
financial values, main profile, device volume, OS density, physical phone or account changes.

## D-346 real OS matrix and exact native failure

The retained fictitious run6d7425c5777045b4a6f2806a9cb69dbe/financial-tables.db contains one account
(opening10000 minor units) and one expense1234. No prepare, reset, reseed, rekey or Main-data access.
This presentation fixture is ordinary isolated SQLite; do not call it the encrypted application.
Actual emulator OS font_scale2.0 and Activity width412dp are required. Each completed group retains
six en/fa/de xLight/Dark cases and unchanged original stored settings/accounts/entries/budgets.
Independent immutable group receipts survive an actual failure without repeating successful work.
New receipts also bind the exact MONO_GC_PARAMS; different GC modes cannot reuse old passes.

Default concurrent-major comparisonf388c3776bc740f194612197db7fb657 first completes tables
(48 monetary viewports) and report legends(12), PID10314. Preserve its startup ANR/Wait and later
persistent actual ANR in the budget group. Stop only that independent unresponsive process after
preserving successful receipts, not because an observation expires. Restore the captured settings.
A sampling request is initially refused because the ordinary caller is not profileable; do not bypass it.

Explicit sampling overlay is independently opt-in, default off, with shell profileable=true and
no debuggable or Internet permission. Strict Release/AOT build passes with zero warnings/errors;
192 x64 AOT images, same development signing identity and unchanged Mono/SQLCipher native bytes.
Resume only missing default-mode groups with original fixture/comparison, PID11349. All six budget
cases pass12 growing captions with unchanged inputs. Home then suffers an actual main-thread
SIGSEGV(SEGV_MAPERR); preserve the process-death event and code frames only. No registers, heap,
raw stack bytes or memory dump are collected. Home/plan groups do not pass from this failure.

The runtime10.0.13 native ELF BuildId is5da5c145fc7fa7ce8afbbdad2d3fa1638294a919. Official Microsoft
symbol-store debug data matches both that BuildId and GNU debuglink CRCea03bcd7; SHA256 is
d0c5cf30116e1e5484cfbc13009bab70f17b1b02bfa58eaedd78924d8e78f183.
Full symbols/DWARF place PC0x2c3b3d in push_object+45, inlined is_opaque_object, raw source
sgen-tarjan-bridge.c:594:55, the vtable gc_bits read. Official runtime source commit is
4099f83977f4d29604a9aeb971fb0a7bc1d5f699. Other native frames are bridge processing, nursery
collection and allocation. This locates the fault; it does not identify the corrupt managed object
or prove the complete root cause. Nearest exported-symbol guesses are explicitly insufficient.
The public pyelftools0.33 wheel is hash-pinned and zip-imported for ELF/DWARF parsing in one local
process, without installation or machine configuration changes.

[Official symbol key format](https://github.com/dotnet/symstore/blob/main/src/Microsoft.SymbolStore/KeyGenerators/ELFFileKeyGenerator.cs),
[matching runtime source](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/metadata/sgen-tarjan-bridge.c).

## D-346 rejected serial-major GC trial

The installed Android SDK defaults AndroidEnableSGenConcurrent to true. The documented supported
.NET10 Mono setting false changes only MONO_GC_PARAMS to major=marksweep. A fresh strict caller
build passes with zero warnings/errors; native Mono/cipher hashes, signing and AOT remain unchanged.
The actual packaged libxamarin-app.so contains the serial-major string and no concurrent-major
string. No nursery-size/bridge algorithm, runtime patch, crypto, SDK downgrade, fork or preview
runtime change is introduced. Production remains unchanged.

New comparisonc1c0a3b9610344228d48d60eaaa16c5c on the same fixture, PID12074, completes the same
three groups with exact serial-mode receipts:60 monetary viewports and12 captions. Home then stops
responding; actual Android input-dispatch ANR is retained, not merely an observer timeout. A bounded
sample during the earlier progressing budget case has124 samples/no loss; a separate sample at the
persistent Home stall has542 samples/no loss, primarily ART indirect-reference/GC work plus Mono
collector functions. These are code-address samples, not complete managed-object/root-cause proof
or comparable performance benchmarks. Both perf masks487 exclude RAW, user/interrupt registers and
user-stack bytes. Exact owned remote recordings are hash-matched before deletion.

Abort only the actual unresponsive independent process, preserve all completed receipts/failure
metadata, and restore font_scale1.0/accessibility_enabled0/services absent. The serial-major trial
has not resolved the remaining failure, so do not adopt it in Main or claim native/ANR stability.
Isolate only the two uncompleted presentation groups next; do not repeat passed financial, cipher,
backend, OCR, unit suites, Windows matrices or the D-344 Main APK build.

[Supported Mono GC setting](https://learn.microsoft.com/en-us/dotnet/android/building-apps/build-properties#androidenablesgenconcurrent),
[related upstream bridge crash](https://github.com/dotnet/runtime/issues/123495),
[upstream investigation and trial guidance](https://github.com/dotnet/runtime/issues/123537).
The related upstream reports do not establish that every native crash has the same cause or that
this candidate is fixed by another user's settings.

## D-346 standalone missing-cohort attempt and final restoration

After stopping only the serial-trial ANR process, restore font1.0, install-r the ordinary default-GC
caller without resetting its data, then temporarily set OS2.0 for one missing Home cohort only.
Same fixture, fresh comparison216e5aa21d3642eda57c3814ea380c29, originalPID12907. Preserve the
actual startup ANR. A proposed abort guard refuses to stop when a fresh authoritative read shows
startup-ready on that same live PID; no restart occurs. The original Android ANR dialog is later
observed with its exact app title/Wait control. Invoke Wait once. The same process then has another
actual input-dispatch ANR and the native dialog recurs; there is no completed Home report.

Stop only that persistently unresponsive independent process after retaining both timed ANR events,
startup-ready evidence and native dialog observations. The untouched plan cohort is not attempted
through the same known failing path. Neither Home legends nor plan rows gains realOS200% acceptance
from this turn. The prior component-level and D-345 ledger results remain valid and are not replayed.
The standalone failure means accumulation alone is not established as the cause; do not call the
runtime crash or startup/ANR fixed or blame a particular managed page without further evidence.

Final readback verifies font_scale1.0, accessibility_enabled0, services absent and all five captured
TTS keys absent. The three standalone hierarchy copies, serial-trial hierarchy and both code-only
sample recordings are removed only after exact local/remote hash matching. Early refused recording
and null-root observations are retained; no unowned/unknown path is deleted. The unchanged Main
PID7654 is brought forward using its existing task, without reset, reinstall, financial Save or
personal authentication. Production Mono GC policy and D-344 complete signed offline APK are unchanged.

A11Y-03 remains In progress: this turn adds60 monetary viewport checks and12 growing captions at
actual OS200%/412dp; it does not accept all controls,360px, Home/plan real-OS cases, spoken readers,
physical devices or iOS. A11Y-01, QA-06/ANR, the separate Windows Canvas AV and external release
acceptance remain open. No new managed test count, unchanged suite rerun or Main build is claimed.

## D-347 precise-Tarjan investigation and uncovered-window guard

Official .NET10 release metadata on2026-10-18 still lists10.0.13 as the latest security/runtime
patch. Do not downgrade or adopt a preview runtime. In the related upstream issue, the runtime
maintainer recommends disabling two Tarjan optimizations as a separate trial. The exact runtime
source accepts disable-non-bridge-scc and bridge-require-precise-merge. Preserve the original
concurrent major collector and crypto/runtime bytes; do not change bridge implementation or
nursery size. This is a scoped hypothesis, not proof of the entire corruption cause.

EnablePreciseBridgeReview=true includes a nonsecret environment file only in the independent
AndroidHome caller. Default is off; Main has no reference to this candidate file. The first compile
rejects ambiguous Environment because MainActivity imports Android.OS; explicitly qualify System.
Environment and repeat only that failed build. Strict Release/AOT final build passes0warnings/0errors
in2:09.52. Signed x64 APK is54,562,588bytes/SHA256
899d91115d993a88e4b6f0d476f27f8a40401cb66bd2dcaf4515d73bfbda4aa1,192AOT images,
profileable trial only, no Internet/debuggable, same signing identity and unchanged Mono/SQLCipher
bytes. Both generated/package environment and actual runtime stage confirm the exact precise mode.

Retain the same ordinary fictitious financial-tables.db, no prepare/reset/reseed. PID14245 Home
comparisonbf59f81d7b6d4416a0f03968c29cdf5d completes six cases/12 monetary observations after
the original startup ANR and one observed Wait. PID14641 plan comparison8c586911eb2b4131ae80716c4f901c77
also returns six cases/12 monetary observations with original targets/identities/inputs intact.
However, its final native hierarchy still shows the Android ANR dialog. These unguarded receipts
are provisional component geometry only, not unobscured-window or usable-flow acceptance. Do not
call startup/ANR fixed, infer production adoption, or erase the negative observation.

Strengthen only the independent native observer: await actual Activity.HasWindowFocus before each
context, and require focus at every rendered-caption assertion. Retained cohorts must prove this guard
as well as matching comparison, actual OS2.0/412dp, GC mode, six cases and unchanged inputs. This
prevents an older covered-window receipt from being reused as stronger evidence. Requalify the
previously failing long sequence under the changed runtime/focus contract; unchanged domain,
backend, OCR, full managed suites and the shipping APK are not repeated for this observer change.

[Current release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json),
[maintainer trial guidance](https://github.com/dotnet/runtime/issues/123537),
[exact runtime parameter parser](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/metadata/sgen-bridge.c).

### D-347 guarded sequence rejects the runtime candidate

Guarded comparison13bb484bdb0a4d31bf06cbd0c962c3b5 uses originalPID15055 and the same retained
fixture. After the actual startup ANR, the observer logs await-original-window-focus and makes
no financial text assertions while Android's dialog covers it. An observed Wait restores focus
on the same PID, then the table and report-legend cohorts complete: six cases each,60 readouts,
explicit focus contract and unchanged inputs. One observer initially assumes a single log chunk
for the legend packet; decode both existing chunks without restarting or repeating either group.

The last German/Dark budget navigation then stops advancing. Before any native input, verify
that this caller owns the foreground Activity. Send Tab only, without a financial command.
Android records an actual input-dispatch ANR after5014ms for that key event. Preserve that event,
startup failure, stages and both completed guarded group receipts. Stop only the persistently
unresponsive independent PID after retaining the actual failure; no observation timeout is used
as a restart reason. Budget/Home/plan groups are not accepted from this complete-sequence trial.
The precise-Tarjan policy therefore does not resolve the remaining runtime/ANR problem.

Remove the experimental environment input/property and trial-only policy marker. Retain their
exact public candidate value only in ignored local evidence. The delivered change is the native
focus/provenance guard, not a production GC workaround. Preserve public recognized GC mode and
actual OS scale/width in ordinary presentation reports without logging arbitrary environment text.
Require focus even before constructing the original review root, so a covered window does not
accumulate more review controls while awaiting Wait.

The ordinary caller strict Release build passes0warnings/0errors in1:44.13. APK is54,165,106bytes,
SHA25698bbc4d03fba46a928e15a3f30dcdcc079aa4947b2b22461989d98ac91d54237,192AOT images,
unchanged Mono/SQLCipher bytes and original development certificate. Generated/packaged GC mode
is major=marksweep-conc; rejected flags, profileable, debuggable and Internet permission are absent.
No Main source, runtime policy, native package, financial/schema/translation or APK change occurs.

After the rejected trial, readback restores font1.0/accessibility0/services absent and all five
TTS keys absent. MainPID7654 remains alive. Three exact owned hierarchy copies are removed only
after local/remote SHA256 matching. Ordinary missing-cohort follow-through below is a new scoped
qualification of the changed observer/foreground contract, not replay of an unchanged passed suite.

### D-347 ordinary Home follow-through

Ordinary comparisonacb789f0e2994b98a701b6de4a9d3d5a/originalPID16074 uses the restored SDK default
major=marksweep-conc and the final foreground guard before root construction. Six en/fa/de xLight/
Dark cases complete12 original Home category/total amount viewports at actual OS2.0/412dp. Each
case retains complete name/percentage, native accessible identity, original command/target and
hidden/visible complete-source behavior. Original stored inputs remain unchanged. The new final
report explicitly requires actual window focus; no ANR/process-death event is observed for this
original run. This is scoped component acceptance, not a general native/ANR fix or speed benchmark.

Stop the completed independent process only after validating its final public report, then qualify
the still-unaccepted Home-upcoming/Plans hosts separately with the same ordinary APK/retained data.
Do not reuse the earlier unguarded or experimental-policy plan packet as default foreground proof.

### D-347 ordinary plan completion and final state

Ordinary comparison70e60b0503ce43c1b5288f624055ac12/originalPID16301 completes six cases and12
monetary viewports in both original Home-upcoming/Plans hosts. Actual OS2.0/412dp, default
major=marksweep-conc, original-window focus contract, complete native accessible identities,
commands/targets/date/icon variants and unchanged stored inputs pass. No financial command is
invoked. An existing final report is read rather than recreated; the reader correctly refuses an
attempt to overwrite it. There is no process restart or repeated qualifier because of that guard.

The final native hierarchy contains the independent app and no ANR controls; the original PID is
alive. No ANR/process-death event is observed for either ordinary HomePID16074 or planPID16301
through their final checks. Together these ordinary runs provide24 monetary observations across
12 contexts under the actual foreground contract. They do not pass a combined whole-app stress
run, prove a runtime fix, or replace retained D-346/D-347 native failure evidence.

Stop only the completed independent process after validating its public receipt and final window.
Restore font_scale1.0/accessibility_enabled0/services absent and all five captured TTS keys absent;
readback matches the original values. Hash-match/remove the exact owned final hierarchy. MainPID7654
still exists and its existing task is brought forward without reinstall/reset, authentication or Save.
The rejected trial's three other hierarchy copies were already hash-matched/removed.

No actual Samsung, Mac, provider/account, Windows setting, OS density, private key or owner data is
changed. Main/APK and managed count remain D-344; no successful unrelated suite, Windows matrix
or Main build is replayed. A11Y-03 gains ordinary foreground Home/plan OS evidence; other controls,
360px/platform combinations, spoken readers, native crash/ANR, phone/iOS and release remain open.

The final restored Main hierarchy again shows the app with0.00EUR and no ANR controls on the same
PID7654. Its exact temporary hierarchy is hash-matched/removed, bringing D-347 owned hierarchy
cleanup to five files. This is final-state restoration evidence, not a new startup/performance pass.

## D-348 remove observer suppression and exercise the retained Main reader

The installed Android16 /system/framework/uiautomator.jar is412874bytes, SHA256
 a76f2710634810ea17a0f84f8195e8d2550aaffd5ef6d71e8ef7b663593d3f17.
Its UiAutomationShellWrapper.connect bytecode invokes UiAutomation.connect() without flags.
The official [platform contract](https://developer.android.com/reference/android/app/UiAutomation#FLAG_DONT_SUPPRESS_ACCESSIBILITY_SERVICES)
requires FLAG_DONT_SUPPRESS_ACCESSIBILITY_SERVICES to retain other accessibility services.
Stock observations can therefore invalidate prior TalkBack lifecycle/speech observations; repeated
reader reconnects are not sufficient evidence of an original Zanance defect.

The independent eng/benchmarks/Zanance.AndroidAccessibility shell observer explicitly connects with
flag1, bounds owner packages/nodes/depth/text/events/time, redacts password values, separates actual
accessibility focus from keyboard focus, and has no input/screenshots/private files/network. Installed
Java21 compiles release11 with -Xlint:all -Werror; installed d8 produces minAPI24 DEX. Final4527byte
JAR SHA2564fdc665b69dc7781b32b004573d862513d0225c0a32cf612c6b90e982243fcd4 passes a final
real Main snapshot. No app dependency, permissions, runtime, Main code, APK or managed count changes.

Initial shell observations failed because no main looper existed and root metadata was not ready.
Match platform shell looper initialization and bound same-client root retries. A later observation
failed because a snapshot attempted registration while events still owned Android's single client.
Await that listener before the snapshot. Preserve these observer failures separately; MainPID7654
was never reset. UTF-8 decoding also failed before input and was corrected without restarting Main.

TalkBack16.0.0.738667889 and installed Google TTS are retained. Public TalkBack master16.2 is not
asserted to be the installed implementation. With the service-preserving observer, the live reader's
TTS connection stays established through the first paired capture until deliberate restoration.
Nominal output/playback captures after a keyboard shortcut have zero energy and no observed focus
events; actual packet durations are shorter than requested. Neither proves permanent engine failure.
Do not repeat the unchanged TTS control or grant notifications to manufacture acceptance.

The corrected gesture run waits for262144 captured bytes before input, checks Main foreground,
then sends three right swipes without activation or financial Save. Awaiting the15second listener
avoids the simultaneous-client failure. Before focus is empty; the final real accessibility-focused
node is Button Recorded balance at[33,105,379,149]. The event listener still emits no focus event;
its empty result cannot establish that focus did not change. The playback WAV has573440frames,
48kHz/stereo16bit, peak18947 and417161 nonzero samples. This establishes captured audio, not its
correct spoken content. Offline en-GB dictation on a derived mono copy yields two incorrect phrases
at confidence0.014/0.023: reject those transcripts. No cloud transcription, microphone, volume,
engine download or notification-permission change is used. Spoken-reader acceptance remains open.

Finally read back every captured secure key and font_scale1.0 exactly, preserving absent keys as
absent. Notifications remain denied with unchanged flags. The existing MainPID7654 remains foreground,
and its final owned snapshot contains0.00EUR. Hash-match/remove the exact transferred observer JAR.
No Samsung, Windows setting, auth, account or personal data is touched. Relevant Java/DEX compilation
and actual native observation are checked; unchanged managed suites, Main/Windows builds and prior
passed OS-layout cohorts are not replayed. A11Y-01/03 and native ANR/QA gates remain open.
