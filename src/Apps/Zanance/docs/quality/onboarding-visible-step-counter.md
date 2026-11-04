# Visible original onboarding step counter (D-818)

4 November2026. UX-06 requires progress information independent of color. The original
OnboardingPage has three segments whose filled state changes color; StepText is bound
only to SemanticProperties.Description. It has no visible current/total count. Reuse
that existing six-language text as a visible label; no new translation is invented.
The preliminary input-hint contrast premise is rejected: original hinted controls
are enclosed by CardBackground, including both AccountFormView hosts. Do not change
the global Muted palette or unrelated control styles on that premise.

Only OnboardingPage.xaml changes: allocate an Auto row and show the original StepText
with the existing Secondary style. The original segment grid/reader description stays;
the duplicate visual caption is excluded from the reader tree. Palette, segment colors,
font/style definitions, commands/viewmodel, financial behavior, layout of the three
independent scrolling forms, source/model/migrations and translations remain unchanged.
This addresses the visible-text principle of [W3C SC1.4.1](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html);
a reader-only description does not provide the same information to a sighted user.
No TalkBack session, recording or owner listening request is started.

The new caption's source is the actual original OnboardingPage/viewmodel. Private
callers link current original app/XAML/Core/Data; the only bootstrap change exposes
its built service provider. They observe actual native TextView text/color/line layout/
visibility/bounds and original managed page background, not a reconstructed screen.
They advance only original Next steps1->2->3 and use Back; Finish is never invoked.
No account, category, ledger or schedule is prepared or saved. Full twenty typed source
collections compare equal inside each independent source-bound run; this is not Main
private-source, whole DB/key or historical byte identity proof.

The 360dp cohort uses API36/x64/4096, density420, actual945x2100 and OSfont2.0 in
UID10221/PID7331. Thirty-six new caption contexts cover six languages/two themes/
three steps. Preserve all accepted contexts and raw native texts. The initial observer
incorrectly compares Latin model digits to Persian native display digits at context6;
only the observer changes to the existing Persian display policy. Accepted English
contexts0..5 and original transitions are not replayed. After original source/UI-key/
draft restoration, a temporarily null handler makes the immediate terminal observer
fail; observe only the same settled state, never another context or restore command.

Remaining412/680dp caption contexts continue in a distinct independent package, with
no replay of the accepted360dp matrix or full product/Windows build. These two exact
dp widths use temporary density160 and height800 at the same actual OSfont2.0. Their
private maximum/context mapping changes for only the named missing widths; product
XAML source stays byte-identical to the first native caller.


## Completed finite native evidence

| Original-source caller | Actual viewport and density | New contexts | Source proof |
|---|---|---:|---|
| UID10221 / PID7331 |945x2100px at420dpi =360x800dp|36|Full20 typed collections equal|
| UID10222 / PID9258 |412x800px then680x800px at160dpi|72|Full20 typed collections equal|

All 108 contexts are at actual OSfont2.0, six languages, both themes and steps1/2/3.
The last72 only fill the missing412/680 widths; accepted360 cases are not replayed.
Product XAML is byte-identical across both private APKs; the observer maximum/index
mapping and independent identity differ. These are108 native observations, not108 new
unit tests or all observations on one private binary. There is no financial setup.

Actual native text is complete in one line with no ellipsis in every case. Its global
visible rectangle equals its native bounds; the caption ends above the actual body
viewport, and the body/footer do not overlap or leave the captured viewport. Saved
native widths independently equal each named width at its captured density, rather
than treating a requested wm size as acceptance. Persian native digits follow the
existing display transformation; raw viewmodel/reader-caption values remain Latin.
Foreground is nativeFF4A5568/FFAEB8C6 and matches the managed label; the original
page/header background resolves E8ECF2/111827. Unrounded ratios are
6.347152785610703:1 and8.84567626560306:1 against those backgrounds, above the ordinary
4.5:1 threshold in [W3C SC1.4.3](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html).
This is actual native foreground plus managed bound-background evidence, not screenshot
pixel/glyph/anti-aliasing qualification. No screenshot or protection bypass is attempted.
The native sizes are65.625px at420dpi and25px at160dpi under the recorded OS scaling.
Caption reader exclusion is the configured attached property, not an actual TalkBack
speech/focus or complete native reader-tree certification.

