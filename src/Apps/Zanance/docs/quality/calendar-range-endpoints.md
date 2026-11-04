# Final full calendar and financial ranges (D-810)

4 November 2026. This boundary concerns PeriodMath full year/month/financial ranges,
not D808 recurrence enumeration or the D809 native crash. Sixteen new pure cases
expose eleven actual failures before the correction: two partial final-year ranges,
one final Hijri month, three final financial starts, three financial-month identities
and two financial ranges ending inside a partial final month.

## Correction and host evidence

End an otherwise valid range at DateOnly.MaxValue when its nominal end lies outside
the representable calendar. A real first day is still required: financial starts
beyond the partial final month remain rejected. Do not synthesize future months or
change nominal DaysInMonth, recurrence rules, amounts, payment policies or storage.
YearRange needs no month twelve in the partial final Persian/Hijri year. MonthOf
financial identity compares the actual local day with the clamped start day instead
of constructing an unrelated full range. Ordinary next-month identity continues to
use the original calendar conversion, including its Hijri calendar-selection policy.

All sixteen new cases pass after the correction. Six directly affected existing pure
financial-month/Hijri month/year cases pass; no successful recurrence, receipt,
authentication, import/Undo, camera, financial write or complete suite is repeated.
These are sixteen new product tests and six selected existing cases, not a full-suite
claim. The changed paths justify checking the five initially passing controls again.
Strict host and Windows builds pass without warnings/errors. No model/migration,
compiled-model or localization/UI-layout change is required.

## Android evidence

One fresh independent package links current original App sources/XAML and Core/Data,
with only the original built-service-provider handoff added to its private bootstrap.
On the already preserved API36/x64/4096 D809 AVD, UID10217/PID4750
executes exactly sixteen new original calculations once from OnboardingPage. All
sixteen pass; original before/after serialization of twenty typed source collections
is exact and accounts/entries/schedules remain empty. This is not a complete schema,
private Main or shipping-process performance proof. No source setup is replayed.

An initial focus observer incorrectly asks the window subcommand for top-level focus
fields; it refuses before issuing calculations. Preserve that observer, read the actual
top-level focused-window ownership, and continue the same installed package/process.
No install/launch/calculation is repeated. No XML/image or layout matrix is needed.

Strict independent APK: 56852386bytes, SHA256
`15a36c4c7fc450a43f437a7deaa858939db3dd164d36f5c836dfb65366bd2bb8`,
192 own AOT/204 ELF/804 static16KiB LOAD. Complete offline shipping APK:
99463071bytes, SHA256
`3e617bd874840f909513b3a8e310b4c9516120fe7c4f1c64cdcf364bfe0fdb21`,
384 own AOT/408 ELF/1608 static16KiB LOAD. Both pass ZIP CRC/alignment, v2/v3,
nondebuggable and no INTERNET checks. The original x64 Mono hash remains
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`.
Static16KiB is not hardware16KiB runtime acceptance. No whole-PE equivalence is claimed;
the new shipping candidate is not installed on the real phone.

After the successful same-PID comparison, only fresh package/UID10217 retires.
The complete preexisting package/UID list returns exactly, including D809's unchanged
V3 APK. Five OS settings, geometry, reverse mappings and preserved-restart boot match;
no setting write, old package launch/provider or phone command occurs. The same AVD,
D809 userdata/system record and all old signed/evidence inputs remain available.
Used signed packages and private sources are retained before standard output cleaning.
Four standard Clean operations succeed without warnings/errors, removing
28916files/1941502127logicalbytes.
3680files/587856200logicalbytes remain in the new build
roots; no supplementary deletion or frozen-scope operation is attempted. These are
logical inventory counts, not measured free-space gains.

## Scope and preservation

D809's same owned emulator is reused with a separately absent package; its original
installed source/evidence package and original system record are preserved. No new
AVD, reset, wipe, system setting or real-phone action is required. Only new package
startup and pure calculations belong to this cohort; no account/plan/ledger setup,
Save, Import, Undo, payment, camera, authentication or accepted source replay occurs.
Original native fault cause/repair, whole performance, security, accessibility and
later platform/provider/Store/release gates remain separate and open. No overall
Home/command latency gain follows from these correctness checks.

Related: [final recurrence months](recurrence-final-calendar-month.md),
[current ordinary native exit](current-large-budget-native-exit.md),
[remaining delivery frontier](remaining-delivery-evidence.md).

D813 later proves that bounded read helpers inadvertently relaxed the separate synchronization complete-period check. [The explicit financial-policy correction](synchronized-budget-complete-periods.md) preserves the original refusal; D810 read/calculation successes above remain finite evidence, not complete peer-write acceptance.
