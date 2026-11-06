# Fixed-day recurrence prefix step rejected at first admission (D-905)

## New bounded premise

Current original recurrence already jumps daily/weekly prefixes, but monthly/yearly
rules generate earlier dates from their anchor. A private class copy adds only an
initial step/number calculation for known one-date fixed-day prefixes: Monthly or
Yearly, more than366elapsed days, SpecificDay, no SecondDay and anchor day<=28.
Earlier complete supported months each contain exactly one such original date.
The100000step cap, original anchor, period arithmetic, end/count, date generation,
numbering, Skip/weekdays/second-day and recent-rule fallback bodies remain intact.
Reversing the declared insertion/class rename/duplicate public record removal gives
the current original source text. No product source is changed. No cache/value-date/
narrow-window/codec/index candidate is reused, retuned or selectively rescued.

Before the new strict build, the admission freezes100rules/shape,22shape definitions
in the compiled private source, seven inverse-order pairs, ten materializations per
sample and one second warmup per role/shape. Every shape requires latency<=1.05 and
allocation<=1.01 of its original. Old Gregorian/Persian monthly/yearly additionally
require at least20percent latency improvement. A first-process failure ends before
second process/native/product work; favorable subsets cannot rescue the result.
These component gates do not accept whole Home/startup/search/migration speed.

## Actual comparison and first process

One strict private build passes without warnings/errors. Actual.NET10.0.14/X64/
PID22304 qualifies3510 new component comparisons:1296 combinations of calendar,
frequency, anchor day, interval, day policy, second day and end policy;2200 rules
from the22fixed timing shapes;12calendar boundary cases; the original maximum-step
and invalid-interval controls. Thirty-six deliberate live-rule mutation/re-enumeration
comparisons preserve output/number/ordered date and final rule metadata. These are
private controls, zero new product tests or financial operations. Validation-refused
rules compare actual refusal types rather than silently replacing them with dates.

All full timing inputs are serialized before warmup and remain equal afterward;
original/candidate complete ordered outputs, source-rule JSON and timed checksums
match. The full input SHA is
`CF135F8B9631E9157C361B9910A96FD28BDF925B1FA672D9047F24EEF175F137`.
The raw seven pairs/all22shapes/full JSON remain local, never combined with D794/
D795 samples. The table contains medians of ten materializations, not single-call
or native Home latency.

| Shape | Original ms | Candidate ms | Latency ratio | Allocation ratio | Gate |
|---|---|---|---|---|---|
| once | 0.0247000 | 0.0272000 | 1.101214575 | 1.000000000 | Fail |
| daily | 4.5709000 | 4.6290000 | 1.012710845 | 1.000000000 | Pass |
| weekly | 0.7093000 | 0.7402000 | 1.043564077 | 1.000000000 | Pass |
| old-Gregorian-Monthly | 5.0736000 | 0.3938000 | 0.077617471 | 0.074985690 | Pass |
| recent-Gregorian-Monthly | 0.4174000 | 0.4169000 | 0.998802108 | 1.000000000 | Pass |
| old-Gregorian-Yearly | 0.4986000 | 0.1069000 | 0.214400321 | 0.238095238 | Pass |
| recent-Gregorian-Yearly | 0.0848000 | 0.0914000 | 1.077830189 | 1.000000000 | Fail |
| old-Persian-Monthly | 2193.1676000 | 149.0272000 | 0.067950666 | 0.075461983 | Pass |
| recent-Persian-Monthly | 173.8215000 | 174.0814000 | 1.001495212 | 1.000000000 | Pass |
| old-Persian-Yearly | 187.2377000 | 34.0757000 | 0.181991661 | 0.190476190 | Pass |
| recent-Persian-Yearly | 33.8017000 | 33.5882000 | 0.993683750 | 1.000000000 | Pass |
| old-Hijri-Monthly | 16.4615000 | 1.1611000 | 0.070534277 | 0.077093733 | Pass |
| recent-Hijri-Monthly | 1.3069000 | 1.3056000 | 0.999005280 | 1.000000000 | Pass |
| old-Hijri-Yearly | 1.5157000 | 0.3067000 | 0.202348750 | 0.227272727 | Pass |
| recent-Hijri-Yearly | 0.2715000 | 0.2626000 | 0.967219153 | 1.000000000 | Pass |
| second | 10.1394000 | 9.7873000 | 0.965274079 | 1.000000000 | Pass |
| skip | 5.8594000 | 5.9594000 | 1.017066594 | 1.000000000 | Pass |
| nth | 2598.4009000 | 2605.4078000 | 1.002696620 | 1.000000000 | Pass |
| last-day | 5.1300000 | 5.1341000 | 1.000799220 | 1.000000000 | Pass |
| last-weekday | 20.9841000 | 21.0416000 | 1.002740170 | 1.000000000 | Pass |
| count | 250.4338000 | 23.1054000 | 0.092261508 | 0.094339623 | Pass |
| end-date | 0.5052000 | 0.1116000 | 0.220902613 | 0.238095238 | Pass |

The once control regresses10.1214575percent and recent Gregorian yearly7.7830189
percent, beyond the fixed5percent ceiling. All allocation and all four old-rule
improvement gates pass. Even large Persian/Gregorian old-rule gains cannot rescue
the two failed controls. The absolute differences are small component intervals;
this does not establish a production regression, exclusive cause or a speed remedy.
No retune, repeated timing, favorable subset, second process, native deployment,
permanent test or product change follows.

## Preservation and terminal scope

Current original recurrence/rule/period files and all used private sources match
the sealed pre-run hashes. No actual compiler-PDB binding or retained DLL checksum
is claimed: one scoped standard Clean already removed build outputs before such a
receipt was taken. This evidence limitation is retained rather than rebuilding an
already rejected candidate. The used private source snapshot, strict build log,
complete inputs/output/contracts/pairs/decision and Clean log remain available.
No supplementary cleanup, frozen scope, SDK/device/phone/auth/photo action or
accepted financial/test replay occurs; original runtime/GC/product models remain.

D904 shipping remains prepared/not Main, with its own sole CI still separately due.
D838 pending authority/D841 undeployed reader, original fault remedy, whole
performance/security/platform/provider/release gates and the overall goal remain
open. This rejection supplies no new acceptance for those requirements.
