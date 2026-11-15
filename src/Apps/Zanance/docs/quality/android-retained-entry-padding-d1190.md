# Retained stock Entry padding and background density (D1190)

Date: 2026-11-15. This continues the actual74pixel intrinsic-entry remainder in
[D1188](android-retained-minimum-dimensions-d1188.md). The minimum repair remains
accepted; its assertions and the previous20SP/font matrix are not replayed.
Phone/Main, protected C7/Q02 and consumed fault classifications are untouched.

## New actual padding boundary

The pinned Core Entry background mapping deliberately restores its existing
native pixel padding after replacing the drawable. Merely remapping Background
therefore updates a drawable without fixing stale padding. A fresh API36/x64/
4096 control uses the actual current font/minimum refresh,15DIP text, transparent
managed background and zero minimums to isolate this new intrinsic boundary.
Its oracle is a detached instance of the exact native constructor, same context
and actual native direction; no old-pixel ratio or guessed logical inset is used.

The first fixture omitted supportsRtl and its requested RTL field resolved to
native LTR. That first packet is disqualified for direction coverage and retained;
no density or candidate action had occurred. The actual product manifest already
enables RTL. After adding that missing flag to the fixture, its actual APK binary
manifest is checked, and one no-reset upgrade retains UID10216/boot and adopts
the original initial marker. An initial observer path typo refuses before that
upgrade and is preserved separately. The six subsequent queries are new phases.

Actual PID4920 has one activity, native LTR0/RTL1, and exact fictitious drafts:

| Phase | DPI | Retained padding L/T/R/B | Fresh native default L/T/R/B |
| --- | ---: | --- | --- |
| Initial |420|10/26/10/29|10/26/10/29|
| Changed, current refresh |160|10/26/10/29|4/10/4/11|
| Public Background remap |160|10/26/10/29|4/10/4/11|
| Private default-padding candidate |160|4/10/4/11|4/10/4/11|
| Restored, current refresh |420|4/10/4/11|10/26/10/29|
| Private default-padding candidate |420|10/26/10/29|10/26/10/29|

Both directions show the same result. The background-remap row updates actual
drawable insets to4/10/4/11 while retained padding stays old; the restored row
also preserves that smaller drawable. Copying padding alone therefore does not
qualify the drawable repair. Immediate setter-row Height is not treated as a
post-layout measurement. The baseline/candidates do not change product code.

## Owned stock-default correction

`AndroidEntryConfigurationPadding` registers after Controls mapping initialization
and before app construction. Its ConditionalWeakTable uses weak handler keys;
values hold only a weak virtual-owner reference and four captured pixel values.
No view, context, drawable or handler is stored in a value.

Capture requires the exact stock EntryHandler/native Entry class, current virtual
ownership, absolute padding, and an initial pixel match to a fresh native default.
The pinned Core's internal layer is recognized by public type identity/assembly
only; no private member lookup/invocation or cache-field modification occurs.
Unknown subclasses/backgrounds and relative/custom padding are not silently
converted. Changed custom pixels abandon eligibility rather than being scaled.

On density refresh, an eligible current control receives its original public
Background mapping. The caller checks the current window/root after that callback
and before applying fresh native default padding. Owner/handler/pixels are checked
again. Current native direction is used for the temporary default; integer-rounded
old pixels are never converted back to supposed resource dimensions. Raw text,
selection, focus, pages, styles and requested minimums are not assigned.

The changed source first exposes IEntryHandler's public AppCompatEditText base
type in compilation. A stricter MauiAppCompatEditText pattern fixes those three
CS1503 errors; the original failed compile is retained. No guard is relaxed.

One necessary no-reset upgrade to PID5325 continues the same UID/boot and requires
the last successful baseline marker. Both actual directions automatically match
current default padding and drawable insets through420->160->420. A third field's
explicit native7/11/13/17padding remains exact. A new original Background mapper
callback replaces Window.Page on its first eligible field; traversal stops after
that single callback and the replacement root stays current. Retired native slots
are deliberately null, not measurements. Drafts remain exact before retirement.
No baseline query, financial operation or old accepted font/minimum case is rerun.

All three used SDK packages pass90AOT/100ELF/390static16KiBLOAD, offline/nondebuggable,
v2/v3/CRC/alignment checks with original serviced Mono. Signed inputs and compiled
source/PDB witnesses are retained for each. The actual compiler AOT outputs match
and are retained separately for the RTL baseline and guarded control; the initial
non-RTL package has no separate compiler-AOT match claim. This is
not arbitrary native-customization, device/platform or whole typography acceptance.

## Original app and exact retirement

Strict Windows and full offline Android builds have zero errors/warnings. Product
APK SHA256 `852d78fdd3b1f23782ae1f96852ae3442a8444c95fdff10b352bd402c542a655`,
99671967bytes:384AOT/408ELF/1608static16KiBLOAD, v2/v3/CRC/alignment and original
Mono identities pass. Four actual app PDB documents bind MauiProgram, MainActivity
and both configuration helpers. Both own ABI AOT outputs equal their packaged bytes.

After terminal SDK-fixture removal, the actual complete original app is installed
once in the same owned emulator: PID5723/UID10217. The two necessary Next actions
reach its changed field; one new fictitious draft is entered, without Start/Save.
Same process/Step3, exact raw name and focus are retained:

| DPI | Actual native XML name bounds | Height px |
| ---: | --- | ---: |
|420|`[108,1346][973,1462]`|116|
|160|`[277,481][803,525]`|44|
|420 restored|`[108,1346][973,1462]`|116|

The changed44pixel height now matches the original minimum target rather than
the previous74pixel intrinsic remainder. These are selected original-page bounds,
not glyph/speech/contrast, every control/state/theme or whole database proof.

Fresh package UIDs are matched before removal. Original five OS values, locale
and locale-key absence, size/density, boot and reverse are exact. Seven remote
XML files/212628bytes match retained local hashes and are removed/absent. Normal
owned-emulator exit and official AVD deletion remove25files/10839340549logical
bytes; six user-home files/7732bytes remain. ADB5037 PID59860/creation ticks remain
exact, with no reset, reconnect, pairing or physical-device command.

Three standard Cleans remove17989files/1288786668logical bytes, leaving3004files/
487221220bytes. Used signed packages/PDB/AOT/source, failed observations and raw
packets remain under `artifacts/d1190-retained-entry-padding`. No supplementary
deletion, frozen scope, old successful managed suite or financial write occurs.

The sole ebca5925 CI response is in_progress/unknown-final; CodeQL is success.
It is consumed with no second status request or job-log polling. D1191 separately
has a new read-only reproduction of source-directory SDK selection differing from
the repository-directory SDK; that does not yet establish the previous CI's inner
reason. Original fault remedy, whole performance/security/accessibility/platform/
provider/release gates and the full delivery goal remain open.

[D1192](android-retained-os-font-scale-d1192.md) separately corrects an actual
font-scale reset of an unsaved original form and its focused-target visibility.
The accepted density/padding cases here are not replayed.
