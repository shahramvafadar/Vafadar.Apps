# Current capacity commitment dates and units (D-903)

## Concrete remaining defects

Capacity's nonmonthly cost component already respects remaining ownership (D902),
but the loan-principal and other-goal components retain obsolete commitments.
An old60EUR monthly installment and its current120EUR continuation are both split
against1000EUR outstanding at12percent annual interest: the old code sums50+110EUR
principal instead of only110EUR. Ended/count-exhausted/paused installment dates and
expired contribution rules similarly continue subtracting current costs.

The contribution method's unit is also significant. The approved design and
ContributionPlan.Amount document SpendingCut as per financial month. A weekly
120EUR monthly cut is formerly multiplied to521.79EUR; a yearly cadence divides
it to10EUR. Finally a negative median eligible income produces a negative goal
commitment, artificially increasing capacity, while the original contribution
preview already defines a nonnegative suggestion.

Twenty new focused cases reproduce eleven failures before production changes.
They cover remaining installment slices and inclusive final-date/count boundaries,
expired contributions, three spending-cut cadences, negative/zero/positive income,
three unchanged fixed-amount cadences and original paused/quantity/excepted-goal
scope. All20 pass after repair. Only six existing affected boundary/history and
actual final-money refusal cases additionally run and pass; no full suite is replayed.

## Correction and preserved contracts

PlanActions.HasScheduledDateOnOrAfter shares the original recurrence anchor/end/
count and slice bounds. D902's exact bound checks and original-date predicate move
without a recurrence rewrite; a saved source reversal and four related boundary/
history cases verify that move. The undated monthly-equivalent formula is byte-for-
byte unchanged after text newline decoding. BudgetPlanning's dated overload delegates
to the shared predicate. Installments use it before their unchanged interest/principal
and annualization calculation. Contribution rules must likewise retain a future date.

Spending cuts remain monthly instead of being scaled by reminder cadence. Fixed
amounts retain per-date annualization, original multiplication/division order and
minor-unit rounding. Income shares use EligibleIncome.Suggestion, including its
existing zero/negative-income policy. Original goal/account/currency exclusions,
exceptGoalId, outstanding principal caps, historical settlement IDs and checked
final commitment bounds remain. No payment, budget, allocation, recurrence identity,
model/migration, runtime or GC rule changes.

## Platform and actual original UI

Strict Windows and full offline384AOT/408ELF/1608staticLOAD pass; private original
x64 is192AOT/204ELF/804LOAD and SDK-only ARM64 is27AOT/37ELF/138LOAD. Signature
full/x64 v2v3 and SDK v3, ZIP/CRC/static16KiB, offline and non-debuggable checks pass.
Static alignment is not actual16KiB hardware acceptance. Eleven actual compiler
document checks bind all three changed Core sources and the test source to native,
full and ARM64 builds; the same bindings match across the private continuation.
The original bootstrap reverses exactly after only service capture; no whole-PE
identity is inferred.

Initial native17885/UID10266 executes20 once with exact empty20component source
before/after. One new fictitious setup creates payment and loan accounts, old/current
installment slices, a monthly-cut goal/weekly reminder plan and a retained goal with
an exhausted monthly contribution rule. Automatic posting/reminders are off and
ledger entries remain0. These are new synthetic opening/plan values, not a replay
of accepted financial UI Save/Import/Undo or a real loan payment.

The original ReportsPage correctly displays the−230EUR summary. The initial observer
assumption that component rows are inline is disqualified: their real location is
the original explanation sheet. One private helper continuation upgrades without
reset to18173/sameUID, requires the consumed case receipt and exact original source
digest, and never reruns calculations or setup. Its reset in-memory count remains0;
the20-case success belongs to the saved initial response, not a fabricated new count.

The attempted original Shell route reports InvalidOperation while the actual
reminder-introduction dialog is observed; preserve the exception and do not infer
an exclusive cause from its type. Select only original Not now, granting no permission.
Actual UI then selects Goals and activates the visible capacity-help button once.
Two immediate hierarchy captures produce no remote XML and are disqualified, with
the same live18173 observed. Do not retap or relaunch: only the incomplete observations
continue after settling. Both failed remote paths remain absent at retirement.

The actual original KpiSheet pairs these labels and values inside its viewport:

| Original row | Actual value | Value bounds |
|---|---|---|
| Loan principal |110.00EUR|[810,1201,994,1245]|
| Planned contributions to other goals |120.00EUR|[805,1266,994,1310]|
| Capacity for goals |−230.00EUR|[778,1331,994,1375]|

Full native XML text, label/value ancestry, enabled state and positive bounds within
the actual viewport support this finite observation; XML does not independently
prove glyph rendering or native ellipsis counts. The actual Close button returns
to the original Goals report and its unchanged−230EUR summary. No transactions
drill-down, Apply to budget, suggestion acceptance, authentication or export occurs.
Full20component source digest
`D7A6D41A71B580BABB1A5550B89270B21C3CD79689022BA306F27829DB04A1B7`
is exact through no-reset continuation, intro, real explanation and Close. Private
observer fields remain unbound after route refusal and are not used as UI acceptance.

Samsung SDK-only8512/UID10431 independently passes20 once and retires. These are20
new tests exercised on host/x64/ARM64, not60 new tests or all cases rerun on the
final private UI observer. Original Main6511/public state returns exactly.

## Main adoption, exact retirement and limits

The used full99491743byte shipping APK SHA256 is
`9f8f480db61f61291b56d95d6996809038716243d7179db864bb22fb3ac15193`.
After original signer/current ARM64 admission, one Main no-reset upgrade6511to10611
preserves UID10380/CE155299/DE154603, grants, other original public packages and
five OS values/boot/geometry. First Ready Home979.00 retains all102owned raw nodes,
attributes and order in existing landscape rotation1. No second frame/Reload/
relaunch/orientation change obtains equality. Private Main database/key and complete
financial equality are not inferred from public Home preservation.

Only fresh review UID10266, SDK UID10431 and seven hash-bound remote XMLs retire:
five emulator files191161bytes and two phone files75732bytes. Local raw evidence and
all used signed APKs remain. Original emulator public state is exact and Q02 returns
normally in samePID15609, without a new cold/performance/source acceptance. Five
scoped standard Cleans pass; no supplementary/frozen deletion or old successful
financial/authentication/photo/test matrix is repeated.

D902single CI response is consumed: CI42150262986in_progress; service42150262978,
backend42150263002 and CodeQL42150262954 completed-success. Never query it again
or replay successful jobs. Independent work does not wait for CI. Original fault,
D838pending/D841undeployed and whole-performance/security/accessibility/platform/
provider/release acceptance remain separate. Overall delivery remains active.
