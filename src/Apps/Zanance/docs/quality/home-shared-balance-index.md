# Shared original Home balance index (D-500)

## Delivered boundary

Home already creates an AccountEntryIndex for its account rows. Its ordinary and confirmed
currency totals each built another complete index over the same captured unchanged entries.
Reuse the first worker-local index through LedgerCalculator.TotalBalancesFromIndex. Existing
TotalBalances callers retain their API and delegate to that same original implementation.
Balance remains the financial oracle; preserve account enumeration/order, archived/default versus
explicit scopes, original transfer endpoints/self-transfers, dates/review state, per-currency
ordering and checked overflow. Do not cache across source reads or share a mutable index across
page lifetimes. Complete ledger reading, validation/authority, cover and native publication stay.
No entity/schema, data model, preferences, SDK, permission, financial writer or visible layout change.

## Isolated allocation evidence

A separate Release console uses actual production Core methods, twenty fictitious accounts and
10,000/100,000 entries. Compare original three-index composition to the one shared index, including
both total views and account-row balances. Eight alternating samples per path follow warmup;
all original financial outputs match. Measurements include this composition only, not Home,
SQLCipher, native layout, process RSS or a representative device distribution.

| Entries | Original / shared median allocated bytes | Reduction | Original / shared median ms |
| --- | ---: | ---: | ---: |
| 10,000 | 564,024 / 190,280 | 66.26% | 3.07 / 1.88 |
| 100,000 | 8,678,488 / 2,893,640 | 66.66% | 9.31 / 4.15 |

Twelve new behaviour cases pass: eight financial-kind/date/review/currency cases, one-source
reuse, explicit archived/excluded/empty scope, checked account/currency overflow, and legacy
self-transfer identity. Twelve affected original index cases and seventeen original App-source
Home refresh/authority/lifetime cases pass. No unrelated suite or old financial cohort rerun;
test output is cleaned with zero warnings/errors afterward.

## Original native qualification

Retain Samsung workload rund53542fe57d6446ab52b82a757ee7f6e and its original protected generations,
keys and rollback oracles. No seed, financial write, keys replacement or Main private-data access.
First candidate26841/comparison3d91e14b72e5478aa4df53d711317969 matches actual balances/recent
rows but refuses the historical complete-presentation equality guard. Preserve the failed report.
Its original baseline da74d44331a64020b4f8fcd166575f29 ran22October; current observations run23October.
Do not declare a financial regression or weaken equality from this cross-date comparison.

Use the already verified D-499 caller as the current-day original code, with fresh baseline
2ca37368058f4cd3bb4db04e6e28357a/process27485 on the same old source. It completes six contexts/
twelve publications and writes new dated presentation packets without replacing old evidence.
D-500 candidate2e62ee8019734aeca402270c71262713/process27959 then compares against those exact
current-day packets. All six contexts/twelve publications pass complete financial presentation,
native text/balance/recent identity and full raw schema/typed-row preservation. Source/base
remain unchanged. This necessary changed-code comparison repeats no accepted financial action.

| Samsung path | Reference median / maximum ms | Tenfold median / maximum ms | Largest reference / tenfold dispatcher gap ms |
| --- | ---: | ---: | ---: |
| Current-day original | 2163.66 / 2369.32 | 5090.95 / 5335.35 | 912.17 / 953.24 |
| Shared index | 1712.36 / 2059.99 | 4953.51 / 5590.87 | 841.68 / 831.49 |

The original-then-candidate sequence is a small local cohort, with caching/device/order effects.
Do not infer a stable whole-Home speedup from its medians. Tenfold maximum worsens, and even one
reference candidate exceeds2seconds. Retain the allocation improvement; two-second/general
ANR/crash/cold/low-memory objectives remain unaccepted. Exact process failure buffers contain
no recorded fatal event; that finite absence does not resolve historical failures.

Independent Windows storageacceptance run84c8a8f3b07341ea87b6620313674857/process10260 passes.
Six en/fa/de contexts/
twelve publications resume the retained marked protected reference/tenfold sources, compare
complete portable schema/typed rows, preserve original balances and apply native layout behind
the cover with zero synchronous settings captures. Six application-only PNGs are visually reviewed.
Reference median642.68ms/max745.49; tenfold median1194.92ms/max1594.21. This is Windows development
qualification, not a Windows product delivery or a cross-platform speed comparison. Both original
and final owner input desktops remain Default; no owner desktop input or full-screen capture.

## Build, restoration and external limits

Strict normal and independent Windows builds and complete signed Offline shipping/ARM64 caller
builds have zero warnings/errors. Preserve the initial nonexistent-project-name refusal before
any native build/install, then use the actual original project; no source workaround is made.
Shipping APK: 1221c6ed92300133facd9092d3485c0f4891433a55a7ea10d64090496faadf65 (96542453bytes),384AOT,
ten original native engines equal, v2/v3, nondebuggable/no INTERNET. ZIP page alignment and all408
ELF-library load segments pass16KiB static checks; runtime/minimum-API acceptance stays open.
Caller: 052a18e0f0c44f7f4e19908d958b9e4d782689e89c4bc9a8f495c28745cb543e (56152412bytes),192AOT,
five unchanged engines/v2/v3/Offline. No shipping Main upgrade while D-494 is armed.

After terminal candidate completion, restore the exact retained independent APK/data/stopped
state and Main25043/Home979/all141 public tuples/APK/OS. Two exact hash-matched owned XMLs are
removed; local original evidence remains. No other package, personal file/account, system setting
or private Main key/database is touched. D-494 remains24914 with its same four resumed pending
alarm/intent identities and no delivery yet; no provider query/rearm/subject install. D-496's
inactive future-day source stays intact. Main adoption of D-497/D-499/D-500 is pending actual
D-494 completion, not inferred from the candidate's independent qualification.

The single delayed D-499 response has CI40001343155 in_progress, while service40001343159,
identity40001343188 and CodeQL40001343317 succeed; its consumed monitor is deleted. No second
request is sent. New-head CI follows push. SDK terms/minimum-API/16KiB runtime, real authentication,
spoken accessibility, remaining security/stability/device, bank/corpus, iOS/provider/Store and
publication gates remain separate. Goal remains active; pending future-day work is not complete.