## Restoration findings and retained limits

The first source preparation has three nullable compile errors. A correction guard
incorrectly expects two unguarded provider references instead of three and refuses;
an unchanged second failed incremental build proceeds because its caller omitted an
exit check. Preserve both failed logs and correct the actual source preparation/check
before the successful native build. No APK or successful observation repeats there.

The360 cohort's restoration preflight detects system_locales en-US versus captured
null. The exclusive actor/cause is unknown. After returning font/geometry and removing
only the fresh package, remove that one added locale setting and verify null/absence.
Before that first cohort, only the getter value was captured; original key presence
was not, so exact pre/post key-presence identity is not claimed. The remaining-widths
cohort explicitly captures absence before and verifies absence after. Five captured
OS getter values, original1080x1920/density420, boot, reverse, shell UID2000 and every
preexisting package/UID/D809 V3 APK match after each retirement. Original language/
theme preference keys and original draft name/currency restore within each caller.

One additional normal-font restoration observation in the second caller has native
34.125px text and full visible caption bounds[180,101,900,143], width720px. The observer
assumes full physical1080px and refuses. Retain that failed assumption and actual
packet; do not relaunch, recapture for a favorable frame or call it complete live-window
geometry restoration. The underlying live density/window behavior remains unresolved.
A follow-up preparation using stdin also initially assumes a file-relative directory
and refuses before action; use the explicit preserved root for the retirement-only
continuation. No accepted context, source preparation, original restore or system
setting command repeats. The108 named OS200% viewports remain independently proven;
that extra packet does not qualify a complete normal-font matrix or overall accessibility.

Only the two fresh packages retire; the requested D809 AVD/userdata/system record stay
available, with no original Activity/provider, actual record read, reboot, reset, owner
authentication, photo, financial Save/Import/Undo, reminder rearm or frozen-scope action.
The permitted R13 unknown observation remains terminal. Main32063/ac3a82f6 remains
only the last D814 phone observation; this batch sends no phone command or new upgrade.

## Complete builds, source retention and cleanup

Strict Windows and complete offline Android pass with zero warnings/errors. The full
shipping APK is99467167bytes, SHA256
`4ed884078045b5805daa80240cedb6aaad2985c338e5e34b3986818c81ce7f66`,384AOT/408ELF/1608static16KiBLOAD.
The360 private APK is56901538bytes, SHA256
`1b1ab319053cb544c0f16ef45712c88abbd718f8854fa5e26483e594c5f46c8d`; the412/680 caller is56901538bytes,
SHA256`9f628c10cd6af718a8679eae0f95b823d6d2243182e9e0116308c2e1e4115c2f`. Each private APK has192AOT/204ELF/
804static16KiBLOAD. All three pass v2/v3 signatures, ZIPCRC/alignment, no INTERNET and
nondebuggable checks; original Mono digests stay unchanged. Static alignment is not
hardware16KiB execution. Successful product tests and old renderer matrices are not
rerun; this reversible XAML-only change needs no mirrored unit test. iOS remains deferred.

D815's earlier retained-source directory wrongly contains its D813 sibling. The actual
D815 project still exists outside Clean and exactly matches its original preparation
literal/run/input substitution; retain correct four-source/project files separately
with raw hashes, without replaying its retired reader. Preserve the wrong archive and
correct its attribution explicitly in [the original report](receipt-apostrophe-grouped-totals.md).
Both D818 archives are explicitly bound to actual project root, package and run, with
no sibling-directory substitution. This is not historical whole PDB/PE identity proof.

Four standard Clean operations retire42260files/
2474448746logicalbytes, leaving4562files/
715871897logicalbytes. Used signed APKs, actual used source,
original/both versioned observer failures, native raw packets and all108 finite results
remain retained; no supplemental or frozen removal occurs. Wider layout/live-DPI,
physical-device, glyph/speech, security/DB/key, original native-fault remedy, whole
performance/platform/provider/Store/release gates stay open. Overall goal remains active.

Related: [earlier independent onboarding viewports](onboarding-independent-scroll-os-text.md),
[original receipt caption colors](receipt-source-caption-contrast.md),
[remaining frontier](remaining-delivery-evidence.md),
[preserved original native exit](current-large-budget-native-exit.md).
