# Incremental Android manifest variants and retained runtime evidence (D-359)

## Actual defect and final implementation

An independent Release/AOT caller is built with EnableCodeSampling=true, but its complete signed
APK has no profileable element. The evaluated AndroidManifestOverlay list contains the requested
SamplingManifest.xml. In installed Android SDK36.1.69, _ManifestMerger's timestamp inputs omit
AndroidManifestOverlay. Neither the unchanged generated base manifest nor the SDK property cache
records this project-specific option, so the previous merged manifest is reused. A successful
build and an evaluated item list therefore do not prove the selected packaged variant.

AndroidManifestOverlays.targets runs before that incremental check. Hash the requested project
overlay files in their original precedence order and write their absolute paths/content hashes
to a project intermediate cache only when different. An explicit sentinel represents the empty
list, including removal of the last overlay. Add this cache to the SDK's existing merger input
list. Do not edit the SDK, delete the merged manifest, force unchanged merges, copy manifest
contents/placeholders into the cache, change permissions or enable profiling in the main app.
Import through the existing shared Android runtime targets. Windows does not execute this target.

The focused process regression has seven cases: absent, added, unchanged, same-timestamp content
edit, removal, order reversal and missing requested file. It exercises the real imported target
with the SDK-style input omission, including relative file paths. Missing input fails the native
build; successful completion clears only the deliberately failed final negative command. CI runs
this regression independently of the existing compression/privacy cases. Managed test counts do
not change and no unchanged financial/full-suite tests are replayed locally.

## Real packaged on/off verification

The refused pre-fix candidate has SHA256
54c12aa710c147a4eb3e889ded676796aa70312500e2f7569edae344adde9a50.
It is not accepted as a profiling build. Its original process7756 is retained through the actual
failure below; its missing profiling permission is not worked around with root or system policy.

With the correction, the signed independent candidate has SHA256
f37f6082a499577e1e581efd606fbacbc23633f787804cdb28ef0c758fcfcf94.
Inspect the actual binary APK manifest before installation: profileable shell=true is present,
debuggable/INTERNET absent, the distinct package/signature valid,192 AOT libraries present and
the four Mono/runtime/cipher library bytes identical to D-358. It is never a main-app variant.

Switch the same existing build output to EnableCodeSampling=false without clean. The complete
APK SHA256ca5d82f714e05019f40966afda34d4fc0fa16ad604a4f8e5d8a1e9980f1a64a5
has no profileable element, INTERNET or debuggable flag, retains192 AOT libraries and the same
four native engine files. The intermediate cache now contains only the ordinary release overlay.
This off candidate is inspected without replaying its already completed runtime cases.

## Bounded original binding investigation, not a runtime fix

The original D-358 fatal Delegate.Remove/Translator.remove_PropertyChanged/BindingExpressionPart
finalizer exception remains unresolved. Retain that actual failure, without replacing the ordinary
event, swallowing its exception, warming a generic comparer or changing GC/runtime policy.

Detached original TranslateExtension bindings retire12000 Labels in60 groups, originalPID6117;
the original publisher still notifies. A native modal version initially refuses an immediate
retirement assertion (PID6321); that is not proof of a leak. Correct its observation to await real
native layout/focus and bounded collection of the same controls. PID6490 then retires6000 original
native Labels in30 groups. These controls neither read financial data nor prove whole-app stability.

The closer original GoalEditorPage/theme/language/Shell sequence uses the retained marked D-358
work-editor fixture a56435d26dfc49d38b112da69cf1796a, without reseeding, Save or authentication.
At actual authorized emulator OS200%/412dp, PID6839 completes12 groups/1620 Labels with zero live
retired goal Labels and unchanged accounts/entries/goals/plans/budgets/settings. It then consumes
UI-thread CPU without completing group12. Back produces an actual Android input-dispatch ANR;
Wait preserves the same process, but the ANR recurs. Close only that confirmed failed independent
process; exit-info records reason6/ANR. Do not treat an observation timeout as terminal evidence.

