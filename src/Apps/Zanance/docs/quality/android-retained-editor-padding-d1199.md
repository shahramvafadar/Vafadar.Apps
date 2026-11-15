# Retained native Editor default padding (D1199)

## Actual remaining multiline input condition

Current product notes and read-only analysis fields use stock MAUI Editor,
including the original15DIP/transparent-background style. The previous
[Entry-specific repair](android-retained-entry-padding-d1190.md) captures only
EntryHandler. The pinned EditorHandler also constructs MauiAppCompatEditText and
uses the same background mapping, but is a separate handler/virtual contract.
No old Entry/font/minimum/Button/Label cohort is replayed as Editor evidence.

An independent source-linked helper uses two stock Editors (editable LTR and
read-only RTL) plus one custom-native-padding exclusion. It has no data layer,
account, ledger, authentication, file picker, keyboard input or financial Save.
Minimum requests are zero to isolate padding; original texts and native directions
are retained, not edited. Baseline PID3150/UID10216 and one activity expose:

| DPI | Retained default L/T/R/B px, both Editors | Fresh native default L/T/R/B px |
| --- | --- | --- |
|420|10/26/10/29|10/26/10/29|
|160|10/26/10/29|4/10/4/11|
|420 restored|10/26/10/29|10/26/10/29|

Both distinct Editor conditions actually fail at160dpi. The native constructor
reference uses the same direction and multiline mode. The custom7/11/13/17padding
remains unchanged throughout. These are saved finite native measurements, not
inferred behavior or new product unit tests.

## Correction and remaining native qualification

The existing weak stock-padding owner also registers Editor's original background
mapping after its type initializer. Eligibility still requires the exact stock
handler paired with its matching virtual contract, exact native class/background,
absolute padding and unchanged captured defaults. The caller's existing root and
handler guards remain. A custom/subclass/replaced handler or background, changed
padding or retired owner is not overwritten. The Entry handler arm retains its
original behavior; there is no unrelated mapper, style, resource, text or layout
change. No runtime/GC or source-data behavior changes.

One necessary no-reset helper upgrade continues the same UID/boot to PID6742.
Its first new marker requires the original baseline-restored checkpoint; none of
the three original queries is replayed. All six default measurements now equal
the fresh constructor across420/160/420, and all three custom-padding exclusions
remain exact. One additional new background callback replaces the root: exactly
one callback occurs and traversal of that retired graph stops. No model/form/
financial writer is invoked and original draft/native text/direction remain.

Three selected actual compiler documents bind each used helper to the selected
sources. Each own AOT module equals its package member. Both used helpers have
90AOT/100ELF/390static16KiB LOADs, stock x64 Mono15, offline/nondebuggable policy,
v2/v3, ZIP CRC/alignment. Exact signed APK hashes:

| Helper | SHA256 |
| --- | --- |
| Baseline | `3c10d740140b86a05782f9bdc066bb5f2c7c4d7704a2259c3e14d40e78130ee8` |
| Corrected | `4c647e4af51a4ad71d94def851b10bbcf82800a1f3ccb6fd6c3cb5ae149f0f3b` |

Both are17,993,377bytes. This qualifies the selected retained native controls,
not every original form, glyph/contrast, stock Entry requalification or whole
accessibility. No unit test is added solely to mirror the mapping.

## Product and unaffected Windows scope

Strict complete default-offline Android publish finishes exit0 with no compiler/
MSBuild warning or error diagnostic under ContinuousIntegrationBuild. The signed
APK is99,688,351bytes, SHA256
`f8e81dba593a653f4c9dfc8d83a3637be48da0846638add106073f1dc46fe661`,
384AOT/408ELF/1608static16KiB LOADs; v2/v3, ZIP CRC/alignment and offline/
nondebuggable policy pass. Four actual compiler documents bind selected startup/
configuration/padding source and both own product AOT modules equal the package.
Private Core version5/source/API witnesses and stock Mono15 remain unchanged;
no producer or advisory scan is repeated. The complete product is prepared,
not installed on a canonical emulator package, phone or Main.

The only changed product file is under Platforms/Android. An initial bare
MSBuild evaluation includes not-yet-filtered platform files, so its exclusion
assertion refused and is retained. The actual SDK
`_MauiRemovePlatformCompileItems` target then produces602final Windows Compile
items with the changed Android file absent. This runs item filtering only, not
compilation/restore or a resource-less Compile target. The strict zero-warning/
error D1197 Windows build is reused; an unchanged Windows build is not repeated.
No Windows acceptance is inferred from the initial unfiltered list.

## Exact retirement and sole prior CI observation

One early fresh-emulator readiness query precedes boot completion. The same
launcher14196/QEMU41920continue with no restart/reset/reconnect/pair; boot completes
in107994ms. This is emulator boot, not app speed. The completed helper is removed.
Five OS values, locale/value/key presence, geometry, reverse list and boot restore
exactly. No UI capture is created. Both original host handles signal after normal
owned shutdown; the same ADB5037/PID59860 creation remains. Official AVD deletion
removes24files/10,867,652,075logical bytes, leaving six files/7732bytes.
Two standard Cleans remove16,816files/930,369,242logical bytes;2,554files/370,956,423bytes remain
in their owned build roots.

Sources, used signed packages/PDB/AOT, original failures and terminal records
remain. No supplemental/frozen deletion, phone/Main/C7/Q02 input, consumed
one-shot interpretation or successful financial/auth/layout replay occurs.

The sole delayed [D1198 CI response](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43697560490)
reports completed-cancelled; [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43697560445)
is in progress in that same response. Its exclusive marker precedes the one GET;
raw response and receipt remain, with no second status request or actor/result
inference. The separate sole Home response reports
[CI in progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43698384965)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43698385030);
its own exclusive marker/response are retained and no final CI outcome is inferred.
Original native-fault cause/remedy and whole performance/security/accessibility/
platform/provider/release gates remain open; canonical parent States remain
unchanged and the full delivery goal stays active.
