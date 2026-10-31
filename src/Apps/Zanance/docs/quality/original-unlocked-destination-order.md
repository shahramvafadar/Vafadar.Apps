# Original unlocked destination order (D-672)

## Original failure and correction

The original access gate preserved arrival order only while draining work that had waited
behind a lock. Its first already-unlocked action bypassed that ownership. While the first
asynchronous destination was waiting, a second arrival could therefore run first. A cover
requested before the first completed could not retain a second action that had already run.
Two new regressions fail against original source79246dd1 for these two cases.

The immediate first action now owns the same drain as pending work until its actual
completion and handoff. Later arrivals enqueue in order. Existing cover checks retain every
unstarted destination until access is granted again; no already-started action is replayed.
Direct failures still propagate to the immediate caller. A failed queued destination does
not discard later input. A reentrant followup queues without blocking its own parent.
The empty/covered observation and ownership release are atomic under the existing queue
lock, so an older finally cannot clear a later caller's newly acquired drain ownership.

This change does not cancel an already-started destination or qualify its source/window
ownership across all awaits. It changes no URI, route, access/authentication policy, financial
operation, database/schema, source format, permission, SDK, runtime or encryption algorithm.

## Focused verification

Four new cases cover overlapping immediate actions, covering during the first action,
direct and queued failure separation, and reentrant followups. All pass after correction.
Twenty-three existing related cases also pass:21external-link cases and2affected access
ordering/failed-cover-removal cases. The host runner reports .NET10.0.12; Android packages
use the pinned10.0.14 runtime/compiler/task. No unrelated suite or financial write was repeated.
An initial combined class/method filter selected zero cases and is disqualified, rather than
counted as a successful run. Final strict Windows has0warnings and0errors.

## Actual original-router native continuation

A fresh API36/x64/4096 fixture used UID10216, run e8ffaff116a64f259b835635d112625e and boot
95fdb29c-c18f-4fa2-bdea-71d3c85988d0. Original App startup, protected storage preparation,
access service and AppLinkRouter were linked unchanged. The private observation dispatches
two explicit original StartAsync callbacks; the first waits once before pushing an owned
nonfinancial modal, and the second pushes another owned modal. Both return no external link.
There is no alternate bootstrap, financial preparation, authentication or key-input port.
These are controlled original API callbacks and actual MAUI/Android modal navigation, not
two genuine OS notification taps or an acceptance claim about notification delivery timing.

Original PID4478 opened the second modal while the first was still waiting. After releasing
the first exactly once, actual modal order was Second/First instead of First/Second. Both
actual windows were captured, then only those two owned pages were closed. Full20source
collections stayed exactly equal, with empty accounts/ledger and no monetary command.

One necessary no-reset upgrade to the first corrected caller retained UID/boot/full20source
and changed PID4478to5171. Its diagnostic CreateNew marker survived from the original caller;
the corrected dispatch refused with IOException before Started became true. State afterward
proved no callback began, no order entry existed and no modal appeared. That refusal is
preserved. The original marker was not removed and accepted original callbacks were not
replayed. Only the corrected observation's two marker names were changed; product code did
not change and no successful product check was rerun for this observation correction.

A necessary second no-reset observer upgrade retained the same UID/boot/full20source,
PID5171to5532. Only the previously unissued corrected callbacks continued. Before release,
the actual order was only first-started and there was no modal. One release completed
first-started/first-ended/second-started/second-ended with actual modal order First/Second.
The two windows were captured and only the owned modal pages were closed. Full20source
remained exact before/after, and the terminal actual root was the original OnboardingPage.
Earlier Preparing observations and the marker refusal remain real failures of readiness/
observation; neither triggered restart, a same-binary installation retry or financial setup. The necessary
second observer upgrade is recorded separately above.

## Packages, preservation and cleanup

Full offline shipping d225e993bb98b7580d95de6dbce8c50400fd7c776ced7442d2369e8599fb2918
passes384ownAOT/408ELF/1608static16KiBLOAD, ZIP16KiB, CRC and v2/v3. Internet/debuggable are
absent. The installed original985c3b3150ee3d5b0cf2d12b7cb6122ca5b5371b05bd45d879519b32d6b41505,
first corrected63a1baf80c74e00dfd7f96657c799f4febba02dd896c09b3a37bf9021fa427e0 and terminal
corrected3e6ca49fe13625dee33f32b3111fd1aded1159d583343744d7f70b788ccaff2b each pass192ownAOT/
204ELF/804static16KiBLOAD and the same offline/nondebuggable/signature/CRC/ZIP checks. Builds
have no warnings/errors. The original App source is exact, and terminal access-service source
matches the qualified shipping source. Shipping excludes the private observation provider.
This is source/package evidence, not whole PE identity or physical16KiB acceptance.

Original boot,1080x1920/density420/locale and all5captured settings stayed exact. Only the
fresh UID/package and5owned XML captures/35608captured bytes were removed after matching
capture-transfer hashes. The actual emulator host exited and official owned AVD deletion
succeeded. Six standard Clean commands removed38780registered files/2154088112logical bytes.
All4used signed APKs and original/used source/evidence archives remain.

Two disjoint supplemental deletion requests were rejected before process creation with
blocked by policy:8unused publish files/271957717logical bytes, and8ended artifact/source/
AVD-residual roots/8535files/1212535513logical bytes. They remain frozen without retry,
subset, alternate method, move, escalation or reuse, as do all earlier refused scopes.
These are logical counts, not host free-space or secure-erasure claims.

Phone lease, D607's3original deadlines, consumed reboot and unread post-reboot source remain
untouched. Genuine PIN/device authentication, arbitrary navigation/source races, broad DB/
key/security, physical adoption, general ANR/crash/stability/speed, remaining accessibility,
iOS/provider/Store/public hosting and release remain separate. The full delivery goal stays
active; only this verified ordering batch is ready for commit/push and one delayed exact-head CI.
