# Original physical incomplete forecast and saved scenario (D-466)

## Uncovered acceptance scope

Continue QA-01/AT-43/AT-44 on the owner-reserved Samsung, with the complete signed Offline D-465
installation unchanged. Source08c2c1de2c864d7fd56b69f586874a530775c5f3; APK SHA256
`42d394c1df0952436df261a8eb48733043670f5c7ef6f99eb653f6407f2d71e8`.
No reinstall, product change, unit-test addition, build or old successful matrix belongs to this
batch. D-316's completed settled-item and leave-out path are retained rather than replayed.
Only ordinary original app UI supplies this new source; no direct database insertion or copied
financial calculator supplies its acceptance result.

## One original financial precondition

Create only D466_Forecast. Onboarding uses English/Light/Advanced and explicitly Gregorian calendar.
The initial unsaved setup shows Persian dates inherited by the current display service; before
accepting the first account, return to its actual calendar selector and choose Gregorian so that
End of month means31 October, rather than the end of a different calendar month. This is a new
fixture precondition, not an established production defect. No device/system settings change.
My account is checking/EUR, opening100 on22 October2026. It is the only financial account created.

Save exactly three Once plans on that original account:

| Original plan | Direction | Amount | Original due date |
| --- | --- | --- | --- |
| D466_Out150 | Expense | EUR150 fixed | 23 October2026 |
| D466_Unknown | Expense | Not known yet | 24 October2026 |
| D466_In200 | Income | EUR200 fixed | 30 October2026 |

Observe the original dates, Once summary and both Reminder/Record automatically switches false
before each original Save. Never enable notification or automatic posting. The original All plans
list exposes all three exact identities, amounts and dates. No occurrence is paid and no actual
ledger transaction is created.

## Incomplete baseline and explicit scenario

Original Forecast/End of month shows its basis22–31 October, end150EUR, lowest-50EUR on23 October,
shortfall warning and exactly one unknown-amount warning. A positive ending does not conceal the
intermediate deficit, and an unknown plan is not treated as a known zero.

Open only D466_Unknown and its original Assume another amount action. The actual native prompt
has an empty field; explicitly enter20 and accept once. Its original row now says Amount assumed
and-20EUR; the current forecast shows What-if changes are applied, end130EUR and lowest-70EUR
on24 October. This is a process-local analysis, not a plan or ledger edit.

Invoke original Save this forecast once. Its native name prompt initially contains10/22/2026;
bounded original input clears that exact initial text and verifies D466_Assumed20 before the one
accepted Save. The original Save button is then disabled, preventing accidental duplicate Save
of the accepted payload. The saved row opens the real detail: original22–31 October range,
lowest-70EUR on24 October and Assumptions: D466_Unknown: Amount assumed. Its read-only explanation
is present. Because the base date is today, comparison correctly states that it starts tomorrow;
today's zero difference parts are not future actual-versus-saved acceptance. No clock change or
invented later outcome is used.

Return to the current forecast and invoke Reset once. End150, minimum-50 on23 October and the one
unknown warning return, while the saved scenario keeps its separate original-70 result. The
original three-plan list is unchanged; D466_Unknown still has Amount unknown. Home stays100EUR.
All/All types history explicitly says No transactions yet.

## Actual cold persistence and original deletion

One package-scoped force-stop retires PID19994. The original Activity launch reports COLD,
Activity1514ms/Wait1544ms, PID27525. Its first public tree is not ready; read the same live process
after publication without restarting it. Home remains100EUR. The current original forecast still
shows150/minimum-50/one unknown. Its one saved D466_Assumed20 detail independently retains the
original minimum-70/date and assumed-amount caption. These are public native persisted-state
observations, not a private full-database/key identity audit or complete Home timing acceptance.

Original Delete this forecast opens real consent. Cancel keeps the same detail and delete action.
Reopen that consent and accept Delete exactly once. The original forecast no longer has the saved
row, while all three plans remain unchanged. Final All/All types is still empty and Home100EUR.
Deleting a saved analytical snapshot therefore does not delete or post one of its source plans.

Return Main through the original profile menu. Delete only inactive D466_Forecast, with the first
question naming it and the second explicit irreversible-deletion question. A first post-action
capture can still show the old profile row; the later fresh original tree confirms deletion.
Final Main979/all141 public node tuples/original three profile names equal the before state.
The same final PID27525 retains the exact D-465 APK hash and font1/1080x2340/density450/
accessibility0/empty services. Remove145 exact current-run remote XML files only after remote/local
SHA256 equality; local originals and33 observation assertions remain. These are not new unit tests.
The owner-reserved phone stays available to this project until the owner says otherwise.

## Preserved observer limitations

Initial profile-create/open and cold-start frames precede final publication; continue the actual
same process rather than replaying accepted setup, plan or snapshot actions. The empty Plans page
has two Add plan targets; use the uniquely observed header+ button for the first original draft.
After switching to Main, More returns to its root rather than the previous Profiles page: exact
profile-action and Delete target guards refuse the mistaken retained-page assumption before input.
Navigate to the actual Profiles list and continue only the pending cleanup. A host quoting error
in a profile-name assertion stops before the first confirmation; correct only the assertion against
its existing original XML, without repeating any accepted action. These are retained observation
failures, not application financial failures or evidence of a general runtime fix.

## Remaining gates

This qualifies original physical month-end-positive/mid-period-negative behavior, one unknown
amount, explicit amount-only what-if isolation, one original saved scenario, actual cold read and
native declined/accepted snapshot deletion. Later-date actual comparison, date-moving scenarios,
missing exchange rates, multiple-currency snapshot batches, every plan/calendar variant, complete
device/DB, general GC/ANR/crash/low-memory, whole-Home two-second target, spoken accessibility,
iOS, OAuth/providers, Store signing/purchases and release stay separate. Keep QA-01 and the full
continuous backlog active. The unchanged successful builds/tests/signature proof are reused.
