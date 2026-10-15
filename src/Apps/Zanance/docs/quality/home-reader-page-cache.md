# D-286 operation-scoped Home reader page cache

## Observed cause and delivery

D-285 reduced UI blocking but loaded tenfold Home still reached 3.11 seconds. A temporary staged
native run isolated the remaining cost: the complete snapshot averaged 1614 ms for tenfold inputs,
versus 34 ms ledger aggregation, 229 ms plans, 88 ms forecast, 91 ms presentation and 101 ms final
validation. A refined two-sample English run measured ordered entry materialization at 1485/1549 ms;
the complete plan read was 8 ms and the initial keyed opening/quick read 126-155 ms.

Changing only this reader's page-cache target reduced those same complete ordered entry reads to
681/846 ms. This supports connection-local page-cache pressure during indexed encrypted-page reads
as a substantial cause; individual decrypt/eviction counters were not collected. The exact same
retained protected files, original DPAPI keys, original SQL ordering, all columns and all rows remain.

Home now requests `PRAGMA cache_size = -32768` only after confirming a non-pooled connection and
before its deferred reader. This is a suggested 32 MiB page-cache allowance for that connection,
allocated as needed, not a process-memory cap, new key cache or persistent database setting.
[SQLite's documented semantics](https://www.sqlite.org/pragma.html#pragma_cache_size) specify a
connection-local suggestion that resets after close. No page-size, KDF, pooling, sync/durability,
transaction isolation, read limit, schema or native producer setting changes. The larger transient
memory allowance still needs reference-phone/low-memory acceptance.

## Final original native page, without temporary timing code

| Retained data / language | First / second publication ms | Largest dispatcher gaps ms |
|---|---:|---:|
| 10,000 entries / en | 610.71 / 541.61 | 167.38 / 100.12 |
| 10,000 entries / fa | 772.57 / 795.19 | 234.48 / 223.53 |
| 10,000 entries / de | 673.63 / 635.70 | 211.50 / 229.01 |
| 100,000 entries / en | 1350.57 / 1614.91 | 106.04 / 248.09 |
| 100,000 entries / fa | 1666.81 / 1561.30 | 197.25 / 155.97 |
| 100,000 entries / de | 1609.39 / 1836.58 | 125.75 / 191.53 |

All twelve original Home publications pass on the retained D-280 reference/tenfold generations.
Complete account counts and original ledger balances match; there are no synchronous appearance
captures, and original layout is applied before uncovering. Both final full schema/header/integrity/
typed-row comparisons against authenticated encrypted rollbacks pass, without Save or key replacement.
Timing boundaries remain D-284's actual navigation/read/projection/native layout, excluding cold
startup, page construction, generation setup and PNG encoding. No phone/two-second claim follows
from all six local tenfold samples now being under two seconds.

Evidence under artifacts/home-latency-d286: profile/home-workload-proof.json and stage-durations.json;
read-profile/home-workload-proof.json and read-stage-durations.json; cache-profile/home-workload-proof.json
and cache-stage-durations.json; preliminary final-native/home-workload-proof.json, then final pooling-guard guarded-native/home-workload-proof.json and six own-window captures.
Exact processes 75120, 14696, 36172, 66288 and final 62264 completed normally. No observation timeout or restart.
The first refined temporary profiling build failed because its observer declaration was missing;
that temporary setup was repaired once. All temporary production observers and reduced review loops
were removed before final build. Final normal source changes only the scoped PRAGMA and its comments.

## Focused verification

All sixteen actual SQLite Home cases pass, zero failures/skips (9.797 s). Three added AT-286 cases
exercise read-only and read/write connections, observe -32768 at the real ordered entry query,
compare complete original packets, validate authority and verify a fresh non-pooled context reverts
to its original default. Ordinary pooled callers retain their default allowance instead. Existing snapshot, foreign-file, rights, layout and concurrent WAL cases
remain covered. The qualified baseline is 3,610 behaviors (3,607 plus three), not a new full-suite run.

The final pooling-guard review build passes without warnings/errors (24.58 s). D-285's unrelated successful
layout/200%/retry matrix, upstream tests, Core, backend and complete application suites were not
repeated. A later source review added the pooled-caller exclusion before delivery; its extra focused case passes. The earlier unguarded candidate APK is retained separately and is not the final handoff. The normal Windows rebuild passes with zero warnings/errors (25.46 s), restoring compiled
pro.vafadar.zanance identity. Release test-output cleanup passes (0.91 s). The strict signed Release
build succeeds through Build-AndroidApk.ps1, including its zero-warning gate.

## Final APK and Android runtime

Final handoff: artifacts/android-home-latency-d286-final/pro.vafadar.zanance-Signed.apk,
88,787,440 bytes, SHA-256 7d2a1039e81925860461b39c207958533ae1e5ab247558533479fb8389b77e65.
Package pro.vafadar.zanance, min API 24/target 36, one signer, v2/v3 verified. Both embedded arm64-v8a
and x86_64 application AOT payloads and qualified original SQLCipher native hashes verify.
The earlier artifacts/android-home-latency-d286 candidate is superseded and was not installed.

Only verified Zanance_Storage_Review, emulator-5570 on adb port 5038, was upgraded. Exact process
23526 reaches ready Home after a cold launch; actual New expense opens and native Cancel returns
to Home with the same bidi-safe 0.00 EUR balance and enabled original action. No Save or account
creation. One transient null entry root was observed again on the same process, without restart
or repeated navigation. The exact package was normally force-stopped afterward. No physical phone,
OS setting or screenshot policy was touched. The 2145 ms first-frame timer is not ready-Home timing.
Proof: android-proof.json, apk-native-proof.json, android-first/ready/entry/return.xml and
android-cold-start.txt under artifacts/home-latency-d286. Logs: android-final-release-build.log,
windows-normal-build.log and test-output-clean.log. No unrelated native producer, UI matrix or full
suite was rerun.

## Remaining acceptance

QA-06 remains open for reference-phone measurements, low-memory behavior, complete cold startup,
Android ANR and loaded reference/tenfold rendering, iOS and device/OEM acceptance. This local result
does not authorize product publication or change separately gated sync/sharing/billing decisions.
