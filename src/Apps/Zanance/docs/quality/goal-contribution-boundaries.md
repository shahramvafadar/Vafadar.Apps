# Goal funding projections at numeric and calendar limits (D-825)

## Result and unchanged rules

Correct genuine goal-projection failures without changing money/date storage, funding
priority, allocation/release ownership, paid-plan decisions, financial writers, schema,
recurrence rules or runtime/GC policy. Final selected evidence has33 new cases and32
related existing cases, all65 passing on the final effective implementation. Original
Android GoalsPage publishes two new metadata-only goal definitions and seven original
Core boundary assertions, with all20 typed source components unchanged after publication.
The original large-Budget fault and whole-performance/security/platform gates remain open.

## Original failures and correction

- Monthly legacy earmark evaluation increments today.AddMonths(count) until after the
  target. A final December9999 target overflows even though that target is valid. Compute
  the month distance and compare its last possible original day anchor once. January31
  still clamps in February, and a target before the anchored day excludes that last
  opportunity. Weekly counting and past-target behavior are unchanged. The revised
  monthly calculation has no elapsed-month loop; no native timing claim follows.
- ceil(remaining/opportunities) previously adds opportunities-1 to a positive long
  remaining amount. For valid large targets this wraps and can suggest a negative
  contribution. Use the existing ContributionSchedule.PeriodsNeeded as the single
  positive quotient/remainder ceiling. No intermediate addition beyond long range occurs.
- The same unsafe ceiling occurs in planned ETA/period count and observed-pace ETA.
  Share that original helper rather than fixing just the first caller. Existing no-date,
  nonpositive input behavior and the1200-contribution ETA caps remain unchanged.
- AccountEarmark.Unallocated subtracts a nonnegative earmark from an extreme negative
  balance before clamping; wrapped debt can look like positive free money. Compare
  balance against its valid nonnegative earmark before subtracting. Funding amounts,
  priority, shortfalls and allocations themselves are not changed.
- The observed-pace one-off threshold3*median can overflow long and classify three
  identical large periods as unusual. Compare with exact decimal3*median instead;
  three times any long fits that representation. The median and three-times policy
  remain unchanged. This is not a relaxation of checked whole-allocation summation.

The first new test batch executes the unchanged original GoalCalculator:24 cases,
16 genuine failures and8 passing controls. Its initial narrow correction then exposes
shared call sites during source review. A separate9-case batch runs the still-original
ContributionSchedule/GoalTrend paths:7 genuine failures,1 passing control and1 flawed
oracle that itself overflows Enumerable.Sum. Preserve that failure; replace only the
oracle sum with BigInteger, and execute only its unfinished original case. It then
fails on the actual false one-off flags. Across the two lineages,24 distinct original
product failures are established; the oracle failure is not counted as one.

Final focused runs on.NET10.0.14/X64 contain46 cases (33 new plus13 existing phase2-goal
cases),17 directly affected original calculator/progress cases and2 original pace
cases, all passing. The17 related cases ran before consolidation and are repeated only
because their shared rounding dependency subsequently changed. The initial41-case pass
is not added to the final65 count. No whole-suite or new33+65 test claim is made.
Date extremes, short-month anchors, exact BigInteger ceiling oracles, original evaluate/
preview consumers, source nonmutation, valid debt bounds, ETA caps and one-off semantics
are covered. Preserve all baseline/partial/final logs and the exact source lineage.

## Actual Android publication

Use the existing owned API36/x64/4096 emulator with original OS values and data intact;
never launch or query original D809, or read its retained native record. Only fresh
package pro.vafadar.zanance.goalboundaryreview/UID10226 is installed once and launched
once. The first private compile uses the wrong facts-property name ContributionPlans;
it fails before any install. Correct that helper-only name to the actual Contributions
and preserve its original source/log. The corrected strict native build succeeds with
no warnings/errors; no successful compile or device cohort is replayed to hide it.

Actual PID14333 first executes seven scoped original Core assertions, without database
preparation: final monthly opportunity, large suggested amount, extreme-debt free money,
large planned count and ETA, capped observed ETA, and equal-period one-off flags. The
last two use only new in-memory fictitious allocation inputs, not stored allocations or
financial commands. These are distinct native assertions, not seven new unit tests.

Then create exactly two original goal definitions through GoalStore, once each:
D825 large target, EUR/long.MaxValue due one month after the actual4November2026 date;
and D825 final calendar, EUR101 minor due31December9999. Accounts, entries, allocations,
contribution plans and schedules remain zero. No account/financial Save, payment,
posting, import, Undo, authentication, photo or prior successful metadata setup is
replayed. There is no time-provider/clock adjustment or synthetic current-date override.

The actual original GoalsPage reaches Ready with both active rows. All8 observed
original native caption packets (names, complete funded amounts, dates and suggestions)
match the original bound strings, with no ellipsis and complete global visible bounds.
The large target shows a positive46,116,860,184,273,879.04EUR per contribution and2
remaining contributions, not a wrapped negative or absent suggestion. The final-calendar
goal publishes normally. This is a finite current-language/default-scale observation,
not another six-language/200%-font matrix, glyph/speech/contrast or every goal route.

All20 typed components (19 lists plus Settings) match after publication and original
Onboarding-root restoration, SHA256
`b53cc439815bf60a52e1ecf0d2ae589ef9d407df4c714b5c5e814bf3950a46dd`.
The fresh process remains alive and its own exit-info has no native-exit record. This
is not a reproduction, explanation or fix of the historical SGen failure. The original
D809 AVD/data/system record and terminal unknown R13 labels are preserved unread.

## Build, package and preservation

Strict selected Core-test, Windows and corrected private Android builds complete without
warnings/errors. The complete offline Android publish exits0 without a warning/error.
Private native APK fdad5aa36241e4c546dd8d0b5c8a55d506f458545381db08fc856fc403dd0a9a,
56737564bytes, has192AOT/204ELF/804 static16KiB LOAD. Complete shipping APK
b87907d1451bd83c57b41b23137a357ba0e07597edfbb7aad7f8e8e10099573a,
99467167bytes, has384AOT/408ELF/1608 static16KiB LOAD. Both pass v2/v3, ZIPCRC/alignment,
offline/nondebuggable policy and original Mono hash checks. Although complete package
length equals the earlier D822 length, both actual Core AOT components differ and match
the current build artifacts byte-for-byte. Length alone is not package identity. Do not
claim cross-build whole-PE identity or hardware16KiB/device/iOS acceptance from these.
The test-build source and later source normalization differ only in line endings;
executable logic stays the final shared implementation, not an asserted PDB-byte match.

Return the fresh original root and verify full source before uninstalling only UID10226.
Original OS getter values, locale-key absence, geometry/density, boot, preexisting
package/UID inventory and reverse are exact afterward. Shared5038/PID50972 survives;
the original emulator remains running/available. No phone command occurs: Main is only
the last D81432063/ac3a82f6 observation, not new liveness, and this shipping is not
installed there. No screenshot/XML capture, security-setting change, real authentication,
original fault-reader call, prior successful suite or frozen action occurs.

Four standard Clean profiles succeed: tests347, native1187, Windows1172 and shipping1289
file-deletion log lines. These counts are not physical-byte/free-space measurements.
No supplemental deletion or previously refused scope is attempted. Actual sources,
used signed packages, failures and raw receipts remain retained. D607/D752 terminal
and accepted successor checkpoints remain protected; broader gates and goal stay active.

Related: [calendar ranges](calendar-range-endpoints.md),
[rejected Accounts timing candidate](account-overview-streamed-effects-admission.md),
[current frontier](remaining-delivery-evidence.md).
