# D-278 protected reference and tenfold workload

## Purpose and implementation

The earlier D-75 plaintext measurements do not establish current encrypted-store cost. The new
independent Windows executable in eng/benchmarks/Zanance.ProtectedPerformance links the existing
fictitious fixture generator and references the actual production Data/Core assemblies and reviewed
source-built SQLCipher 4.19.0 Windows dependency. Actual protected bootstrap, identities, DPAPI keys,
durability, generation publication and original readers/validators are used without a copied financial
algorithm, reduced derivation cost or key-bearing global pool. Application code/configuration is unchanged.

The reference shape contains 10,000 entries, 20 accounts and 100 manual schedules; tenfold contains
100,000, 200 and 1,000. The original fixture uses seed 42 and fixed date 2026-10-09, with no automatic
posting or enabled reminders. All data and keys belong to a new marked artifacts root, never app data.

## Measured Windows native results

Milliseconds below show first operation / median of the following three samples. First operation
means first measured call in this process, not cold OS caches or app launch. Raw samples remain in
artifacts/protected-workload-d278/measurements.json.

| Original operation | Reference first / warm median | Tenfold first / warm median |
|---|---:|---:|
| Read all entries | 453.388 / 158.617 | 1203.928 / 1191.737 |
| Complete Plan read and fresh validation | 235.396 / 131.983 | 163.378 / 138.041 |
| Quick-resource packet and fresh validation | 191.518 / 127.675 | 129.036 / 126.937 |
| Original balances and period totals | 12.779 / 1.487 | 29.447 / 19.066 |

Fresh tenfold protected preparation took 27,561.853 ms, including protected bootstrap, verification,
current generation open and publication. Initial fixture/schema generation is outside this timer.
Reference preparation was a **439.797 ms resume/revalidation**, not a fresh migration measurement.
Encrypted database sizes were 3,776,512 and 35,332,096 bytes respectively. The original plaintext
file was absent for both after protection. Complete schema/header/integrity/typed-row comparison
against each authenticated pre-migration encrypted rollback passed after the measured operations.

## Failures retained and final verification

An initial compile used an incorrect restore-history constructor argument; the call was corrected
to the actual initial key binding. The first executable then terminated after reference migration
because a raw exported-file hash was compared. Portable export changes SQLite page representation;
that failure is retained in artifacts/protected-workload-d278.log and is not a financial mismatch
claim. The reference generation and original keys were retained. The correction uses the actual
SqliteSnapshotVerifier over the authenticated rollback and exported in-memory source, comparing all
schema and typed values rather than file layout. Explicit resume reopened that same reference;
tenfold did not previously exist and was created once. No replacement reference/key was generated.

Strict Release compilation succeeded with no errors/warnings. The corrected resumed process exited
zero with both workloads complete; its terminal evidence is artifacts/protected-workload-d278-resumed.log.
The executable refused an existing root without resume and an output outside repository artifacts,
without creating the rejected output. The first guard-check wrapper expected the word "existing";
the actual guard correctly said "must be new". That wrapper assertion failed; inspecting the actual
exception confirmed refusal. No workload was regenerated for this observation.
No application source/UI/dependency/schema change requires a repeat of the successful application
build/runtime matrix or APK. Covered application behaviors remain 3,574: D-276's full 3,573 pass plus
D-277's focused additional scenario, not a new complete 3,574-test run. The complete signed APK remains
artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk (SHA-256
6c01033549817a9b69994b0fb957302d5acc2655fc8e44f284c2c5e7339d5654).

## Acceptance boundary

This closes the current protected Windows store/calculation workload evidence gap, not all QA-06.
Actual loaded Home/search/save on the identified reference phone, cold startup/ANR, Android/iOS and
the approximate two-second experience objective remain separate. Historical D-75 search/save data
must not be relabeled as current protected runtime acceptance. No CI execution is claimed here;
Native cipher candidates currently runs Linux/Android upstream candidates, not this DPAPI executable.


D-279 subsequently adds this executable to a dedicated Windows Native cipher candidates lane.
That configuration does not retroactively turn the local D-278 observations into hosted results.
See [windows-native-ci.md](windows-native-ci.md) for scope and the pending final-commit check.
