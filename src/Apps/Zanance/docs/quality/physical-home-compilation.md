# Profiled Android Home under retained load (D-320)

## Completed native scope

The approved Samsung SM-A176B, API36, ARM64, 4096-byte pages retains run
2566e73c942f49daaf33d58d0b0e2be4, its ciphertext, identities, encrypted rollback and keys.
Reference is 10,000 entries/20 accounts/100 schedules; tenfold is 100,000/200/1,000. Exclusive input
is coordinated with the other project. No fixture regeneration, financial Save, OS setting, authentication
bypass, global profiling property or screenshot-policy change occurs.

The independent caller now uses the production managed AssemblyName and current AndroidAotProfile input,
while retaining its distinct Android application id/storage. Source/native bytes and RunAOTCompilation alone
were insufficient compilation parity: old Data/Core AOT images were 779,216/159,224 bytes versus main
D-319's 1,429,000/835,624. Existing startup metadata already includes the complete Home reader and VM paths.
Earlier D-314/315 timings describe those independent builds, not production compilation or a main speed ratio.
Their functional preservation and relative same-compilation read comparison remain historical evidence.

## Observed failures and final configuration

Public VM/property/collection boundaries identify 5,242-6,486ms before the first tenfold quick-target caption
in baseline 39a9c08aebd2428e820192f0044d5780 (PID21795). Two materialization candidates preserve focused
behavior but show no total gain; both are completely withdrawn. Data C# and unit tests match D-319.
Their candidate receipts, initial fixture errors, corrected checks and temporary private-method probes
remain local negative evidence, without a shipping algorithm or new whole-suite count. Test output is cleaned.

Adding the original profile without matching the assembly ends in a native SGen assertion (PID27926).
The first assembly/profile-parity attempt PID30862 refuses the layout boundary; the display is subsequently
observed Dozing. Keep only explicit independent review windows awake; preserve system locks/settings.
With the display awake, PID32127 reproduces the SGen assertion, so display state is not its explanation.

A separate build defect pairs packaged Mono 10.0.13 with installed compiler/task 10.0.12. Restore matching
compiler/task packages from the central servicing pin and select them fail-closed, leaving installed workloads
unchanged. The initial missing path separator is rejected before compilation and corrected. Task product
version 10.0.13+4099f83977f4d29604a9aeb971fb0a7bc1d5f699 and compiler --version 10.0.13 are verified.
This alignment alone does not fix the failure: PID2146 becomes authoritatively ANR on Back, then only that
independent process is stopped; same-comparison continuation PID5310 skips completed groups and later
SIGSEGVs in a worker inside libmonosgen. Its native library hash exactly matches the official restored 13
runtime. Preserve the scoped exit/events/native frames. No root access or heap dump is taken.

The controlled next candidate removes only additional System.Private.CoreLib method roots from the captured
profile, preserving SDK default profiles, other application/framework roots and all runtime/GC/crypto settings.
Use the compatible .NET10 profile reader/writer, not manual tokens/MVID edits. The older utility's skip option
produces a no-op file; hash/count inspection detects it and that package is never installed. The compatible
negative module filter produces 24,359 -> 18,833 methods, with zero additional CoreLib method records.
Dependent CoreLib type/module metadata remains legitimate. Original startup input: 3,197,424 bytes,
SHA-256: 454e9b35b75bc7ee68233a2c5dd87b2861116862d7aa61cf71e2a34072e9e05d. Qualified input: 2,631,193 bytes,
SHA-256: 51a838bac4bffe91c093fcae7fd26f9b5bc5cdb57cba251bf4f2bd89dcd09231.

## Same-file physical result

Fresh comparison fec3dfdc08e148f39d36f7e60229f805 (PID7344) completes all 12 native publications,
original full financial presentation/native balance checks and complete raw schema/typed-row preservation.
The original Data module identity remains 6478b9a5-5a00-404b-a1ba-ab61777caf38. Public diagnostics carry
only exception type/source stack or phase/shape/language/sample; never messages, keys or financial packets.
No shown failure or matching ANR/crash event appears for this completed process. It is then stopped normally.

| Shape | Language | First / second loaded publication, ms |
|---|---|---:|
| reference | en | 1957.21 / 2138.91 |
| reference | fa | 2362.67 / 2652.33 |
| reference | de | 2651.83 / 2629.49 |
| tenfold | en | 8830.24 / 9022.08 |
| tenfold | fa | 9999.27 / 10292.73 |
| tenfold | de | 10134.90 / 10101.95 |

These timings do not show a general speed improvement. Reference remains about1.96-2.65 seconds and tenfold
8.83-10.29 seconds. Boundaries include original navigation/read/projection/publication and Android native
layout/frame callbacks; they approximate stages, not private-method timing or GPU completion. Extra raw
native stepping, input-tamper, writes and unrelated successful UI/provider/crypto matrices are not replayed.
This controlled compilation change avoids the observed repro; it does not diagnose every upstream GC bug.

## Final main-package qualification

Windows strict development build: 0 warnings/errors. The ordinary complete Release APK build passes its
warning gate. Its restore boundary initially defaults to Debug, which would omit the conditional release
compiler downloads on a fresh machine. Pass the chosen Configuration into restore; verify that exact Release
restore and all three exact 13 download dependencies without repeating the already successful publish. The
APK script parses. The companion sync caller's selector also resolves the 13 toolchain; its unrelated suite
is not replayed. Installed workloads and the central servicing version remain unchanged.

Main APK: artifacts/android-aot-runtime-d320-qualified/pro.vafadar.zanance-Signed.apk;
94,699,253 bytes; SHA-256: 8850a54474a1053717239b06d4ec46f40d2a195c3f83253a3f309a7d351988c0.
Permanent package pro.vafadar.zanance, min24/target36, arm64-v8a+x86_64, 384 AOT libraries, no INTERNET,
debugging, recorder/compiler/profile asset. v2/v3 local test signature SHA-256:
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. ARM64 SQLCipher remains
48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c; native Mono matches restored 13,
SHA-256: 6532983fcd509af0c6fac4d39c37764128f5b12feedf9a9b32079635f70fc3e2.

Upgrade without reset starts main PID9681 and reaches real Home with the retained 979.00 EUR. Actual New
expense opens; its first null-root observation is retried on the same PID without repeating navigation.
The original native Cancel returns to 979.00 EUR without Save. A host read-variable error after Cancel is
corrected by rereading that same captured hierarchy, never tapping Cancel again. No matching main ANR/crash
event is observed. These are displayed main values, not a new full main-database equality claim. The
independent package is stopped, main remains on Home and exclusive phone use is explicitly released.


Local evidence: artifacts/phone-home-stages-d320. No financial/model/native-cipher algorithm change, whole
unit-suite rerun or count increase. QA-06 remains In progress: remaining latency, full loaded cold start,
ANR/low-memory statistics, all phone scenarios, API/page-size matrices, iOS and release are separate gates.