The stage-marked PID7756 similarly reaches group12/goal and records an actual ANR. Its requested
profiling overlay is absent, so do not label it a sampled run. The corrected profileable PID8164
reaches the same boundary. Its bounded20s cpu-clock:user/frame-pointer recording has2823 samples,
zero lost,1325356 bytes, SHA2563454d8d94957392506c47677fa76b9ecc8f7df85a0b62b9fcfdafa719d820265.
No raw memory/register/stack-byte fields, kernel samples, root or security-policy changes are used.
The exact remote capture is hash-matched before removing only its owned path.

Native samples include ART reference-table/collection work and the JNI bridge. The unchanged
Mono ELF BuildId5da5c145fc7fa7ce8afbbdad2d3fa1638294a919 and official debug file SHA256
d0c5cf30116e1e5484cfbc13009bab70f17b1b02bfa58eaedd78924d8e78f183 match GNU debuglink CRCea03bcd7.
Containing symbols resolve offsets2ee3af/2ee387 to drain_gray_stack,2e87aa to major_is_object_live,
and2d69d4 to sgen_collect_bridge_objects. These samples locate collection/bridge cost; they do not
prove the cause of the earlier managed finalizer exception or a production correction. PID8164
also ends with an actual input-dispatch ANR. Preserve all completed groups and failed outcomes.

All three original-page runs restore the captured independent-emulator values exactly after
authoritative ANR closure: font_scale1.0, accessibility_enabled0, absent enabled_accessibility_services,
touch_exploration_enabled0. Temporary diagnostic phases, explicit collections and stage instrumentation
are removed from tracked callers. Original source copies, immutable packages and derived receipts
remain local. Do not expose them as new production behaviour or managed test cases.

## Scoped Samsung continuation

Coordinate the exclusive turn with the concurrent phone project first. Upgrade the already qualified
complete D-358 offline APK on Samsung SM-A176B with install-r, without reset. Explicitly launch only
Zanance; refuse taps outside its foreground package. Original mainPID23656 shows the fictitious
account Zanance_Test_D312_Main and Recorded balance979.00EUR. Open Savings goals through Insights,
open the original Simple New goal, inspect its ordinary caption/native-name hierarchy, scroll the
unsaved form and use its actual Cancel. The original empty-goal state remains; return Home with
the same PID/account/balance. No Save, financial input, authentication, system setting, language,
theme or other project's data changes. The phone turn is released after the final observation.

This is English/current-device-scale navigation and unsaved cancellation evidence. It is not
advanced goal-type/contribution selection, actual OS200%, glyph-fit, TalkBack, all phone scenarios,
the two-second target, resolution of the runtime failures, Mac/iOS or release acceptance.

## Ordinary final package and restoration

The main source and financial model remain D-358. The strict Windows build finishes in32.97s
with zero warnings/errors. Build-AndroidApk.ps1 -Offline succeeds under the strict Release gates.
Its complete signed APK is artifacts/android-manifest-overlays-d359/pro.vafadar.zanance-Signed.apk,
94822133 bytes, SHA2560fddb92e904ff085584e1f3d6c8e9e722a6ee72a2062a2436492ef35829d23cb.
Certificate SHA25692cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b,
minAPI24/target36, ARM64/x64 and384 AOT libraries are verified. No INTERNET/debuggable/profileable
flag is present; the entire binary AndroidManifest.xml and all eight Mono/runtime/cipher entries
match D-358 byte-for-byte. A metadata observer initially uses the legacy sdkVersion label; correct
only its assertion to the actual aapt2 minSdkVersion field against the same immutable APK.

Install-r on the owned emulator preserves data. Ordinary mainPID8735 shows Recorded balance0.00EUR
and the existing to-My-account caption without Save. Restore the independent install with its
qualified profiling-off APK, without clearing its original fixtures, and stop only the identified
unmeasured notification-receiver process if one starts. MainPID8735 remains unchanged. The final
own-window XML SHA25623dfb32213adb0d40635f185d14957679f41edaeb93c282a6064cd21d13b2f2f
and four exact OS readbacks establish restoration. Three owned remote XML paths, including the
initial phone observation, are hash-matched before removing only those files. The phone turn is
already released; this cleanup sends no further phone UI input.

This closes the incremental-overlay defect, not QA-06, GC/finalizer/ANR, complete physical-device,
spoken reader, Mac/iOS or release acceptance. Hosted confirmation follows this commit; the last
pre-change commit0d75361 already has successful CI/CodeQL and is not polled or tested again here.
