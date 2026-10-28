# Bound actual Android camera source bytes (D-613)

## Actual source gap, not an image-framing workaround

Original CameraCapture checked Java file length before File.ReadAllBytesAsync. An
external camera writer can grow that output between the early metadata check and
the actual read; the old read itself does not enforce the existing64MiB encoded-byte
ceiling. This is independent of the clipped D-598 frame. Do not invent missing text,
change OCR or claim that a bounded reader repairs camera framing/merchant evidence.

Move the existing AttachmentFiles encoded-source constant and bounded stream reader
unchanged into AttachmentFiles.Source.cs, a partial class shared by the original
picker and camera. The camera retains its early availability/empty/length checks,
original URI/request/result and finally deletion. After native success, open the
owned output read-only, use that exact existing bounded loop and dispose the stream.
Reject actual bytes beyond the original limit. Keep IOException/unauthorized failures
and existing receipt error mapping; no new text/permissions, SDK, model, financial
writer, image preparation, cancellation or original stream-ownership policy changes.
The encoded-byte ceiling is not a claim that peak allocations equal64MiB.

## Focused original filesystem proof

First extract the old File.ReadAllBytesAsync operation without changing its behavior.
Two new controlled cases create actual fictitious camera files at8 and67108863 bytes,
verify that each original metadata length is admitted, then grow each to67108865 bytes
before that same original file-read operation. Both fail because no refusal occurs.
This reproduces the stat/read contract on the actual .NET filesystem; it is not a
claim that a malicious or racing physical camera was exercised.

After the change, six distinct new selected cases pass: both growth refusals,
unchanged4096-byte and exact67108864-byte sources including all interior bytes,
original missing-file exception, and the original nonseekable reader's actual-byte
limit without taking caller ownership. The source file is never deleted or modified
by the shared reader; native CameraCapture retains that separate finally obligation.
The moved picker algorithm text,81920byte chunk and original limit remain exact.
No old financial/camera cancellation/OCR/Save cohort or full suite repeats.

## Finite actual native boundary

One fresh owned API36/x64/4096 emulator uses its emulated camera, never a webcam or
real phone. One explicitly admitted camera-byte branch uses the original covered
startup, CameraCapture.TakePhotoAsync and actual Camera2 Activity result. Only one
shutter and one Done are needed. Require the actual focused camera UID/window and
native target before input. No source chooser/cancel, text reader/parser, receipt
editor, Save, seed, ledger, attachment or prior accepted financial command is invoked.
The returned byte array must be a complete JPEG within the original limit, have
positive original dimensions and correspond to exactly one actual successful native
callback. The original owned raw camera folder must already be empty after return.
No raw image, private Main file/key, account or credential is exported. The native
observation qualifies normal integration/retirement; controlled host cases establish
growth refusal, not an oversized native attack or whole-camera quality guarantee.

Native proof: `9b1a77adaf2d411ca9449a0a79f216b5`, PID4844/UID10216; 75315 source bytes.
Complete Offline product: `1cf35204112a51aff956844efe1d3346e379ca80ba599163ca129d6fef626cb5`, 97066911bytes, ownAOT384/ELF408.
Strict Windows/Android builds, own AOT/actual package checks, CRC/offline/nondebuggable,
original-owner v2/v3 and ZIP/ELF static16KiB checks pass. Static16KiB is not hardware
acceptance. Native and product managed bytes are not declared shipping-equivalent.

Actual run `9b1a77adaf2d411ca9449a0a79f216b5`, UID10216/PID4844, uses the signed
independent APK `d289e42135bc35e2c5eabb0bc0a356021dc91c5796599023b21bf7a01356a78a`
(59256738bytes,192AOT/204ELF/804LOAD). Actual Camera2 UID10153/CaptureActivity,
Shutter and Done targets are bound before each of the two inputs. One original
request23107/Result.Ok returns75315bytes,1392x1856; original source SHA256 is
`3a5eaa06cb617671506b680b41b2822f063565a22def403ceae645ec0534212d`.
Only fixed metadata is exported. Both actual product/independent App images contain
the new source-read entry point; camera/storage/microphone/network permissions stay
absent. No source images or previous camera/financial cases are replayed.

After terminal UID/data absence and exact original fresh settings/boot/display/shell,
two owned XMLs/15224bytes are hash-cleaned. Fresh AVD `Zanance_D613_Camera` and its
registration retire through avdmanager only after actual serial/ports absence:
24files/4636619584 logical bytes. Retained AVD configuration and existing ADB servers
are unchanged. Standard Clean removes2828 registered files/809261077 logical bytes.
Further four-root retirement is rejected before process creation with `blocked by policy`:
1517artifact files/248559729bytes and six isolated user-home files/7732bytes remain.
Separately, unsigned APK body entries equal the retained signed bodies, but deletion
of those two unused unsigned APKs/two standalone resource-designer DLLs is also rejected
with the same stated reason:fourfiles/157483478bytes remain. Neither scope is retried
by subset/alternate wrapper; no private key contents are read/hashed. Preserve the
initial copied cleanup-label metadata separately and correct only its D-613 label.
Sizes are logical file sizes, not claimed host free-space recovery.

## Preservation and remaining gates

Only the fresh terminal fixture/AVD and hash-bound XML retire. Capture and compare
original settings/boot/display/shell, retained AVD configuration and original ADB
servers. Keep actually used APKs and all evidence; standard Clean and any further
residual deletion refusal are recorded separately without alternate retries.
No phone command or D-607 source/handle change occurs. Product no-reset adoption
stays behind that cohort. Full camera-text/corpus, punctual reminders, general
stability/performance/security/A11Y, iOS/provider/Store/publication remain open;
TalkBack remains last and the overall delivery goal stays active.

## Consumed one-use hosted follow-up

[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40631425691): completed/success. [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40631425718): completed/success.
Only the original single exact-head response is used. Its consumed marker and response
are retained; only that one-use monitor is deleted. No second request or broader release
conclusion is inferred. Cancellation cause and an unfinished CI result are not guessed.
