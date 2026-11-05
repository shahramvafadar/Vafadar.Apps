# Exact manual exchange-rate input (D-844)

5 November 2026. Finish a confirmed original manual-rate input defect and its
misleading inline feedback, without changing recorded money or replaying accepted work.

## Original defect and scope

Decimal.TryParse may silently round an unsigned positive plain decimal. Inputs such as
`0.12345678901234567890123456789` and `79228162514264337593543950335.1` become a
different valid decimal; the old original Add command persists that different ratio.
Thirty new refusal cases cover six input cultures and five values. Four values per
culture wrongly save a rounded rate, giving 24 real baseline failures. The tiny
underflow control already refuses. Eight exact positive syntax/range controls pass.

The original numeric culture/digit/separator normalization remains. Before constructing
an ExchangeRate or entering its writer, require positive decimal parsing and equality
of canonical unsigned fixed-point meanings. Strip only redundant leading integer and
trailing fractional zeros; never approximate or truncate a ratio. Manual exchange
rates retain the entire positive Decimal range: unlike an account percentage, there
is no 100 cap. Exact tiny/large values, optional leading zero and redundant zeros remain
valid. Pair/file/draft/root ownership, date/estimate, committed receipt, rate-table
valuation and original amount/conversion/financial policies are unchanged.

No Data/Core/model/migration/compiled-model/runtime/GC or third-party change is needed.
All six existing Rates_Invalid translations now explain positive input that can be
stored without rounding. No other resource value/key/placeholder changes. Existing
culture-bound partial input and retained feedback behavior remain.

## Selected host lineage

The first corrected binary passes 38 new plus four directly affected original cases:
three culture/accepted-receipt/pending-writer cases and one retained partial-input
case. These create only fictitious rate metadata, never a ledger posting. The original
account/annuity/financial suites are not repeated. Actual host is .NET10.0.14/X64.

Native inspection then exposes the inadequate old message, "Enter a rate greater than
zero", for an already positive input. After six-language text correction, six new
original-command cases verify the actual translated category and its retained raw
value through a formatting-culture change without another command. The rate-model
source remains byte-identical to the first corrected checks; the previous 42 cases
are not replayed. Final review finds a filename collision with an existing one-Fact precision class.
Restore that original file exactly and place the 44 new cases in a distinct file/class;
only their declaring class name changes. Strict final compilation and the directly
affected restored small-positive list-precision Fact pass. Three new test methods are
discovered without execution; no old case or financial workflow is removed. Evidence
is 44 new + five affected across the lineage, not 49 on one final test binary, 49 new,
or a full suite. Resource-only changes trigger necessary
strict Windows/shipping/native rebuilds, not unchanged product verification.

## Original native continuation without successful-command replay

Use the original RatesPage/XAML/ViewModel and Data/Core source directly in a fresh
API36/x64/4096 package, UID10240. One reversible private MauiProgram DI capture is the
only bootstrap difference, compared with explicit line-ending normalization. No
account, entry, schedule, authentication, camera or external provider is prepared.

1. PID22497 shows the original Ready page and executes one actual visible native Add
   with the inexact fractional value. Raw input remains; all20 typed protected-source
   components equal the original snapshot. The old message is visible but disqualified
   as sufficient precision feedback. No rate is saved.
2. One necessary no-reset upgrade to22675 preserves UID and exact original20source
   before any new input. A different near-maximum value, not the completed fraction,
   exercises one native Add. The corrected two-line English message is completely
   visible/unellipsized at [87,967,994,1055]; raw text stays and no rate is saved.
3. The continuation observer incorrectly reuses V1's retained refusal-proof marker.
   Its proof callback returns no JSON; PID22675 is then absent. Preserve that failure,
   original marker and completed native observations. This is not original SGen cause
   or general stability evidence. A corrected observer uses distinct markers and
   contains its own errors. A second necessary no-reset upgrade to22893 admits the
   exact existing20source and both prior marker checkpoints; no preparation/refusal
   click repeats. One new unknown-observer-operation returns only a contained
   InvalidOperationException category and the same process continues.
4. Only the unfinished exact input `0.1234567890123456789012345678` is entered through
   the actual native EditText. One visible enabled original Add saves it once. The
   original reader clears the submitted draft and shows exactly one complete native
   row, `1 USD = 0.1234567890123456789012345678 EUR`, at [87,1753,964,1800], no ellipsis.

There are three versioned native Add clicks: two distinct refused inputs and one
accepted reference-rate metadata row. No transaction/payment/import/financial replay,
delete, refresh or old native validation is repeated to improve evidence. V3's local
click count is one, not an invented three-click same-process run. Native error text
coverage is this English context; six translated host categories are not six native
screens, glyph, contrast, OS200%, RTL or whole accessibility qualification.

Original20source SHA256 before writes and after both no-reset admissions is
`298C557EAC4AC7C32A8CB56BBC58B6104945AC2DB713DE72096C623D71404A4F`.
After the single accepted row, its exact pair/date/value/estimate are checked and all
other19 typed components remain equal. Account/entry/schedule counts remain zero.
This is the declared source set, not every table/schema/cipher/WAL/key. Return to
original Onboarding and retire only fresh UID10240. Original AVD/packages/boot/five OS
values/locale-key presence/geometry/reverse/shell are exact. No retained-cohort launch/read, R13 operation, phone, emulator restart/reset/reboot
or ADB server/reconnect/pair action.

An early shell attempt to observe the fresh CE/DE directory inodes is denied before
the first no-reset upgrade marker. No root/fallback/inode retry follows and no inode identity is
claimed. The already planned original-source comparison establishes only its explicit
20-component preservation, not substitute filesystem identity or complete private DB.

## Artifacts and continuation

Strict Windows, complete offline shipping and all three private native versions pass
without warnings/errors. Nine final App/Core/Data AOT ZIP members match actual current
build artifacts; no cross-build managed PE identity is inferred. Private provider is
absent from shipping. Final packages pass original Mono, v2/v3, ZIP CRC/alignment,
offline/nondebuggable and static16KiB LOAD checks, not actual16KiB hardware proof.

| Package | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Final private native |15ed49eb7e51b367b0e6eecbc5f44811076120707c1e73acf0b23c92c364ddb8|56897442|192/204/804|
| Complete shipping |06a6963733f57876352c860714ed2f0b879c4ffd80df2af78d47310466562f91|99471263|384/408/1608|

Five standard Clean runs across four profiles pass: tests384, Windows1172, native1282,
shipping1289, then tests384 after final test-layout compilation. These are deletion
lines, not unique-file/physical-byte totals. All used private
versions, initial unused shipping candidate, final shipping/source/tests/observations
and failures remain. No supplemental deletion, frozen scope or new XML capture.
Current shipping is prepared, not on Main. D8425939/22a remains the last dated Main,
not new liveness/private-source proof. D843 sole CI41969317113 and CodeQL41969317292
completed-success; no successful job log or second request is fetched.

D838 new-PC authority remains pending, D841 actual candidate unexecuted, D809 terminal
unknown and D833 retained data protected. Broader original native-fault remedy,
performance/security/accessibility/platform/provider/release gates and the overall
goal remain active. This closes only the stated manual-rate input/native continuation.

Related: [account-rate precision](exact-account-rate-editor.md),
[original manual-rate continuation](original-manual-rate-workspace.md),
[current Main](current-interest-arm64-main-adoption.md),
[canonical backlog](../enhancements/2026-10-commercial-release/04-backlog.md).
