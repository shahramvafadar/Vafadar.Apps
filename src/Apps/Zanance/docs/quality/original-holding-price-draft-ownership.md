# Original holding-detail price drafts and accepted ownership

## Scope and original failures (D-644, 29 October 2026)

Complete the inline manual-price form in the actual holding detail, preserving its coherent
same-file source, original quantity/total/per-unit calculations, manual valuation identity,
original numeric-culture and currency context, and existing history/archive writers.
There is no model, migration, permission, runtime or financial-formula change.

Eight distinct cases fail against published 3c1e33c4: a real accepted manual price clears a later
raw/date/mode draft both with and without a failing Changed notification; the parent does not
expose its inline draft as unsaved; and a departed parent leaves Refresh available. The original
failed report is retained. Tests use the actual SQLite store and confirm the accepted valuation,
empty ledger and empty asset-event history before asserting the lost input.

## Original writer and independent newer input

The existing save/delete writer publishes an optional accepted receipt immediately after its
actual transaction commit and before ordinary Changed handlers, inside the typed committed-write
notification boundary. This is not speculative success before commit. Existing standalone callers
keep their original optional-parameter behavior and original commercial/source validation.

The price form records its exact raw/date/mode/input-context history. Only the unchanged submitted
raw input is cleared after acceptance. Later user input remains separate and requires explicit
read-only Refresh before another deliberate Save. A deleted historical row never clears a separate
price draft. Known accepted identity survives subsequent source or notification failure. One new
retained-draft message has genuine translations in all six supported languages.

## Actual frame, Back and terminal archive

The parent exposes the actual inline price as unsaved. Original toolbar Back and native hardware
Back use the same current-frame/current-draft consent. A pending answer disables further price
submission/Refresh and another Back prompt; a newer date, even changed away and back, departure,
query replacement or retired weak native root cannot receive the old answer. Cancellation retains
input. Accepted archive departure also reviews this independent price draft, and both archive
consents remain attached to the same frame and draft. Archive refusal does not clear the draft.

Source readiness is separate from actual frame availability: a failed current read retains explicit
Retry, while a departed or root-retired view cannot refresh itself back into authority. A pending
original source load cannot start another price Refresh. Live translated price-mode captions retain
selected mode, original numeric parsing context and exact raw/date values.

## Focused evidence and preserved implementation failure

Twenty distinct new cases pass. Forty-four affected existing cases cover the original price
flow, coherent detail, publication, history integration and archive behavior. These are selected
checks, not a repeated whole-suite qualification. The host runner reports .NET 10.0.12.

One affected integration check initially failed after the new departure guard: a known price commit
followed by page departure no longer attempted old-view Refresh, but did not show accepted feedback.
That intermediate failure remains. The corrected code preserves the saved-result notice without
reopening the departed view; only that failed case was rerun, retaining the other 26 successes from
the same affected-owner execution.

## Retained-tab defect found in the actual native run

The first native run preserved raw12.34, pending Back command exclusion, Keep editing, actual tab
departure and refusal of the departed Save. Its complete17-table source remained identical and no
valuation/event/ledger write had occurred. On returning through the actual Home tab, Android retained
the routed detail without another OnAppearing callback. Explicit tab retirement had replaced its
loading state, but the original retained-tab activation dispatcher had no holding-detail branch.
The page therefore stayed covered. Two early/terminal observer refusals remain, and the original
loading UI and full unchanged source were preserved. This is an implementation defect discovered
by the native run, not a successful return or an unexplained timeout to be restarted.

The corrected retained-tab dispatcher calls the same original covered page loader used by appearance.
Continuation requires a complete signed no-reset update of only the fresh independent package,
the same UID/boot and exact original full-source digest/type, plus guarded read-only adoption.
No setup, account, asset event, accepted price or financial command is replayed. The nonpersisted
raw draft is preserved as evidence before update; restoring that same raw input afterward is not
claimed as OS process-death draft recovery. Earlier successes retain their actual original APK/PID.

## Historical receipts and truthful new failure feedback

Retaining a historical accepted identity initially exposed a second focused regression: after an
explicit successful refresh, a later refused writer and failed read could be labelled as saved merely
because the old identity still existed. Its failing report remains. Separate pending-accepted-refresh
state now distinguishes the last known receipt from the latest attempt. The corrected new case and
the two affected existing save/delete notification cases pass. The receipt itself is not erased.

## Native qualification and actual source lineage

