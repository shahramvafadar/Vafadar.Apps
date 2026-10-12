# 07 - Current commercial entry-point inventory

Reviewed against current source after D-170 on 2026-10-12. This is a finite completion checklist for
ENT-02/03/04, not another feature backlog. Historical quality records describe the state of their own
slice; their remaining-work paragraphs do not override later implementations. The canonical state stays
in [04-backlog.md](04-backlog.md). Production commercial sources remain inactive.

## Implemented boundaries: do not rebuild

| Area | Current original boundary and representative coverage |
|---|---|
| Account creation/correction/archive and default cleanup | ZananceStore.AccountArchive.cs, CommercialWriteTransaction; AccountWritePolicyTests, SelectedAccountWritePolicyTests, AccountArchiveFlowTests |
| Ordinary, split, holding-backed and template-created ledger writes | ZananceStore SaveEntry/SaveEntries and original group validation; LedgerWritePolicyTests, SelectedAccountWritePolicyTests, LedgerUndoWritePolicyTests |
| Plan creation/split/correction, occurrence payment/reopening and automatic posting | PlanStore, ZananceStore.OccurrencePayment/OccurrenceReopening/AutomaticOccurrence; GoalPlanWritePolicyTests, OccurrenceCorrectionWritePolicyTests, AutomaticOccurrenceWriteTests |
| Goal creation/correction, allocations, contributions and reminders | GoalStore, GoalWorkPolicy, original reminder input/store/coordinator; GoalContributionWritePolicyTests, GoalWorkFlowTests, ReminderInputFlowTests |
| Budget definition choices, editor, copy/delete, alerts and destinations | BudgetChoiceStore, BudgetActionsViewModel and original budget store partials; BudgetChoiceStoreTests, BudgetEditorFlowTests, BudgetActionsFlowTests, BudgetAlertFlowTests |
| Templates, saved filters and category rules | ZananceStore and ResourceChoiceStore/QuickResourceWorkPolicy; TemplateFilterWritePolicyTests, CategoryRuleWritePolicyTests, ResourceChoiceStoreTests |
| Local profiles and actual-file new work | ProfileService.Work and ProfileCommercialAccessSource; ProfileWorkFlowTests, ProfileFileWorkFlowTests, ProfileReminderFlowTests |
| Holdings writes/corrections/import and retained values | HoldingStore plus original ledger group writer; HoldingWritePolicyTests, LedgerWritePolicyTests |
| Import/Undo and backup/restore | ImportAggregateStore, HoldingStore, ZananceDatabaseBackupSource, ImportExportViewModel and BackupRestoreSession; RecoveryWritePolicyTests, DataFileOutcomeFlowTests, BackupRestoreFlowTests |
| Active-item chooser and retained original history | ResourceChoiceStore, BudgetChoiceStore, ProfileService.Work and ActiveItemsViewModel; original scoped choices/projections and corresponding store/application tests |
| Limit reasons, original editor readiness and accepted outcomes | CommercialFailureMessage, EditorWorkStore, original editor/action flows; pre-save/editor/import/restore flow coverage through AT-169 |
| Read-only local plan comparison | CommercialAccessStore and ProductPlansViewModel; ProductPlansFlowTests. Verified purchase/renewal facts are deliberately absent |

Widget/deep-link/template routes open the original entry editor and ultimately use the same guarded
ledger writer. They do not have an independent financial writer to retrofit. Native Intent, real device
and app-lock delivery acceptance remain separate. Manual attachments, tags, language, theme, account
balances, retained holdings and essential warnings remain approved Free/history capabilities.

The ordinary D-170 main run passes 3,104 tests, including these existing suites, without rerunning them
solely for inventory updates. D-169/170 native acceptance is bounded in its evidence record.

## Confirmed ready gaps: complete each group once

| Order | Group | Actual current gap | Completion boundary |
|---|---|---|---|
| 1 | Forecast and saved forecasts | Delivered locally (D-169 / AT-170): ForecastWorkStore/ForecastWorkspace, original atomic batch writer and retained saved view | Free financial-month forecast/history/Delete, explicit retained drafts, currency-safe new scopes and legacy comparison; no activation. See [evidence](../../quality/forecast-work.md) |
| 2 | Receipt recognition | Delivered locally (D-170 / AT-171): ReceiptWorkStore/ReceiptReadingSession and typed EntryEditor destination | Shared original reader/parser, exact-file/current-source checks, compact-only pending copy, old amounts/no Save and separate manual attachments; no activation. See [evidence](../../quality/receipt-work.md) |
| 3 | Loan analysis and reports/PDF | LoanScheduleViewModel and AccountDetailViewModel compute schedules/estimates directly. ReportsViewModel.LoadCoreAsync/CreatePdfAsync and package partials build advanced calculations without commercial access | Bind LoanAnalysis, AdvancedReports and AdvancedPdf at actual computations/publication. Preserve ordinary balances, recorded history, corrections, debt closing, basic reports/owned export and essential incomplete-data/shortfall warnings. Do not turn Simple/Advanced into a payment check |

Each group uses the already approved Core feature classification and actual-file cached access. New
coverage should exercise the real operation, inactive unrestricted builds, Free refusal/retained draft,
Plus/Pro/shared membership, changed facts and unrelated stored rows. Do not test a copied algorithm or
add a new policy purely to make an existing guard look incomplete. Native checks and a complete signed
APK follow actual changes; unchanged successful checks are not repeated.

ENT-02 remains in progress for loan/report/PDF; forecast/snapshots and receipt recognition are delivered. ENT-03's original resource-choice,
history and automation integration is implemented; analytical affordances depend on the same groups.
ENT-04's verified purchase/manage/renewal/restore UI depends on real billing/provider facts. Completing
these groups permits a final bounded review of ENT-02/03, not activation or a fabricated purchase flow.

## Owner/external dependencies: do not replace with temporary behavior

- Encryption SEC-01..08: ADR/provider licence, permanent key/recovery and OS-backup decisions; automatic
  local backup depends on encrypted safety copies. Interim exclusion was explicitly declined.
- Billing/identity/sync/sharing: store/account/backend/cost/scope decisions and verified provider facts.
- LOC-05/10: explicitly owner-supplied texts. LOC-07/08: confirmed target markets and sourced holiday sets.
- Import presets/receipt corpus: actual representative samples; invented bank presets are not acceptance.
- Physical phone, real OAuth and Mac/Xcode/iOS/signing: external setup/device acceptance.
- Tax, AI, bank/rates and sharing encryption: their separately recorded scope/cost decisions.
- Additional languages: last feature wave, stable owner translations and cultural review.

Generic mapped CSV is already Free under the approved matrix; original import/restore is deliberately
owned-data recovery above quota. The D-166 proposal for an additional CSV paid/quota gate is withdrawn.
No download, account, credential, product policy, schema, code, commercial activation or build is changed
by this inventory.
