# Complete physical ledger read (D-315)

## Observed cause and final change

The retained D-314 Samsung workload exposed a slow unbounded GetEntriesAsync query. On the same protected
100,000-entry database, the original public ledger read took 7,772.78 ms while the complete Home snapshot
already took 4,561.43 ms. A diagnostic 200,000 model timestamp conversions took 42.40 ms, versus 4.90 ms
for a BCL comparison; that small component does not explain the multi-second gap. The shared timestamp
converter, model and native library were deliberately left unchanged. Raw CLR field reads alone took
3,119.62 ms; native row stepping from D-314 excludes conversion and is not a complete-read benchmark.

Reuse Home's existing complete sequential reader only when both public date bounds are absent. The
indexed date traversal otherwise revisits encrypted full rows out of page order. The existing reader
retains every mapped field, converters, exact 64-bit values, independent tag collections, stable newest-first
ordering, original EF command/interceptor/transaction boundaries and cancellation. Optional inclusive
from/to queries retain their original SQL path. No new cache, reduced projection, KDF change, schema,
migration, compiled-model change or financial write is introduced.

## Same retained physical data

Owner-approved SM-A176B, API36, ARM64, 4096-byte pages. Exclusive input was coordinated with the other
project and released afterward. No hardware identifier, OS setting or personal record belongs to this report.
The existing independent package pro.vafadar.zanance.androidhomereview was upgraded without reset.
Run 2566e73c942f49daaf33d58d0b0e2be4, original ciphertext, identities, encrypted rollbacks and keys were
retained. Reference remains 10,000 entries/20 accounts/100 schedules; tenfold 100,000/200/1,000.

Baseline comparison b2871bb77ef64124a5196a22ed6c7368 ran in PID18893. After comparison
572574a002e14f3faa7e9985414227ef ran in PID21541. Expired report observation was followed on that same
live process, without reseeding or restarting. Complete ordered JSON digests are identical before/after:

- Reference: ba99b4e76712634048cb82f41f65c8381fcc0211388f2dc99cb61202bcf7f648.
- Tenfold: 405a7ba1c142a8f22c7bdf201e6f9fc5e3e602fc38b9cf9bc809048ff0798564.

Both phases also compare complete portable schema and typed rows before/after each shape. Derived hashes
are streamed to a null sink; no plaintext financial file is written. These are local fictitious reports,
not production telemetry. Already successful component conversion/raw-field loops were not repeated
in the after phase, and no financial Save occurred.

| Actual protected public read | Before ms | After ms |
|---|---:|---:|
| Complete reference ledger | 851.23 | 705.94 |
| Complete tenfold ledger | 7,772.78 | 4,313.16 |
| Reference full Home snapshot, unchanged implementation | 1,588.70 | 1,326.37 |
| Tenfold full Home snapshot, unchanged implementation | 4,561.43 | 4,506.02 |

The tenfold public ledger sample is about 45% shorter. These are finite same-data samples, not statistical
speedup or full cold-start acceptance. The unchanged Home path is a context observation, not a new Home fix.
Baseline Data module is 63d37561-20e3-4fef-a55f-7ff31d0e5f2d; final is
de3a7c1c-1501-4354-b44d-41b345079d00. Caller modules are respectively
1f857d89-53c9-4a90-aaa4-faebbfa78057 and 665831e6-4dbf-42be-b31f-17b418a43e68.

## Original native ledger and main upgrade

Fresh read-only comparison f984134f94ec46aa8ffcc3f6807cc043, PID22836, uses the original TransactionsPage,
view model, native SearchView, debounce, grouping and realized exact-result viewport. All three queries
return exactly 1/0/10,000 or 1/0/100,000 distinct original rows. Complete portable schema/typed rows remain
unchanged. SavedRowsReadBackAndRemoved is zero; D-314's eight successful unchanged validated writes were
not repeated. Retained per-shape progress refuses incompatible write/read-only continuation.

| Original native load and layout | D-314 ms | D-315 ms |
|---|---:|---:|
| Reference | 2,978.08 | 2,754.26 |
| Tenfold | 10,446.41 | 6,964.25 |

Final exact/empty/wide native-search samples are 439/426/521 ms reference and 716/869/1,093 ms tenfold.
Largest dispatcher gaps, including caller checks, are 587/1,270 ms. This is not ANR or low-memory acceptance.

The complete offline main Release APK was signature/package/native-byte checked and installed with -r,
without clearing data. Original main Home still shows 979.00 EUR and Zanance_Test_D312_Main. Transactions
and All open with the original fictitious transfer, fee and linked refund. Home again shows 979.00 EUR.
No new money, settings, profile, key or authentication operation was submitted. Main was left at Home;
the independent package was stopped and its fixtures/keys retained. Original UI text/layout did not change.

## Focused final checks and packages

34 HomeReadSnapshotTests pass under ContinuousIntegrationBuild, zero failures/skips. Expected rows now
come from an independent EF query rather than the newly shared public reader: complete packets/order,
tied/deleted rows, NULL/empty fields, exact values above 2^53, malformed tags, cancellation and all four
optional-bound combinations remain covered. This is a focused run, not a repeated full-suite total.
Main Windows strict build and Android full offline publish pass without warnings/errors. Independent
ARM64 Release/AOT/trim publish also has no warnings/errors. Test outputs were cleaned once afterward.
Unchanged successful financial writes, six Home layout groups, crypto/provider/backend tests and broad
Windows visual matrices were not replayed.

| Signed local package | Bytes | SHA-256 |
|---|---:|---|
| Independent baseline | 50,790,792 | 0494a52b2d6127c01435e190928a815e41b7aafe74c533d273abf74ff42c0e68 |
| Independent final | 50,397,406 | a2aa2a4672a958c44226a49035831b4ca3a76fa4d609620aef669903b32ed4dd |
| Main final | 95,411,957 | f6602c3cb75fc7b7d1fd929981d7d1959888b99098242ff410e134bba18fcab3 |

Main: artifacts/android-sequential-ledger-d315-qualified/pro.vafadar.zanance-Signed.apk.
Independent and report receipts: artifacts/phone-read-d315. Final packages retain v2/v3 local test
certificate SHA-256 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b,
min24/target36, no INTERNET/debugging/profiler, and reviewed ARM64 SQLCipher SHA-256
48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c. Main includes both ABIs and 384 AOT
assemblies; the independent ARM64 package includes 192. No Store signing or publication is implied.

QA-06 remains In progress: large initial Home/ledger is still slow and the two-second objective is not
accepted. Full loaded cold startup, low-memory/ANR, remaining physical scenarios, genuine authentication,
provider/Store/OAuth, other API/page-size matrices and Mac/iOS remain separate gates. This delivered
ledger improvement does not complete the full development goal.
