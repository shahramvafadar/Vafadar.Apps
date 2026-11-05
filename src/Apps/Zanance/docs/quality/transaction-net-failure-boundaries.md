# Exact transaction net results and covered filter failure (D-854)

## Actual requirement and complete integration

ZEX-S0104 requires checked monetary results and a clear error instead of a wrong
number. EntrySearch.SignedResult negates without checked arithmetic; DayNet adds
currency effects without checking. Four unrepresentable total/day cases, two Int64
minimum sign cases and one invalid day hidden by interval cancellation expose7 actual
failures among12 new Core cases. Five controls preserve representable completed
cancellation, currency partition/key order/zero results, original row identity and
exclusion of unknown accounts/transfers. No application financial write is needed.
An initial test-only required CurrencyCode omission fails compilation before execution;
that refusal is separate from the12 actual baseline cases and7 product failures.

Signed effects now negate with checked arithmetic. Ordinary currency totals retain
the original sorted result dictionary and checked addition. Only an overflowing
currency is promoted into a local Int128 accumulator; later original refunds/reversals
may cancel exactly before one checked final narrowing. No public money/storage type,
kind classification, query/grouping algorithm, enumeration order, schema/model,
original ledger balance-order policy, migration, cache/source guard, runtime or GC
configuration is changed. The original ByDay/Apply query logic stays intact.

The integration also covers synchronous Transactions filter/reformat callbacks.
Previously Refresh calls PresentSnapshot without the LoadAsync exception boundary;
a new unrepresentable filtered sum must not escape a UI property callback. The current
original publication/page/query references own a failed presentation. SnapshotLoadState
covers only a completed source, refusing uninitialized or pending-read takeover. The
original inline failure/accepted-operation feedback is shared with LoadAsync, without
resetting the filter or replaying a write. Original completed rows may remain in the
model, but the actual results and Add action are covered/disabled on failure.

Final selected host evidence:12 new Core+3 new snapshot-presentation cases+2 directly
affected existing day/drill-down cases pass:17 across two test assemblies, not a full
suite. New presentation cases qualify retained-source failure/retry, pending original
read ownership and uninitialized-source refusal. Core checks are not rerun merely
because later private observation or App integration changes. Strict final Windows,
full offline shipping and private builds pass without warnings/errors.

## Actual Android, preservation and observer lineage

Use original retained5606/API36/x64/kernel4096/boot
6663f98e-6546-4809-a0bc-285f11a96674 and existing5038. No original D809/D833 data,
provider/history/trace/register/R13 read, reset/reboot/reconnect/pair, phone command,
OS/locale/geometry write or intentional fault is performed. Own package is
pro.vafadar.zanance.transactionnetreview, freshUID10245.

| Stage | Actual PID | Accepted work and preserved limit |
| --- | ---: | --- |
| V1 |27402|12 original Core calculations pass with empty full20source equal. One distinct Checking opening0/two Income rows MaxValue and2 is prepared once. Actual original SnapshotLoadState records OverflowException. The observer wrongly resolves a fresh transient VM instead of the page BindingContext, so its UI-state assertions refuse and do not qualify the page. One original-root return verifies full fixture equality. |
| V2 |27704|One necessary no-reset read-only adoption preserves UID and the exact original A1/E2 hash. Core/setup actions explicitly refuse. Actual native appearance, with the page's own BindingContext, qualifies initial OverflowException/HasFailed/Readyfalse/Days0/Addfalse/Resultsfalse and fully visible original error. |
| V3 |27956|Second necessary no-reset continuation preserves A1/E2 exactly, then adds only one distinct Expense2. Removing that added row from the complete typed snapshot reproduces all old source exactly. Original All read is Ready with one day group and three source rows; one enabled, fully visible native Income chip is clicked. The original filter retains KindIndex2, observes OverflowException, covers results/Add and displays the full inline error without another load or financial action. |

V1 empty20source hash:
5A14C098E3E9E4139270F00C8FC597BC56FD810AEF9ADFBA51D51556D71FE835.
Retained original A1/E2 before/after the initial error and V2 adoption:
ECF18A5FB42725BAAE89766120EF0AE5B366E9BD59E7FAA85BF663A97E72DA80.
Declared one-row-delta A1/E3 source, unchanged through native Income click/error:
E50FB590169B44C924FBCC4EC08EEB748D017FB8F48A0400E5E04CD4AC49022E.
These are actual fictitious database preparations, not zero data writes; no successful
Save/Import/Undo/authentication/photography is replayed. Native calculations V1 and
native page covers V2/V3 are separate versioned evidence, not all observations on the
final binary. V3 has CoreCases0, explicitly preserving the earlier12 without rerunning.

The original error TextView is fully visible at[84,1170,996,1214], with the same visible
bounds and zero ellipsis. Text is "The action could not be completed. Please try again."
V2 has HasLoadedfalse/Days0. V3 has HasLoadedtrue and one retained model day group;
actual ResultsVisiblefalse/AddEnabledfalse and no pending publication prevent showing
stale or partial results. Do not relabel the retained V3 model count as zero rows.
No Retry or extra financial command is pressed; input kind remains Income.

