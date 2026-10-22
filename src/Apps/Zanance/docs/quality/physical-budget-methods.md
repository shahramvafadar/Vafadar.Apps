# Original physical envelope and Flex budget qualification (D-464)

## Scope and unchanged implementation

QA-01 continues on owner-reserved Samsung SM-A176B. Main starts in PID26903 with979EUR and141
public nodes. Reuse the complete signed Offline D-463 APK, source7dd110343d817072c9c3da2787fc74b168fc9ecf,
SHA256e777f2ac8b30cc662475b1453fb60ef3c7d1d6aadb696a0cb91acd0c678fdb68. No install, code,
resource, data-model, permissions, service or provider change is made. D-463 strict builds,384 AOT
libraries, ZIP/privacy and local v2/v3 signature evidence remain valid; successful tests/builds
and native matrices are not repeated. The emulator is not touched.

Inspect the current BudgetFinancialProjection, EnvelopeCalculator and FlexCalculator contracts
before constructing the source. Envelope cash uses history to today; goal earmarks and unspent
category envelopes are separate parts of that balance. Overspending is already in cash. Flex
separates default fixed/nonmonthly/flexible category types and preserves planned versus actual
money, including yearly comparison shares. Only original phone UI enters or reads the fixture;
no private database, key or account credential is read.

## One original fictitious source

Create D464_Methods once, English/Light/Advanced, My account checking EUR opening900 on22 October.
Record one Food expense10, giving actual cash890. Create earmark target150 and set aside25 once;
this is an allocation, not another ledger expense or an account transfer. Create Housing monthly
plan D464_Fixed30 with amount30 and Insurance yearly plan D464_Annual120 with amount120, first
due22 October2026. Both reminder and automatic posting switches are explicitly observed false
before their single accepted Saves. Never tap Pay/settle. Both remain due, not paid.

Three accepted budget metadata saves on one original monthly budget:

| Original state | Actual public result |
|---|---|
| Envelopes, total200, Food100, no carry, alerts off | Cash890, goals25, envelope remainder90, unassigned775; overall10/200 and Food10/100/90 |
| Correct only Food100 to5 | Envelope overspend5, still assigned0, cash890/goal25 unchanged, unassigned865; explicit already-reflected-in-balances explanation |
| Change the same budget to Flex, total50 | Flexible10/50/40; fixed0 paid of30 expected,30 open; nonmonthly10 monthly share/0 paid; total monthly plan90 |

The separate planned section shows150 due in two items. The annual120 corresponds to10 per month
for comparison only; it is not falsely recorded as a10 payment or charged alongside the full120.
Home remains890 and shows40 left of50. No earlier accepted purchase, plan, allocation, budget Save
or source setup is replayed to repair an observation.

## Cold continuation and original-state restoration

One explicit package-scoped force-stop and launch yields COLD, new PID3591, Activity1350ms. This
is Activity timing, not full Home/startup performance acceptance. Read the original protected
profile, without reseeding: Home890 and40/50, Flex10/50/40, fixed0/30/open30, monthly nonmonthly
share10/paid0 and total90 persist. The goal card retains25/150/125. Both named plans remain due
today. All-history has exactly one Food expense; no plan/allocation/budget action created a second
ledger transaction. These are public native observations, not a private database/key identity proof.

Return Main, delete only inactive D464_Methods with both real native confirmations. The first
question names that exact profile. Final original three profile names remain and Main979/all141
public Home nodes equal their before snapshot, including text, semantics, class and selection.
Final PID3591, APK hash and exact font1/1080x2340/density450/accessibility0/empty service list match
the expected original state. The phone stays reserved for this project until the owner says otherwise.
Twenty-eight assertions over retained observations pass. Remove133 exact current-run remote XML
files only after comparing their remote and local SHA256; retain all private local evidence.

## Preserved observation failures

One hierarchy read immediately after opening the goal editor is unavailable; reread the same
live form, without reopening or restarting. The original goal card has a complete progress
description, not the bare name; the bare-name driver refuses input, then the unique original
prefixed card is used. Empty Plans exposes two Add plan targets; the ambiguous driver refuses
input, then the original header+ button is selected. A swipe sent during modal publication does
not scroll the new editor; the published form is observed before scrolling the same form.
A selected Insights tab is not an extra clickable target; its driver refuses input and the
already-visible original Savings goals child tab is selected. A host print uses the wrong console
encoding, then reads the same saved XML as escaped JSON. A verifier misses its Path import;
fix only the verifier and read the same retained observations. None requires an accepted action
replay, new fixture, discarded failure or application restart beyond the one planned cold test.

## Remaining boundary

This qualifies original physical AT-39/40 envelope cash/earmark/no-double-count and Flex
fixed/nonmonthly/flexible calculations on this one source. It does not accept all category trees,
multiple-account/currency/calendar scopes, alerts/notification delivery, future historical
envelopes, every goal/budget combination, whole device, general GC/ANR/crash/low-memory, target
two-second Home, system scaling/spoken TalkBack, iOS, OAuth/providers, Store signing/purchases or
product publication. No managed cases are added. Keep the full backlog and goal active.
