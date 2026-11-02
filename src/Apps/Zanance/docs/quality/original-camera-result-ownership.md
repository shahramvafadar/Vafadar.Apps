# Keep the original Android camera result with its pending owner (D-804)

## Actual defect and original-source reproduction

2 November2026, published baseline ebfa363b. D803's one owner-taken physical photo is
already complete and must not be repeated. Independent review finds that original
CameraCapture.TakePhotoAsync uses one static result and request code0x5A43, yet every
new caller completes the previous result with false and replaces it before launching
another camera. The first caller can return cancellation before its actual native
result; that result can then complete the replacement operation instead.

A strict private host links the exact current original CameraCapture.cs, not a copy
of its logic. Inert native activity/intent/file-provider boundaries expose only this
interleaving, with real test-owned temporary file operations. Its unused read-boundary
substitute is not the product bounded-reader acceptance; cancellation never reaches
that read in this baseline. Actual.NET10.0.14/X64
runs the original: concurrent refusalfalse, first completed before its own resulttrue,
and two native launch attempts. The expected ownership condition fails with exit1.
This is a host reproduction, not a second physical camera photograph or proof that
D803's ordinary operation failed. No financial/authentication operation is involved.

## Production correction and focused checks

A nonblocking process-wide owner gate now refuses a competing capture before output
creation or native launch. The first capture awaits its own local completion and owns
that slot through bounded source reading and file cleanup. Finally clears its static
result and releases the gate even after launch/read/cleanup failure. Do not queue a
second launch that might execute later from a retired page. Original request code,
callback routing, FileProvider authority/grants, cancellation,64MiB encoded source
limit and D613's actual bounded reader remain. No layout, string, permission, financial
formula, schema/model/migration, runtime/compiler or GC setting changes. The internal
concurrent exception uses the existing localized generic failure boundary; its English
message is not a new visible resource or a replacement native dialog.

Five new product tests link the real Android adapter and existing bounded file reader,
substituting only native platform boundaries. They verify concurrent refusal/first
ownership, launch-failure release, exact accepted bytes/owned-file removal, unknown
result plus cancellation/next-owner release, and encoded-size refusal/cleanup/release.
All five pass on actual.NET10.0.14. Initial strict test compilation refuses six missing
WaitAsync cancellation arguments under xUnit1051; supply the test cancellation token,
without changing warning policy. The final five-case build/run is clean. Additional
required internal-member XML comments leave every executable test token unchanged;
no successful suite is replayed afterward. New product-test count is five, not a full
suite total. Original camera/file/financial/authentication successes are not rerun.

Windows strict and complete offline Android shipping builds pass without warnings/
errors. Used signed packages, actual v2v3/ZIPCRC/ZIP16KiB/nondebuggable/no INTERNET:

| Role | Bytes | SHA256 | Own AOT / ELF / static16KiB LOAD |
|---|---:|---|---|
| Complete shipping |99463071|`27d8070b638df21fe51b5317349ebbb407397a34980f45fc7f577cab2b4fed1b`|384 /408 /1608|
| Independent original x64 |56569762|`763b5e80fcb0b8951039ffb2306d083994c94cbc99c637aba3ade9f0b94b51c1`|192 /204 /804|

The private provider exists only in the independent APK. Portable-PDB source-document
checksums observed before standard Clean bind the original CameraCapture input in both
builds to current SHA256
`235d8a3eafdb3156e382927b87003a66ad961d5cf4df382703a8fd2d76afc187`.
Original bootstrap differs only by the explicit private service-provider hook. This is
compile-input binding, not whole linked/unlinked assembly identity. An attempted Core/
Data PE-byte identity check refuses: both actual PEs differ, with distinct Core CodeView
artifact paths observed. No exclusive cause of every byte difference is established;
whole PE equality is not accepted, normalized or required as evidence of this adapter
interleaving. Actual runtime native engines remain original14 ARM64f03c6356/X64edfa02c3.
No SGen/finally/interpreter/original-crash repair follows from this source correction.

