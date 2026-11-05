# Private compact holding-change admission (D-873)

## Fresh hypothesis after measured cost

D872 records a material cost of correct chronological holdings. This separate private
candidate replaces only Positions storage/application: first-seen type/place keys get
local indices, changes store index/date-sign key/delta in one owned buffer, and that
buffer sorts in place. Source entities/order are never sorted or mutated. Each output
is separately owned. Date/sign keys fit the DateOnly domain; increases/zero precede
decreases on a day. Equal sort keys have equal date/sign, so unstable tie ordering
cannot change a checked per-location sum or expose partially accumulated local state.
Both transfer legs still call original EffectAt. Checked long accumulation stays.
Quantity, FindConflict, Basis, writers/model and all product code remain unchanged.
This is not the previously rejected ByDay fallback/index sharing/cache/raw-order reversal.

Original current reference is ccb2a09c, HoldingsLedger SHA256
`0db03caad08dc2251a6dd0b9fc460bcfa6eb786915ed1c4d3e1af7cd6bd0ff5f`. No old raw-order algorithm is a candidate or reference
here. No product/permanent-test change, full Windows/Android build or device action
occurs in this host-admission step.

## Fixed guards and private contracts

Before build/timing, freeze12shapes10k/100k,1/20/200types, chronological/descending-Id
source order, one place per type. Separate Positions and combined Positions+all-type
Quantity modes use original AssetEventIndex outside timing. Quantity itself is unchanged
and is not timed alone again. Each role/mode/shape warms one second; seven alternating
pairs contain three calls per sample. Absolute times/current-thread allocations exclude
generation/index/validation/JSON/audits. No forced GC or runtime/config change occurs.

Every Positions shape requires median latency ratio<=0.95 and allocation ratio<=0.95;
every combined shape requires latency<=1.05 and allocation<=1.01. A failure closes the
candidate before second process/native/product, without retune, rescue or relaxed guard.
Only after all first-process guards pass is an independent same-binary process frozen
and run. That repeat is required candidate reproducibility, not replay of old product
tests. Actual PIDs79540/7160 both run .NET10.0.14/X64.
Strict private build has zero warnings/errors; used binary hashes remain identical.

512fresh generated private contracts per process compare current/candidate ordered
Positions or actual OverflowException, across seven event kinds, three types/two
places, same-place/cross-place transfers, two cutoffs, varied signs/ordering and large
quantities.191actual overflow refusals agree per process. Full15field snapshots stay
equal and each source enumerates exactly once. Separate null refusal and independent
result/record ownership controls pass. Both processes produce exactly equal contract
outcome packets. These are private admission conditions, not1024new product tests,
all-domain proof or an old successful17/28/53 suite replay.

The12performance histories are benign opening10/outflow9; original FindConflict accepts
them. Every full15field input JSON is retained, byte-identical between processes.
Before/after timing, original row references/order/multiplicity and ordered type-quantity/
position outputs remain equal.48paired measurement packets/672raw samples and12input
files per process are retained with SHA256 manifest/source archive/used binary. Contract
input snapshots are compared in memory; full generated contract inputs are not separately
archived. Timing role labels identify current reference versus compact candidate.

## Both-process results

All48fixed shape/mode guards pass. Positions medians in milliseconds:

| Shape | Current ms / first | Compact ms / first | Current ms / second | Compact ms / second | Compact/current allocation / first |
|---|---:|---:|---:|---:|---:|
| 10000-1-chronological | 0.6819 | 0.1481 | 0.7275 | 0.1664 | 0.3672 |
| 10000-1-id_order | 0.7372 | 0.2115 | 0.8444 | 0.3003 | 0.3672 |
| 10000-20-chronological | 0.9325 | 0.2154 | 1.1233 | 0.2956 | 0.3697 |
| 10000-20-id_order | 0.9970 | 0.2032 | 0.8677 | 0.2462 | 0.3697 |
| 10000-200-chronological | 0.9164 | 0.2358 | 1.1345 | 0.3760 | 0.3920 |
| 10000-200-id_order | 0.8482 | 0.2010 | 1.2228 | 0.3365 | 0.3920 |
| 100000-1-chronological | 8.1104 | 2.1640 | 8.4606 | 2.2574 | 0.3441 |
| 100000-1-id_order | 11.6552 | 4.1273 | 10.6939 | 4.0271 | 0.3441 |
| 100000-20-chronological | 9.2359 | 2.5072 | 11.5868 | 3.7855 | 0.3444 |
| 100000-20-id_order | 12.5648 | 3.2722 | 9.3386 | 2.3324 | 0.3444 |
| 100000-200-chronological | 11.5349 | 3.4546 | 10.9537 | 3.2660 | 0.3472 |
| 100000-200-id_order | 12.8871 | 3.2950 | 12.0723 | 3.5333 | 0.3472 |


Across both processes/all shapes:

- positions: latency ratio 0.2038–0.3766; allocation ratio 0.3441–0.3920.
- combined: latency ratio 0.3385–0.8305; allocation ratio 0.4000–0.4634.

This admits only the measured private hypothesis for further verification. It is not
Android/ARM64/AOT, whole Home/two-second performance, retained-heap/leak, production
integration or general multi-location/transfer-workload acceptance. No product
optimization has shipped from this evidence.

## Complete admission, remaining integration conditions

The one private host-admission task is complete. Next independent checks must cover
performance with high location cardinality and transfer-heavy shapes before integration;
the timing corpus here has one place/type. Then only relevant changed-code host/native
conditions, original reader/editor publication and required packages/adoption may close
the product batch. Preserve all correctness boundaries; do not replace this gap with
repeating unrelated successful suites or financial writes. Existing12shape results
and512contract outcomes are retained and need no arbitrary rerun.

Both measurement handles terminate successfully. One standard private Clean follows,
without supplemental/frozen cleanup/reuse. No compiler/contract/timing failure occurs
in this admission. Phone/Main/emulator/R13/authentication/photo/financial operations
remain untouched. Main32234/f342113f is only dated D871, not fresh liveness. Original
fault/D838 and broader security/accessibility/platform/external gates remain; overall
goal stays active. D870/D871 one-check CI responses both show CodeQL success and CI
cancelled; newer pushes cancel older runs. Neither run is queried/replayed again.
This commit receives its own single due-time exclusive-marker check, without polling
or waiting/job-tracking automation.
