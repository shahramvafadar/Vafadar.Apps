# Original nullable request text binding - D-1056

## Actual original failure and boundary correction

The public [InputView.Text contract](https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.controls.inputview.text?view=net-maui-10.0)
and [immutable upstream declaration](https://github.com/dotnet/maui/blob/43db9d77f2ff59999dea36ab8befb8541e919013/src/Controls/src/Core/InputView/InputView.cs)
motivate a nullable-empty boundary check, not an assumption of installed binary
identity. The actual reviewed runtime independently reports TextProperty defaultnull
and default binding mode TwoWay. No direct unsupported null assignment to the
non-null viewmodel setter is substituted for actual binding behavior.

Baseline actual original PersonalSyncPage/Entry/native EditText binds one fictitious
nonempty input to its original model. Setting that real bound Entry.Text to null
raises its original null TextChanged event and empties the native/MAUI control, but
the prior request remains in the still-interactive model immediately and after400ms.
No exception escapes the controlled setter. This is one actual stale-request failure,
not an observed application crash, HTTP submission, credential reuse or physical
keyboard/Clear-button timing result. Original modal departure eventually clears the
field; that later cleanup does not qualify the active clear.

Only Password's XAML binding gains a page-local PersonalSyncRequestTextConverter.
Both conversion directions map null to string.Empty and return every non-null object
unchanged. In particular, no trimming, digit/culture conversion, normalization,
secret copy/store/log or new credential policy is introduced. The original generated
Password property, three model source files, ownership/retirement/notification
checks, commands, masking, text-prediction setting and semantic caption stay exact.
The correction adapts the framework's nullable empty input at its original boundary;
it does not weaken the non-null domain or remove its guards.

## Final source-linked regression and original native evidence

Three new permanent headless cases link the actual converter and original model:
current nullable clear, retired nullable clear and retired nonempty refusal. Each
also retains the same raw whitespace/nonbreaking/RTL string through both directions,
then verifies the actual model state, interaction authority, one capture/no pending
work and zero transport opens. They pass in one strict.NET10.0.12 host binary,
3total/0failed/errors/skips. The headless interface declaration is only a signature
shape; it neither implements nor qualifies the native MAUI binding engine.
No old model/financial suite is selected or replayed.

The final actual Android original-control phase additionally verifies raw leading/
trailing whitespace and nonbreaking/RTL text before its one nullable clear. Both
immediate and settled model values are the canonical empty string, controls are
empty, the original null event occurs and the form stays interactive. Two
TextChanged callbacks represent the nullable assignment and canonical update, not
two clear operations. These are two bounded final native conditions, not two new
unit tests or a full authentication/physical-input qualification. Capture1/Close0/
transport0/authenticator0 and account/entry/plan/allocation0 remain explicit.

Fresh API36/x64/page4096 baseline PID4405/UID10216 and one necessary no-reset
continuation PID5448 retain boot`e815151f-1eea-4398-8d5d-2b5887b2916f`, public identity
and exact complete sqlite_master/all54tables/56typed rows across both stages:
`C11CB1DF1FBEF7F5511BA6591AF49DD49368F7FD9BC8A8CCC7DD2D8514CC0A3B`.
Each once-issued phase's cached terminal equals its response; no fixture/setup or
accepted command is replayed. Original root/OS/locale/key presence/geometry/boot/
reverse state restores. All request text is fictitious and no real authentication,
financial Save/Import/Undo, photo/share, PIN/enrollment or phone action occurs.

The first corrected private build refuses duplicate XAML attributes: a local raw
baseline archive was accidentally picked up by its default MauiXaml glob. It is
explicitly removed from that private project item set; evaluated input admission
then has exactly one original page. This is a preserved predeployment build/harness
failure, not a product failure or an installed candidate. No failed stage is counted
as checked. The source/archive, original packets and actual used binaries remain.

## Complete builds, compiler binding and retirement

Final strict Windows/complete offline Android and qualified private builds have zero
warnings/errors.25actual PDB source documents bind final host/native/Windows/shipping
inputs; the three original model bodies remain byte-exact. These are source checksums,
not whole-PE/Core/Data equality or all-platform behavior. The complete signed
99631007byte package has SHA256
`e1862eee6395d1cc17da0b7153fd7274aad1df43b98aa6a42180f3fb9629b963`,
384AOT/408ELF/1608static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/alignment and
unchanged original Mono slices. It is prepared, not Main-installed.
The used corrected private56709026byte package has SHA256
`6294b6fbca409259dee9669e85e253215faeb06980d1cf0188f5cce669ebd550`,
192AOT/204ELF/804static16KiBLOAD with equivalent package checks. Static alignment
is not hardware16KiB, API24/ps16k/ARM64 new-binding or release acceptance.

Only the fresh UID and its official24file4485559623logical-byte AVD retire. Four
original emulator process handles become signalled; ADB5037/PID59860creation ticks
639293902901445383 remain exact without reset/start/reconnect/pair. Four first standard
Cleans retire9321registered files/1342459987bytes. Six duplicate publish intermediates
retire214793345bytes after ZIP CRC/size-map and retained-signature checks; four current
private bin/obj roots retire837files/125091591bytes. Three signed packages and four
PDBs/source/packets remain. No frozen/unrelated root or retained5606/Q02 is touched.
Two later standard host/reader Cleans retire399files/181839275bytes, leaving112registered
host/reader files and19host-temp files/2057595bytes;6userhome files/7732bytes remain.
Logical sizes are not measured disk savings; remaining shared Release output is not
silently labelled unused or recursively deleted.

D1054's single due CI response is consumed: CodeQL completed/success, CI in_progress;
no second query, job-log replay or rerun follows. Main25353/D1053 remains only a prior
dated observation. Original native-fault cause/remedy, whole performance/security/
accessibility, external findings/provider access, iOS/Store/release and the complete
delivery goal remain open. Terminal D607/D752/enrollment/financial checkpoints and
all canonical parent State values remain unchanged.
