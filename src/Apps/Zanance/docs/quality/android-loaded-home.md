# D-291 original protected Release/AOT Android Home

## Scope

The independent `eng/benchmarks/Zanance.AndroidHome` caller compiles original production source,
XAML and resources at D-290 (`034caefa42135e35bc9cfc1898e0379a5c62fbb0`), with the existing
qualified source-built SQLCipher and patched 10.0.13 runtime. Main application source, schema,
financial algorithms, encryption parameters and its complete signed APK are unchanged.

Only package `pro.vafadar.zanance.androidhomereview` on `Zanance_Storage_Review`, emulator-5570,
adb port 5038, owns the fictitious data. Native scope is x64/API36/4 KiB, 412 px. It has no Internet
permission, retains FLAG_SECURE and uses no debugging/private-file extraction or device setting
change. Public derived JSON is transferred through exact PID/tag/run-scoped indexed log chunks;
the decoder checks completeness/conflicting chunks and refuses overwriting an output.

Preparation and measurement are separate processes. Fresh marked workloads have 10,000 entries,
20 accounts and 100 schedules; tenfold has 100,000, 200 and 1,000. Actual original migrations,
Android Keystore generation storage and protected bootstrap create the candidates/rollbacks once.
Measurement only opens the same existing identities/keys/ciphertext. Financial Save is never invoked.

Each language/shape has two new original HomePage/HomeViewModel instances. Timing starts before
native navigation/appearance and includes original coherent read, full projection/validation,
covered layout/publication, Android layout and frame callbacks. Page constructor, initial app startup,
fixture creation, generation verification and final export comparison are outside this boundary.
The dispatcher timer requests 50 ms ticks; its largest observed gap is separate from total latency.
Complete original ledger totals are compared to canonical VM packets, then to native TextView text
using the original NativeDigits display transformation. No sign, currency or direction mark is stripped.

Final bounded in-memory portable exports are compared against each authenticated encrypted rollback
with SqliteSnapshotVerifier: complete schema, header, integrity and typed rows. Previously completed
language/shape samples remain in private progress and are skipped on a focused continuation; final
preservation still checks both shapes. These retained metrics must not be reused after a production
source change.

## Retained failures and focused corrections

- Early build failures were confined to this new caller: the Android SDK prepends ProjectDir to
  AndroidManifest (use its relative original path), and the original plan snapshot property is Plans.
- The first generated activity name was guessed incorrectly; Android refused launch. The actual
  resolved component is crc64c987a4721ae93162.MainActivity. Subsequent starts use that observed component.
- An initial attempt had no public result; the caller now uses the Application.Current window owner
  rather than requesting a concrete App registration. This change alone did not qualify preparation.
- The fixture's unchanged no-redirect guard refused Android's /data/user/0 OS alias. Canonicalizing
  the app-data root before the guard allowed preparation; no marker/financial fixture was present in
  the earlier refused measurement. Existing failed files are retained, not deleted or silently reseeded.
- Missing Locale configuration handling in the independent activity let a language change recreate
  onboarding over the original Shell. Matching production activity handling passed this boundary.
  A namespace typo in that caller-only correction failed compilation and was corrected before execution.
- Native Persian money correctly used Persian digits/separators while the canonical VM packet remained
  Latin. The original exact-text assertion failed on that difference, not on the ledger amount. Reuse
  the production NativeDigits transformation for native expectations; retain successful English samples.

Evidence is retained under artifacts/android-home-review/ca28fce07b29472a95ff4da3cb66d2f0, including
prepare.json, measure.json, measure-canonical.json, measure-check.json, measure-locale.json,
measure-balance.json and successful prepare-canonical.json. Build logs android-home-d291-build-*.log
preserve the failed and corrected strict Release/AOT builds. No unrelated successful suite, Windows
matrix, upstream native corpus or unchanged main APK was rebuilt for this caller.

## Completed measurements

| Protected workload | Language | First / second publication ms | Largest dispatcher gap ms |
|---|---|---:|---:|
| Reference | en | 1786.76 / 1104.51 | 205.54 / 243.13 |
| Reference | fa | 1760.70 / 1212.50 | 212.16 / 267.54 |
| Reference | de | 1351.99 / 1578.36 | 363.15 / 232.50 |
| Tenfold | en | 7222.53 / 7225.87 | 266.54 / 294.73 |
| Tenfold | fa | 6586.67 / 6640.32 | 302.56 / 235.14 |
| Tenfold | de | 6895.45 / 6947.76 | 251.04 / 252.06 |

All twelve original pages, canonical/native balances and both complete final export comparisons
pass. The English reference case was retained from the earlier locale-correct caller; it was not
rerun after the native Persian expectation fix. All linked production Home/data/native source is
unchanged across those caller-only corrections. The final process measured the remaining five
cases and verified both shapes. Slow tenfold latency remains an observed concern, not a claimed fix.

Preparation PID 25550 completed once. Final PID 26746 continued the same run/data/keys, with no
restart when the first observation window expired. measure-final.json is the complete result;
final-ui.xml was captured only after timing ended, then the exact independent package was stopped.
Its fixture remains retained. No physical device or main package was touched.

The final strict Release/AOT build passes with zero warnings/errors (81.68 seconds,
artifacts/android-home-d291-build-11.log). Independent APK: 50,224,045 bytes, SHA-256
76e168195971b86079c047ab65c95fda4056f42359705702409dd9173ebbb100. Package/min24/target36,
v2/v3 signature with one signer, embedded x64 AOT and qualified native SHA-256
9579006cdaca361daba6b08cf5228319d605a60c84bfc33e6a1287a06e702644 verify. No INTERNET or
debuggable attribute is added. package-proof.json records the exact final identity/bytes/native/PIDs.
The installable main application APK remains artifacts/android-startup-d290/pro.vafadar.zanance-Signed.apk;
this benchmark APK is independent evidence, not a replacement phone handoff.

## Acceptance boundary

This is a loaded emulator baseline and complete fictitious-data preservation check. It does not
prove cold startup, GPU/hardware-rendered frame completion, physical-phone speed or ANR behavior,
arm64/API24/16 KiB OS, iOS, low-memory acceptance, the two-second product objective or release.
Existing 3,614 covered application behaviors remain the baseline; this is not a new full-suite run.
Personal Pro sync was separately approved for complete local delivery on 2026-10-15; this benchmark
does not implement it. Paid hosting, household sharing and commercial activation remain separate gates.
