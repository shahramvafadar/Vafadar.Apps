# Complete current-row ledger reading (D-321)

## Cause and implementation

The retained physical ARM64 workload isolates the dominant complete read cost: 100,000 ledger rows take 4,231.32 ms inside the original reader, versus about 168 ms to open its non-pooled keyed connection and 316 ms for fresh final Home validation. Independent EF DiagnosticListener observation retains only fixed table/stage labels, durations and EF-call counts; it never emits SQL, paths, parameters or financial values. EF read-call counts are not row counts for direct reader consumers.

The exact unwrapped SqliteDataReader now captures 32 storage types once per successfully advanced row in one operation-local buffer. A borrowed statement stays inside the original EF command/transaction/lifetime. Native scalar access uses the original SQLite coercion/decoder; narrowing remains checked. Canonical textual GUIDs avoid temporary strings; alternate textual/blob representations retain provider fallback. Required-null failures, model tag/timestamp conversions and original date parsing stay intact. Replaced/derived readers retain their own typed getters. No row cap, cache of financial results, changed SQL/order/model, key pooling, KDF/native cipher change or skipped authority check is introduced.

## Controlled phone read evidence

Approved Samsung SM-A176B/API36/ARM64/4096-byte pages; original run 2566e73c942f49daaf33d58d0b0e2be4, identities, ciphertext, rollback and keys retained. Baseline pass 76b6af0ab128459e9b2dfab8d34d4585/PID 13739; candidate 62c4704aadf4490c8d18e9cb44363d1d/PID 14372.

| Shape | Entries reader before/after ms | Complete Home read before/after ms | Public complete ledger before/after ms |
|---|---:|---:|---:|
| reference |423.21 /321.61|1245.72 /1059.93|644.83 /642.66|
| tenfold |4231.32 /3343.73|4653.38 /3810.74|4509.28 /3570.87|

Ordered full-entry digests and before/after complete portable schema/typed rows match both shapes. This is one controlled same-fixture comparison, not statistical whole-app/cold-start acceptance.

## Final original native Home

Final pass f02798622828486db0765b00325488d1/PID 15274, Data MVID e38afa55-4666-45bf-8948-7e315ef0d3ce, caller 5ee19cbd-1283-4f65-9b49-5d76c4f1f286. All 12 original en/fa/de loaded publications pass, complete balances/native labels/financial presentation match, full raw schema/typed rows remain unchanged and no financial Save occurs. Observation expires twice while the same live process advances; only its report observation continues. No matching scoped ANR/crash event is observed. The completed independent process is stopped normally.

| Shape | Language | First / second publication ms |
|---|---|---:|
| reference |en|2037.31 /1831.92|
| reference |fa|2288.10 /2479.64|
| reference |de|2438.54 /2730.62|
| tenfold |en|8596.50 /8474.70|
| tenfold |fa|9202.58 /9582.70|
| tenfold |de|9114.64 /9171.54|

These loaded times still miss the performance objective. Reference timing overlaps the prior result; no general reference-speed or full cold/ANR acceptance is claimed.

## Verification and package boundary

42 focused Home read cases pass, including eight added storage/overflow cases; one final interceptor-reader case passes separately. These are 43 distinct related behaviors, not a new full-suite run. Test output is cleaned once. The final Windows development build has zero warnings/errors. The independent final signed Release/AOT/trim build passes and retains the original native hash and production assembly/profile inputs. No unrelated successful finance/crypto/native/UI matrices are repeated.

The first default main publish unexpectedly contains INTERNET; it is retained as negative packaging evidence and never installed. The final candidate is rebuilt with explicit Offline configuration. No credential value is inspected. Final offline package and main upgrade evidence follow below.

Local evidence: artifacts/phone-home-read-d321. QA-06 remains In progress for remaining ledger/projection/layout latency, full cold/ANR/low-memory and platform acceptance. Physical/iOS/provider/store/release gates are separate.

## Final main-package qualification

Explicit Offline publish passes the ordinary complete APK warning gate. Final package: artifacts/android-home-row-d321-offline/pro.vafadar.zanance-Signed.apk; 94707445 bytes; SHA-256 26859c40a14d1d3b08fe5f27267c735f6cc03edd6edf9e43d733fc83d3c2fa02. Permanent package pro.vafadar.zanance, min SDK 24/target SDK 36, ARM64+x64, 384 AOT libraries, no INTERNET/debugging/recorder/profile assets. Local test v2/v3 certificate SHA-256 remains 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. ARM64 native cipher remains 48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c; Mono remains 6532983fcd509af0c6fac4d39c37764128f5b12feedf9a9b32079635f70fc3e2.

Upgrade without reset starts main PID 16004. Its covered startup completes on the same process; original Home displays 979 EUR. Original Transactions then All invokes the complete-history path and retains the displayed 979 net, fee, transfer and refund packets. Return to Home retains 979 and original actions; no Save occurs. This is displayed main evidence, not a fresh full raw comparison of main private storage. A foreground guard refuses input while the display is Dozing; wake only follows a false keyguard state, without changing settings/authentication. A host stdout encoding error is corrected by reading the same saved hierarchy, never repeating navigation. Scoped ANR/crash queries return no matching events; this is not a full statistical failure matrix. Main remains Home; independent review is stopped and exclusive phone use is released.

Provider behavior was reviewed against the official versioned [value reader](https://github.com/dotnet/efcore/blob/v10.0.12/src/Microsoft.Data.Sqlite.Core/SqliteValueReader.cs) and [native record](https://github.com/dotnet/efcore/blob/v10.0.12/src/Microsoft.Data.Sqlite.Core/SqliteDataRecord.cs), then compared through independent EF expectations, rather than assuming all SQLite storage is canonical.
