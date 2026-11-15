# Retained Label and button-layer geometry (D1196)

## Five actual remaining failures

[D1195](android-retained-button-padding-d1195.md) fixes Button padding. This batch
examines different native properties: Label padding and the three public gradient
layers of Button/ImageButton backgrounds. The original estimate badge uses6,1,
history labels16, primary/outlined buttons radius14, and PageHeader's image Back
button radius22/border1. Original source/style hashes and the finite seven-control
protocol were retained before deployment. No price/account/ledger data was prepared.

Baseline PID3351/UID10216 runs on API36/X64/4096, boot
`5496f8d1-3c04-49be-b85c-5a6fc71d065d`. At420->160dpi, five conditions fail:

| Selected condition | Actual at160dpi | Expected |
| --- | --- | --- |
| Primary radius14 |gradient radii14,37,37|14,14,14|
| Outlined radius14/border1 |14,36,36|14,14,14|
| Image Back radius22/border1 |22,57,57|22,22,22|
| Estimate Label padding6,1 |16,3,16,3|6,1,6,1|
| History Label padding16 |42,42,42,42|16,16,16,16|

The pinned public mapper source explains the partial result: a size-triggered
background update replaces the background gradient, while the existing stroke
and ripple-mask radii remain converted for the prior density. The original
stroke mapper updates all three layers. Expected radii retain its exact current
density conversion and integer half-stroke adjustment. Width, raster appearance
and actual touch-path behavior were not independently measured. Public
LayerDrawable/InsetDrawable/GradientDrawable APIs provide the selected radii;
no private fields, pixel readback, screen-protection bypass or process memory is used.
Initial and restored420dpi values match; zero-radius/default-zero-label controls
remain correct. Their data are retained alongside every failure.

## Narrow correction and actual versioned results

AndroidConfigurationFonts now reapplies ILabel.Padding and
IButtonStroke.CornerRadius on density changes, requiring the same handler/virtual
view and checking window/root after each callback. These public mappings retain
original managed values, style/default semantics, stroke conversion and existing
shadow handling. Font-only refresh, source/model/schema, financial/authentication,
runtime/GC and collection policy are unchanged. No new cache or lifetime is added.

One no-reset helper upgrade preserves UID/boot. PID6895 passes all seven controls
at420/160/420dpi:21 native control observations, not21 new product unit tests.
Both formerly stale gradient layers now match the expected radius, all Label
padding edges match, controlled captions remain exact and zero values stay zero.
Button-padding, old font/minimum/Entry, financial and unrelated successful suites
are not replayed. A controlled Label-padding callback replaces the actual window
root and traversal stops after one selected callback in that same process.

The first corner-callback guard creates a replacement page and invokes refresh
before its native control is attached. It yields zero callbacks and is explicitly
disqualified, not a pass or product defect. A necessary helper-only no-reset
upgrade retains all earlier phase files and accepted packets. Only the unfinished
corner condition runs in PID7218, same UID/boot, using an explicitly checked
already-attached original control. Its real callback replaces the current root;
traversal stops after exactly Corner:ZeroRadius. No successful geometry or Label
guard runs again. These results have explicit helper-version lineage; they are
not all observations on the final helper, genuine navigation/authentication or
whole-accessibility acceptance. Native object identity/focus, glyphs and rendered
contrast are not broadly qualified.

## Product preparation and provenance

Strict Windows and complete default-offline Android builds pass without warnings
or errors. The complete APK is99,684,255bytes, SHA256
`50f43d64b7e4828e0c5b34cf79c558c8c8588e1e0ffaac031d694e59ed27faa1`,
384 AOT/408 ELF/1608 static16KiB LOADs. ZIP CRC/alignment, v2/v3 and offline/
nondebuggable policy pass. Private Core version5 and original stock Mono15 remain
unchanged; no successful framework/source/API preparation or dependency scan is
repeated. The actual application package graph equals D1195 exactly.

Each independent used helper has90 AOT/100 ELF/390 static LOADs with stock x64
Mono15. Exact APK hashes are:

| Used helper | SHA256 |
| --- | --- |
| Baseline | `6881b1ea58f56c0bced38422d66e4e47a012668f8c9454d016cc9f7c7b61b417` |
| Corrected geometry and Label guard | `ee3f16d030333e4b1e9c0f102f1e7ddc2c2933e4d5172a4873db1803c33767ea` |
| Remaining corner guard | `c9edda4b4645c50f4a44e9c2e4f9251d5f0ac9c5cf2026073e1bad5f9d847718` |

Three actual compiler-document bindings pass for each used helper; the final
own-build AOT member equals its package. Four actual product compiler documents
bind selected inputs and both own product AOT members equal the complete APK.
The product and both corrected helper versions bind the same changed source.
This is selected-input provenance, not whole-PE identity. The complete product is
prepared; it is not installed on a canonical emulator package, phone or Main.
No first-run, financial or owner-authentication success is replayed.

## Exact retirement and remaining delivery

One fresh SDK UID is removed after terminal evidence. No UI/XML capture is
created. Five OS values, locale/value/key presence, geometry, reverse list and
boot restore exactly. Original launcher61236/QEMU7180 handles signal after normal
owned shutdown; ADB5037/PID59860 retains its original creation identity. Official
AVD deletion removes24files/10,946,491,899logical bytes; six files/7732bytes remain.
Three standard Cleans remove17,989files/1,288,868,838logical bytes;3004files/
487,262,227bytes remain in their build roots. Used binaries, sources, PDB/AOT,
original failures and all packets remain under the owned D1196 artifact root.
No supplemental/frozen action, phone/Main, C7/Q02, consumed register operation,
database preparation, financial effect or unrelated successful test occurs.

The sole D1195 status response reports [CI in progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43691619884)
and CodeQL success; its exclusive marker and full response remain without another
query. No final CI outcome is inferred. Original native-fault cause/remedy, whole
performance/security/accessibility/platform/provider/release gates remain open.
Canonical parent States are unchanged and the full delivery goal remains active.
