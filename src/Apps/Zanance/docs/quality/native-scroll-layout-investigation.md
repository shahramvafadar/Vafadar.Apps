# Native scroll-readiness investigation (D-455)

## Candidate and evidence boundary

The official MAUI10.0.110 ScrollView handler reposts a request while native layout is pending.
Upstream39213 addresses a persistent readiness wait; its inflight change and draft SR12 candidate
are not a released fix adopted by this app. The original10.0.110 pin and matched10.0.13 runtime
remain unchanged. A source resemblance alone does not establish any historical Zanance ANR cause.

The bounded independent control uses real MAUI/native controls and the original completed secondary
source, without seeds or financial commands. It does not substitute application algorithms or run
an old successful scene. No production handler or system/GC setting is changed. The native setup
must actually establish the suspected predicate before any stock ScrollToAsync request is armed.

## Actual original control and incomplete second precondition

Process17676 makes16 bounded RequestLayout calls from actual global-layout callbacks. There are17
callbacks. Of twelve native samples, the first two have IsLayoutRequested=true; the remaining ten
have false. IsLaidOut remains true. Thus the permanent latch is not reproduced and no scroll task
is sent. Complete original secondary-source and independent-base row comparisons pass afterward.
This is a negative setup observation, not passing regression evidence for the upstream retry loop.

The separate never-laid-out continuation requires that exact original stored receipt and does not
repeat the16-request control. Process17848 ends in its actual45-second native readiness refusal
before a scroll task exists. Preserve the typed IOException/retirement packet. The observation is
extended, without weakening the predicate or making a product change, in process18054.

Three actual snapshots in18054 establish the missing condition: virtual visibility is false, but
the control is loaded, has its logical window/handler, and its native view is attached/focused,
not shown, already laid out at412x640 and not layout-requested. Hiding this logical root therefore
does not construct the requested never-laid-out native case. The unchanged readiness predicate
refuses again; retirement still reports no pending scroll task. No timeout is relabelled as a
scroll hang or a successful native test. This does not disprove the upstream defect or exercise
its supplied native pre-layout fixture; it does not justify a production workaround here.

## Build, source retirement and restoration

The three complete independent Release/AOT builds have zero errors/warnings,192 x64 AOT modules,
valid v2/v3 signatures and no INTERNET permission. Durations are2:48.46,1:50.76 and1:53.87.
The original control and two actual readiness refusals are retained with their own identities;
no accepted finance, managed/resource/business suite or successful source matrix is replayed.
No managed-test baseline increases.

After each terminal packet, stop only its exact independent process. Experimental control source
and dispatch/allowlist changes are removed before committing; no temporary native setup remains
in shipping or independent tracked sources. Restore the exact previously installed D-451 signed
APK, hash e55a48f1e3c13abc4144afa326a922dfb117aad7dc2b644208b5a9b3a93df637,
and keep its source/data. Main remains the exact D-450 APK, hash
db7cbd9a3656a28b7771cdc69116f378efa2a11ee6cfb33be594d82f798e85f3.
The same Main17431 and all139 public nodes match, excluding only focus/drawing order. Font1.0,
physical412x800/density160 and accessibility0/servicesnull match exactly. No Main private data/key
is read. Only two exact hash-matched owned remote XML captures are removed; local originals remain.
Samsung is untouched; D-452 keeps its separate pending continuation and coordination requirement.

General ANR/crash, natural low-memory, complete loaded-Home speed, spoken accessibility, remaining
physical/Mac/iOS/provider/store/expert and release gates stay open. Further changes need an actual
application failure boundary or a correctly established new native condition, not a weaker observer,
unreleased dependency or another unchanged successful run.

Primary references: [pinned handler source](https://github.com/dotnet/maui/blob/10.0.110/src/Core/src/Handlers/ScrollView/ScrollViewHandler.Android.cs),
[upstream scroll-readiness change](https://github.com/dotnet/maui/pull/39213),
[SR12 draft candidate](https://github.com/dotnet/maui/pull/38782),
[serviced runtime metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json).

## Later connected/unlaid fixture (D-622)

[New bounded native observations](connected-unlaid-scroll-lifetime.md) establish the
underlying stock handler predicate with a separately visible native child and
suppressed parent layout. They do not turn these old hidden-root setup failures
into successes, reproduce a historical ANR or change the product. No old original
request, retained source or financial operation is replayed.
