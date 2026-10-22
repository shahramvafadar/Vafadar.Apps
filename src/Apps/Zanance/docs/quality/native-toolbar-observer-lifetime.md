# Native toolbar observer ownership (D-450)

## Original defect and actual native evidence

The D-449 tracker keeps a ViewTreeObserver obtained before attachment and uses its managed event
for global layout. The independent original Release/AOT renderer is inspected on three actual
Shell roots. Every active tracker has CachedPresent=true, CachedAlive=false, CurrentAlive=true,
SameNativeObserver=false, Retired=false. Thus the disposal branch guarded by the cached observer's
IsAlive cannot remove the callback from the current live tree. One active translation subscription
per root does not prove that native observer removal happened.

The inspection reads only known source event targets and the owned native toolbar observer
identities. Output is limited to type/lifetime booleans and counts; no objects, addresses, private
Main database/key, heap, raw stack-memory or register contents are captured. Original independent
base rows are unchanged and financial writes are zero.

Android documents that a dead observer cannot be used for registration/removal and provides explicit
listener removal by identity: [ViewTreeObserver reference](https://developer.android.com/reference/android/view/ViewTreeObserver#removeOnGlobalLayoutListener(android.view.ViewTreeObserver.OnGlobalLayoutListener)).

## Correct lifetime boundary

Use an explicit native IOnGlobalLayoutListener with its own Java peer, retained by the current
tracker. Its weak owner prevents a dispatch snapshot from retaining an old Shell graph. On tracker
disposal, first mark retirement, remove that exact listener from the toolbar's current live observer,
detach its owner and release references. A callback already in Android's current dispatch snapshot
sees no live owner. Preserve the D-449 scaled-title measurement and monotonic/deduplicated height
requests. Neither text size, colors, native Back nor financial behavior changes.

A first candidate implements the listener on the ShellToolbarTracker Java peer itself. It really
crashes in the independent package at native activation with System.NotSupportedException. That
candidate is rejected and never installed as Main. The final listener has a separate Java peer so
it does not share the base tracker's disposal boundary. The crash is preserved; this does not prove
that all existing native crashes or ANRs share this cause.

## Focused final original checks

At actual OS200%/360dp and OS200%/412dp, three real root replacements per width retain one current
registered native listener. The two previous source trackers per width report Retired=true,
ListenerRegistered=false and ToolbarReleased=true. An original Persian Goals title per width
retains exact text, no ellipsis, complete native visibility, TextSize34px, last-line bottom58px,
TextView height58px and toolbar height66px. It is the real on-tab Goals page, not a substitute title.
Return to the original root/language completes. Both complete reports preserve independent source
rows with FinancialWrites0. No accepted payment, seed, prior successful Retry, full216-scene scope,
resource cohort or successful unrelated business suite is replayed. No managed test-count increase
is claimed. Early caller compilation errors (missing namespace and nonexistent route constant) are
preserved and repaired before these final native checks.

The source reference in the D-449 report is corrected from10.0.90 to the actual centrally pinned
MAUI10.0.110; the checked builds already used10.0.110. No dependency version changes occur.
Runtime/compiler/task remain10.0.13. The upstream Mono investigation remains open; its status is
not proof that it causes this application defect or that an unverified update would fix it.

## Delivery scope

Windows strict build completes with zero warnings/errors. The final complete signed Offline Main
APK is96,546,549 bytes, SHA256db7cbd9a3656a28b7771cdc69116f378efa2a11ee6cfb33be594d82f798e85f3,
with384 AOT entries, arm64/x64, verified v2/v3 signatures and no INTERNET permission. The no-reset
upgrade succeeds; original empty Goals and return Home preserve139 native nodes, My account/0.00EUR
and the same PID15971. An initial Insights dump returns a null hierarchy; read-only continuation
confirms the already accepted tab input without repeating it, before Goals and Home proceed.
No new Save, profile reset or physical-phone input occurs. OS font1.0, physical
412x800/density160, accessibility0/servicesnull are restored after the final native checks. Main is
returned to foreground. General GC/finalizer/ANR, startup/two-second/low-memory, spoken accessibility,
physical Samsung, iOS, external providers/accounts/stores and product release stay open.
