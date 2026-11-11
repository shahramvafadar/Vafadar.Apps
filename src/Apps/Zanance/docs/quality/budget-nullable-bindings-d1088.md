# Retained empty budget inputs — D-1088

[Original identity input continuation D1089](identity-nullable-bindings-d1089.md)
covers null bindings across all five existing account-form modes without a
primary identity request or genuine authentication.

The [original goal input correction](goal-nullable-bindings-d1087.md) led to two
remaining budget failures: null total text and null category-limit text throw
before the original monetary review/validation can run. Both are reproduced in
the original source-linked editor. This does not diagnose or repair the separate
native runtime fault.

## Correctness and retained interpretation

Four new host cases cover total/category null and empty controls. The authoritative
pre-change run has2real `NullReferenceException` failures and2passing empty
controls. All four pass on the final binary, including complete source comparison,
explicit original Reload, retained raw null/empty, subsequent valid reentry and
refused Save with a different invalid field. Initial fixture preparation is one
Advanced settings value and one expense-category metadata definition, without an
account, budget, schedule or ledger entry.

The tests first retain en-US monetary contexts, change displayed formatting to
de-DE and clear only one field. Only the cleared field loses its old-format note;
the other retains its original parser. The cleared category's `2,50` reentry
parses to250minorEUR; original Save validates the current total/category while
the other invalid field prevents every writer. All discovered table contents
remain exact across Reload and refusal.

Native old/new text callbacks now use null/empty checks; review requirements and
unit/format captions do the same. Raw properties, retained draft rows and their
contexts are not normalized. Monetary parser/precision, source/draft/row identity,
paired writer, receipt ownership, period/calendar rules, schema and translations
are unchanged. The original nonempty unit-change refusal remains in place.

Two directly affected existing tests qualify total-unit and independent category-
unit refusals on the same final host binary. Their isolated existing account/
category metadata setup and refused budget Save do not write a budget or ledger
entry. This is4new+2affected selected cases, not6new tests or a complete suite.

The first control oracle used four fractional digits for EUR and failed two
assertions in addition to the two real null failures. Those logs remain separately
as `initial-precision-oracle-*`. Original central money parsing correctly refuses
that precision; only the control samples were corrected to valid currency
precision before the authoritative baseline. Production money parsing was never
relaxed to obtain a pass.

The sole D1088 response subsequently reports
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43109594912)
completed/success and
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43109596070)
in_progress/no conclusion. The exact response, exclusive consumed marker and
receipt remain; no second GET, successful-job log or rerun is used.

## Original Android page and package qualification

A fresh API36/x64/4096 cohort runs UID10216/PID4751. One initial Advanced-mode
settings value and one expense-category metadata row prepare the original
BudgetEditorPage; there is no account, budget, plan or ledger setup. Actual total
and category Entry handlers are Android EditTexts. Both clears publish null into
the original model. The original Reload retains null, its replacement row/control
is reacquired, reentry succeeds and a different invalid field refuses each Save.
Complete schema and typed contents of every discovered table remain exact after
fixture preparation; Accounts/Entries/Schedules/Budgets are empty throughout.

These are programmatic binding actions on the original controls in a standalone
NavigationPage, not OS typing/Shell-route/physical-phone acceptance. One76node/
29017byte final XML is retained; its remote bytes are hash-checked before removal.
The private signed offline/nondebuggable APK has192AOT/204ELF/804static16KiBLOAD,
valid v2/v3/ZIPCRC, SHA256
`6288396a97521b4ce909063293c52e6afbb50f2ec71875e239c8049a8f38643b`
and size56504092bytes. Two exact PDB document checks bind the actual original
model and private caller before installation; the Mono15payload matches its
qualified x64 input.

Strict Windows and complete offline Android builds finish without warnings or
errors. The prepared complete APK has384AOT/408ELF/1608static16KiBLOAD,
valid v2/v3/ZIPCRC, no INTERNET/debuggable flag, size99643295bytes and SHA256
`f2b49876422bb668b3bcb59e281fffc52ba819ba8ca0785606d6e58b9263ef16`.
Both qualified Mono15hashes match; two exact PDB checks bind the changed model
in Windows and Android. This APK is not installed on Main in this step.

## Cleanup and continuity

After accepted native completion, a read-only pre-uninstall OS observation fails
to connect to ADB5037 while reading accessibility_enabled. No uninstall/stop is
issued by that failed preflight. Original server59860/creation639293902901445383
and its5037listener, plus the original owned emulator listeners, remain live.
The error does not establish server/emulator death or a reason to restart.
The same pending cleanup subsequently completes: only fresh UID10216 is removed
with public OS/settings/key-presence/locale/boot/geometry/reverse exact, then both
original emulator/QEMU handles signal before official AVD deletion. No successful
native input, installation, test or financial action is repeated. The client
failure remains; its exclusive cause is not established. No explicit ADB reset,
start, reconnect or pair occurs, and original59860/creation remains afterward.

Official AVD retirement removes24files/4270273885logical bytes. Five standard
Cleans pass and retire28962files/2161683316logical bytes;3708files/588432384bytes
remain in those build roots. Used signed packages, compiler inputs, logs and raw
evidence stay preserved. No supplemental/frozen-scope action or new policy refusal
occurs; no phone command is issued. Main stays the dated D1085observation, not
fresh liveness or private-source proof.

## Delivery boundary

All evidence belongs to `artifacts/d1088-budget-nullable-bindings`; signed inputs
and actual compiler documents accompany strict builds and original Android-page
qualification. No successful actual financial/authentication/photo/provider
operation is replayed. Frozen scopes, retained cohorts and canonical parent
States remain untouched. The full delivery goal and broader security, original
fault, performance, accessibility, platform/provider/Store/release gates remain
open; a finite input correction does not close them.
