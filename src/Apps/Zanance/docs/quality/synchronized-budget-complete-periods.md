# Preserve complete synchronized budget periods (D-813)

4 November2026. The one exact-head D810 CI response identifies failure in
[Local identity backend](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41880093910).
One job-metadata read identifies only personal-sync(planning), job126750006492, failing
at Qualify actual service and original clients. Five other jobs have successful
metadata; none of their logs or successful work is replayed. Read only the failed job
log: its original test fails at PersonalSyncPlanningTests.cs145/Refused183,
Assert.Contains with an empty issue collection. It fails before Submit, not inside
the HTTP commit or because of runner setup. The original integration test is unchanged,
blob1a239a567a48d77cd107be61d774b740d5a60ab6 at the failed head and current worktree.

## Actual current cause and unchanged financial policy

D810's valid bounded financial read range9999/12/start25 now ends at DateOnly.MaxValue.
SyncFinancialValidation previously relied on the original range exception to reject
an incomplete peer period. That coupling unintentionally changes the synchronization
guard. The still-unpublished D812 bounded weekly reader would have the same issue.
Do not change the old expected refusal or declare the input valid to hide CI failure.

Add an explicit complete-period check inside the original planning validator. Weekly
and fortnight starts must support their whole nominal duration. Monthly policy keeps
the previously required full current calendar month, a real clamped financial start,
and, for later start days, the full following calendar month and financial end.
Keep the exact InvalidBudgetPeriod reason/fields, structural schema, other financial
rules, original source and all publish/worker/conflict ownership unchanged. Bounded
read helpers stay useful for display/calculation; they do not authorize a peer write.

Nine new pure original-validator cases isolate the failing pre-Submit boundary with
complete canonical fictitious Budgets/Settings rows and no account/ledger/identity or
service preparation. After correcting a missing namespace import, four genuine policy
failures reproduce among nine cases. They cover the actual final financial month,
final Hijri month and the two new weekly readers. The unavailable Persian start and
four full-period controls already behave correctly. All nine pass after correction;
input rows remain exact. This is nine new tests, not the full PostgreSQL/TLS sequence.
No original successful enrollment, money setup, publication, receiver or job is rerun.

## New original Android guard boundary

A separately absent package links current original App sources/XAML/Core/Data with the
single private built-service-provider handoff. From original OnboardingPage, actual
UID10219/PID5855 runs only nine new canonical-graph cases through the original
structural and financial validators. All9pass; complete input rows remain immutable
and twenty original typed source collections match before/after. Accounts, entries
and schedules stay empty. No accepted D812 seventeen-case calculation is repeated.
This is actual calculator/guard execution, not authenticated service or financial Save.

Strict Windows and complete Android packages are rebuilt after the guard change.
Final independent native: 56860578bytes/SHA256
`bedc57b32d95d1103e272f093814ac2e4bda6cbbeb3292c3d2d64ede933d5b93`,
192AOT/204ELF/804static16KiBLOAD. Final complete offline shipping:
99463071bytes/SHA256
`ac3a82f6b366090fb7b1691af6c797546c3d56cad6d0c09553cc770a0b922fef`,
384AOT/408ELF/1608static16KiBLOAD. Both pass ZIPCRC/alignment/v2v3/nondebuggable/no INTERNET.
Original x64 Mono remainsedfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4.
Static alignment is not hardware16KiB acceptance; whole-PE identity is not claimed.
The final shipping candidate is not installed on the phone.

Only fresh UID10219/package retires after the same-PID/source result. All preexisting
package/UIDs and retained D809 V3 APK, five settings, boot, geometry and reverse match.
No reset, AVD deletion, old package/provider/record read, phone/auth/financial command,
XML or frozen-scope operation occurs. The D809 emulator/data/system record remain.
Used signed packages, source archives and bounded failure excerpts stay retained.
Four standard Clean operations retire28916files/1941663480logicalbytes;
3680files/587863602logicalbytes remain, without supplementary removal.
Counts are logical output inventories, not measured free-space gains.

## CI limits and remaining work

The D810 exact response separately records self-hosted qualification success, main CI
cancelled and CodeQL in progress. D811's single response records CodeQL success and
main CI still in progress; neither pending head is polled again. This correction is
locally verified at the actual failed function; its new pushed-head CI remains a
separate one-time follow-up. Original fault cause/remedy, full performance/security/
accessibility/platform/provider/Store/release gates and the overall goal remain open.
Terminal D607/D752 and accepted successors remain preserved.

Related: [fixed-length period endpoints](fixed-period-endpoints.md),
[full calendar ranges](calendar-range-endpoints.md),
[remaining delivery frontier](remaining-delivery-evidence.md).
