# Original Home receipt command startup - D-953

## Actual original failures and resulting behavior

D952 guards the nested receipt session. Home itself still publishes Busy before its
own try/finally and does not recheck its original owner after that notification.
Two controlled original native Task executions reproduce the remaining boundaries:
a throwing Busy subscriber leaves Home Busy true; permanent original Home retirement
during Busy still acquires the nested receipt origin once. The latter ends before
source reading because the nested D952 guard refuses it, but its origin/subscriptions
should never have been acquired by that retired Home command.

Home now publishes Busy inside its existing try/finally and rechecks its captured
original Current predicate before calling the original receipt session. Busy releases
on the controlled subscriber exception; exception propagation is preserved, not
swallowed or presented as a general crash remedy. The nested original predicate
remains publication-retirement-only, preserving its existing support for temporary
app-lock covers. The full immediate Home page predicate is not incorrectly applied
to resumed receipt work behind a lock cover. No parser, source/file, native source,
financial policy, model/schema, crypto/runtime/GC or navigation design changes.

## Why original native Tasks, not a copied headless method

Home depends on real Shell/page identity. This step uses its actual generated
ReadReceiptCommand.ExecuteAsync on the actual original HomeViewModel in a real
AppShell. The private controller catches only its deliberately injected IOException
so the original faulted Task is observed without an unhandled UI exception or crash
injection. These are controlled native command Tasks, not physical OS taps, all
possible races or new host unit tests. No replacement Shell or copied product
method is used. A private decorator counts captures while forwarding every original
MauiReceiptRuntime method unchanged. The original DI factory is scoped by AsyncLocal;
its independent baseline control acquires/disposes/refuses one actual context before
opening a reader and is not replayed after upgrade.

One new zero-opening cash metadata account makes the original Home receipt action
available. It is prepared once; entries/plans remain0. Original x64 PID3079/UID10216
has boot3ffc1b6b-4ab5-47a1-bebc-0c55344793fd on API36/kernel4096,320x640/density160.
The baseline observes BusyAtTerminaltrue/IOException/context0/origin0 for the first
case and Busyfalse/noException/context0/origin1 for retired Home. After measuring
the first failure, the observer unsubscribes and restores only the presentation
Busy flag so its cohort can finish; this is not a database/data reset or a pass.
Original root/public state/source restore completes before the required upgrade.

## Corrected continuation and positive entry control

One necessary install-r and one corrected launch retain UID10216 and that boot.
PID5692 adopts the saved full20 typed source read-only before any new command,
with SHA
`8717E6F19FC3205022AACB0848AF7CFA4E70AB6A10611DA129FEA0600B927CE5`.
No preparation/account/source-control/financial replay occurs. The final controller's
baseline-control flag is false because that successful baseline-only action is not
reissued; its separate current-owner original command provides the new entry control.

Three corrected original Task outcomes pass:

- Current Home reaches the original receipt origin1 and original factory context1.
  The scoped sentinel disposes/refuses before opening source or camera/picker/OCR.
  Original safe read-failure feedback appears and one owned-emulator OK acknowledgement
  ends that same Task with Busyfalse and no escaping exception.
- A controlled Busy subscriber exception still propagates to its awaiting controller,
  but BusyAtTerminal is false, with context0/origin0.
- Original Home retirement during Busy ends with context0/origin0 and Busyfalse.

The first safe-error observer expects button1, while the retained actual dialog has
one OK button2. Its title/message/owner UID and exact retained XML identify only the
original nonfinancial read failure. The actual waiting ADB client71108 is confirmed
live and bound by process handle/creation identity. Only that existing dialog is
acknowledged; the same original query client exits0 and its original packet is read.
No command/source request or successful capture is replayed. The two corrected
negative cases then continue. All three final outcomes retain the exact full20 typed
source, accounts1/entries0/plans0; original root and public state restore again.
This is not whole private schema/key equality or a successful receipt/financial save.

## Exact source/package qualification

Baseline private APK SHA256:
`d650b23aace3bb05bb1e1224a4542eedb86a776fe8568a9e8caf65b6580d01cd`.
Corrected private APK:
`013b0eaf33cdd700dc1ae4aa4925d687248774a6c6bef552bf65c34c5eaa2000`.
Both56942498bytes and192AOT/204ELF/804static LOAD; equal size does not imply equal
code. Seven actual compiler source documents match archived original/corrected
lineages. Complete corrected shipping APK is
`ee8f07d4fa13cfc17c756d656aea6742453528f86b22c1867357f476168310e8`
(99573663bytes),384AOT/408ELF/1608static LOAD; five actual compiler source documents
match. Strict Windows and both complete Android variants finish without warnings
or errors. Offline/nondebuggable/v2v3/ZIPCRC/static16KiB checks pass. Original Mono
f03c ARM64/edfa x64 remains; no whole-PE, hardware16KiB or physical acceptance follows.
Shipping is not installed on Main while D944 is held. No unchanged managed suite,
old OCR/photo/authentication or accepted financial operation is repeated; new host
unit-test count is0. The two original failures plus three corrected observations
are lineage evidence, not five new product tests or all on a final binary.

Initial private-helper nullable captures refuse before installation; exact local
page/model captures fix only that observer. The corrected-phase constant then makes
a baseline-only control unreachable; a readonly private phase guard fixes that
observer without changing product behavior or replaying baseline successes. These
strict compiler refusals remain, separate from the two actual product failures.

## Restoration, cleanup and open full delivery

Three XMLs/102595bytes are local/remote hash-bound before exact remote retirement;
local evidence stays. Only the fresh private package is uninstalled, absence checked.
Five OS values, locale/key absence, geometry/boot/reverse remain exact. Own host85292
exits after normal console stop. Official AVD deletion retires24files/4691539189
logical bytes,0AVD files remaining, small user-home residuals retained. Existing
ADB5038/PID50972 creation ticks stay exact. Four standard Cleans pass (Windows,
final private Android,shipping Android,compiler reader). Earlier baseline native
Clean reports NETSDK1005 after Windows target restore; that failure is preserved.
Required Android restore/build later restores the real target assets, and final
standard cleanup succeeds. No manual subset/supplemental/frozen deletion follows.
Used signed packages/source/PDB/packets remain under
`artifacts/d953-home-receipt-command`.

D952soleCI response at2026-11-09T12:20:54.787451Z reports
[CI42613879798](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42613879798)
in_progress and
[CodeQL42613879892](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42613879892)
completed/success. No second query, final CI inference, successful job-log request
or rerun follows. D944 physical7552/UID10439/data/original request stay held while
the owner is away; no phone command/authentication/Main adoption occurs. Retained
5606/Q02 and all terminal financial/frozen checkpoints remain untouched. Original
native fault/ANR, whole performance/security/accessibility, iOS/provider/Store/
release and overall delivery remain open; independent ready work continues.
