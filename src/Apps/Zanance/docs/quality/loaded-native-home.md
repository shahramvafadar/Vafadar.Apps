# D-284 loaded native Windows Home

## Scope and method

The new Debug-only `home-workload` route opens the original HomePage/HomeViewModel on the retained
protected D-280 reference and tenfold fixtures. It does not seed rows, replace keys, migrate a new
fixture or invoke a financial Save. The compiled storageacceptance identity is required before
startup; the fixture root and both children must have their existing synthetic markers. Existing
identity/history/key files are required, with redirected directories refused. No main installation
data is moved or inspected.

Each real page receives the actual store for its retained protected generation. Complete account
and entry counts are checked, and all published currency totals must equal the original Core ledger
calculation over the complete snapshot. Each language has two separately created native pages at
412 px. Timing starts immediately before original native navigation/appearance, includes original
Home layout preference reading, complete financial reads/projection/publication and WinUI UpdateLayout,
and stops before PNG capture. Page construction, application startup, authentication, independent
generation preparation, final export comparison and PNG encoding are outside this boundary.

The process-local dispatcher timer requests a tick every 50 ms; its largest observed gap is recorded
through publication and one subsequent tick window. This is an indication of responsiveness on this
Windows host, not an Android ANR test or a physical-device measurement. Existing theme, calendar,
digits and native scaling are retained; this is not another layout/theme/large-font matrix.

## Observed results

| Retained data | Language | First / second native publication ms | Largest dispatcher gap ms |
|---|---|---:|---:|
| 10,000 entries / 20 accounts | en | 785.33 / 713.42 | 249.46 / 222.97 |
| 10,000 entries / 20 accounts | fa | 928.77 / 692.30 | 289.73 / 254.95 |
| 10,000 entries / 20 accounts | de | 627.14 / 633.25 | 227.49 / 287.24 |
| 100,000 entries / 200 accounts | en | 1849.33 / 1998.23 | 296.14 / 327.69 |
| 100,000 entries / 200 accounts | fa | 1974.04 / 2052.44 | 497.92 / 480.66 |
| 100,000 entries / 200 accounts | de | 1920.95 / 2311.73 | 410.09 / 682.63 |

All twelve original page publications and currency comparisons pass. Six own-window captures are
retained, including the RTL tenfold first viewport. Both final bounded in-memory backup exports pass
SqliteSnapshotVerifier against their authenticated encrypted pre-migration rollbacks: complete schema,
headers, integrity and typed rows remain unchanged. No search, Save, migration benchmark or old full
test suite was repeated.

Evidence: artifacts/home-native-d284/native/home-workload-proof.json, incremental
home-workload-progress.json and reference/tenfold-en/fa/de-loaded.png. The exact process 71996 completed
normally; no observation timeout or restart occurred. The review build and normal-identity rebuild
have zero warnings/errors. Automatic command review refused manual removal of four generated files;
the standard Rebuild target regenerated the correct compiled identity instead. No hook or review was
bypassed. The strict Android Debug build also passes with zero warnings/errors (android-build.log, 99.44 s); normal Windows metadata is restored to pro.vafadar.zanance (windows-normal-build.log, 21.33 s).

## What remains

This closes the missing loaded Windows native Home baseline after D-283's data-read-only comparison.
It does not prove cold startup, first hardware-rendered frame, every scrolled account row, phone
speed, two-second acceptance, Android ANR behavior, iOS, system scaling or product publication.
The 2.31-second tenfold sample and 683 ms dispatcher gap remain explicit performance concerns.
The actual appearance still synchronously reads Home layout preferences; any future removal must
preserve saved order/visibility, legacy device fallback, covered publication and native handler reuse.
Do not call the current baseline a fix for that remaining UI-thread work. QA-06 remains in progress.

Only Windows Debug diagnostics and its pre-start identity guard changed. Release application behavior,
financial algorithms, schema, native libraries and translations are unchanged. D-283's signed complete
Release APK and 3,604 behavior coverage remain the baseline; no new APK or full-suite run is claimed.
