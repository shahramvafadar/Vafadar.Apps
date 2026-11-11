# Empty definition bindings — D-1086

[Original goal input continuation D1087](goal-nullable-bindings-d1087.md) covers
four further null-binding failures across the complete target/contribution/name/
percentage boundary, preserving the original financial and parser policies.

Two actual null-input failures remained in the original definition editors:
`DisplayUnitsViewModel.UpdatePreview` dereferenced a cleared name and
`RulesViewModel.AddAsync` dereferenced cleared match text before required-text
validation. These are application input failures, not evidence about the separate
native runtime fault.

## Reproduction and correction

Six new selected host cases use the original source-linked models and actual
isolated store. Each editor receives null, empty and whitespace drafts. Before
the correction, the two null cases throw `NullReferenceException`; four controls
pass. All six pass together on the final host binary, with zero skipped cases.
The tests compare the complete discovered table contents before and after the
read/refusal, using the existing model/table inventory assertion.

The unit preview treats null as empty only for preview formatting. The rule's
existing required-text refusal now explicitly handles null/whitespace before
trimming. Raw drafts, command/source ownership, accepted receipts, selected
category requirements, writers, financial policy, schema and translations remain
unchanged. No account, category/rule definition or financial row is seeded for
these tests; no accepted write is replayed. The first candidate's nullable-flow
compiler refusal is retained separately and is not a test pass.

## Build and package evidence

Actual SDK10.0.404/runtime10.0.15 builds use the applied
[project servicing configuration](project-servicing-adoption-d1085.md), project-owned
outputs/caches and process-only isolation. Strict Windows and full offline Android
builds finish with no errors or warnings. The complete signed APK has384AOT,
408ELF and1608statically aligned LOAD segments, valid v2/v3 signatures and ZIP/CRC,
without INTERNET or debuggable flags. Its SHA256 is
`7912dd505b0d6b3f67edf30e111b169be28aa046f1309a0310431c3b44dbf3e4`
and size99643295bytes. Both Mono payload hashes equal the qualified15inputs.
Four actual Windows/Android PDB document checks bind both changed model sources.
This APK is prepared; this step does not install it on Main.

## Retained intermediate observations

The independent native helper's first build wrongly included its sibling Windows
output XAML and used an ambiguous string conversion for MatrixCursor. Both
compiler refusals remain; only the helper's explicit item exclusions/conversion
changed. A later helper-only read-guard edit raced compilation: its PDB/source
checksum comparison correctly refused, rather than claiming identity. The shell
nevertheless continued the already prepared fresh installation; that sequencing
mistake is retained. This intermediate helper then stopped at an ambiguous visual
tree `Single` lookup before either new null condition. It is not acceptance.
The continuation selects the actual Entry by its original semantic description,
preserves the same installed data, and requires exact final compiler provenance
before upgrading that independent package. Product sources are unchanged by
these observer corrections, and successful host cases are not rerun.

## Original Android pages and cleanup

Fresh API36/x64/4096 initially runs UID10216/PID4966. The necessary no-reset
observer continuation reaches PID5378 with the same UID/boot and public state.
The final private APK has SHA256
`f9532cf4491f4d2ec4c0d52d67642c091b824338cb378aca851edad1b6eb1842`,
56102514bytes,192AOT/204ELF/804staticLOAD, offline/nondebuggable/v2v3/ZIPCRC.
Three exact PDB document checks bind both original model files and the final
private caller before this continuation; no helper edit races that build.

Actual original DisplayUnitsPage and RulesPage Entry handlers are Android
EditTexts. Clearing each Entry really publishes null into its original model.
The unit preview stays empty through currency/factor changes and explicit read;
Add stays unavailable. The rule's original Add returns the exact required-text
error while ready, unaccepted and retaining null. Complete schema and typed
contents of every discovered table match before/after; Accounts/Entries/Schedules
are empty. These are original controls hosted in the independent NavigationPage,
not a claimed normal Shell route or physical-device acceptance.

One final27node/10865byte native XML contains the actual required-text caption.
Its remote bytes are hash-checked against retained raw XML before deletion.
Only the fresh UID is uninstalled; five OS values, key presence, locale, geometry,
boot and reverse list remain exact. Own emulator stop is issued; no phone command
or financial/authentication/photo action occurs. Main remains the dated D1085
observation, not a new liveness/private-source claim.

Five standard Cleans succeed and retire28956files/2159153870logical bytes;
3708files/588431448bytes remain in those build roots. Signed used packages,
compiler inputs, logs and raw evidence are retained. The official AVD manager
returns0 but explicitly says "deleted with errors"; the residual-file assertion
correctly fails, so full AVD retirement is not claimed. A guarded deletion of
only `artifacts/d1086-nullable-definition-bindings-owned-avd/avd/Zanance_D1086_DefinitionNull.avd`
is rejected before process creation with `blocked by policy`. Its plan/measurement
does not execute and no file is deleted by that command. That exact target is
frozen: no retry/subset/alternate/move/escalation/reuse or further inspection.
All earlier frozen scopes remain untouched.

Local logs, compiler inputs, signed used packages and the failed observer receipt
are retained under `artifacts/d1086-nullable-definition-bindings`. No frozen scope
is read, retried or repurposed. Canonical parent States, terminal cohorts and the
full delivery goal remain unchanged; whole security, stability, performance,
accessibility, iOS, external providers and release are separate gates.
