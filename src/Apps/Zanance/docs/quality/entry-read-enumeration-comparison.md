# Full entry enumeration comparison (D-112 / QA-06)

## Question and boundary

The Android Debug stage evidence in home-debug-platform-and-native-stages.md identifies full entry materialization
as a remaining cost. Microsoft.Data.Sqlite executes asynchronous I/O synchronously, as documented by
[Microsoft](https://learn.microsoft.com/dotnet/standard/data/sqlite/async). This motivates a bounded experiment;
it does not establish that synchronous EF enumeration improves application or device performance.

An independent Release executable uses the existing PerformanceFixture to create new fictitious directories,
10,000/100,000 entries, 20/200 accounts and 100/1,000 schedules. It compares actual current GetEntriesAsync with
the same EF AsNoTracking query and ordered complete entities enumerated synchronously inside Task.Run. Both
capture their context before the worker starts. The candidate checks cancellation on each row. No production
source, model, schema, database, security state, SDK, permission or financial algorithm is changed.

## Observations and decision

After separate first-use reads, seven paired measurements alternate the query order in one process for each shape.
Complete serialized entity packets, ordered ids, all four date-bound shapes and original fixture fingerprints match.
Full-entity comparisons and fingerprints run outside timing; no row cap or selected-field projection is used.

| Shape | Current async median | Candidate sync median | Paired candidate outcome |
|---|---:|---:|---|
| 10,000 entries | 78.80 ms | 69.38 ms | Faster in 6 of 7 pairs |
| 100,000 entries | 931.34 ms | 825.38 ms | Faster in 3 of 7 pairs; slower in 4 |

Tenfold current samples range from 736.60 to 1,639.28 ms; candidate samples from 732.38 to 1,113.60 ms.
Allocation is approximately 184.7 MB per complete tenfold query in both paths. The differing medians do not
demonstrate a reliable paired gain or a meaningful allocation reduction. This desktop observation does not
establish Android Release, cold startup, first paint, cancellation latency, ANR or Q-02 acceptance.

Decision: retain the current production read. Do not adopt a query-path change from this noisy result or repeat
the experiment without a new observed cause. Existing D-75 profile capture/cancellation and D-110 full native-source
reuse remain. Further performance work needs a more specific measured cause or controlled device evidence.

Evidence: artifacts/entry-read-comparison/comparison.json and the independent EntryReadCompare executable.
The executable is a local experiment, not an application dependency or production feature. It never opens the
application's data directory or the owner's files.

This is a documentation-only decision. No tests, native flows, build or APK are repeated: D-111's 1,535 passing
main tests (App.Tests 299), strict Windows/Android Debug/Release builds, native matrices, signed complete
artifacts/android/zanance-d111-release.apk and exact original-data comparisons remain the unchanged app baseline.
QA-06 stays partial; the remaining owner, physical-device, OS-reader, provider, iOS and release gates are not closed.
