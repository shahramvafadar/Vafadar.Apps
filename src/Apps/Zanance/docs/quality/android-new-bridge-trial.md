# Supported new bridge trial remains unaccepted (D-390)

## Distinct hypothesis and exact runtime

D-378 matched an original ARM64 fault to the Tarjan bridge route, without proving its original
cause. The unchanged serviced .NET 10.0.13 source supports a separate new bridge implementation.
This differs from D-347's rejected precise-Tarjan flags and D-384's serial-major/64MiB nursery.
The original general Mono issue remains open; another application's result is not acceptance here.
[Official issue](https://github.com/dotnet/runtime/issues/123537),
[exact selector](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/metadata/sgen-bridge.c),
[exact parser](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/metadata/sgen-mono.c).

Artifact-only targets select MONO_GC_PARAMS=major=marksweep-conc,bridge-implementation=new only
for the independent AndroidHome caller. Keep concurrent major, default nursery, actual compiler/
task/runtime alignment and original production sources. No runtime fork, downgrade, preview,
canary/remset setting, debugger, sampling or raw heap/register/stack collection is introduced.
Packaged environment, fixed managed mode marker and unchanged engine code support this mode;
filtered original runtime warnings contain no invalid, unknown or late bridge-option warning.

## Complete original protected Home cohort

Strict Release/AOT x64 build: zero warnings/errors,115.05s. Immutable signed offline APK SHA256
f35e60e00c0a07c4b2b28d6bd0e74a88b586daab140d1eb5547e5ec914a68220;
55,127,836bytes,192 AOT libraries. Four native engine libraries match ordinary Main byte-for-byte.
It is nondebuggable/nonprofileable. Only the independent package is installed on the owned emulator.

Retained protected run25b9dab64bf849fcb24e39246d773215 and financial baseline
539d2e32456d4109a480eb0d920b66b0 are reused, without prepare/reset/reseed/new keys or financial Save.
Comparison7203c278807d455da80cbd0c03059157 completes all six reference/tenfold en/fa/de cases, two
publications each, in PID15917. The initial observation deadline expires; follow the same confirmed
live process to terminal evidence, without restarting. Original native focus, financial/native/recent
packets, full raw schema and typed rows match. No retained-case continuation or native death occurs
in this finite cohort. This does not establish that a random crash or general ANR is fixed.

| Shape | Full publication samples, ms | Median, ms | Dispatcher median / maximum, ms |
| --- | --- | ---: | ---: |
|10,000 rows|2587.1491,1585.7610,1810.3893,1595.6223,7015.0748,8770.7684|2198.7692|807.5764 /2465.7064|
|100,000 rows|3642.8318,3453.3261,4028.5763,3640.1759,4277.1448,4165.7783|3835.7041|442.1752 /749.7701|

Reuse all historical D-383 default samples from the retained D-384 comparison; do not replay a
successful unchanged baseline. Default gap medians289.8526/254.1783ms and maxima402.2347/299.5722ms
are lower; all twelve corresponding trial gaps are longer. Historical default full-publication
medians992.8371/3274.71765ms include mixed-process continuation after an actual native failure.
These sequential descriptive observations are not a causal or statistical comparison. They provide
no reliable responsiveness benefit and do not meet the two-second target.

Post-terminal aggregate PSS509,172KiB/RSS595,756KiB/swapPSS395KiB is one terminal observation,
not peak, comparative memory, leak, low-memory or long-term stability evidence. No heap is collected.

## Reject, restore and retain evidence

Reject promotion and phone extension. Do not run more root-lifecycle cohorts for a candidate already
failing the responsiveness gate. Do not repeat it unchanged without materially new evidence.
After terminal15917, stop only that independent caller and reinstall the ordinary D-389 caller:
SHA25676d9d298b0d89283ffdc623dad2f51b2663330f56e1bb116407958e5eb784cda, verified installed and stopped.
Main PID15560 remains unchanged. Its exact meaningful native Home/My account/0.00EUR nodes, settings,
size/density and APK SHA256d89eebf4dc9c816ec603bde0c9edaedc2173ba11854af567b23d309e7aa0dd6c match.
No Main reinstall, phone input/lease, OS-setting change, fixture/key replacement or financial Save.
Owned independent Release cleanup succeeds with zero warnings/errors; immutable inputs/results remain.

Local artifacts/home-new-bridge-d390 retain the pre-execution plan, exact source, isolated inputs,
package/mode validation, stages, original result, comparison, gate decision, aggregate memory and
restoration/cleanup receipts. No production source/APK/schema/permission/SDK or managed-test count
changes; no unrelated suite, Windows build or ordinary Main APK is repeated. Main remains D-389 and
Samsung D-385. QA-06/runtime/ANR, final accessibility, wider device/iOS/provider/Store/release gates
remain open. Ready development continues under D-386; unavailable external gates do not justify
repeating rejected GC configurations or claiming all development/release acceptance complete.
