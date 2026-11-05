# Chronological original holding quantity reads (D-869)

## Actual source-linked mismatch

Baseline is8a9e5551. Original HoldingPriceReview reads events by Id. HoldingDetail's
reader passes those events into Quantity/Positions, whose checked aggregates had used
input order. Same type/place with Max opening on day1, Max outflow on day2 and Max gift
on day3 is valid under original FindConflict, with final Max. Id/input order opening,
gift,outflow instead overflows before the decrease. Conversely, openingMax/day1,
outflow1/day3,gift1/day2 can hide the unsupported day2 increase in its input order.
A final representable sum alone must not qualify that invalid chronological prefix.

Baseline17new host cases has8failures: four valid permutation failures, two valid
single-enumeration-source cases failing at that same arithmetic boundary, and two
unsupported chronological prefixes incorrectly accepted. All six permutations are
checked independently for Quantity and Positions. Source-enumeration and first-seen
position-key order are preservation requirements, not newly discovered enumeration bugs.

## Finite correction and preserved guards

Quantity filters the original type/date scope, orders by date and per-day increase
before decrease, then uses its existing checked long aggregate. Positions records both
original transfer legs, seeds keys in original first-seen order, and applies checked
changes by date/sign. Source is enumerated once; no input list/entity is sorted in place
or mutated. Result zero filtering and key order stay original. Same-location transfers
remain neutral, future/other-type quantities stay excluded, and original same-day
unsupported intermediate overflow still refuses. FindConflict, financial writers,
models/migrations and the entire previous Basis/RoundedShare tail remain unchanged.

Corrected17new cases and10selected related date/type/transfer/same-day-overflow cases
pass on .NET10.0.14/X64. An initial combined class/method filter selected0cases despite
runner exit0; it is explicitly disqualified. The corrected method-only selection runs
exactly10, not an invented pass or successful whole-suite rerun. Initial wildcard/path
lookup errors remain discovery failures only. No old wealth/quantity,28/53-case or D866 display matrix,
accepted financial command, authentication or camera is replayed.

## Original native source and detail

Final original native UID10253/PID3033 runs all17current test bodies once.
The first ready observer returns InvalidOperationException before any case/fixture.
The same live PID continues to the actual original Onboarding root without install,
launch or case replay; its initial exception origin is not independently proven.
Full20-component empty source stays exact, account/ledger/schedule0;
SHA256 E6C908342D9989841DF1AB373657B2B469703798181ADB2BA70D42139FD3BB7A.

One typed fixture transaction adds only a type/location and three noncash events.
Ids intentionally put opening/gift/outflow in that order while actual dates are1/3/2;
this is the original Id-ordered price review, not a pre-sorted helper input. The actual
original HoldingDetailPage/VM reader becomes ready and publishes
`9,223,372,036,854,775.807 tokens` with unknown market value. Its bound
native TextView equals the managed quantity and has no ellipsis. The existing long
horizontal readout may clip to its viewport intentionally; this step does not replay
D866's endpoint/multilingual/font matrix or claim all digits/glyphs visible at once.
Direct hosting is not normal Shell-route or financial Save acceptance.

All20prepared source components remain exact after the original read;
SHA256 83EFBD5376516AB48D2907D4B795F1EB105D2F246DF711E936E2FADAE1531FEF.
Only the declared five fixture rows differ from initial source; no account/ledger/
schedule/valuation or financial App Save/Import/Undo occurs.

## Actual packages, retirement and next remaining condition

Strict Windows/full offline Android/private builds pass without warnings/errors.
Complete shipping SHA256 8239cbe344ea66debef0eb11c2e57d2a06d1bf4d08c17516b6e3f5ed31a957b0,
99491743bytes/384AOT/408ELF/1608staticLOAD. Used private SHA256
ea84e15c4728b3c3463849feb60387265745bcc264e94a6f11d0a5f3a3e3fe64,
56917922bytes/192AOT/204ELF/804staticLOAD. Offline/nondebuggable/v2v3/ZIPCRC/
static16KiB alignment/original Mono and nine own App/Core/Data AOT bindings pass.
The sole private MauiProgram service-container capture reverses exactly; no whole-PE,
physical ARM64 or hardware16KiB claim is made.

Only freshUID retires, with original5606 boot/package UID list/five OS values/locale-key/
geometry/reverse/shell state exact. Four standard Cleans pass (4090deletion lines).
Used source/signed packages/local evidence remain; no XML, supplementary/frozen retry,
phone/Main/R13 command, server reset/reconnect/pair or old financial success replay.
Main23174/060a remains dated D868; shipping8239cbe3 is prepared, not adopted.

Ordering adds work/allocation: this is correctness evidence, not latency/allocation or
whole-performance acceptance. The original reference/cost requirements stay open.
AssetEventEditor's separate raw Sum effect preview is a known next consumer to inspect
and qualify before further Main adoption; it is not claimed fixed by these two Core
reads. Original fault/D838, broad security/accessibility/platform/external/release
gates and the overall goal remain open. D867's sole due response has three completed
successes and generalCIcancelled. D868 sole response records CodeQLsuccess and
CIin_progress; no second request/job replay is allowed for either. This commit
gets one exclusive-marker read-only CI check after due time, without polling or a
job-tracking automation.
