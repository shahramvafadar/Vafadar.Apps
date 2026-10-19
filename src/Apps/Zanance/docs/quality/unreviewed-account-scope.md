# D-373 original unreviewed scope and checked totals

## Delivered behaviour

LedgerCalculator.Unreviewed now builds one operation-local account lookup instead of scanning the
entire scope for every unreviewed entry. It merges the source/destination record groups by their
original positions; it does not concatenate, deduplicate account records or route a self-transfer
twice. Preserve original entry order, ordinal currency grouping, explicit/default scope, historical
destination metadata and the existing EffectOn/per-currency Sum calculations. No global cache,
financial formula copy, filtered history, key-bearing pool or weaker crypto is introduced.

Cross-entry currency accumulation now uses checked long addition, matching the calculator's
existing documented money boundary. Previously individually valid effects could wrap when their
combined result exceeded long. The count increment is checked too. Existing checked within-entry
sum order remains significant: repeated source records can overflow before an offsetting destination,
so the lookup preserves the complete original scope order. No schema, model, migration, product
preference, permission, provider, pricing, language or UI layout changes.

## Managed and original AOT boundary evidence

Fourteen new Core cases cover one count per touched currency, destination amounts, duplicate account
records and order-sensitive overflow, explicit archived/excluded selection, pre-opening review,
legacy self-transfer, nontransfer destination metadata, ordinal currency case, irrelevant/confirmed
rows, input immutability, one source enumeration and both cross-entry overflow directions.

The first twelve cases pass against the original implementation. Two additional arithmetic cases
then fail before the checked-accumulation correction: no exception was thrown. Final execution
passes all 48 related cases (14 new, 15 ReviewFix, 5 GoldenData, 2 Reconciliation, 12 AccountEntryIndex)
with zero errors/failures/skips. This is not a rerun of the unchanged full suite. Initial harness
compilation omits required fictitious Account.Name values; fix only those inputs. Direct DLL execution
rejects the MTP-style filter option before any tests run; use its actual xUnit -class/-result-xml
options and verify report counts. Neither attempt adds managed coverage. Clean Release test output
after qualification; cleanup and builds complete without warnings/errors.

Both independent Release/AOT x64 and actual Samsung ARM64 preparation call the original calculator
and observe Income/Expense OverflowException refusals, without storing either boundary input.
Actual Core MVID f5cdbbf9-289a-4fef-b0f7-7c12eb00473b matches later candidate Home completion.
These four platform assertions are not four extra managed unit tests. Production Core source
SHA256: c07d5e89ed3aa4a08e44f29733e47e6e8ecdc9da7ddd15eda6a6063e6e564583.

## Distinct protected review corpus and preserved failed preparation

The older reference/tenfold corpus contains confirmed rows only, so it cannot qualify this path.
Use fresh independent roots with the original 10000/20/100 and 100000/200/1000 entry/account/plan
shapes, marking every entry Unreviewed before the original protected bootstrap. Old qualified
ciphertext, rollback, identities and keys are never reseeded. No main financial Save occurs.

Emulator run 25b9dab64bf849fcb24e39246d773215 first fails on PID 31028 in a raw pre-encryption UPDATE.
Its completed reference fixture is retained; no keys/identities exist yet. A narrowly guarded
continuation validates that exact failure/root, completed migrations/counts and confirmed rows,
then creates only the missing tenfold shape. Read-only EXPLAIN establishes missing
zanance_household_write on the raw connection. Use the original ZananceDbContext to register its
actual function; never substitute a permissive callback, drop a trigger or weaken household policy.

A caller-owned in-memory SQLite backup receives the same intended Review update through an original
context. Compare its complete schema and typed rows with the updated real fixture before encryption;
all other records/fields remain identical. Both shapes pass on resumedPID31234, followed by the
original protection pipeline and both AOT overflow refusals. Keep the original failure packet.
The phone independently prepares run d53542fe57d6446ab52b82a757ee7f6e on PID 5851 using the corrected
context path. Its initial host XML import error occurs before installation/native preparation;
the same host intent is retained and the still-installed original APK proves no native rerun.

## Original complete Home comparisons

No smaller history, inactive-section omission, cached finance or copied presentation algorithm is
used. Require original Activity focus, covered publication, complete canonical financial fields,
native balances/layout and full raw schema/typed-row preservation. No timing threshold is asserted
in unit tests. These are descriptive bounded cohorts, not statistical causal or release proof.

Emulator: original control 38f39add0d4243a2ba9b29361873042d/PID31475, candidate
1d2b201782724f24821e35d8b9f9eb8a/PID31711, same protected run above. Original Core MVID
1dc8f662-f73f-4c76-ac66-78fe515068c0; candidate uses the recorded f5cdbbf9 Core.

| Emulator shape/language | Original first/second ms | Candidate first/second ms |
| --- | --- | --- |
| reference/en | 1216.52 / 857.09 | 1271.87 / 884.36 |
| reference/fa | 716.27 / 897.12 | 847.30 / 850.71 |
| reference/de | 885.07 / 814.97 | 762.33 / 745.90 |
| tenfold/en | 3032.90 / 3028.15 | 2550.67 / 2674.90 |
| tenfold/fa | 3010.81 / 2887.78 | 2612.91 / 2611.47 |
| tenfold/de | 3127.78 / 3113.27 | 2615.02 / 2650.39 |