Private diagnostics add only exception-type observers in original SnapshotLoadState
and the synchronous filter catch, plus the original service-container capture. The
private copies reverse to the current original normalized sources without their
passive additions. No exception message/stack, memory, key or private Main data is
exported. One unused intermediate continuation with the wrong transient-model lookup is never installed; V1 was installed and its observer refusal is retained. Private continuation
CS0649/IDE0044 refusals are preserved before correction; original successful Core and
fixture work is never replayed. Premature package verification finds no finished APK,
then follows the same live publisher to completion; no build is restarted for timeout.

## Fixed cost measurement: acceptance refused

This is the cost of a required correctness correction, not revival of rejected day
bucket/sorted-list/query/index hypotheses. Before benchmark execution freeze10000/
100000 rows,1/6 currencies,1/365 days, NetByCurrency/ByDay:16 shapes. Each role warms
one second;7 alternating pairs execute two materializations/sample. Require median
regression at most5%, allocation growth at most1%, additional pair peak at most64MiB.
A second process requires every first-process gate. No own build runs during timing.

Actual .NET10.0.14/X64/PID80008 retains full32-field JSON inputs for all8 source shapes,
exact original-reference outputs, group ordering/row object identity and independent
original tag-list references before/after. The exact original reference is copied
before the correction; current EntrySearch input hash still equals that measured
correction after later App-only integration. These are synthetic in-memory inputs,
not device financial preparation,17 more tests or whole Home timing.

| Rows / currencies / days | Operation | Original ms | Corrected ms | Regression | Gate |
| --- | --- | ---: | ---: | ---: | --- |
|10000 /1 /1|NetByCurrency|0.135800|0.164350|+21.0236%|Refuse|
|10000 /1 /1|ByDay|0.354850|0.369900|+4.2412%|Pass|
|10000 /1 /365|NetByCurrency|0.088700|0.126600|+42.7283%|Refuse|
|10000 /1 /365|ByDay|0.320550|0.315100|-1.7002%|Pass|
|10000 /6 /1|NetByCurrency|0.140800|0.131250|-6.7827%|Refuse|
|10000 /6 /1|ByDay|0.288800|0.298500|+3.3587%|Pass|
|10000 /6 /365|NetByCurrency|0.203400|0.114300|-43.8053%|Pass|
|10000 /6 /365|ByDay|0.363550|0.434450|+19.5021%|Refuse|
|100000 /1 /1|NetByCurrency|1.283850|1.214800|-5.3784%|Pass|
|100000 /1 /1|ByDay|3.792300|3.513300|-7.3570%|Pass|
|100000 /1 /365|NetByCurrency|2.202900|1.960050|-11.0241%|Pass|
|100000 /1 /365|ByDay|4.337500|5.031950|+16.0104%|Refuse|
|100000 /6 /1|NetByCurrency|1.597050|1.588550|-0.5322%|Pass|
|100000 /6 /1|ByDay|4.025450|3.482500|-13.4879%|Pass|
|100000 /6 /365|NetByCurrency|1.688900|1.577700|-6.5842%|Pass|
|100000 /6 /365|ByDay|5.675750|5.994750|+5.6204%|Refuse|

Six distinct shapes fail: five latency gates and three allocation gates overlap.
Worst measured latency regression is42.7283%; three10000-row ungrouped samples also
observe32 additional allocated bytes/query (200to232 or480to512). All-thread allocation
and pair high-water measurements are not exclusive algorithm allocation/leak evidence;
no exclusive cause is claimed. Other improvements do not rescue the failed matrix.
No second process, retune/resampling/threshold relaxation or native timing follows.
The correction is required to stop wrong monetary output; it is not a performance
improvement, speed-neutral acceptance or closure of the two-second goal. Whole
performance remains an explicit open prerequisite.

## Artifacts and exact retirement

| Used final artifact | SHA256 | Bytes | AOT / ELF / static16KiB LOAD |
| --- | --- | ---: | --- |
| Complete shipping |adef63e1344f47f2e531bbb44b58ce63aa9dfb50d13e1cc1ce3f7a5aed16d0b2|98881135|384 /408 /1608|
| Final private V3 |c748dcec835ecdb57f94504a3baead2e3f64580af9a8ea01a564a3af59263526|56758044|192 /204 /804|

Final packages pass original per-ABI Mono, offline/nondebuggable, v2/v3, CRC/ZIP and
static16KiB checks. Nine current App/Core/Data AOT ZIP members equal their own build
artifacts (six shipping/three private). Earlier used V1/V2 APK/source/bindings, unused
wrong-model candidate, final source archives, full input JSONs/raw cost pairs and all
failures remain. This is not whole managed-PE, actual hardware16KiB or crash acceptance.

Only freshUID10245 is uninstalled after terminal PID27956/finished publication/source
equality. Original emulator package UIDs/five OS values/locale-key presence/size/density/
boot/reverse/shell match the preflight exactly. No remote XML is created. Seven own
standard Clean profiles pass (Core tests, presentation tests, cost, Windows, full shipping,
private V1, private continuation); no supplementary/frozen scope or alternate cleanup.
Evidence is under local Zanance-D854-current.txt. Prepared shippingadef63e1 is not Main;
Main26327/655 is only the dated D853 checkpoint, not new liveness. D607 is terminal,
D838 pending and D841's actual branch unexecuted. No original native cause/remedy,
whole performance/security/A11Y/platform/provider/Store/release acceptance is inferred;
the overall goal stays active.

The sole historical D853 response records [CI success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41998634132)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41998634321).
That older revision does not qualify D854; no second query or old job rerun occurs.
