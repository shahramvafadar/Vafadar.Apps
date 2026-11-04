# Fixed-length budget period endpoints and stepping (D-812)

4 November2026. This batch concerns the original weekly/two-week BudgetPeriods APIs,
not D810 full calendar ranges, recurrence enumeration, financial writers or the native
crash cause. Seventeen new pure cases expose ten actual failures: four final ranges,
two otherwise valid explicit fortnight anchors and four overflowing period-count cases.
The large weekly int.MaxValue control already refuses, so arithmetic failure is not
claimed for every overflowing input.

## Correction and focused host evidence

A valid weekly/two-week start ends at the last representable day when its nominal end
exceeds DateOnly.MaxValue. Complete final periods retain their ordinary length.
An explicit fortnight anchor no longer calculates an unused earlier week start; the
original alignment formula remains. An actually unrepresentable aligned week/fortnight
start is still invalid: no artificial first weekday or invented earlier date is added.
Period stepping multiplies in long, validates the complete target day and only then
narrows it. Out-of-domain counts refuse instead of becoming another valid date or
silently returning the starting date. Valid positive/negative endpoint steps remain.
Do not clamp an invalid step or alter historical-period ordering, month rules,
recurrence intervals, money, financial posting or schema.

All seventeen new cases pass after the change. Five directly affected existing pure
cases pass: three chosen-first-weekday cases, one anchored fortnight/range case and
one prior-week ordering case. The seven initially passing controls are rechecked
because the same changed range/step methods execute them. No successful D810/recurrence,
financial, receipt, authentication, photo, import/Undo or full suite is replayed.
Strict host and Windows builds pass without warnings/errors; no UI layout/string,
model/migration/compiled-model or privacy/provider change is made.

## Android verification

One fresh independent original-source caller on the already preserved D809
API36/x64/4096 AVD runs all seventeen new calculations once. Actual UID10218/PID5377
passes every result with twenty typed source collections exactly equal before/after;
accounts/entries/schedules remain empty. Its initial state observation reaches
StartupPage and refuses before calculation issuance. Continue that same process after
Onboarding readiness; no install, launch, successful calculation or source setup repeats.
The initial state file is subsequently replaced by the ready observation; retain the
initial StartupPage result separately from its recorded execution output.

The strict independent APK is56852386bytes, SHA256
`3a93ea93e5af5b46cda89e976304437839b05f662d8818cbd77d5658e0803ddb`,
192AOT/204ELF/804static16KiBLOAD with v2/v3/ZIPCRC/alignment/offline/nondebuggable checks.
Only this fresh package/UID retires; all preexisting package/UIDs and D809 V3 APK remain
exact. Five OS values, boot, geometry and reverse match without setting writes or XML.

A subsequently observed D810 CI planning failure exposes another dependency: a peer
budget must retain its original complete-period policy even when read ranges clip.
[The D813 correction](synchronized-budget-complete-periods.md) is included before this
batch is published. The seventeen accepted calculations and five affected old pure
cases are not replayed after that separate validator change. Final strict Windows and
complete shipping packages are rebuilt for that actual code change; the earlier
shipping candidate is retained as unused/superseded and never installed.
The D812 and D813 evidence is versioned: seventeen plus nine Android calculations are
not claimed to be all executed on the final private binary. Product tests add seventeen
plus nine distinct cases, with five existing affected cases, not a full-suite result.

## Preservation and scope

Use only a previously absent independent package on the already preserved D809 AVD,
with fictitious empty source and no account/plan/ledger setup. Keep D809's original
package/userdata/system record and all frozen/terminal checkpoints. No reset, create,
new fault, settings write, owner authentication or real-phone operation is required.
Original native fault remedy, whole performance/security/accessibility/platform and
release acceptance remain separate; overall goal stays active.

Related: [full calendar ranges](calendar-range-endpoints.md),
[current actual native exit](current-large-budget-native-exit.md),
[concurrent descriptor correction admission](original-concurrent-descriptor-correction-admission.md).

Four standard Clean operations retire28916files/1941508223logicalbytes from the initial new build scopes, leaving3680files/587856362logicalbytes. No supplementary removal or frozen action occurs; used sources/packages and the superseded candidate remain retained.