## Actual original native ownership interleaving

One fresh isolated official API36/x64/4096 emulator uses emulated back camera, no webcam,
accounts, financial setup or authentication. Initial transport offline is not terminal;
same host83996/QEMU81612 reaches boot readiness without restart. Original startup
finishes at OnboardingPage in fresh UID10216/PID5031, account/ledger/schedule counts0.

A shell/run/package/PID/hardware-bound private caller invokes the actual original
TakePhotoAsync on the main thread once. It is a controlled adapter call from the
original app Activity, not another Home receipt gesture or physical photo. Actual
Camera2 UID10154 owns the foreground. Exactly one competing original method call is
refused by the new guard with InvalidOperationException; the original task remains
pending and no completion has occurred. After one Back in that actual camera window,
original MainActivity callback completes the first task once as Cancelled, no failure,
samePID5031 and original Onboarding root. Owned camera outputs0; original account,
entry and schedule counts remain0. These three counts are not whole typed-source/schema
equality. No shutter, recognition, account/PIN preparation, Save/Import/Undo/payment,
owner SDK prompt or successful normal-camera/cold/financial cohort is repeated.
Five OS values/locales/boot/geometry/reverse and source ownership remain exact.

## Current physical adoption and retained raw differences

Immediate original Main19128/D800/6a5574d1 Home141 and public package/OS facts are
captured before exactly one authorized install-r and normal launcher entry. Complete
shipping27d8070b becomes Main1069 without reset; this execution issues no installer-
prompt input. Public
UID10380/CE155299/DE154603, grants/stopped flags, every other Zanance package, five OS
values/locales/boot/geometry remain exact. No private Main DB/key is read.

The first raw Home comparison refuses equality: all141 nodes, displayed979EUR,
semantic/state attributes and bounds match, but anonymous native ScrollView node11
at[0,0][1080,2047] changes drawing-order2to1. Preserve both original raw XMLs and that
actual delta; do not claim all raw attributes equal or invent its exclusive cause.
Continue the same captured evidence/public metadata checks, without relaunch, another
install or capturing a later frame to fish for equality. The pre-install helper's
misspelled proof-key refusal occurs before an installation marker/action; correct only
that local lookup, not the passed native operation. Actual changed capture-source
checksum matches both completed builds. This is bounded public adoption, not whole
private-source, every OEM capture or full stability/performance acceptance. A separate
D752 adoption receipt preserves its original interrupted frame/immutable checkpoints;
accepted D792/D793/D802 cancellations remain retired.

## Retirement and remaining gates

Two owned emulator XMLs41846bytes and two phone XMLs103864bytes hash-match original
local raw before exact per-device removal; all four remote files are absent afterward.
Only the fresh independent package retires. Original emulator process handles are
bound before one owned emu kill; both original processes actually end. The official
AVD registry/path is checked inside its intended fresh root before official deletion:
26files/4521801264logicalbytes retire, six files/7732bytes remain without another delete.
SharedADB5038/PID66176, real phone and unrelated cohorts remain.

Five standard Clean operations remove28968files/1992852832logicalbytes;3708files/
587481135bytes remain. No supplementary/frozen scope is read/retried/reused. Used signed
packages, original/final private source and failure/test/native/public receipts remain.
The first Git-raw baseline byte expectation also refuses on line endings; reconstruct
working CRLF from Git and match the previously recorded original digest exactly,
without any product/financial-source normalization or another baseline run.

The pending-camera replacement defect is corrected and checked in actual Android.
Original camera source limits, D803 owner framing and broader receipt corpus remain
their own evidence. No native original-fault cause, whole security/DB/key, two-second
Home target, complete accessibility, iOS/provider/Store/release claim is made. Overall
delivery remains active and no owner request is pending for this completed batch.

Related: [owner physical framing](physical-owner-framed-camera-draft.md),
[bounded camera source](bounded-camera-source-read.md),
[current execution frontier](remaining-delivery-evidence.md).
