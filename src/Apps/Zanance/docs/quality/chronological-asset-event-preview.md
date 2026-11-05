# Chronological original asset-event previews (D-870)

## Actual missing consumer, original native failure

D869 corrects Quantity/Positions chronology. AssetEventEditor.UpdateEffect still used
raw events.Sum(EffectAt) for each location after its total guard, so it could lose a
valid preview on Id-ordered history. Baseline is aab2f34d; its Core is unchanged here.
Prepare only one type/two places/four noncash events in fresh UID10254: Max opening
(day1), Max gift(day3), Max outflow(day2), then1000-unit location transfer(day4).
Ids intentionally order the first three as opening/gift/outflow. Original FindConflict
accepts this history; accounts/entries/schedules/valuations0.

Actual original AssetEventEditorPage/VM in PID3658 has two missing expected previews:
a new Simple correction decreasing1base unit and an existing transfer changed to2000.
Both forms are Ready/CanEdit/dirty, no money/errors, exact raw fields and unchanged
source20; EffectText is null. These are real observed missing previews, not a source-only
or imaginary unit-test baseline. No Save/Import/Undo/financial/auth/photo command occurs.

## Limited repair and permanent host regression evidence

Keep the original checked total Quantity guard. Compute original chronological
Positions once and select the original type/place result for each displayed transfer
leg, retaining Math.Max(0), names, formatting and money-change branches. No new amount,
rounding, financial writer, ownership/session, model/migration or Core policy is added.

Three new focused tests use the actual linked VM/data/localization with separate
isolated host fixtures, no application Save. Correction, transfer and true-overflow
controls pass on .NET10.0.14/X64. Complete raw table snapshots (including all current
model/cache/migration tables) compare before/after each; account/ledger stay empty and
no route/alert/failure occurs. These host fixtures are independent permanent regression
setup, not re-preparation of the retained native cohort. The initial test build fails
CS1503 because a positional cancellation token targeted a bool parameter; only the
named test argument changes before any host case. No host failure/pass is invented.
D869Core17 and old successful suites are not repeated after this VM-only change.
Initial staged diff-check flags one blank line at test EOF; only trailing line endings
are trimmed after verification, with executable body exact and no test replay. The
compiled source receipt is retained separately from that formatting-only file hash.

## Same source/UID continuation and actual final captions

One necessary no-reset independent-package upgrade keeps UID10254/CE360478/
DE90988, with PID3658→4118. The seven prepared rows are not inserted again.
Read-only adoption compares all20typed source components with the retained original
image, SHA256 A42ADAF26EE26716F226B581E50EB0DF857A91572EF3C7E89E979C71E46ABA76.
Failed raw quantity/reason/note/date/direction/transfer/money/dirty fields match across
the private test handoff. Inputs are applied to new original form instances after
upgrade: this is not a claim of product OS/process-death draft recovery or root identity.

Only the failed two cases continue, then one new total-overflow control:

- Correction: `After saving: 9,223,372,036,854,774.806 tokens at Fictitious vault`.
- Transfer: `After saving: 9,223,372,036,854,773.807 tokens at Fictitious vault · 2 tokens at Fictitious box`.
- Increase1 beyond the supported total: no invented zero/preview, ready editable form
  and raw0.001/reason/note/date retained, no source change.

The two actual native TextViews equal original bound captions, each two lines/no
ellipsis/full visible bounds after one original vertical ScrollTo. No screenshot or
privacy bypass is used. This is controlled original-page/VM binding and programmatic
scroll, not normal Shell route, finger input, glyph/contrast or whole accessibility.
Full20source stays equal across upgrade and every case; no financial source/writer
normalization is used. Actual protected Main/private keys are not read.

## Packages, retirement and remaining gates

Strict Windows/full offline Android/baseline/final private builds have no warnings/errors.
Complete shipping SHA256 f342113fc1330dcf49fb693350a656b5b966f731b23b7d2e2492c7c4030e4edd,
99491743bytes/384AOT/408ELF/1608staticLOAD. Used baseline SHA256
f4d95ffa13649defc512503d3638b3285d8aaa614f6fdb0cee9aeec310bbb748; used final
3e3f50e844f34299adbf29decf78328875e92a542c80afa2897ca6457943bd8b,
each56905634bytes/192AOT/204ELF/804staticLOAD. Offline/nondebuggable/v2v3/ZIPCRC/
static16KiB alignment/original Mono pass. Twelve own App/Core/Data AOT bindings
(baseline3/final3/full6) match; sole private MauiProgram DI capture reverses exactly.
No whole-PE/physical ARM64/hardware16KiB claim is made.

Only freshUID10254 retires after its final source proof; original5606 package UID list,
five OS values/locale-key/boot/geometry/reverse/shell state match exactly. Five standard
Cleans pass (5409deletion lines); used signed/source/local packets remain. No XML,
supplementary/frozen retry, old fault/R13, phone/Main, server reset/reconnect/pair or
old financial/accepted metadata preparation replay occurs. Host fixture cleanup is
its ordinary isolated-test disposal, not deletion of a protected scope.

Shippingf342113f is prepared, not Main;23174/060a remains the dated D868 checkpoint.
Next missing conditions are current D869Core17 plus these final3 previews on ARM64
and complete Main adoption, without old28/53 or emulator success replay. Ordering/
preview cost and whole performance remain unqualified; no speed claim is made.
Original-fault cause/D838, security/accessibility/platform/external/release gates and
the overall active goal remain separate. D869 sole due response has three completed
successes and CIin_progress; no second request/job replay. This commit gets one
exclusive-marker read-only CI check after due time, without polling or job automation.
