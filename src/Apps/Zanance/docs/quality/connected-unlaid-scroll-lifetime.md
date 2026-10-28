# Connected but unlaid native scroll lifetime (D-622)

## Named missing predicate and exact scope

D-455's original hidden-root attempt was already laid out. Its three readiness
snapshots and two refusals remain failures; neither old case is replayed or called
successful. D-622 constructs a different, explicitly visible native child whose
actual parent temporarily suppresses layout. This establishes the underlying
stock-handler predicate, not D-455's additional hidden-root condition or a
production Zanance failure. No financial application, existing workload or private
Main source is required.

The current10.0.120 handler defers a scroll request when the native view has not
been laid out or still requests layout; its posted continuation checks connection.
The original public operation and stock handler are used, without a copied mapper,
replacement completion callback, CPU sampler or modified framework. Native parent
suppression is a fixture-local API, never a production workaround.

Primary references: [exact framework revision](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Core/src/Handlers/ScrollView/ScrollViewHandler.Android.cs#L144-L177)
and [Android native layout suppression](https://developer.android.com/reference/android/view/ViewGroup#suppressLayout(boolean)).
The runtime framework informational revision is actually
10.0.120+6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0; retained tag and immutable-revision
source bytes match. This is framework provenance, not proof of shipping-linked PE
identity or any exclusive historical ANR cause.

## Independent package and admission failures

Create one fresh isolated API36/x64/4096 AVD, Zanance_D622_UnlaidScroll, using the
already installed official SDK. Original1080x1920/density420 and all five captured
settings remain unchanged; camera inputs are disabled. Boot is
4987e161-e13e-41bd-88a9-a15c694bf5cc. Initial ADB observation cannot reach the still
booting emulator, then boot is genuinely pending; inspect that same live host and
continue after its original boot, without a restart. Actual boot takes90840ms.

Only a minimal independent MAUI application is compiled. It references no Zanance
Core/Data, store, financial factory, original startup or resource fixture. First
strict compilation refuses four CA1416 sites because native SuppressLayout requires
API29. Add an explicit actual-API runtime guard before the workload, preserve that
failure and compile the corrected source strictly without errors/warnings. Product
minimum API24 and its source remain unchanged; this experiment only runs on API36.

The complete independent APK is17936026bytes, SHA256
50b5744802003d4e319f2091c25c1b348ed27f3e8381e2cb96b26e0bb551c89c,
with90 own AOT modules,100ELFs and390 statically16KiB-aligned LOAD segments. CRC,
no INTERNET/debuggable and v2/v3 verification pass. This isolated package uses the
SDK's Android Debug certificate, not a claimed shipping owner identity. Static
alignment is not hardware16KiB, phone or product-package acceptance. The used APK
is retained in E:/Zanance.ReviewBuilds/D622-native-package.

Require initial absence including retained data, install only the fresh
pro.vafadar.zanance.scrolllayoutreview identity once, and use its ordinary launcher
without source/seed/review extras. Original actual PID4105 stays alive through both
bounded cases. The startup label and native Activity root belong to this package.
The real MAUI control is converted through its original handler and added as a
native child to the temporary Activity host; it is not an original financial page
or a logical ScrollView inserted into Zanance's normal page hierarchy.

## Two original native requests; no successful replay

Before either request, require actual attachment, window focus, IsLaidOut=false,
IsLayoutRequested=true and native size0x0. Both controls are visible. The actual
stock ScrollToAsync(0,250,false) is issued exactly once per admitted case. There is
no synthetic financial setup, frame counter or request retry.

| Distinct bounded case | While parent layout is suppressed | Terminal observation |
|---|---|---|
| Release layout | Still attached/focused,0x0,never laid out,layout requested; original task pending after the nominal250ms hold | Releasing suppression produces actual1080x1920 layout, clears layout request, scrollY657 and successful original task completion;275.7752ms from request |
| Retire before layout | Same actual predicate and pending original task after nominal250ms | Disconnect original handler, remove child and release suppression; after another nominal250ms the task is still incomplete and platform handler disconnected;503.501ms from request |

These intervals describe only this fixture's bounded observations. There is no
claim of infinite task lifetime, actual repost count, exclusive CPU load, ANR,
whole-Home latency, physical responsiveness or a reproduced historical crash.
No global timeout, cancellation shim, polling handler or shipping layout policy is
introduced from this artificial setup. A product correction still needs a named
original application call/lifetime failure and its own affected verification.

The result's Success flag alone is not admission. A separate retained-result check
verifies every before/held native predicate, task outcome, actual framework version,
empty loaded financial-assembly list and original package/PID. The first terminal
observer incorrectly forbids every ScrollView in the Activity hierarchy and fails:
MAUI's normal navigation_layout is itself a ScrollView. Preserve that refusal and
interpret the same saved XML. Android content has exactly its original single
navigation_layout child and original label, with no temporary host/content. No
native request, layout case, screenshot or financial operation is repeated to repair
the observer. All five actual settings and original size/density match afterward.

## Exact retirement, cleanup refusal and phone handoff

Hash-match and remove the1938byte owned result plus one5298byte terminal XML; retain
local originals. Uninstall only the new package including retained data, verify
absence, stop only the fresh AVD, and prove serial/ports absent. Official AVD deletion
retires25files/5077087259 logical bytes; the older retained AVD configuration remains
byte-exact. No physical Samsung, D-607, retained source or existing ADB server is
queried or changed. Standard Clean removes463 registered files/79548944 logical bytes.

Supplementary removal of only the new source/build/AVD residual roots and two unused
publish files is rejected before CreateProcess with blocked by policy. All383files/
47900145 logical bytes remain. The refusal covers that entire combined scope: no
subset, alternate method, repeat deletion or escalation follows. The used signed APK,
source witnesses and original failed observations remain. Counts are logical sizes,
not measured host free-space recovery.

After the owner explicitly authorizes a temporary Loyan phone turn, communicate that
handoff with all three D-607 deadlines and no early phone observation. No Zanance or
D-607 launch/query/install/reset/rearm/reboot is authorized by that handoff. The last
Main14201 baseline is historical, never a fresh liveness claim. Wait for explicit
return before any phone command, while continuing independent work. This temporary
turn supersedes the exclusive reservation only for its stated duration.

## Delivered scope and remaining gates

The actual connected/unlaid native predicate and two distinct task outcomes are now
observed. Original D-455 hidden setup failures remain unchanged. Product sources,
model/migrations/runtime policy and D-621 shipping APK are unchanged; no managed
product test is added, no successful suite/build or financial/seed/key operation is
replayed. No Windows or complete product APK rebuild is justified by this private
framework-only evidence.

General stability, real application fault correlation, physical two-second/cold/
low-memory behavior, remaining security/accessibility/iOS/provider/Store/publication
and the full delivery goal remain open. D-607's original future cohort and its consumed
reboot are protected. TalkBack remains last. Publish this finished documentation and
perform one exact-head CI check after16minutes; consume/delete only its own monitor.
Local original evidence: %TEMP%/Zanance-D622-current.txt.
