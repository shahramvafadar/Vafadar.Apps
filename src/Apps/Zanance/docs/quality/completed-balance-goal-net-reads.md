# Exact completed balance and goal contribution nets (D-875)

## Actual failures and original reader contract

Baseline5f3e59da contains four completed-net readers: LedgerCalculator.Balance and
GoalTrend's AccountBalance/HoldingQuantity/Earmark period contributions. A supported
chronological +Max/-Max/+Max history supplied in +Max/+Max/-Max storage order rejects
before its offset, despite completed Max. Original domain design section4 defines an
end-of-day balance as opening basis plus the complete in-scope sum; goal trend defines
a complete month's net contribution. Storage order is not a separate financial limit.

The initial strict private Core probe encounters an uncaught original Balance overflow
in its input-validation observer, before durable three-branch output; its source/log
remain. This is a probe termination, not an Android native crash or a product test pass.
The observer is corrected to preserve raw Balance refusal separately and verify that
same account history in actual chronological order. .NET10.0.14/X64/PID24688 then records
three actual GoalTrend OverflowExceptions with expected Max and exact in-memory source.
Holding FindConflict/Quantity also accept the represented chronological holding input.
No database or financial Save is involved in these probes.

54new selected unit conditions run on baseline:14actual failures/40passes.42conditions
cover all six source permutations for four running readers and three completed-month
goal readers. Eight controls retain positive/negative true final overflow, four retain
original cutoff/identity filters. The14failures span both current and complete-month
source consumers, not14unrelated defects.

## Finite correction and preserved limits

Balance keeps its one-pass original date/opening/review/effect filters and includes the
opening balance exactly once. An Int128 accumulator is checked during accumulation;
the completed result narrows to long with checked conversion. GoalTrend's three
existing filtered sources use one private exact-net helper with the same final guard.
No amount/currency/effect/adjustment/transfer/earmark policy, rounding, writer, migration,
model, derived-cache conflict/source guard or financial row changes.

This is completed end-of-day/period arithmetic. A query whose actual completed value
is out of range still throws; an out-of-scope future offset cannot rescue it. It is
not a replacement for history validation. D869 HoldingsLedger and its unsupported
chronological-prefix guard remain byte-identical, SHA256
`0db03caad08dc2251a6dd0b9fc460bcfa6eb786915ed1c4d3e1af7cd6bd0ff5f`.
No raw-order rollback or rejected compact-buffer code is introduced.

All54new conditions now pass on .NET10.0.14/X64. Typed original32-field ledger/15-field
holding values and allocation/identity snapshots preserve represented source/order.
13selected affected existing conditions pass: all eight entry-kind date/review/currency
effects, legacy self-transfer, mixed goal funding/transfer/refund behavior, true active
balance overflow, location goal/move exclusion and original median/one-off pace.
This is not the whole test suite, another108new tests, old17/28/53/multilingual matrix,
successful financial command, authentication or photo replay.

## Current original Android calculations and goal page

Fresh full-source pro.vafadar.zanance.completednetreview UID10255/PID5230
runs on actual API36/x64/kernel4096, original retained5606 boot6663f98e-6546-4809-a0bc-
285f11a96674. Shared ADB5038/PID50972 is reused without reset/start/reconnect/pair.
Only the fresh package is installed/launched once. Initial focus observation uses the
window subcommand that does not include global current-focus state, times out before
any provider/check/fixture action and is explicitly disqualified. Actual full-window
focus/owned UID/PID bind the same live process; no install/relaunch/case is repeated.

All54current original test bodies pass once on that native process. Before/after
original20component source is exact and account/entry/schedule0, SHA256
`6E0EDDC7A5C3C3549BC2ACE09A280BD096142DDE260BDF02AC93E5A1EB38A81B`. These are54current changed-platform conditions, not
54more new product tests. Private assertion adaptation executes actual test bodies.

One typed transaction then prepares six noncash rows only: type/place/three events and
one quantity goal. Id order is openingMax/giftMax-1/outflowMax, actual days1/3/2; original
FindConflict accepts it. Original GoalDetailPage/VM reader becomes CanView/Ready with
no failed cover and actual bound native text:

`This month so far: 9,223,372,036,854,775.806 tokens`

Trend is original `Not enough history yet (0 of 3 months).`. Native bounds/visible rectangle are exactly
[87, 708, 994, 750], no ellipsis; caption is already fully visible, with no scrolling,
alternate frame or screenshot/privacy bypass. Prepared20component source remains
exact, SHA256 `8AA13B2F08F042CB9FBCD0C77BED3B14ABFCDA2437956139B7E319321B2D8E9D`. Account/entry/schedule/valuation0;
no Allocate/Save/Import/Undo is invoked. Direct original-page binding is not normal
Shell/finger input, every goal-type screen, glyph/contrast/TalkBack or whole A11Y proof.
Other UI consumers are not inferred correct merely from pure reader cases.

## Actual packages and terminal preservation

Strict Windows has zero warnings/errors. Full shipping APK SHA256
`acf7d16d10d7475c300aa0b7bb38fc4b771ff6637d194fb48b2ee68c7f53a35f`, 99290905bytes/384AOT/408ELF/1608staticLOAD;
private APK `85e183adbf6b8425694e44bb31a8b4e12f6350fc310301346637c4ff60f8e741`,
56778524bytes/192AOT/204ELF/804staticLOAD. Both offline/nondebuggable/v2v3/
ZIPCRC/static16KiB/original Mono pass. Three current source hashes and nine actual own
App/Core/Data AOT objects match their corresponding built package entries. Sole private
MauiProgram DI capture reverses exactly to current original source. This is source/
own-AOT evidence, not whole managed-PE equivalence or hardware16KiB acceptance.

Only freshUID10255 is retired. Original retained emulator package list, five captured
OS values/locale key presence/geometry/boot/reverse/shell identity compare exactly.
Existing accountcost200/original fault sources and all frozen scopes stay untouched;
no original app/provider/history command, R13 reader or real-phone action occurs.
Five standard Cleans succeed after terminal native observation; no supplemental delete,
XML capture or refused-scope retry/subset/alternate/move/escalation/reuse. Used signed
APKs/source/AOT bindings/packets and baseline observer failure remain retained.

## Remaining acceptance

This completes the represented exact completed-net reader and original holding-goal
publication step. Current ARM64/native parity and adoption are separate ready work.
Prepared shippingacf7d16d is not installed on Main. Main32234/f342113f is only dated
D871, not fresh liveness or private financial/DB/key equality. Correctness work is not
a latency/two-second acceptance; new reader cost still needs its own evidence.
Original-fault/D838, broader security/accessibility/platform/external/release gates and
the overall active goal remain. D873 compact candidate remains rejected by D874.
Prior due CI checks consume their single response only; this commit gets one exclusive-
marker due-time read-only check, with no polling, job replay or tracking automation.
