# Retained Android minimum dimensions follow density (D1188)

Date: 2026-11-15. This continues the actual116pixel retained-entry boundary in
[D1186](android-retained-font-configuration-d1186.md), using the now-default
[D1187 private Core](default-android-core-linux-producer-d1187.md). It does not
repeat the accepted font-size assertions, financial operations or one-shot fault
classifications. Phone/Main, C7/Q02 and consumed diagnostics receive no command.

## New actual failure

The original Entry style requests minimum width/height44DIP. The pinned framework
`ViewExtensions.UpdateMinimumHeight/Width` converts each request once to native
pixel minimums; its public mapping must be reapplied when retained density changes.
The original `ToPixelsUsingMetrics` uses ceiling, not integer truncation.

A fresh API36/x64/4096 cohort uses actual MAUI EntryHandler controls, independent
fictitious text, one retained activity, and the source-linked current product
refresh. Two fields request width/height44/44 and52/60DIP. No application data
layer, account, ledger, authentication, provider or external file is involved.

Original PID3774/UID10216, same boot and activity:

| Phase | DPI | First min H/W px | Second min H/W px | Current expected H/W px |
| --- | ---: | --- | --- | --- |
| Initial |420|116/116|158/137|116/116;158/137|
| Changed, current font refresh only |160|116/116|158/137|44/44;60/52|
| Public minimum mappings |160|44/44|60/52|44/44;60/52|
| Restored, current font refresh only |420|44/44|60/52|116/116;158/137|
| Public minimum mappings |420|116/116|158/137|116/116;158/137|

All five original queries and markers are retained. The initial observer used
integer truncation for its expected420dpi values and failed its final assertion.
That observer failure is preserved. A separate analysis of the same saved packets
uses the pinned original ceiling rule and establishes the table above; no query,
density transition, setup or successful operation was repeated to correct it.
The original packet's erroneous ExpectedHeight/Width fields remain unchanged.

The first readiness observer encountered incomplete boot. The same emulator
process was preserved; only its readiness was reobserved, without a new launch.

## Product correction and changed-boundary verification

`AndroidConfigurationFonts.Refresh` now reapplies the public minimum-height and
minimum-width mapping for positive explicit requests, as well as its existing
font refresh and measurement invalidation. It does not change requests, text,
focus, page or native defaults. Nonpositive requests are left alone. Current
handler/virtual-view ownership is checked before the minimum callbacks, and the
existing current-window/root check follows every callback. A changed root ends
the entire traversal rather than visiting another retired control.

One necessary no-reset upgrade continues the fresh cohort to PID5152, same
UID10216/boot. The first new phase requires the original last marker, proving
its retained checkpoint without replaying the five baseline queries. With the
corrected source, all four positive minimums automatically match in420->160->420.
A separate explicit-zero field remains0/0. The actual minimum-height mapper then
replaces Window.Page during its first positive callback; exactly one positive
field is visited and the replacement root stays current. The zero field is not
mapped. Retired native field slots are deliberately null, not measurements.

Both source-linked SDK packages pass strict90AOT/100ELF/390static16KiBLOAD,
offline/non-debuggable, v2/v3, ZIP CRC and alignment checks. Original serviced
Mono remains byte-exact. Baseline and corrected source/PDB/AOT/APKs are retained;
their two source documents each and actual packaged own AOT are bound separately.
The correction does not replay the successful restore of the unchanged project.

Windows and complete Android product builds pass with no errors/warnings. Product
APK SHA256 `a410eb0a7ca4f597722a25e97b581e936208266396e2d7a0ceb4198288c88938`,
99663775bytes,384AOT/408ELF/1608static16KiBLOAD, v2/v3/CRC/alignment and offline
policy pass. The two actual product PDB documents match current MainActivity and
refresh source; both own ABI AOT outputs equal the packaged bytes. No Core source
producer, unrelated successful managed suite or financial operation is rerun.

## Original app screen, restoration and limits

The fresh cohort's completed SDK package is removed only after matching its UID.
The complete original app is then installed once in that same owned emulator,
with canonical package prior absence established: PID5541/UID10217. Only the two
necessary Next actions reach the changed account-name field. One new fictitious
draft is entered; no Start/Save or account/financial creation is requested.

In that same process/Step3, actual native XML name-field bounds are:

| DPI | Bounds | Height px |
| ---: | --- | ---: |
|420|`[108,1346][973,1462]`|116|
|160|`[277,481][803,555]`|74|
|420 restored|`[108,1346][973,1462]`|116|

Text and focus remain exact; the field stays within the1080x1920 viewport. These
XML bounds are not a direct native-minimum getter, full typography or glyph proof.
The source-linked component proof supplies the minimum values. The actual74pixel
intrinsic height at160dpi remains explicit; no exclusive padding cause or whole
height/drawable/accessibility correction is inferred from this minimum repair.

Both fresh packages are removed with their matched UIDs. Original five OS values,
locale and locale-key absence, size/density, boot and reverse are exact. Seven
remote XML files/212637bytes are hash-matched to retained local raw evidence and
removed; all are absent. The owned emulator exits normally. Official AVD deletion
removes25files/10865686046logical bytes, leaving six user-home files/7732bytes.
The original ADB5037 PID59860 and its creation ticks remain exact; no reset,
reconnect, pairing or physical-device action occurs.

Three standard Cleans remove17989files/1288544033logical bytes. Used signed
packages, compiled-source/PDB/AOT witnesses, failures and raw packets are retained
under `artifacts/d1188-retained-minimum-dimensions`; no supplementary or frozen
scope is attempted. These finite native observations are not new unit-test counts.
Original fault remedy, whole performance/security/accessibility/platform/provider
and release gates remain open. The full delivery goal stays active.

The sole subsequent D1187 status response reports CI failure. Failed-job metadata
shows Libraries/tests succeeded, Windows stopped in APK handoff regressions, and
Android stopped in the private-Core source-proof phase. Only those two failed
job logs were read. Windows' mock SDK reaches the newly added real preparation
step; Android's inner source-proof reason is not emitted, so that reason remains
unknown. No second status request, successful-job log read or rerun occurs.
This local minimum correction does not claim hosted CI acceptance; the separate
build/harness failures continue in D1189. See the
[actual failed run](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43671266347).
