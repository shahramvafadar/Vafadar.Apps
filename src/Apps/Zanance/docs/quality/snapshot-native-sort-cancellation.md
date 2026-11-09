# Snapshot native-sort cancellation - D-955

## Two actual failures in the public verifier

`SqliteSnapshotVerifier.VerifyAsync` compares stable captured databases with direct
`sqlite3_step` calls. It checked cancellation only at the start of each loop. A
request raised during the first native sort therefore still started the peer sort;
a request raised during the peer sort could be reported as content mismatch before
the next cancellation check. This concerns the shared verifier used by protected
staging, not a change to financial writes, restore policy or accepted receipts.

Two new focused contracts run the real public verifier on two independent in-memory
connections. A collation callback uses ordinary ordinal ordering of explicitly
prefixed ASCII labels and requests cancellation from the actual native sort, with
no timer, worker scheduling or elapsed-time oracle. Host inputs are1024rows for the
expected-side cancellation and768rows for the actual-side cancellation. The latter
has deliberately different payload values, so cancellation must win before content
mismatch classification. The original run has2failed/0passed/0skipped: the peer
performs5120sort callbacks after expected-side cancellation; actual-side cancellation
returns InvalidDataException instead of OperationCanceledException. Full typed
source and caller-usability assertions precede both failing assertions and pass.
The original logs, compiled PDBs and exact original fixture/verifier sources remain.

## Bounded correction and meaningful related checks

Check the original token immediately after each native step, before starting the
other sort or interpreting either result. The existing pre-loop check, prepare/
status guards, storage-class and full typed-value comparisons, floating-point bits,
BLOB spans, duplicate multiplicity, transaction ownership and finalization remain.
There is no sqlite3_interrupt/progress handler, connection closing, native/runtime/
GC, schema/model/migration change, abbreviated source comparison or new financial
operation. Cancellation uses the original token and still unwinds both statements
and caller-owned read transactions.

The fixture's original reflection-based full-value JSON source oracle is refined
before the corrected build to a canonical full typed value sequence, including
integer type/value and text type/all UTF8 bytes represented as hex. Fixed type,
numeric and hex fields have unambiguous separators. Full values are compared in
memory, not replaced with a digest; this permits the exact corrected scenario
source to be linked under native AOT without reflection metadata. Original and
corrected fixture/compiler lineage is retained rather than called byte-identical.

Corrected2new contracts pass, followed by7distinct directly affected existing cases
from the same corrected test binary:5content/type/duplicate refusal cases,1ordering/
multiset/typed-packet positive control and1last-BLOB-byte refusal. All9selected cases
pass with0skipped; this is not a full suite or9new tests. The existing successful
financial workload and unrelated tests are not repeated. Actual PDB documents bind
all3original and all3corrected verifier/scenario/test sources; Windows,shipping and
native compiler documents independently bind their archived actual inputs.

A running native call can finish its sort before returning. These new checks refuse
the next stage after it returns; they do not preempt the call, bound worst-case sort
latency, establish UI responsiveness or complete the two-second performance gate.
The [SQLite interrupt documentation](https://www.sqlite.org/c3ref/interrupt.html)
describes separate cooperative interruption behavior; that API is not introduced
by this correction. No performance percentage or general crash repair is claimed.

## Two corrected native cases with the selected engine

A separate SDK-only Release application references the real Vafadar.Data library
and source-links the same corrected scenario. It selects the existing SQLCipher
provider before opening a connection, with no startup database work. Its exported
reader accepts only shellUID2000, owned package, ranchu/x86_64, exact run and actual
PID; exclusive per-case markers prevent replay. Queries run the actual scenario
on a worker and return only cancellation/count/source-equality/caller-usability
outcomes. There is no copied verifier, hidden API or private memory/register input.

Fresh API36/x64/kernel4096 uses PID4948/UID10216, boot
`8b1eb8aa-dad5-4562-abc7-54f2de16eaa1`. Two distinct native-only inputs execute once:

| Cancellation requested during | Rows | Expected-sort callbacks | Peer-sort callbacks | Result |
| --- | ---: | ---: | ---: | --- |
| Expected sort | 512 | 2304 | 0 | Original-token OperationCanceledException |
| Actual sort | 384 | 1728 | 1728 | Original-token OperationCanceledException |

Both retain every typed source value and both caller connections remain open and
usable. The source comparison runs after the cancellation outcome is captured and
its callback counter is disarmed; it cannot fabricate the reported sort counts.
These are2new native observations, not host test-count additions or native baseline
failure reproductions. The SQLCipher engine is current selectedC5 and each packaged
ABI library hash matches its tracked package manifest. These unkeyed in-memory
fixtures are not encryption/key-management or whole private-source acceptance.

Strict Windows and full shipping Android are warning/error free. Shipping SHA256
`997b1ee79ea47d4c79ba16c431ce6a742c28db698d64ff4d5e37ff974ebeab85` (99573663bytes) has384AOT/408ELF/1608staticLOAD.
SDK-only native SHA256`0b94923f69d9d6a0122663aee260066ead3b392869fbe4667d26ded86fd82478` (12566947bytes) has
74AOT/85ELF/329staticLOAD. Both pass offline/nondebuggable/v2v3/ZIPCRC/
ZIP alignment/static16KiB checks and preserve the selected Mono hashes. This is
static alignment, not hardware16KiB or ARM64 runtime qualification. Actual native
PDB documents bind the verifier, source-linked scenario and private reader. No
whole-PE identity or independent installed physical version is inferred.

## Retirement and protected unfinished delivery

The initial45second boot observation expires before readiness. The same live owned
host67700 reports boot complete after74905ms and continues without relaunch/reset.
No case or installation had started during that refusal. There is one fresh install/
launch and no OS/font/display/key/locale/permission write. Before/after five OS
values, locale/key presence, boot, geometry and reverse are exact. Only the fresh
package is uninstalled and absence checked. Normal console stop signals host67700;
official AVD deletion retires25files/4366743016logical bytes with
0AVDfiles left. Small scoped user-home residuals and source/signed-package/log/PDB/
measurement evidence remain. There is no supplementary or frozen-scope deletion.
Five standard Cleans pass:Windows,tests solution,native Android,shipping Android and
compiler reader. Existing ADB5038/PID50972/creationticks639294095614173469 stays exact.

D954soleCI response at2026-11-09T13:29:27.621404UTC records
[CI42623376196](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42623376196)
in_progress and
[CodeQL42623375795](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42623375795)
completed/success. No second request, final-CI inference or successful-job replay
occurs. The owner is still away: physical D9447552/UID10439/data/original request and
Main adoption remain held, with no phone command or repeated question. Retained
5606/Q02 and terminal financial/frozen checkpoints are untouched. Original native
fault/ANR, whole performance/security/accessibility, iOS/provider/Store/release and
overall delivery remain open; this completed correction does not finish the goal.
