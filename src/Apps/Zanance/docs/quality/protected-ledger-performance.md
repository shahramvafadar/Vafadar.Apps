# D-280 protected search and validated-save workloads

## Remaining gap and complete delivered operations

D-278 measured current protected resource reads and calculations, but Q-02's previous search/save
numbers were still the historical D-75 plaintext baseline. The independent protected executable now
executes the original EntrySearch.Apply/ByDay and actual ZananceStore.SaveEntryAsync against both
original protected workload shapes. It does not copy financial/search algorithms or change app code.

A new explicit `ledger` mode creates only a new marked fictitious artifacts root and skips the already
qualified read/Plan/quick-resource/calculation timing cohorts. The default full run also includes the
new operations, so the D-279 hosted Windows lane automatically covers them. Resume retains its original
meaning and existing keys; no existing user profile or original D-278 fixture is changed.

Each search records a first call and three following samples, including complete filtering/grouping
and its result-count/no-duplicate checks. The known unique result is the original transfer at fixture
index 12, never two entries. Missing searches return zero; broad searches retain the whole ledger.
Numeric results are observed rather than assuming a match where the generated data contains none.

Four independent expense saves per shape use the real validated transaction. Read-back verifies exact
ID/account/amount/date/kind/title, then the original delete writer must return exactly that one entry
and a fresh lookup must find no leftover. Read-back/cleanup are outside the SaveEntryAsync timer.
The final original complete schema/header/integrity/typed-row verifier compares exported data against
the authenticated retained pre-migration encrypted rollback. This checks all original data, not just
entry count or totals, and requires no unprotected temporary database file.

## Actual Windows native measurements

Milliseconds: first call / median of the following three samples. Neither column is cold MAUI launch
or a physical-device latency. Search cohorts operate on the actual fully materialized protected data;
they exclude the initial database read and native rendering/input. Different workload/run order and
host load preclude causal speed comparisons with earlier cohorts.

| Original operation | 10,000 entries | 100,000 entries |
|---|---:|---:|
| Unique transfer search/group + result checks | 16.056 / 9.084 | 92.204 / 17.202 |
| Missing search/group + result checks | 7.580 / 8.087 | 16.631 / 16.996 |
| Numeric search/group + result checks | 13.298 / 9.122 | 25.898 / 23.063 |
| Complete broad search/group + result checks | 10.576 / 4.447 | 23.065 / 15.008 |
| Actual validated entry save | 170.024 / 68.810 | 70.038 / 69.033 |

Search result counts were respectively 1/0/0/10,000 and 1/0/2/100,000. All eight saves were read back
and exactly removed. Both complete logical comparisons passed, with both original plaintext files
absent. Fresh protected preparation was 2,232.752 / 37,349.106 ms; initial synthetic schema/data creation
is outside that boundary. These are observed protection times, not Home-opening performance.

Evidence: artifacts/protected-ledger-d280/measurements.json, artifacts/protected-ledger-d280.log,
artifacts/protected-ledger-d280-build.log and protected-ledger-d280-final-build.log. The process exited
zero. Initial and final strict Release compilation passed with zero warnings/errors; the final build
followed indentation-only cleanup, without replaying the unchanged measured operations. No failed
workload, data replacement, suppressed native warning, key change or fixture retry occurred in this run.

## Scope retained

Only the independent executable and its documentation changed. No schema, app UI, SDK/permission,
commercial enforcement, portable policy or app-native dependency changed. Current complete signed
APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk, SHA-256
6c01033549817a9b69994b0fb957302d5acc2655fc8e44f284c2c5e7339d5654. No unrelated app/UI/APK or full
test run is replayed: covered behaviors remain D-276 full 3,573 plus D-277 focused one (3,574).

QA-06 still requires actual loaded Home, native result rows/Save/read-back on the identified reference
phone, responsiveness/cold/ANR and iOS/platform acceptance. These current protected store/search/save
observations close the engineering baseline gap without claiming the two-second experience objective,
whole security acceptance, hosted success or release completion. The continuous goal remains active.
