# D-285 responsive covered Home publication

## Delivered behavior

Home appearance no longer opens SQLCipher synchronously to read its layout. The original complete
financial snapshot carries the original nullable profile layout; the page applies its original
section order/visibility before SnapshotLoadState uncovers actions. Null still uses the legacy
device choice, an explicit empty value still means the current default, and custom layouts retain
their complete order. Repeated layout application retains already placed native handlers. The
layout editor's explicit load/save path is unchanged.

Ledger balance/totals/unreviewed scans, per-account balances, open reimbursements, plan occurrence/commitment
enumeration, forecast and category aggregation now execute on workers over the captured original
inputs. They call the original Core methods with the same scopes, filters, dates and work rights;
no financial algorithm, quota, recurrence rule, result cap or cache is introduced. Translation,
formatting, brushes, observable collections and native layout remain on the presentation thread.
Each worker return rechecks the original page lifetime; fresh file/resource/rights validation
still precedes final publication. Period changes still cause a new captured read before uncovering.

## Actual native comparison on the retained data

The D-284 native workload route resumes the same protected reference/tenfold generations and keys.
Its factory now records synchronous context captures and its presentation event checks that native
layout is applied exactly once while the cover is still present. All twelve publications have zero
synchronous captures, matching complete original balances and successful native layout. Both final
full schema/header/integrity/typed-row comparisons against authenticated encrypted rollbacks pass.

| Shape / language | First / second native publication ms | Largest dispatcher gaps ms |
|---|---:|---:|
| 10,000 / en | 677.57 / 650.73 | 126.47 / 186.46 |
| 10,000 / fa | 825.53 / 878.26 | 227.38 / 207.39 |
| 10,000 / de | 815.84 / 763.21 | 231.30 / 306.88 |
| 100,000 / en | 2117.95 / 2243.18 | 118.74 / 127.39 |
| 100,000 / fa | 2272.61 / 2418.44 | 186.79 / 179.90 |
| 100,000 / de | 3112.26 / 2540.63 | 171.22 / 179.73 |

The largest tenfold dispatcher gap is about 187 ms versus D-284's 683 ms. The complete publication
time did not improve: a sample reaches 3.11 seconds versus the previous 2.31-second maximum.
These small local cohorts demonstrate reduced large-workload UI blocking, not an overall latency
improvement. No two-second/phone/cold-start/ANR acceptance is inferred. The 307 ms reference gap and
remaining projection/native publication costs remain explicit performance concerns. Do not undo
worker isolation or omit original financial work to obtain a shorter reported time.

Proof: artifacts/home-publication-d285/native-workload/home-workload-proof.json. Exact process 61156
completed normally; no restart or new fixture/key generation occurred. Page construction, initial
app startup, independent generation preparation and PNG encoding remain outside this timer.

## Focused verification

Thirteen actual SQLite Home cases pass (8.373 s), including three new nullable/default/custom layout
cases with one original read context and no second settings read. This adds three behaviors to the
qualified 3,604 baseline: 3,607 covered behaviors is not a new complete 3,607-test run. Unchanged Core,
library, backend, upstream producer and unrelated application suites were not replayed.

Actual protected Windows Home passes all 18 en/fa/de, light/dark, 360/412/1280 cases at process-local
200% typography, including initial cover, failed read, 18 native retries and retained amount handlers.
Nine extra actual saved/default/legacy layout cases change only the independently owned settings
field, restore its exact value and device fallback, and retain native section order/handlers. The
final complete financial fingerprint is unchanged:
a1f00084323125f47bc4cf0f3288df5bddc8b0da8c162172070b04efae3564f9.
There are 2868 native caption observations and 36 own-window captures. Proof:
artifacts/home-publication-d285/native-loading/home-loading-proof.json; exact process 15196 completed
normally. No main installation data, desktop input, physical device or OS setting was used.

The strict independent Windows build passes with zero warnings/errors (25.22 s). The normal-identity Windows rebuild also passes without warnings/errors (59.03 s), and compiled metadata is restored to pro.vafadar.zanance.

## Signed Release and Android runtime

The complete APK is artifacts/android-home-publication-d285/pro.vafadar.zanance-Signed.apk,
88,787,440 bytes, SHA-256 875523b4969a03282ec6de077a3cb9c52accc0d7b94696430ed565a66710dfbc.
Build-AndroidApk.ps1 passes its strict warning gate. Package pro.vafadar.zanance, min API 24/target 36,
one signer and v2/v3 signatures verify. Both embedded arm64-v8a/x86_64 application AOT payloads exist;
both native SQLCipher hashes match android.runtime.qualification.json. No producer/native suite rerun.

Only emulator-5570 on adb port 5038 was used after verifying AVD Zanance_Storage_Review. Exact cold
process 23236 reaches original ready Home, opens the actual New expense form, and native Cancel
returns to Home with the unchanged bidi-safe 0.00 EUR balance and enabled original action. No Save,
account creation, OS change, screenshot exception or physical phone operation occurred. The first
entry observation returned a transient null root; the same process was read again without restart
or repeated tap and the real form was confirmed. Proof: android-proof.json, android-first/ready/
entry/return.xml and apk-native-proof.json under artifacts/home-publication-d285. The 2728 ms cold
first-frame timer is not the ready-Home timer and does not establish cold performance acceptance.
The exact package was normally force-stopped after the successful same-process check. The final strict Android Debug build also passes with zero warnings/errors (45.79 s); Release test-output cleanup passes (0.87 s). Logs: android-debug-build.log and test-output-clean.log.

## Remaining work

QA-06 remains in progress for overall loaded latency, cold startup, reference-phone/tenfold behavior,
ANR/native rendering and iOS. Owner decisions for personal sync and other external/product gates
remain separate. This delivery changes neither encryption policy nor commercial activation.
