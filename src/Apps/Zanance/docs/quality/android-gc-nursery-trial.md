# Serial major collector with a larger nursery remains unaccepted (D-384)

## A distinct bounded hypothesis

D-346 changed only major=marksweep and retained an actual Home ANR. It explicitly did not change the
nursery size. D-383 subsequently retained native SIGSEGVs on x64 and Samsung using the ordinary
concurrent-major configuration. Investigate the distinct combined major=marksweep,nursery-size=64m
setting only in an isolated compiled caller; do not repeat the unchanged serial/default-nursery trial.
An upstream firsthand report describes this combination, while the maintainer still has no general
Mono fix or conclusive reproduction. That report is a hypothesis, not Zanance acceptance.
[Official runtime issue](https://github.com/dotnet/runtime/issues/123537).

Inspect the exact .NET10.0.13 source commit4099f83977f4d29604a9aeb971fb0a7bc1d5f699 before execution.
Its parser accepts a power-of-two nursery size with m suffix, default1<<22 (4MiB); the64-bit compiled
upper limit1<<35 allows64MiB. The parser fixes both minimum/maximum nursery allocation to the supplied
value. This is not a64MiB cap on the process, managed heap or SQLite cache.
[Exact parser](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/sgen/sgen-gc.c),
[exact limits](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/sgen/sgen-conf.h).

Artifact-only targets select AndroidEnableSGenConcurrent=false and the explicit environment input for
only Vafadar.Zanance.AndroidHome. A copied Activity requires that exact mode and emits fixed known mode/
completion markers; it does not log arbitrary environment contents. No tracked build/source change,
runtime patch/fork/preview, remset/canary setting, heap/register/raw-stack dump, debugger or sampling.
All original D-383 application/Core/Data sources, financial guards, keys and native engines remain.

## Complete original Home on the owned emulator

Strict Release/AOT/trim x64 build has0warnings/errors in130.45s. Immutable signed APK SHA256:
c6f6349e0da052ee496204278f4ee5ee8c862beab4c8b5777abdbf2a4dacc2ea;55,021,340bytes,192AOT libraries.
Four native engine entries match byte-for-byte. It is offline, nondebuggable/nonprofileable with no
native prototype. Actual packaged environment and fixed runtime marker both prove the combined input.
The ordinary Main APK and its concurrent-major configuration are untouched.

Use retained protected run25b9dab64bf849fcb24e39246d773215 and original financial baseline
539d2e32456d4109a480eb0d920b66b0. Fresh comparisonb05b95443e814459981350944b89cfd9 completes all six
reference/tenfold en/fa/de cases, two publications each, in PID9934. Original window focus, balances,
complete financial/native/recent packets and full raw schema/typed rows match. There is no native death
or retained-case continuation in this finite cohort. An observation timeout is followed on that same
confirmed-live PID, never restarted. No reset, reseed, new key or financial Save.

| Shape | Full publication samples, ms | Median, ms | Largest public dispatcher gap, ms |
| --- | --- | ---: | ---: |
|10,000 rows|1547.3277,2878.2012,2011.0180,906.1803,771.2134,515.9195|1226.7540|722.0383|
|100,000 rows|2832.1148,1989.6977,2063.0217,2131.9608,1838.9765,1972.1396|2026.3597|623.1321|

Reusing all existing D-383 samples, without replaying a successful baseline: public dispatcher-gap
medians become289.8526->432.03325ms reference,254.1783->320.61995ms tenfold. Respectively3/6 and5/6
corresponding gaps are longer. Previous maxima402.2347/299.5722ms become722.0383/623.1321ms. Full-page
medians were992.8371/3274.71765ms; these historical default samples mix processes after an actual
native failure. Neither sequential observation nor the apparently shorter trial tenfold median is
statistical causal speed evidence, a two-second result or a reliable stability fix.

Only after the terminal cohort, Android aggregate memory reports totalPSS837,487KiB (about818MiB),
RSS894,116KiB and swapPSS27,661KiB. This is not peak usage, a default-mode memory comparison, a memory
leak diagnosis or low-memory/OEM acceptance. No heap contents are collected. The observed pauses and
unqualified memory behavior prevent promotion on the strength of one clean cohort.

## Actual root ownership and native Back under the new mode

After terminal Home evidence, stop only9934 and launch the same immutable package with a fresh original
root-retirement comparison1a5712d4516842219635ce19a2edda43 in PID10354. This is justified new-mode
lifecycle qualification, not a replay of D-360's already qualified default configuration. Eight original
root/theme replacements all release their captured retired roots, retain exactly one native root
fragment and one original toolbar subscription. Six actual native Back actions cover en/fa/de and
Light/Dark with the complete original translated names. Original stored accounts/entries/settings and
initial language/theme are restored; no financial action or OS-setting change. The actual mode and
cohort completion markers match. Terminal aggregatePSS514,302KiB/RSS600,972KiB/swap429KiB belong to this
different, separately launched workload and are not evidence of a memory improvement.

## Withdraw and preserve ordinary production

Do not extend this unaccepted configuration to Samsung, ship it to Main or run additional cohorts
until something materially changes the responsiveness/stability hypothesis. One passing cohort and
an external report do not outweigh the observed pauses or establish a general GC fix. Retain the
original D-383 SIGSEGVs, native-stage/packet results and the prior D-346 ANR as negative evidence.

After terminal10354, reinstall and verify the ordinary D-383 independent x64 APK
93af429ced01309cc943ccee9f1659504710bc9cb78811f429a601ef9c72bbf0; keep it stopped. Original Main9634,
all meaningful native Home nodes/My account/Home0, APK60af0e7b02826f342efb23f0513b7015ce4c2268402cefd79694d8f8290dccf7,
font/accessibility/size/density match exactly. Remove only the content-matched owned XML. Owned
independent-output cleanup exits0. No phone lease/input, OS change, reset, fixture/key replacement,
Main reinstall, new managed case, unrelated successful suite, Windows build or production APK rebuild.

Local artifacts/home-gc-nursery-d384 retain the pre-execution plan, exact source limits, artifact-only
inputs, immutable package, mode/stage/results, timing/memory summaries and restoration/cleanup proof.
Application/APK remain D-383. QA-06/native GC/ANR, cold/low-memory, two-second target, broader device,
spoken-reader/iOS/provider/Store and release gates remain open.
