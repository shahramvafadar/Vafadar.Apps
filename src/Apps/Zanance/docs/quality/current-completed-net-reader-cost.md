# Current completed-net reader component cost (D-877)

## Descriptive scope after the correctness fix

D875 fixes actual completed balance/goal-period overflow and D876 qualifies current
ARM64/public Main adoption. This step describes component cost without a new candidate,
source edit, guard relaxation or production reversal. Baseline is30f13ba07b5c166ecc68ee9c98950110018ef806.
The pre-D875 algorithms remain disqualified at their known range failures regardless
of speed; they are used only on benign agreeing histories here.

The historical Balance method is copied exactly into a private wrapper. The complete
historical GoalTrend file is normalized only to a private namespace and reverses
exactly. Its unchanged external helpers use current original Core. Current functions
are invoked directly from original Core. This normalization/assembly placement and
runtime inlining/devirtualization can affect code generation: timings/allocation
differences do not isolate an exclusive Int128 causal share.

Freeze24new shapes:10k/100k rows,1/20/200 groups, four modes. `balance` computes all
account balances using original AccountEntryIndex slices, whose construction is outside
timing. `goal_account`, `goal_holding`, `goal_earmark` compute original GoalTrend for
every respective goal using the full input collection, as that API expects. These
are batch costs for all groups, not individual goal-page times. A200goal batch scans
the source separately for each original goal call; this is not a claim about Home's
actual call graph or a shared-index optimization.

Inputs are fictitious in-memory rows only. Positive10/negative9 effects are distributed
over188days from1May through4November2026, Gregorian, with supported net values and
fixture remaining inputs computed outside timing. Balance/account sources include
opening100; holding/earmark sources use their original respective identifiers. Original
FindConflict accepts every represented holding history. No database/writer/financial
Save/seed, provider, native/device/authentication/photo action occurs.

## Actual single process and preserved complete outputs

Strict private build succeeds with zero warnings/errors. Actual.NET10.0.14/X64,
PID82164, warms each role/shape for one second, then measures seven alternating
pairs with three complete batch calls per sample. Stopwatch/current-thread allocations
exclude source generation/index construction/remaining calculation/JSON/validation and
output normalization. Result fields are consumed during timing, without mapping/IO.
No forced GC, runtime/config change, second process or native timing occurs.

Before/after every shape, complete public input JSON including all32ledger/15holding
fields where present, account/goal/allocation inputs and remaining targets is exact.
Original row references/order/multiplicity and tag-list ownership are preserved. Full
ordered balances or every goal status/pace/ETA/SoFar/complete-period count and every
period's dates/contribution/one-off flag agree between roles.24full input JSONs,
24paired packets/336raw samples, complete ordered output audits/source archive/used
binary and SHA256 manifest are retained. These are24private workload audits, not new
product unit tests or another54case replay. Generated goal identities are captured in
the retained full inputs; a second generated corpus is not being treated as identical.

## Actual per-batch medians

| Mode | Rows | Groups | Old reference ms/batch | Current ms/batch | Old bytes/batch | Current bytes/batch |
|---|---:|---:|---:|---:|---:|---:|
| balance | 10000 | 1 | 0.0224 | 0.0217 | 32 | 0 |
| goal_account | 10000 | 1 | 0.2539 | 0.4267 | 86184 | 86184 |
| goal_holding | 10000 | 1 | 0.4220 | 0.4145 | 86120 | 86120 |
| goal_earmark | 10000 | 1 | 0.3328 | 0.3323 | 86160 | 86160 |
| balance | 10000 | 20 | 0.0248 | 0.0250 | 640 | 0 |
| goal_account | 10000 | 20 | 0.7445 | 0.8863 | 203680 | 203680 |
| goal_holding | 10000 | 20 | 0.9743 | 0.9787 | 202400 | 202400 |
| goal_earmark | 10000 | 20 | 0.9826 | 1.0081 | 203200 | 203200 |
| balance | 10000 | 200 | 0.0290 | 0.0300 | 6400 | 0 |
| goal_account | 10000 | 200 | 5.8424 | 6.1565 | 1316800 | 1316800 |
| goal_holding | 10000 | 200 | 6.0295 | 6.2919 | 1304000 | 1304000 |
| goal_earmark | 10000 | 200 | 3.8478 | 4.0921 | 1312000 | 1312000 |
| balance | 100000 | 1 | 0.4138 | 0.4707 | 32 | 0 |
| goal_account | 100000 | 1 | 3.9376 | 5.0165 | 806184 | 806184 |
| goal_holding | 100000 | 1 | 4.3823 | 4.2727 | 806120 | 806120 |
| goal_earmark | 100000 | 1 | 5.1327 | 5.1948 | 806160 | 806160 |
| balance | 100000 | 20 | 0.4508 | 0.5133 | 640 | 0 |
| goal_account | 100000 | 20 | 10.1741 | 12.1359 | 923680 | 923680 |
| goal_holding | 100000 | 20 | 8.6335 | 8.9813 | 922400 | 922400 |
| goal_earmark | 100000 | 20 | 8.7976 | 8.7683 | 923200 | 923200 |
| balance | 100000 | 200 | 0.6387 | 0.7461 | 6400 | 0 |
| goal_account | 100000 | 200 | 52.0134 | 57.7280 | 2036800 | 2036800 |
| goal_holding | 100000 | 200 | 44.7712 | 48.7669 | 2024000 | 2024000 |
| goal_earmark | 100000 | 200 | 42.7052 | 49.5434 | 2032000 | 2032000 |


Across all six sizes/group counts for each mode:

- balance: observed current/reference median latency ratio 0.9702–1.1682; current allocation 0–0bytes per batch.
- goal_account: observed current/reference median latency ratio 1.0538–1.6802; current allocation 86184–2036800bytes per batch.
- goal_holding: observed current/reference median latency ratio 0.9750–1.0892; current allocation 86120–2024000bytes per batch.
- goal_earmark: observed current/reference median latency ratio 0.9967–1.1601; current allocation 86160–2032000bytes per batch.

There is a finite observed cost in several large goal batches. Keep absolute values
and raw pairs; percentage ratios on tiny batches are descriptive and not significance
or stable Android evidence. Historical wrapper/Core placement can also influence
enumerator allocation and must not be renamed an exclusive arithmetic optimization.
The account index, database/decode/IO, source verification, UI publication, retained
heap and full Home startup are not measured. No leak, two-second goal or whole-app
speed acceptance follows. Correct D875 completed-net guards remain unchanged.

## Terminal scope and next independent source question

This named one-process component-cost task is complete. No candidate is accepted or
retuned; no legacy reversal/previous rejected compact-buffer rescue is authorized.
Source hashes for current LedgerCalculator/GoalTrend still match the preregistered
inputs. Measurement handle terminates successfully, one standard private Clean passes,
with no supplemental/frozen delete/reuse or old successful suite/full product build.
No build/observer/measurement failure occurs in this step. An initial literal wildcard
path lookup for goal stores is a discovery failure only and its corrected glob search
does not change any source. Main9535/acf7d16d remains only dated D876, not fresh liveness.

Source inspection identifies another concrete unqualified consumer question: signed
earmark aggregation in GoalCalculator funding/account readers and original goal UI.
Those original raw Sum calls need an actual valid offsetting-history reproduction
before claiming or changing a defect. This is next ready independent work, not a
reason to repeat D875/D876 successes or financial writes, or to wait for CI/owner.
Original fault/D838 and broader performance/security/accessibility/platform/external/
release requirements remain, overall goal active. Each due CI check preserves its
single response after exclusive marker, with no polling or tracking automation.
