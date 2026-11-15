# Retained Android Button padding across density changes (D1195)

[D1196](android-retained-native-geometry-d1196.md) separately corrects Label padding
and the retained stroke/ripple-mask radii of Button/ImageButton. Its distinct
geometry cases do not replay this Button-padding cohort.

## Actual failure and correction

The existing refresh reapplied text, explicit minimum dimensions and separately
guarded Entry padding. Native Button padding still retained converted pixels:
layout invalidation did not repeat its public DIP-to-pixel mapper. A new
source-linked MAUI component used the original application Button style's16,10,
an asymmetric value and the original unset/default. The exact original style
hash and fixed protocol were retained before execution. No account/ledger fixture
was prepared. Baseline PID4531/UID10216, API36/X64/4096 on boot
`518d4130-ec8a-41fa-a67d-9ffaa959c99b`, retained one Activity/root across420->160->420dpi.
All three failed at160dpi:

| Managed padding | Retained pixels at160dpi | Expected current pixels |
| --- | --- | --- |
| Original16,10 |42,27,42,27|16,10,16,10|
| Asymmetric10,3,20,7 |27,8,53,19|10,3,20,7|
| Unset/default |42,23,42,23|16,9,16,9|

Expected values use the actual current-context conversion and original
ButtonHandler default, including rounding. Initial/restored baseline values
matched; the middle failures remain retained.

AndroidConfigurationFonts now remaps IButton.Padding only on density changes,
after confirming the same handler/virtual view. It rechecks window/root after
the callback before continuing traversal. It does not scale rounded old pixels,
replace managed padding/style, or expand the font-only branch. Database, keys,
authentication, financial, runtime and GC policies remain unchanged.

## Native and product verification

One no-reset helper upgrade preserves UID/boot. Final PID6935 accepts the three
corrected conditions plus an explicit-zero overlay condition, all four edges
exact at420/160/420dpi in one Activity/root. Captions remain exact; zero stays
zero. These are twelve native control observations, not new unit tests or an
old font/minimum/Entry matrix replay. Original static virtual controls remain;
native object identity and OS focus were not independently exported or broadly
qualified. A distinct final controlled Padding callback replaces the actual
window root; traversal stops after exactly one callback. This is a native
controlled-owner condition, not a genuine authentication, notification or lock route.

Strict Windows and complete default-offline Android builds pass with zero
warnings/errors. The complete signed APK is99,684,255bytes, SHA256
`005e0c79fc01e0100ee202ff1c73babd80c8709f3d02c60fca4203959ad66ef1`,
with384 AOT/408 ELF/1608 static16KiB LOAD segments. ZIP CRC/alignment, v2/v3,
offline/nondebuggable policy pass. Current private Core version5 is used without
replaying successful framework/source/API preparation. Original stock Mono15 is
byte-exact on both ABIs. No private runtime is shipped.

The independent packages have90 AOT/100 ELF/390 static LOADs and stock x64 Mono.
Final helper SHA256 is
`242955f37af3568757f518943b45b2ba4378c891f8457c37b90742c3174f7ae4`.
Three actual compiler-document bindings pass on each used helper; the final own
AOT module equals its package member. Four actual product compiler documents
bind selected sources and both own product AOT members equal the complete APK.
This is selected-input provenance, not whole-PE equality.

The identical complete APK is installed once into the originally absent
canonical package on the same fresh emulator. One ordinary launch reaches
original Onb_WelcomeTitle/Welcome in PID7345/UID10217 with55 owned native nodes.
An earlier Preparing capture remains; the same process continues without
relaunch. No form, Save, permission, authentication or financial input is issued.
This proves ordinary first-run integration, not original-style page layout/
glyph acceptance, whole private-source equality, speed or old native-fault repair.

## Preserved failures and retirement

Initial helper compilation refused wrong ContentProvider array signatures,
then a nullable handler dereference. A quoting error caused one redundant
unchanged failed compilation; it is retained, not a successful test. Correct
signatures and a checked local handler resolve those preparation failures.
The first corrected unused package still named consumed baseline markers;
predeployment admission rejected it. New corrected-prefixed markers preserve
all original receipts without deleting them or replaying successful inputs.
Two early offline transport observations retain the same emulator processes;
later normal boot is admitted, without server reset/restart/reconnect/pair or
another emulator launch.

Both fresh UIDs are removed after terminal evidence. Two remote XML files/
25,538bytes are removed only after exact hash/size matching; local raw remains.
Five original OS values, locale/value/key presence, geometry, reverse list and
boot match exactly. Original launcher3180/QEMU16040 handles signal after normal
console shutdown; the same ADB5037/PID59860 creation remains. Official AVD
deletion removes24files/11,122,914,795logical bytes; six files/7732bytes remain.
Three standard Cleans remove17,989files/1,288,850,693logical bytes;3004files/
487,251,495bytes remain in their build roots. Used binaries, source/PDB/AOT,
packets and failures remain in the owned D1195 artifacts. No supplementary or
frozen deletion, phone/Main, C7/Q02, consumed register read, accepted financial
action or unrelated successful suite is touched.

The sole D1194 response reports [CI in progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43689417488),
with local identity backend, self-hosted service qualification and CodeQL
success. That response/exclusive marker remain without another query; no later
outcome is inferred. Original native-fault remedy and whole performance/security/
accessibility/platform/provider/release gates remain open; the full goal stays active.
