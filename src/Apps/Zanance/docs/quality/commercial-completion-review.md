# Current local commercial boundary completion review (D-173)

Reviewed on 2026-10-12 at application baseline c276676c4c830e3f3e8039716b7f33d2d6041a0d.
This is the bounded review requested by D-168/172. It closes that review, not commercial
activation, verified purchases, server operations or the complete commercial-release backlog.
No production source, dependency, schema, permission or device configuration changes.

## Requirements and current evidence

| Requirement | Actual boundary and behaviour evidence |
|---|---|
| Account quota, correction, archive and contention | ZananceStore/CommercialWriteTransaction; AccountWritePolicyTests checks real competing independent SQLite providers, one accepted last slot, retained corrections/archive and rollback |
| Money through ordinary/split/holding/template routes | Original SaveEntry/SaveEntries; LedgerWritePolicyTests, SelectedAccountWritePolicyTests and HoldingWritePolicyTests cover complete groups, both transfer accounts, retained correction and exact-file checks |
| Goal/Plan creation and splits | GoalStore/PlanStore; GoalPlanWritePolicyTests covers counted pauses, reactivation, independent contenders and whole-batch rollback |
| Budget definitions, methods and current-period choices | Original budget store/CommercialWriteTransaction/BudgetChoiceStore; BudgetWritePolicyTests, BudgetChoiceStoreTests and BudgetEditorFlowTests |
| Templates, filters and rules | Original stores/ResourceChoiceStore; TemplateFilterWritePolicyTests, CategoryRuleWritePolicyTests and QuickResourceFlowTests; replacements/corrections do not consume a new slot |
| Profiles and actual-file new work | ProfileService.Work/ProfileCommercialAccessSource; ProfileWorkFlowTests and ProfileFileWorkFlowTests preserve complete unchosen profile data, recovery and original corrections |
| Import and restore above quota | Original import/holding stores and ZananceDatabaseBackupSource; RecoveryWritePolicyTests preserves complete over-quota rows without reviving cached paid facts; DataFileOutcomeFlowTests and BackupRestoreFlowTests cover accepted outcomes |
| Widget/deep link/template creation | Native routes open the same EntryEditor and guarded original writer; no independent native financial writer. QuickResourceFlowTests covers reviewed template handoff. Native Intent/app-lock acceptance is separate |
| Receipt computation and editor handoff | ReceiptWorkStore/ReceiptReadingSession; ReceiptWorkFlowTests, D-170/AT-171 evidence; manual attachment remains separate Free work |
| Forecast and stored comparisons | ForecastWorkStore/ForecastWorkspace/original batch writer; ForecastWorkFlowTests, D-169/AT-170; original Free month-end projection/history/Delete and currency-scoped paid computation |
| Loan and portable report computation | LoanWorkStore/ReportWorkStore and original payment/contribution writers; LoanWorkFlowTests and ReportWorkFlowTests, D-171/172 and AT-172/173 |
| Downgrade account choice and corrections | ResourceChoiceStore/CommercialWriteTransaction; ResourceChoiceStoreTests, SelectedAccountWritePolicyTests, ActiveItemsFlowTests; explicit selected subset, retained history, reconciliation and overdue settlement |
| Downgrade goal choice and earmarks | GoalResourceChoiceTests and GoalWorkFlowTests; original progress/earmarks remain financial inputs, only new work/reminders are filtered |
| Downgrade Plan choice and automation | PlanWorkSnapshotTests, AutomaticOccurrenceWriteTests, ReminderInputFlowTests; selected future generation/posting/reminders, retained past correction, upgrade ignores old bounded choice |
| Downgrade budgets and holdings | BudgetChoiceStoreTests/BudgetActionsFlowTests and HoldingWritePolicyTests; original amounts/history remain, new advanced work is checked separately |
| Downgrade profile/quick-resource choice | ProfileFileWorkFlowTests, ProfileReminderFlowTests, ResourceChoiceStoreTests and QuickResourceFlowTests; original file choices and retained saved query/history stay independent of paid rights |
| Inactive test registration | AddZananceData registers InactiveCommercialWriteAccessSource; MauiProgram registers InactiveProfileWorkAccessSource. ProfileCommercialAccessSource returns inactive facts before preferences/device reads; no commercial activation |
| Shared host expiry and membership | Existing actual-file policy/application tests distinguish retained correction from new work and never substitute personal Pro for membership. Real shared server grace/retention remains SHR-03/OD-05 work |

The reviewed source includes the actual serialized count/save transaction, persisted original choices,
noncreating reads, original-file profile adapter, recovery writer and actual application ports.
Tests assert stored rows and outcomes rather than copied application algorithms. Representative source
checks inspected the account contention implementation, recovery package round trip, original choice
read/save validation and the production inactive registrations. The finite current inventory supplies
additional delivered original store/UI boundaries; historical intermediate gaps are not new requirements.

## Reused verification and its limits

The D-172 ordinary main run passed 3,169 tests, zero failed/skipped, including App.Tests 1,112.
The existing log is artifacts/report-work-all-tests.log; no new test run or increased count is claimed.
Final strict builds, 36 Windows contexts, targeted final PDF/suggestion checks and the normal complete
Release Android share/cancel/data-restoration proof remain the evidence in [report-work.md](report-work.md).
The final native proof has all ten recorded checks true. Earlier boundary-specific native evidence
remains linked by the [current inventory](../enhancements/2026-10-commercial-release/07-current-entry-point-inventory.md).
Neither the latest report native check nor application-port tests prove every native Intent/provider path.

No unchanged successful build, matrix or emulator cycle is repeated for this documentation-only review.
The installable app stays artifacts/android/zanance-d172-release.apk; this review does not create a new APK.

## Conclusion and remaining work

The confirmed local ENT-02 operation boundaries and local ENT-03 resource/history/automation integration
are implemented and the bounded completion review is closed. This is not release acceptance or enabled
commercial enforcement. Keep ENT-05/BIL activation and QA-01/02/07 real device/OAuth/iOS gates explicit.
Real shared-space grace/retention belongs to SHR-03/OD-05; do not describe simulated membership tests as
that server implementation. ENT-04 purchase/manage/renewal/restore requires verified billing facts.

Next ready engineering is the existing Android accessibility review (A11Y-01), with explicit distinction
between emulator evidence and physical-device acceptance. Encryption/licence/recovery, owner texts/markets,
real samples, billing/backend decisions and the other canonical backlog remain open.