The fresh independent emulator is API36/x64/4096, UID10216, with unchanged boot
23027509-fd9e-4eb1-b527-ec18a68d0e31. Initial PID4626 verifies pending Back exclusion, Keep editing,
actual tab departure and refusal of the departed price Save. The corrected no-reset package reaches
PID5596 and adopts only the exact original type and complete17-table source digest
96ED8CAE0B1B8EF60E7583E30143A285D3D20051E753A1A44E71B00F59A895D2, without preparation replay.
The actual retained-tab return uncovers the same raw12.34 while requiring explicit read-only Refresh.
A real read refusal and native Retry retain that input and restore original authority.

Twelve normal-scale contexts cover en/fa/de, Light/Dark and360/412dp: original price Save captions
are complete and native targets meet44dp; raw12.34, original EUR interpretation, selected mode and
all observed source facts remain unchanged. Native hardware Back and explicit discard return to
ordinary Home without a valuation, event or ledger write. These successes retain their exact APK/PID.

A second necessary no-reset update for truthful historical-receipt feedback reaches PID7228 with
the same UID/boot and exact full original source. Only the remaining acceptance cases continue.
Exactly one original per-unit manual price12.34EUR is saved as1234000 milli-minor units for the actual
local day. Its real receipt exists before a Changed handler introduces raw99.99 and fails. The new
raw remains dirty, explicit Refresh is available and Save remains disabled until review. Only one
AssetValuations row differs; all other observed17-table facts are exactly equal, including ledger0,
asset events0 and accounts0. No historical valuation deletion, purchase, opening or transaction
posting is repeated. Six new360dp accepted-feedback contexts cover en/fa/de and both themes with
complete original saved/newer-draft captions. Original Close/discard removes only that newer draft,
and final ordinary Home retains exactly the same accepted source.

## Exact selected packages

| Use | SHA256 | Bytes | Own AOT / ELF / static16KiB LOAD |
|---|---|---:|---|
| Initial native frame evidence, PID4626 | `04f40b5ae2a368193aba668bc3fb98d13fd2115d7fbc56a4501d59e04476137b` |56299426|192/204/804|
| Corrected tab return and12ready contexts, PID5596 | `ef8a2d6bf780fbd472ba0943751a3586729f7ad75dbb336d2e538d7d0b465c6c` |56303522|192/204/804|
| Final receipt and one price acceptance, PID7228 | `936256d5da869cef11f86b7412b2018634972c44a1f158da57fdc51b10c286db` |56303522|192/204/804|
| Selected complete shipping package | `9aa8479d2ddbac187a707afcfc79e94f1236bdd3804e7f43615c4abd9aed47ec` |98344863|384/408/1608|

Final Windows and selected Android builds are strict and pass without warnings. All used packages
pass CRC, ZIP/staticELF16KiB, offline/non-debuggable andv2/v3 signature checks; the private independent
provider is absent from shipping. Native original sources are compiled in the separate caller;
whole linked PE/BCL identity and hardware16KiB acceptance are not claimed. Earlier pre-terminal builds
are explicitly superseded, not substituted for the final code.

## Restoration, observer failures and cleanup

The original boot,1080x1920/density420, system locale and five observed setting values are restored;
app display returns to en/System. Standard wm size reset leaves an empty display_size_forced value
instead of its original null readout. That restoration refusal remains; deleting only the fresh
owned emulator's temporary key restores the original queried value. No clock, battery, authentication
or physical-device action is involved. Original key presence was not separately measured.

Host observer refusals for an uppercase stage name and looking up a shared German dialog key in
the app resources remain. Only naming/resource lookup and the same pending dialog continue; no
successful native gesture, preparation or accepted write is replayed. Python source warnings are
not presented as compiler failures or silently removed from the retained transcript.

Only the fresh UID/data and51hash-matched XMLs1995996bytes retire. Its emulator port closes and the
official owned AVD deletion removes25files5573587979logicalbytes. Four standard cleans remove28955files/
1974307074logicalbytes. Supplemental deletion of nine new roots and ten unsigned/copy files is rejected
before process creation with blocked by policy;3732files1457130556logicalbytes remain. Never retry,
subset, alternate, move, escalate or reuse these refused targets. Used signed packages, original
failures, evidence and the private source archive made before refusal remain. These are logical
counts, not a causal host free-space claim.

D-607's physical package, all three deadlines, consumed reboot and post-reboot unread source remain
untouched. Physical adoption, broad DB/key/security/stability/performance/accessibility, iOS/provider/
Store/release gates remain separate. Overall delivery stays active and further TalkBack stays last.