Reference median 871.08 ->849.01ms; tenfold 3030.53 ->2613.96ms (about 13.75% reduction).
Do not apply this result to the previous all-confirmed corpus.

Samsung: original comparison/baseline 0542c6ade5bb44cc9a333080305bd4cd, candidate
7b999fc35452422da310960516ac7e6c. The original signed D-371 caller is reused rather than rebuilt.
Actual physical model/API36/ARM64/4096-byte pages and owned foreground are required. Control Core
MVID 073d7d44-04b3-4368-9e25-98e1259b44b1 and Data 7fa88f14-7072-414f-8eab-f952e630a375;
candidate Core f5cdbbf9-289a-4fef-b0f7-7c12eb00473b, Data 9bb39b57-5856-4a86-ad8b-fe9d406ad0d6,
caller 6974a631-db68-4f12-90f0-3db219bd64a2. Do not equate architecture-specific module identities.

| Samsung shape/language | Original first/second ms | Candidate first/second ms |
| --- | --- | --- |
| reference/en | 2532.98 / 1873.45 | 2534.94 / 1877.49 |
| reference/fa | 1872.29 / 1854.58 | 1966.51 / 1950.70 |
| reference/de | 2038.61 / 2065.83 | 2007.61 / 1890.63 |
| tenfold/en | 7536.52 / 7534.31 | 6512.85 / 6352.22 |
| tenfold/fa | 7704.54 / 7743.47 | 6622.93 / 7063.96 |
| tenfold/de | 7381.98 / 7485.30 | 6366.69 / 6488.93 |

Reference median 1956.03 ->1958.61ms (essentially unchanged); tenfold 7535.41 ->6500.89ms
(about 13.73% reduction). Every new tenfold sample is below the old cohort's minimum. This does not
establish the two-second objective, whole startup, GPU completion or general GC/ANR stability.

## Actual native failures are retained

Control PID 6385 authoritatively ends APP CRASH(NATIVE), status6/SIGABRT on a .NET TP Worker.
Monodroid reports the sgen-scan-object.h:93 assertion. After confirming death, preserve the exact
APK/hash/comparison/fixtures and continue on PID 7134: its five already complete language/shape
cases are retained; only tenfold/de remains. Candidate PID 7571 ends EXIT_SELF/status255 after
SIGSEGV in SGen worker, without a captured managed exception. The single bounded same-binary
continuation PID 7933 retains all three completed reference cases and finishes the tenfold cases.
Both final packets verify the complete protected data. No passed completed case is replayed.

These are real runtime failures, not observer timeouts. No registers, heap, dump or raw stack is
captured, and no GC/runtime setting or source binary is changed to make the continuations pass.
Successful retained case completion does not fix or erase either crash. SGen scanning is in the
[official runtime source](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/sgen/sgen-scan-object.h);
the corrupt object/full cause is not identified by this evidence. General native stability stays open.

## Complete main package and restoration

Strict Windows development build: zero warnings/errors, 27.66s. The canonical complete signed
Release offline Android script passes its warning gate. Main APK:
artifacts/android-unreviewed-scope-d373/pro.vafadar.zanance-Signed.apk, 95268767 bytes,
SHA256 8b818aa4f806eb4e5539a4d4dfd514410ec58819604e458d933f61961e7c17af.
Local test certificate 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b,
min24/target36, ARM64+x64, 384 AOT images, no INTERNET/debugging/profiling. Eight existing native
runtime/cipher files match D-369 exactly. No native bridge from withdrawn D-372 is included.

The host verifier initially expects the wrong badging label; actual minSdkVersion24/target36 are
confirmed before any install, then only the label is corrected. Emulator upgrades without reset
from PID 12893 to 32060. One transient pidof failure preserves that same live 32060; no second launch
or reinstall. Original meaningful Home and exact app/OS display state match. Samsung upgrades
11372 ->4166; native Home/profile Zanance_Test_D312_Main/979 EUR and exact original settings match.
After terminal independent completion, stop only that caller, retain every fixture/key and return
to the same upgraded main 4166 with identical native state and installed bytes. Release the
coordinated phone turn. No real authentication or phone/system setting is changed.

Independent x64 APK 1ec73526f7040a0f84efd8b97419425b4624b9cc0f614e30021056ce50cc152c;
ARM64 APK 3fb952f6e40bd0a3d9d3bb97906af222e76b27e669382281bd676b7a4ca1b87d. Both have 192 AOT
images, match the four existing engine files and contain no network/debug/profiler/new-native assets.
Their strict builds pass. Native main-state equality is not a full typed database comparison of
all main profiles; the full comparisons above belong to independent protected workloads.
Receipts, failures, signed bytes and retained-case timings remain in artifacts/unreviewed-scope-d373.

QA-06's latency/cold/GC/ANR/low-memory, wider native/reader/iOS/provider/Store and release gates
remain open. No unchanged successful full finance/crypto/backend/UI suite is repeated.
