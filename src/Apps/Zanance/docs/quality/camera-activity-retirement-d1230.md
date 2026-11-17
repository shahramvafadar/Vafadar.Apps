# Retire the original camera wait with its Activity (D1230)

## Remaining defect and correction

The [D804 concurrency guard](original-camera-result-ownership.md) already prevents
simultaneous callers from replacing one pending camera result. The remaining
adapter retains only a global completion and one fixed native request code.
The original MainActivity destruction bridge retires the document picker but not
the camera wait. A different/recreated Activity can therefore deliver that code
without an owner check; destruction can strand the wait and its capture gate.

A new strict host links the unchanged original CameraCapture source. One native
launch is held, the controlled current Activity changes without launching another
camera, and an incoming cancellation settles the old global task. The ownership
expectation fails with exit1. This is a native-boundary host reproduction, not an
observed OEM destruction, new photograph or claim that the earlier ordinary
camera acceptance failed. The missing destruction hook is established separately
from the actual source; the baseline does not pretend to invoke a nonexistent hook.

The corrected adapter retains an exact Activity/code/local-completion owner.
Original MainActivity forwards its own instance with results and retires its own
pending wait during OnDestroy. Cancellation still holds the nonblocking gate
through original output cleanup. Foreign destruction cannot cancel another owner.

Issued camera codes are not recycled within the process. The bounded camera range
0x4000–0x5AFF ends before household document codes and excludes sign-in codes.
Exhaustion refuses rather than wrapping. Already issued late camera results are
consumed without forwarding them to authentication or completing a replacement
capture. Native launch/file grants, original encoded-size/bounded-reader policy,
strings, layout, financial behavior, schema, runtime and GC settings are unchanged.
Both independent Activity bridges use the changed signature and destruction hook.

## Focused managed and build evidence

Three new linked-source product cases verify foreign-Activity results, destroyed
owner cancellation/partial-output cleanup with a distinct next request, and foreign
destruction refusal. The recreation case supplies the old code to both old and new
Activities while a replacement task is pending; neither can settle that new task.
Two directly affected existing concurrent/cancel ownership cases also pass.
Actual.NET10.0.15/X64 selects5tests, errors/failures/skips/not-run0. Accepted-image,
OCR, financial and unrelated successful suites are not selected. The runner's
deprecated XML-output-option warning is retained; strict compilation has0warnings.

Strict Windows and complete offline Android builds pass without errors/warnings.
Ten actual compiler documents bind the host camera/tests/boundaries, production
camera/MainActivity in shipping and private native builds, original unchanged
bootstrap copy, and private lifecycle Activity. Used DLL/PDBs and signed packages
remain after standard Clean. These checks do not claim whole PE byte identity.

| Prepared package | SHA256 | AOT / ELF / static16KiB LOAD |
| --- | --- | --- |
| Complete dual-ABI shipping | `5699adea9718e5f6a3cac3701e6e05667ceba4de6500a95f672835f5f32cfad6` | 384 /408 /1608 |
| Independent original App x64 | `bb4f3efb1c6c9c7cd71d01d61a9f1c31f4bedcd2a9ca3b7da04e18ce4e81f203` | 192 /204 /804 |

Both are nondebuggable/offline, v2/v3-signed, CRC/aligned. Original serviced15
Mono identities remain unchanged. Shipping is prepared, not installed on Main.

## Actual Android lifecycle

One fresh owned official API36/x64/4096 emulator uses an emulated back camera,
without webcam, account preparation or real authentication. Fresh independent
UID10216/PID4636 starts the original full App. A private subclass waits for original
startup, invokes the original adapter once, and schedules one native Recreate
while the camera completion is pending. This is controlled lifecycle input,
not an OS low-memory termination or a second Home/camera workflow.

Actual original MainActivity.OnDestroy executes with ChangingConfigurations=true;
the replacement Activity is created once in the same PID. The old task ends with
OperationCanceledException and owned camera outputs0. Actual focus belongs to
Camera2 CaptureActivity. After exact native camera-window/UID verification, one
Back returns one real Cancelled result to the recreated original Activity:
issued request16384, creates2, old wait still cancelled, outputs0, samePID4636.
No replacement capture is launched on-device; late-result isolation from a new
pending request is covered by the new host case, not overstated as native evidence.

No shutter, image read, OCR, Save/Import/Undo/payment, owner SDK prompt, Main action
or old accepted timing/search/classifier operation occurs. Original boot, five OS
values, locale/key presence, geometry and reverse mapping agree. This fresh
component cohort does not establish whole typed database/source equality or
unsaved-draft restoration. Samsung and the retained crash AVD are untouched.

## Refusals, retirement and open boundaries

The first private build launcher has a local flags-scope NameError before invoking
either product build. Corrected isolated launchers perform the actual builds.
The first private native compile refuses ambiguous Android/System cancellation
exception names; only that private reference is qualified before publication.
No successful test or native operation is replayed for these corrections.

Fresh package removal, normal owned emulator shutdown and original shared ADB
preservation complete. The first official AVD-delete launcher lacks System32 in
its private PATH: findstr fails and the SDK does not start, despite exit0. Its
initial deletion claim is refused. Correcting only that private PATH lets the
same official SDK delete the original owned AVD/INI; both are actually absent.
25files/4239078892logical bytes retire, with6userhome files/7732bytes retained.
Five standard Cleans remove28962files/2163309467logical bytes after retaining
necessary evidence. No supplemental deletion, frozen scope or alternate cleanup.

This corrects the named camera native-wait lifecycle boundary. It does not repair
the [current concurrent GC fault](current-concurrent-mark-word-operation-d1229.md),
prove every OS recreation/camera behavior or close whole Android stability,
security/performance/accessibility/platform/provider/release acceptance. The
overall goal remains active. The separately proposed actual-record interpretation
still requires its own owner decision; Brevo/provider inputs do not gate this fix.
