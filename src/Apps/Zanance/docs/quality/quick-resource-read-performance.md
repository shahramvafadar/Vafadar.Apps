# D-274 complete protected quick-resource read performance

## Complete original operation

The original quick-resource review reopened keyed native connections between EF queries.
One DbContext alone did not improve the measured warm operation. The final operation captures
one original context/file and explicitly keeps its non-pooled connection open through both
choice snapshots, complete original inputs and every original access/stale-review validation.
Standalone resource-choice reads also keep their operation's connection open. Disposal releases
the original connection/read lease; later explicit actions validate through fresh contexts.

No cipher format, KDF cost, global key-bearing pool, credential cache, financial schema, money
calculation, commercial switch, permission or network dependency changed. Provider behavior:
https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings and
https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async.

## Direct native evidence

BinaryCompatibility times actual production GetQuickResourceReviewAsync, not copied queries.
The same independent encrypted fictional fixture (-321 EUR minor units), native provider,
key and production model are used. Five samples separate first use from four warmed reads.
Original 32 migrations, portable read-back and intentional wrong-key refusal pass outside
the timer. Expected HMAC errors belong to that negative-key case.

| Actual production variant | First read ms | Four warmed median ms |
|---|---:|---:|
| Prior Release Data assembly | 1220.5975 | 999.38165 |
| One context without explicit open | 1411.534 | 988.697 |
| Operation-scoped open connection | 565.2337 | 82.42435 |

Baseline DLL SHA-256: 391d47a3fea7e4771bc5b171d49bca237e9e0b253b3e23be1595190040f1545a,
retained from the prior Release output before compilation. The context-only variant was rejected
as a performance fix. Warm operation reduction is about 91.8% here, not overall app startup or
physical-device acceptance. First-use/JIT and background load remain distinct.
Evidence: artifacts/quick-review-performance-d274/{baseline,candidate,candidate-open}.json;
fixture: artifacts/native-sqlcipher/binary-host/quick-review-d274.

## Tests and native application review

Two AT-274 cases use real SQLite and a forwarding factory counter, without copied policy/query
algorithms. They cover source/no-source, one complete-read context, original rows, absent defaults
and selections, fresh subsequent validation and drained ownership. The first focused assertions
completed but fixture cleanup failed because an idle plain-SQLite pool retained its file. Exact-file
pool retirement fixed that test fixture; unrelated pools remain untouched. Cancellation analyzer
warnings were corrected.

The first complete 3,571-test run passed before the explicit-open refinement. Final affected
Data.Tests (796) and App.Tests (1,179) passed on the final dependency; unchanged projects retain
the successful full baseline. No failing/skipped test remains and successful unrelated modules
were not repeated after the refinement.

The first Windows review launch retained stale main PackageName despite the requested independent
ApplicationId. Its exact process was stopped, with no independent acceptance claimed. No inspection
or assumed restoration of another installation was performed; absence of capture output does not
prove that installation's state. Touch/rebuild and compiled storageacceptance metadata checks
preceded the corrected launch. Debug Home/holiday routes now reject main identity at CreateWindow,
before covered storage startup; Release behavior is unchanged.

Corrected own-window native results, final builds and signed APK identity accompany the delivery
commit. The original Home review checks en/fa/de, both themes, 360/412/1280, scale 2, modal/ordinary
reloads, native amount handlers and complete financial fingerprints. Android uses only
Zanance_Storage_Review, adb port 5038, emulator-5570. Original New expense opens with the original
account/categories; Cancel returns to Home without Save. Transient null hierarchy and stale prior
XML are negative observations, not entry acceptance. Successful fresh entry/return trees are kept.
Foreground screenshot protection and device settings remain unchanged.

## Loaded startup remains a separate concern

Before: system first-frame TotalTime 1664/1401/1462 ms; ready-Home upper observations
15354.47/13384.76/12559.66 ms. Sample zero reconstructs elapsed start from its initial evidence
timestamp and includes the observation gap; other samples use a continuously running timer.
These are process-cold/warm-cache observations of the original empty emulator fixture.

The initial candidate cohort overlaps Windows rendering/fingerprint review and affected tests.
Ready observations are 18932.94 ms, unresolved at 16235.18 ms, and 17442.35 ms. Null native roots
and repeated stale false observations are retained. The predetermined next cold sample is not
proof that the unresolved sample succeeded. This cohort is not comparable startup-improvement
evidence; accessibility overhead and host load prevent attribution. No two-second loaded-Home
claim is made. Physical/reference workload, native large-data publication/ANR, real OS scaling,
spoken screen readers and iOS acceptance remain open. The development goal stays active.

## Final delivery evidence

Corrected Windows native review passed 18 contexts, 36 explicit original Home reloads and 360
native observations. Amount handlers were retained and the complete financial fingerprint stayed
a1f00084323125f47bc4cf0f3288df5bddc8b0da8c162172070b04efae3564f9.
The original Persian light narrow capture was inspected directly. The independent process exited
normally after completing its proof. Windows builds have zero warnings/errors; the final normal
PackageName is restored to pro.vafadar.zanance before handoff, with no main review launch.

Complete signed Release APK: artifacts/android-quick-review-final/pro.vafadar.zanance-Signed.apk,
88,238,406 bytes; SHA-256 c726b091f181f8ba7d70c584193395185b945acf3b517a262fee40ab886aeb94.
Package pro.vafadar.zanance, target SDK 36, arm64-v8a/x86_64, v2/v3 signature verified.
Both libsqlcipher.so hashes match the accepted source-built baseline. Upgrade, actual editor open
and Cancel return are verified on the owned emulator; Home retains 0.00 EUR.

Next ready complete QA-06 batch: cover original Home until its entire first snapshot is loaded,
with translated failure/retry and disabled premature actions. The current native startup evidence
shows a temporary No accounts card before the existing account is published. Do not misreport
that empty intermediate frame as usable Home or solve it by skipping protected startup/security.
Identity/sync/billing/sharing, samples, device and iOS decisions remain separate gates.
