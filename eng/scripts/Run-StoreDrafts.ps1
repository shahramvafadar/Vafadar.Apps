<#
.SYNOPSIS Renders six-language store drafts in a fresh independent Windows Debug installation.
.DESCRIPTION Never moves, resets or reads the main installation's data. A timeout retains the original live process.
#>
[CmdletBinding()]
param(
    [ValidateRange(30, 1800)][int]$TimeoutSeconds = 600,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$RunId,
    [switch]$UseExistingBuild,
    [ValidatePattern('^$|^[0-9a-f]{32}$')][string]$AccountTabRefreshContinuation = '',
    [switch]$ReportRowsReview,
    [switch]$BudgetTitlesReview,
    [switch]$BudgetActionsLifetimeReview,
    [switch]$BudgetFinancialReadReview,
    [switch]$BudgetPeriodCaptionReview,
    [switch]$BudgetScopeCaptionReview,
    [switch]$OriginalAmountErrorReview,
    [ValidatePattern('^(360|412|1280)-(en|fa|de)-(Light|Dark)-(USD|JPY|KWD)$')][string]$OriginalAmountErrorRecapture,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$BudgetPeriodSourceRun,
    [switch]$BudgetActionsObservationsOnly,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$BudgetActionsSourceRun,
    [switch]$HomeLegendsReview,
    [switch]$PlanRowsReview,
    [switch]$PlanRowsEdgeProbe,
    [switch]$LedgerRowsReview,
    [switch]$ReimbursementsReview,
    [switch]$HomeTemplatesReview,
    [switch]$HomeReviewCountReview,
    [switch]$HomeAddHeaderReview,
    [switch]$PlansAddHeaderReview,
    [switch]$ReconciliationReview,
    [switch]$SettlementCommitReview,
    [switch]$OccurrenceCommitReview,
    [switch]$OccurrenceStateReview,
    [switch]$OccurrenceRefreshReview,
    [ValidatePattern('^$|^[0-9a-f]{32}$')][string]$HoldingFinancialContinuation = '',
    [ValidatePattern('^$|^[0-9a-f]{32}$')][string]$HoldingFeeInputContinuation = '',
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingQuantityContinuation,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingPriceContinuation,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingHistoryContinuation,
    [switch]$HoldingHistoryReview,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingArchiveContinuation,
    [switch]$HoldingArchiveReview,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingTypeContinuation,
    [switch]$HoldingTypeReview,
    [switch]$HoldingLocationReview,
    [switch]$HoldingEventPublicationReview,
    [switch]$PlanEditorReview,
    [switch]$GoalBudgetMoneyReview,
    [switch]$EntryMoneyReview,
    [switch]$AccountMoneyReview,
    [switch]$HoldingMoneyReview,
    [switch]$SecondaryUnitsReview,
    [switch]$SecondaryFieldsReview,
    [switch]$GoalListReview,
    [switch]$GoalDetailReview,
    [switch]$GoalDetailEditFailureOnly,
    [switch]$SecondarySettingsReview,
    [switch]$SecondaryHouseholdReview,
    [switch]$PhysicalRatesReview,
    [switch]$PhysicalRatesNotesOnly,
    [switch]$PhysicalRatesOutcomesOnly,
    [switch]$PhysicalRatesGuideOnly,
    [ValidatePattern('^$|^[0-9a-f]{32}$')][string]$PhysicalRatesBaselineRun = '',
    [string]$ReviewWorkspaceRoot,
    [switch]$AccountDefaultOnly,
    [switch]$AccountCurrencyOnly,
    [switch]$PlanMoneyReview,
    [switch]$PlanMoneyDebtOnly,
    [switch]$PlanMoneySubmissionOnly,
    [switch]$PlanEditorArchivedOnly,
    [switch]$PlanEditorCaptionsOnly,
    [ValidatePattern("^$|^[0-9a-f]{32}$")][string]$PlanEditorObservationRun = "",
    [ValidatePattern("^$|^[0-9a-f]{32}$")][string]$PlanEditorSourceRun = "",
    [switch]$EntryWorkflowReview,
    [switch]$EntryWorkflowAggregateOnly,
    [switch]$EntryWorkflowCaptionsOnly,
    [switch]$EntryWorkflowBlockedOnly,
    [ValidatePattern('^$|^[0-9a-f]{32}$')][string]$EntryWorkflowSourceRun = '',
    [switch]$EntryFormReview,
    [ValidatePattern("^$|^[0-9a-f]{32}$")][string]$EntryFormSourceRun = "",
    [switch]$EntryDetailReview,
    [ValidatePattern("^$|^[0-9a-f]{32}$")][string]$EntryDetailSourceRun = "",
    [switch]$EntrySaveReview,
    [ValidatePattern("^$|^[0-9a-f]{32}$")][string]$EntrySaveSourceRun = "",
    [switch]$EntryAttachmentsReview,
    [ValidatePattern("^$|^[0-9a-f]{32}$")][string]$EntryAttachmentsSourceRun = "",
    [switch]$HoldingDetailReview,
    [switch]$HoldingDetailQueryOnly,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingDetailCompletedRun,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingDetailSourceRun,
    [switch]$HoldingOverviewReview,
    [switch]$HoldingOverviewCaptionsOnly,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingOverviewCompletedRun,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$HoldingOverviewContinuation,
    [switch]$HoldingPriceReview,
    [switch]$HoldingQuantityInputReview,
    [switch]$HoldingFeeActionReview,
    [switch]$HoldingFeeInputReview,
    [switch]$HoldingRefundReview,
    [switch]$HoldingFinancialReview,
    [switch]$SplitFinancialReview,
    [switch]$ImportFinancialReview,
    [ValidatePattern('^$|^[0-9a-f]{32}$')][string]$ImportFinancialContinuation = '',
    [switch]$HomeRefreshReview,
    [switch]$HomeRefreshReferencesOnly,
    [switch]$ReconciliationVisualOnly,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$ReconciliationContinuation,
    [switch]$GettingStartedReview,
    [switch]$EcbReferenceReview,
    [switch]$AccountTabRefreshReview,
    [switch]$CsvInputReview,
    [switch]$CsvInputEmptyOnly,
    [switch]$CsvQuotationOnly,
    [switch]$EditorTextReview,
    [switch]$EditorRetainedAggregateOnly,
    [ValidatePattern('^$|^(en|fa|de)-(Light|Dark)-(360|412|1280)-(Expense|Income)(,(en|fa|de)-(Light|Dark)-(360|412|1280)-(Expense|Income))*$')][string]$EditorAggregateCompleted = '',
    [switch]$EditorDebtDetailsOnly,
    [switch]$EditorGoalChoicesOnly,
    [switch]$EditorNarrowActionsOnly,
    [switch]$EditorOnboardingFooterOnly,
    [ValidatePattern('^$|^(en|fa|de)-(Light|Dark)-(360|412|1280)(,(en|fa|de)-(Light|Dark)-(360|412|1280))*$')][string]$EditorOnboardingContexts = '',
    [switch]$ReimbursementsVisibilityOnly,
    [switch]$ReimbursementsEmptyOnly,
    [ValidateSet('en,fa,de','de')][string]$ReimbursementsLanguages = 'en,fa,de',
    [ValidateSet('en,fa,de', 'fa,de', 'de')][string]$LedgerRowsLanguages = 'en,fa,de',
    [ValidatePattern('^$|^(en|fa|de)-(Light|Dark)-(360|412|1280)(,(en|fa|de)-(Light|Dark)-(360|412|1280))*$')][string]$LedgerRowsContexts = '',
    [ValidateSet('en,fa,de', 'fa,de')][string]$HomeLegendsLanguages = 'en,fa,de')
$ErrorActionPreference = 'Stop'

# D-437: large original native evidence may live on another volume, with the same strict package/desktop/output guards.
$reviewWorkspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($ReviewWorkspaceRoot) {
    if (!$PhysicalRatesReview -or !$PlanEditorReview -or ![IO.Path]::IsPathRooted($ReviewWorkspaceRoot)) { throw 'An explicit physical/rate native workspace requires its dedicated review mode.' }
    $candidate = [IO.Path]::GetFullPath($ReviewWorkspaceRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $temporary = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (!$candidate.StartsWith($temporary, [StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $candidate -PathType Container) -or
        !(Test-Path -LiteralPath (Join-Path $candidate 'fixture.marker') -PathType Leaf) -or
        [IO.File]::ReadAllText((Join-Path $candidate 'fixture.marker')) -ne 'Fictitious Zanance physical/rate native workspace v1') { throw 'Original privately owned native workspace marker required.' }
    $part = Get-Item -LiteralPath $candidate
    while ($part) { if (($part.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Redirected native review workspace refused.' }; $part = $part.Parent }
    $reviewWorkspace = $candidate
}
if ($PhysicalRatesGuideOnly -and (!$PhysicalRatesReview -or !$PlanEditorSourceRun -or $PhysicalRatesOutcomesOnly -or $PhysicalRatesNotesOnly)) { throw 'Restored guide requires the exact completed original source.' }
if ($PhysicalRatesOutcomesOnly -and (!$PhysicalRatesReview -or !$PlanEditorSourceRun -or $PhysicalRatesNotesOnly)) { throw 'Numeric outcomes require the exact original input source.' }
if ($PhysicalRatesNotesOnly -and (!$PhysicalRatesReview -or !$PlanEditorSourceRun -or !$PhysicalRatesBaselineRun)) { throw 'Numeric wording continuation requires original source and baseline observations.' }
if ($GoalDetailEditFailureOnly -and !$GoalDetailReview) { throw 'Edit failure qualification requires its original detail source mode.' }
if ($GoalDetailReview -and ($GoalListReview -or !$PlanEditorSourceRun -or !$PlanEditorReview -or !$SecondaryFieldsReview -or $PlanEditorObservationRun)) {
    throw 'Goal detail qualification requires only its exact original secondary-fields source mode.'
}
if ($GoalListReview) {
    if (!$PlanEditorSourceRun -or !$PlanEditorReview -or !$SecondaryFieldsReview -or $PlanEditorObservationRun) {
        throw 'Goal list qualification requires only the exact original secondary-fields source.'
    }
}
if ($PhysicalRatesReview -and (!$PlanEditorReview -or $SecondaryHouseholdReview -or $SecondarySettingsReview -or $SecondaryFieldsReview -or $SecondaryUnitsReview -or
    $HoldingMoneyReview -or $AccountMoneyReview -or $EntryMoneyReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview -or $PlanEditorArchivedOnly -or $PlanEditorCaptionsOnly)) {
    throw 'Physical/rate numeric review requires one dedicated original source.'
}

if ($ImportFinancialContinuation -and !$ImportFinancialReview) { throw 'Import continuation requires its original financial review mode.' }
if ($HomeRefreshReferencesOnly -and !$HomeRefreshReview) { throw 'Home reference qualification requires its original review mode.' }
if ($ReconciliationContinuation -and !$ReconciliationReview) { throw 'Reconciliation continuation requires the original reconciliation review.' }
if ($ReconciliationVisualOnly -and (!$ReconciliationReview -or !$ReconciliationContinuation)) { throw 'Visual reconciliation continuation requires the exact original financial fixture.' }
if ($AccountTabRefreshContinuation -and !$AccountTabRefreshReview) { throw 'Account continuation requires the original account review.' }
if ($EditorAggregateCompleted -and (!$EditorRetainedAggregateOnly -or @($EditorAggregateCompleted.Split(',') | Select-Object -Unique).Count -ne $EditorAggregateCompleted.Split(',').Count)) { throw 'Completed aggregate cases require a unique original review subset.' }
if ($EditorRetainedAggregateOnly -and !$EditorTextReview) { throw 'Retained aggregate review requires the original editor review.' }
if ($HoldingDetailQueryOnly -and (!$HoldingDetailReview -or !$HoldingDetailCompletedRun)) { throw 'Original completed detail review required.' }
if ($HoldingDetailReview -and !$HoldingDetailSourceRun) { throw 'Retained original overview source is required for detail review.' }
if ($HoldingOverviewCaptionsOnly -and (!$HoldingOverviewReview -or !$HoldingOverviewContinuation -or !$HoldingOverviewCompletedRun)) { throw 'Changed overview captions require original completed source and review mode.' }
if ($HoldingOverviewContinuation -and !$HoldingOverviewReview) { throw 'Overview continuation requires its original review mode.' }
if ($HoldingTypeContinuation -and !$HoldingTypeReview) { throw 'Type continuation requires its original review mode.' }
if ($HoldingArchiveContinuation -and !$HoldingArchiveReview) { throw 'Archive continuation requires its original review mode.' }
if ($HoldingHistoryContinuation -and !$HoldingHistoryReview) { throw 'History continuation requires its original review mode.' }
if ($HoldingPriceContinuation -and !$HoldingPriceReview) { throw 'Price continuation requires its original review mode.' }
if ($HoldingQuantityContinuation -and !$HoldingQuantityInputReview) { throw 'Original quantity field continuation requires its explicit review.' }
if ($HoldingFeeInputContinuation -and !$HoldingFeeInputReview) { throw 'Original fee caption continuation requires its review mode.' }
if ($HoldingFinancialContinuation -and !$HoldingFinancialReview) { throw 'Original holding continuation requires its explicit review.' }
if ($EntryAttachmentsSourceRun -and !$EntryAttachmentsReview) { throw 'Original attachment continuation requires its review mode.' }
if ($EntrySaveSourceRun -and !$EntrySaveReview) { throw 'Original editor continuation requires its review mode.' }
if ($EntryDetailSourceRun -and !$EntryDetailReview) { throw 'Original detail continuation requires its review mode.' }
if ($SecondaryHouseholdReview -and (!$PlanEditorReview -or $SecondarySettingsReview -or $SecondaryFieldsReview -or $SecondaryUnitsReview -or $HoldingMoneyReview -or $AccountMoneyReview -or $EntryMoneyReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview)) { throw 'Secondary household requires its dedicated protected source mode.' }
if ($SecondarySettingsReview -and (!$PlanEditorReview -or $SecondaryFieldsReview -or $SecondaryUnitsReview -or $HoldingMoneyReview -or $AccountMoneyReview -or $EntryMoneyReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview)) { throw 'Secondary settings require their dedicated independent mode.' }
if ($SecondaryFieldsReview -and (!$PlanEditorReview -or $SecondaryUnitsReview -or $HoldingMoneyReview -or $AccountMoneyReview -or $EntryMoneyReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview)) { throw 'Secondary monetary fields require their dedicated independent mode.' }
if ($SecondaryUnitsReview -and (!$PlanEditorReview -or $HoldingMoneyReview -or $AccountMoneyReview -or $EntryMoneyReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview)) { throw 'Secondary unit review requires its dedicated independent mode.' }
if ($HoldingMoneyReview -and (!$PlanEditorReview -or $AccountMoneyReview -or $EntryMoneyReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview)) { throw 'Original holding money requires its dedicated mode.' }
if ($AccountCurrencyOnly -and (!$AccountMoneyReview -or $AccountDefaultOnly -or $PlanEditorSourceRun -or $PlanEditorObservationRun)) { throw 'Currency ownership requires fresh separate monetary fixtures.' }
if ($AccountDefaultOnly -and (!$AccountMoneyReview -or $PlanEditorSourceRun -or $PlanEditorObservationRun)) { throw 'Default ownership requires fresh separate monetary fixtures.' }
if ($AccountMoneyReview -and (!$PlanEditorReview -or $EntryMoneyReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview)) { throw 'Original account money requires its dedicated mode.' }
if ($EntryMoneyReview -and (!$PlanEditorReview -or $GoalBudgetMoneyReview -or $PlanMoneyReview)) { throw 'Original transaction money requires its dedicated mode.' }
if ($GoalBudgetMoneyReview -and (!$PlanEditorReview -or $PlanMoneyReview -or $PlanEditorArchivedOnly -or $PlanEditorCaptionsOnly -or $PlanMoneyDebtOnly -or $PlanMoneySubmissionOnly)) { throw 'Original goal/budget monetary review requires its dedicated independent mode.' }
if ($PlanMoneyReview -and (!$PlanEditorReview -or $PlanEditorCaptionsOnly -or $PlanEditorArchivedOnly)) { throw 'Monetary review requires only its original plan mode.' }
if ($PlanMoneySubmissionOnly -and (!$PlanMoneyReview -or !$PlanEditorSourceRun -or $PlanEditorObservationRun -or $PlanMoneyDebtOnly)) { throw 'Submission-only review requires its completed original money source.' }
if ($PlanMoneyDebtOnly -and (!$PlanMoneyReview -or !$PlanEditorSourceRun -or $PlanEditorObservationRun)) { throw 'Debt-only monetary review requires its completed original money source.' }
if ($PlanEditorArchivedOnly -and (!$PlanEditorReview -or !$PlanEditorSourceRun -or $PlanEditorCaptionsOnly)) { throw 'Archived reference review requires only its original plan source.' }
if ($PlanEditorCaptionsOnly -and (!$PlanEditorReview -or !$PlanEditorSourceRun)) { throw 'Changed captions require their original plan source.' }
if ($PlanEditorObservationRun) {
    if (!$PlanEditorSourceRun) { throw 'Retained observations require the exact original plan source.' }
    $observationRoot = $reviewWorkspace
    $observationProcess = Get-Content -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorObservationRun + '/process.json')) -Raw | ConvertFrom-Json
    if ((Get-Process -Id $observationProcess.ProcessId -ErrorAction SilentlyContinue) -and
        !($SecondaryHouseholdReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/secondary-household-prepared.json')))) -and
        !($SecondarySettingsReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-secondary-settings/secondary-settings-prepared.json')))) -and
        !($SecondaryFieldsReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-secondary-fields/secondary-fields-prepared.json')))) -and
        !($SecondaryUnitsReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-secondary-units/secondary-units-accepted.json')))) -and
        !($AccountMoneyReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-account-money/account-money-latest.json')))) -and
        !($HoldingMoneyReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-holding-money/holding-money-latest.json')))) -and
        !($HoldingMoneyReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-holding-money/started.json'))) -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-holding-money/prepared-accepted.json')))) -and
        !($EntryMoneyReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-entry-money/entry-money-actions-complete.json')))) -and
        !($GoalBudgetMoneyReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-goal-budget/budget-native-accepted.json')))) -and
        !($PlanMoneyReview -and (Test-Path -LiteralPath (Join-Path $observationRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-plan-form/money-save-verified.json'))))) { throw 'Retain original live observation process.' }
}
if ($PlanEditorSourceRun) {
    if (!$PlanEditorReview) { throw 'Original plan source requires its review mode.' }
    $planRoot = $reviewWorkspace
    $planOriginal = Get-Content -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/process.json')) -Raw | ConvertFrom-Json
    if ((Get-Process -Id $planOriginal.ProcessId -ErrorAction SilentlyContinue) -and
        !($SecondaryHouseholdReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/secondary-household-prepared.json')))) -and
        !($SecondarySettingsReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-secondary-settings/secondary-settings-prepared.json')))) -and
        !($SecondaryFieldsReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-secondary-fields/secondary-fields-prepared.json')))) -and
        !($SecondaryUnitsReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-secondary-units/secondary-units-accepted.json')))) -and
        !($AccountMoneyReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-account-money/account-money-latest.json')))) -and
        !($HoldingMoneyReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-holding-money/holding-money-latest.json')))) -and
        !($HoldingMoneyReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-holding-money/started.json'))) -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-holding-money/prepared-accepted.json')))) -and
        !($EntryMoneyReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-entry-money/entry-money-actions-complete.json')))) -and
        !($GoalBudgetMoneyReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-goal-budget/budget-native-accepted.json')))) -and
        !($PlanMoneyReview -and (Test-Path -LiteralPath (Join-Path $planRoot ('artifacts/store-drafts/' + $PlanEditorSourceRun + '/original-plan-form/money-save-verified.json'))))) { throw 'Preserve the live original plan process.' }
}
if ($EntryFormSourceRun -and !$EntryFormReview) { throw 'Original form continuation requires its review mode.' }
if ($EntryWorkflowSourceRun) {
    if (!$EntryWorkflowReview) { throw 'An original workflow continuation requires its explicit mode.' }
    $workflowRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    $originalProcess = Get-Content -LiteralPath (Join-Path $workflowRoot ('artifacts/store-drafts/' + $EntryWorkflowSourceRun + '/process.json')) -Raw | ConvertFrom-Json
    if (Get-Process -Id $originalProcess.ProcessId -ErrorAction SilentlyContinue) { throw 'Retain the original live workflow process; do not open another writer.' }
}
if ($EntryWorkflowAggregateOnly -and (!$EntryWorkflowReview -or !$EntryWorkflowSourceRun)) { throw 'Aggregate continuation requires its exact original workflow source.' }
if ($EntryWorkflowCaptionsOnly -and (!$EntryWorkflowReview -or !$EntryWorkflowSourceRun -or $EntryWorkflowAggregateOnly)) { throw 'Caption review requires only its exact original workflow source.' }
if ($EntryWorkflowBlockedOnly -and !$EntryWorkflowCaptionsOnly) { throw 'Blocked captions require the original caption review mode.' }
$reviewCount = @($OriginalAmountErrorReview.IsPresent, $BudgetScopeCaptionReview.IsPresent, $BudgetPeriodCaptionReview.IsPresent, $BudgetFinancialReadReview.IsPresent, $BudgetActionsLifetimeReview.IsPresent, $PlanEditorReview.IsPresent, $EntryWorkflowReview.IsPresent, $EntryFormReview.IsPresent, $EntryDetailReview.IsPresent, $EntrySaveReview.IsPresent, $EntryAttachmentsReview.IsPresent, $HoldingEventPublicationReview.IsPresent, $HoldingLocationReview.IsPresent, $HoldingDetailReview.IsPresent, $HoldingOverviewReview.IsPresent, $HoldingTypeReview.IsPresent, $HoldingArchiveReview.IsPresent, $HoldingHistoryReview.IsPresent, $HoldingPriceReview.IsPresent, $HoldingQuantityInputReview.IsPresent, $HoldingFeeActionReview.IsPresent, $HoldingFeeInputReview.IsPresent, $HoldingRefundReview.IsPresent, $HoldingFinancialReview.IsPresent, $SplitFinancialReview.IsPresent, $ImportFinancialReview.IsPresent, $OccurrenceRefreshReview.IsPresent, $OccurrenceStateReview.IsPresent, $OccurrenceCommitReview.IsPresent, $HomeRefreshReview.IsPresent, $SettlementCommitReview.IsPresent, $ReconciliationReview.IsPresent, $PlansAddHeaderReview.IsPresent, $AccountTabRefreshReview.IsPresent, $EcbReferenceReview.IsPresent, $ReportRowsReview.IsPresent, $BudgetTitlesReview.IsPresent, $HomeLegendsReview.IsPresent, $PlanRowsReview.IsPresent, $LedgerRowsReview.IsPresent, $ReimbursementsReview.IsPresent, $HomeTemplatesReview.IsPresent, $HomeReviewCountReview.IsPresent, $HomeAddHeaderReview.IsPresent, $GettingStartedReview.IsPresent, $CsvInputReview.IsPresent, $EditorTextReview.IsPresent | Where-Object { $_ }).Count
if ($CsvQuotationOnly -and (!$CsvInputReview -or $CsvInputEmptyOnly)) { throw 'Quotation continuation requires only the original CSV input review.' }
if ($CsvInputEmptyOnly -and !$CsvInputReview) { throw 'Empty-input continuation requires the original CSV input review.' }
if ($reviewCount -gt 1) { throw 'Choose one original review matrix.' }
if (($ReimbursementsVisibilityOnly -or $ReimbursementsEmptyOnly) -and !$ReimbursementsReview) { throw 'Visible reimbursement checks require their original review mode.' }
if ($PlanRowsEdgeProbe -and !$PlanRowsReview) { throw 'Plan amount edge probe requires its original review mode.' }
if ($LedgerRowsContexts) {
    if (!$LedgerRowsReview) { throw 'Explicit ledger contexts require their original review mode.' }
    $contexts = $LedgerRowsContexts.Split(',')
    if (($contexts | Select-Object -Unique).Count -ne $contexts.Count -or
        @($contexts | Where-Object { $_.Split('-')[0] -notin $LedgerRowsLanguages.Split(',') }).Count -ne 0) {
        throw 'Ledger contexts must be unique and belong to the explicit review languages.'
    }
}
if ($EditorDebtDetailsOnly -and !$EditorTextReview) { throw 'Debt-details continuation requires the original editor review.' }
if ($EditorGoalChoicesOnly -and (!$EditorTextReview -or $EditorDebtDetailsOnly)) { throw 'Goal-choice review requires only the original editor review.' }
if ($EditorNarrowActionsOnly -and (!$EditorTextReview -or $EditorDebtDetailsOnly -or $EditorGoalChoicesOnly)) { throw 'Narrow-action review requires only the original editor review.' }
if ($EditorOnboardingFooterOnly -and (!$EditorTextReview -or $EditorDebtDetailsOnly -or $EditorGoalChoicesOnly -or $EditorNarrowActionsOnly)) { throw 'Onboarding footer review requires only the original editor review.' }
if ($EditorOnboardingContexts -and (!$EditorOnboardingFooterOnly -or
    @($EditorOnboardingContexts.Split(',') | Select-Object -Unique).Count -ne $EditorOnboardingContexts.Split(',').Count)) {
    throw 'Explicit footer contexts require a unique original footer review subset.'
}
if ($BudgetActionsObservationsOnly -and (!$BudgetActionsLifetimeReview -or !$BudgetActionsSourceRun)) { throw 'Read-only budget observations require the retained original source.' }
if ($BudgetActionsSourceRun) {
    if (!$BudgetActionsLifetimeReview) { throw 'A retained budget source requires its dedicated lifecycle review.' }
    $budgetSourceRoot = Join-Path $reviewWorkspace ('artifacts/store-drafts/' + $BudgetActionsSourceRun)
    $budgetSourceProcess = Get-Content -LiteralPath (Join-Path $budgetSourceRoot 'process.json') -Raw | ConvertFrom-Json
    if ($budgetSourceProcess.Run -ne $BudgetActionsSourceRun -or (Get-Process -Id $budgetSourceProcess.ProcessId -ErrorAction SilentlyContinue)) {
        throw 'Preserve the original budget process; a live or unrelated source cannot be continued.'
    }
    if (!(Test-Path -LiteralPath (Join-Path $budgetSourceRoot 'budget-action-lifetime.db'))) { throw 'Original budget source is missing.' }
}
if ($OriginalAmountErrorRecapture -and (!$OriginalAmountErrorReview -or !$BudgetPeriodSourceRun)) { throw 'Error recapture requires its retained original source.' }
if ($BudgetPeriodSourceRun) {
    if (!($BudgetPeriodCaptionReview -or $BudgetScopeCaptionReview -or $OriginalAmountErrorReview)) { throw 'Retained caption source requires its dedicated mode.' }
    $captionSource = Join-Path $reviewWorkspace ('artifacts/store-drafts/' + $BudgetPeriodSourceRun)
    $captionProcess = Get-Content -LiteralPath (Join-Path $captionSource 'process.json') -Raw | ConvertFrom-Json
    if ($captionProcess.Run -ne $BudgetPeriodSourceRun -or (Get-Process -Id $captionProcess.ProcessId -ErrorAction SilentlyContinue)) { throw 'Retain the live original caption process.' }
    if (!(Test-Path -LiteralPath (Join-Path $captionSource $(if ($OriginalAmountErrorReview) { 'original-amount-errors.db' } elseif ($BudgetScopeCaptionReview) { 'budget-scope-captions.db' } else { 'budget-period-captions.db' })))) { throw 'Original caption source missing.' }
}
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $repositoryRoot
$root = $reviewWorkspace
if ($UseExistingBuild -and !$RunId) { throw 'An existing build requires its exact original run id.' }
$run = if ($RunId) { $RunId } else { [Guid]::NewGuid().ToString('N') }
$identity = 'pro.vafadar.zanance.storepreview.' + $run.Substring(0, 12)
$output = Join-Path $root ('artifacts/store-drafts/' + $run)
$entryFormCompleted = 0
if ($EntryFormSourceRun) {
    $originalProgress = Join-Path $root ('artifacts/store-drafts/' + $EntryFormSourceRun + '/original/progress.json')
    if (Test-Path -LiteralPath $originalProgress) {
        $entryFormCompleted = (Get-Content -LiteralPath $originalProgress -Raw | ConvertFrom-Json).CompletedStages.Count
    }
}
if ((Test-Path -LiteralPath $output) -and (!$UseExistingBuild -or (Test-Path -LiteralPath (Join-Path $output 'process.json')))) {
    throw 'Preserve the existing draft run/process; never restart it after observation expiry.'
}
New-Item -ItemType Directory -Path $output -Force | Out-Null
$project = 'src/Apps/Zanance/Vafadar.Zanance.App'
$framework = 'net10.0-windows10.0.19041.0'
$log = Join-Path $output 'build.log'
$buildRoot = if ($env:VAFADAR_REVIEW_BUILD_ROOT) { $env:VAFADAR_REVIEW_BUILD_ROOT } else { Join-Path $output 'build' }
if ($env:VAFADAR_REVIEW_BUILD_ROOT -and !$UseExistingBuild -and (Test-Path -LiteralPath $buildRoot) -and @(Get-ChildItem -LiteralPath $buildRoot -Force).Count -gt 0) { throw 'A new independent review requires fresh alternate-volume build output.' }
if (!$UseExistingBuild) {
    # Each live review owns its binaries and PRI files as well as its fictional data. A continuation
    # must not overwrite a previous process's mapped resources or the ordinary development output.
    dotnet build $project -f $framework -p:ContinuousIntegrationBuild=true "-p:ApplicationId=$identity" --artifacts-path $buildRoot -nologo *> $log
    if ($LASTEXITCODE -ne 0) {
        Get-Content -LiteralPath $log -Tail 20 | Write-Host
        throw "Independent store draft build failed; retain $output."
    }
}
if (Select-String -LiteralPath $log -Pattern ': warning ' -Quiet) { throw 'Resolve draft build warnings first.' }
$executables = @(Get-ChildItem -LiteralPath (Join-Path $buildRoot 'bin/Vafadar.Zanance.App') -Recurse -Filter Vafadar.Zanance.App.exe)
$manifests = @(Get-ChildItem -LiteralPath (Join-Path $buildRoot 'obj/Vafadar.Zanance.App') -Recurse -Filter Package.appxmanifest |
    Where-Object { $_.FullName -match '[\\/]resizetizer[\\/]m[\\/]' })
if ($executables.Count -ne 1 -or $manifests.Count -ne 1) { throw 'One original isolated executable and compiled manifest are required.' }
$directory = $executables[0].DirectoryName
$manifest = $manifests[0].FullName
if (!(Test-Path -LiteralPath $manifest)) { throw 'Independent compiled manifest is missing.' }
[xml]$metadata = Get-Content -LiteralPath $manifest -Raw
if ($metadata.Package.Identity.Name -ne $identity) { throw 'Compiled draft identity does not match the requested run.' }
$variables = @('VAFADAR_ORIGINAL_AMOUNT_ERROR_RECAPTURE', 'VAFADAR_ORIGINAL_AMOUNT_ERROR_SOURCE', 'VAFADAR_BUDGET_PERIOD_SOURCE', 'VAFADAR_BUDGET_ACTION_OBSERVATIONS_ONLY', 'VAFADAR_BUDGET_ACTION_SOURCE_RUN', 'VAFADAR_GOAL_DETAIL_EDIT_FAILURE', 'VAFADAR_GOAL_DETAIL_REVIEW', 'VAFADAR_GOAL_LIST_REVIEW', 'VAFADAR_PHYSICAL_RATES_GUIDE_ONLY', 'VAFADAR_PHYSICAL_RATES_OUTCOMES_ONLY', 'VAFADAR_PHYSICAL_RATES_NOTES_ONLY', 'VAFADAR_PHYSICAL_RATES_BASELINE_RUN', 'VAFADAR_PHYSICAL_RATES_REVIEW', 'VAFADAR_SECONDARY_FIELDS_REVIEW', 'VAFADAR_SECONDARY_UNITS_REVIEW', 'VAFADAR_HOLDING_MONEY_REVIEW', 'VAFADAR_ACCOUNT_CURRENCY_ONLY', 'VAFADAR_ACCOUNT_DEFAULT_ONLY', 'VAFADAR_ACCOUNT_MONEY_REVIEW', 'VAFADAR_ENTRY_MONEY_REVIEW', 'VAFADAR_PLAN_MONEY_SUBMISSION_ONLY', 'VAFADAR_PLAN_MONEY_DEBT_ONLY', 'VAFADAR_GOAL_BUDGET_MONEY_REVIEW', 'VAFADAR_PLAN_MONEY_REVIEW', 'VAFADAR_PLAN_FORM_ARCHIVED_ONLY', 'VAFADAR_PLAN_FORM_CAPTIONS_ONLY', 'VAFADAR_PLAN_FORM_OBSERVATION_RUN', 'VAFADAR_PLAN_FORM_SOURCE_RUN', 'VAFADAR_HOLDING_DETAIL_QUERY_ONLY', 'VAFADAR_HOLDING_DETAIL_COMPLETED', 'VAFADAR_HOLDING_DETAIL_SOURCE', 'VAFADAR_HOLDING_OVERVIEW_CAPTIONS', 'VAFADAR_HOLDING_OVERVIEW_COMPLETED', 'VAFADAR_HOLDING_OVERVIEW_CONTINUATION', 'VAFADAR_HOLDING_HISTORY_CONTINUATION', 'VAFADAR_HOLDING_PRICE_CONTINUATION', 'VAFADAR_HOLDING_QUANTITY_CONTINUATION', 'VAFADAR_HOLDING_FEE_CONTINUATION','VAFADAR_HOLDING_FINANCIAL_CONTINUATION','VAFADAR_IMPORT_FINANCIAL_CONTINUATION','VAFADAR_RECONCILIATION_VISUAL_ONLY', 'VAFADAR_RECONCILIATION_CONTINUATION', 'VAFADAR_ACCOUNT_TAB_CONTINUATION','VAFADAR_RETAINED_AGGREGATE_COMPLETED','VAFADAR_EDITOR_RETAINED_AGGREGATE_ONLY','VAFADAR_EDITOR_ONBOARDING_CONTEXTS','VAFADAR_EDITOR_ONBOARDING_FOOTER_ONLY','VAFADAR_EDITOR_NARROW_ACTIONS_ONLY','VAFADAR_EDITOR_GOAL_CHOICES_ONLY','VAFADAR_EDITOR_DEBT_DETAILS_ONLY','VAFADAR_SNAPSHOTS','VAFADAR_SNAPSHOT_ONLY','VAFADAR_STORE_DRAFT_RUN',
    'VAFADAR_WINDOW_SIZE','VAFADAR_SNAPSHOT_FONT_SCALE','VAFADAR_START_ROUTE','VAFADAR_STORE_DRAFT_REVIEW', 'VAFADAR_ENTRY_ATTACHMENT_SOURCE_RUN', 'VAFADAR_ENTRY_FORM_SOURCE_RUN', 'VAFADAR_ENTRY_DETAIL_SOURCE_RUN', 'VAFADAR_ENTRY_SAVE_SOURCE_RUN','VAFADAR_HOME_LEGEND_LANGUAGES','VAFADAR_PLAN_ROWS_EDGE_PROBE','VAFADAR_LEDGER_ROWS_LANGUAGES','VAFADAR_LEDGER_ROWS_CONTEXTS','VAFADAR_REIMBURSEMENTS_LANGUAGES','VAFADAR_REIMBURSEMENTS_VISIBILITY_ONLY','VAFADAR_REIMBURSEMENTS_EMPTY_ONLY')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $env:VAFADAR_BUDGET_PERIOD_SOURCE = if ($BudgetPeriodSourceRun -and !$OriginalAmountErrorReview) { $captionSource } else { '' }
    $env:VAFADAR_ORIGINAL_AMOUNT_ERROR_RECAPTURE = if ($OriginalAmountErrorReview) { $OriginalAmountErrorRecapture } else { '' }
    $env:VAFADAR_ORIGINAL_AMOUNT_ERROR_SOURCE = if ($BudgetPeriodSourceRun -and $OriginalAmountErrorReview) { $captionSource } else { '' }
    $env:VAFADAR_BUDGET_ACTION_SOURCE_RUN = $BudgetActionsSourceRun
    $env:VAFADAR_BUDGET_ACTION_OBSERVATIONS_ONLY = if ($BudgetActionsObservationsOnly) { '1' } else { '' }
    $env:VAFADAR_GOAL_DETAIL_EDIT_FAILURE = if ($GoalDetailEditFailureOnly) { '1' } else { '' }
    $env:VAFADAR_GOAL_DETAIL_REVIEW = if ($GoalDetailReview) { '1' } else { '' }
    $env:VAFADAR_GOAL_LIST_REVIEW = if ($GoalListReview) { '1' } else { '' }
    $env:VAFADAR_PLAN_FORM_CAPTIONS_ONLY = if ($PlanEditorCaptionsOnly) { '1' } else { '' }
    $env:VAFADAR_PLAN_MONEY_SUBMISSION_ONLY = if ($PlanMoneySubmissionOnly) { '1' } else { '' }
    $env:VAFADAR_PLAN_MONEY_DEBT_ONLY = if ($PlanMoneyDebtOnly) { '1' } else { '' }
    $env:VAFADAR_ACCOUNT_CURRENCY_ONLY = if ($AccountCurrencyOnly) { '1' } else { '' }
    $env:VAFADAR_ACCOUNT_DEFAULT_ONLY = if ($AccountDefaultOnly) { '1' } else { '' }
    $env:VAFADAR_PHYSICAL_RATES_GUIDE_ONLY = if ($PhysicalRatesGuideOnly) { '1' } else { '' }
    $env:VAFADAR_PHYSICAL_RATES_OUTCOMES_ONLY = if ($PhysicalRatesOutcomesOnly) { '1' } else { '' }
    $env:VAFADAR_PHYSICAL_RATES_NOTES_ONLY = if ($PhysicalRatesNotesOnly) { '1' } else { '' }
    $env:VAFADAR_PHYSICAL_RATES_BASELINE_RUN = $PhysicalRatesBaselineRun
    $env:VAFADAR_PHYSICAL_RATES_REVIEW = if ($PhysicalRatesReview) { '1' } else { '' }
    $env:VAFADAR_SECONDARY_HOUSEHOLD_REVIEW = if ($SecondaryHouseholdReview) { '1' } else { '' }
    $env:VAFADAR_SECONDARY_SETTINGS_REVIEW = if ($SecondarySettingsReview) { '1' } else { '' }
    $env:VAFADAR_SECONDARY_FIELDS_REVIEW = if ($SecondaryFieldsReview) { '1' } else { '' }
    $env:VAFADAR_SECONDARY_UNITS_REVIEW = if ($SecondaryUnitsReview) { '1' } else { '' }
    $env:VAFADAR_HOLDING_MONEY_REVIEW = if ($HoldingMoneyReview) { '1' } else { '' }
    $env:VAFADAR_ACCOUNT_MONEY_REVIEW = if ($AccountMoneyReview) { '1' } else { '' }
    $env:VAFADAR_ENTRY_MONEY_REVIEW = if ($EntryMoneyReview) { '1' } else { '' }
    $env:VAFADAR_GOAL_BUDGET_MONEY_REVIEW = if ($GoalBudgetMoneyReview) { '1' } else { '' }
    $env:VAFADAR_PLAN_MONEY_REVIEW = if ($PlanMoneyReview) { '1' } else { '' }
    $env:VAFADAR_PLAN_FORM_ARCHIVED_ONLY = if ($PlanEditorArchivedOnly) { '1' } else { '' }
    $env:VAFADAR_PLAN_FORM_SOURCE_RUN = $PlanEditorSourceRun
    $env:VAFADAR_PLAN_FORM_OBSERVATION_RUN = $PlanEditorObservationRun
    $workflowSourceBefore = [Environment]::GetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_SOURCE_RUN', 'Process')
    $workflowAggregateBefore = [Environment]::GetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_AGGREGATE_ONLY', 'Process')
    $workflowCaptionsBefore = [Environment]::GetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_CAPTIONS_ONLY', 'Process')
    $workflowBlockedBefore = [Environment]::GetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_BLOCKED_ONLY', 'Process')
    $env:VAFADAR_ENTRY_WORKFLOW_SOURCE_RUN = $EntryWorkflowSourceRun
    $env:VAFADAR_ENTRY_WORKFLOW_AGGREGATE_ONLY = if ($EntryWorkflowAggregateOnly) { '1' } else { '' }
    $env:VAFADAR_ENTRY_WORKFLOW_CAPTIONS_ONLY = if ($EntryWorkflowCaptionsOnly) { '1' } else { '' }
    $env:VAFADAR_ENTRY_WORKFLOW_BLOCKED_ONLY = if ($EntryWorkflowBlockedOnly) { '1' } else { '' }
    $env:VAFADAR_HOLDING_FEE_CONTINUATION = $HoldingFeeInputContinuation
    $env:VAFADAR_HOLDING_QUANTITY_CONTINUATION = $HoldingQuantityContinuation
    $env:VAFADAR_HOLDING_FINANCIAL_CONTINUATION = $HoldingFinancialContinuation
    $env:VAFADAR_IMPORT_FINANCIAL_CONTINUATION = $ImportFinancialContinuation
    $env:VAFADAR_SNAPSHOTS = $output
    $env:VAFADAR_SNAPSHOT_ONLY = 'store-draft'
    $env:VAFADAR_STORE_DRAFT_RUN = $run
    $env:VAFADAR_ACCOUNT_TAB_CONTINUATION = $AccountTabRefreshContinuation
    $env:VAFADAR_WINDOW_SIZE = if ($AccountTabRefreshReview) { '1280x892' } else { '412x800' }
    $env:VAFADAR_EDITOR_DEBT_DETAILS_ONLY = if ($EditorDebtDetailsOnly) { '1' } else { '' }
    $env:VAFADAR_RETAINED_AGGREGATE_COMPLETED = $EditorAggregateCompleted
    $env:VAFADAR_EDITOR_RETAINED_AGGREGATE_ONLY = if ($EditorRetainedAggregateOnly) { '1' } else { '' }
    $env:VAFADAR_EDITOR_GOAL_CHOICES_ONLY = if ($EditorGoalChoicesOnly) { '1' } else { '' }
    $env:VAFADAR_EDITOR_NARROW_ACTIONS_ONLY = if ($EditorNarrowActionsOnly) { '1' } else { '' }
    $env:VAFADAR_EDITOR_ONBOARDING_FOOTER_ONLY = if ($EditorOnboardingFooterOnly) { '1' } else { '' }
    $env:VAFADAR_EDITOR_ONBOARDING_CONTEXTS = $EditorOnboardingContexts
    $env:VAFADAR_SNAPSHOT_FONT_SCALE = if ($CsvInputReview -or $EditorTextReview -or $ReportRowsReview -or $BudgetTitlesReview -or $HomeLegendsReview -or $PlanRowsReview -or $LedgerRowsReview -or $ReimbursementsReview) { '2' } else { '' }
    $env:VAFADAR_RECONCILIATION_VISUAL_ONLY = $ReconciliationVisualOnly.IsPresent.ToString().ToLowerInvariant()
    $env:VAFADAR_RECONCILIATION_CONTINUATION = $ReconciliationContinuation
    $env:VAFADAR_HOLDING_DETAIL_QUERY_ONLY = if ($HoldingDetailQueryOnly) { '1' } else { '' }
    $env:VAFADAR_HOLDING_DETAIL_COMPLETED = $HoldingDetailCompletedRun
    $env:VAFADAR_HOLDING_DETAIL_SOURCE = $HoldingDetailSourceRun
    $env:VAFADAR_HOLDING_OVERVIEW_CAPTIONS = if ($HoldingOverviewCaptionsOnly) { '1' } else { '' }
    $env:VAFADAR_HOLDING_OVERVIEW_COMPLETED = $HoldingOverviewCompletedRun
    $env:VAFADAR_HOLDING_OVERVIEW_CONTINUATION = $HoldingOverviewContinuation
    $env:VAFADAR_HOLDING_TYPE_CONTINUATION = $HoldingTypeContinuation
    $env:VAFADAR_HOLDING_ARCHIVE_CONTINUATION = $HoldingArchiveContinuation
    $env:VAFADAR_HOLDING_HISTORY_CONTINUATION = $HoldingHistoryContinuation
    $env:VAFADAR_HOLDING_PRICE_CONTINUATION = $HoldingPriceContinuation
    $env:VAFADAR_ENTRY_FORM_SOURCE_RUN = $EntryFormSourceRun
    $env:VAFADAR_ENTRY_DETAIL_SOURCE_RUN = $EntryDetailSourceRun
    $env:VAFADAR_ENTRY_SAVE_SOURCE_RUN = $EntrySaveSourceRun
    $env:VAFADAR_ENTRY_ATTACHMENT_SOURCE_RUN = $EntryAttachmentsSourceRun
    $env:VAFADAR_STORE_DRAFT_REVIEW = if ($OriginalAmountErrorReview) { 'original-amount-errors' } elseif ($BudgetScopeCaptionReview) { 'budget-scope-captions' } elseif ($BudgetPeriodCaptionReview) { 'budget-period-captions' } elseif ($BudgetFinancialReadReview) { 'budget-financial-read' } elseif ($BudgetActionsLifetimeReview) { 'budget-action-lifetime' } elseif ($PlanEditorReview) { 'plan-form' } elseif ($EntryWorkflowReview) { 'entry-workflow' } elseif ($EntryFormReview) { 'entry-form' } elseif ($EntryDetailReview) { 'entry-detail' } elseif ($EntrySaveReview) { 'entry-save' } elseif ($EntryAttachmentsReview) { 'entry-attachments' } elseif ($HoldingEventPublicationReview) { 'holding-event-publication' } elseif ($HoldingLocationReview) { 'holding-location' } elseif ($HoldingDetailReview) { 'holding-detail' } elseif ($HoldingOverviewReview) { 'holding-overview' } elseif ($HoldingTypeReview) { 'holding-type' } elseif ($HoldingArchiveReview) { 'holding-archive' } elseif ($HoldingHistoryReview) { 'holding-history' } elseif ($HoldingPriceReview) { 'holding-price' } elseif ($HoldingQuantityInputReview) { 'holding-quantity-input' } elseif ($HoldingFeeActionReview) { 'holding-fee-action' } elseif ($HoldingFeeInputReview) { 'holding-fee-input' } elseif ($HoldingRefundReview) { 'holding-refund-review' } elseif ($HoldingFinancialReview) { 'holding-financial-review' } elseif ($SplitFinancialReview) { 'split-financial-review' } elseif ($ImportFinancialReview) { 'import-financial-review' } elseif ($OccurrenceRefreshReview) { 'occurrence-refresh' } elseif ($OccurrenceStateReview) { 'occurrence-state' } elseif ($OccurrenceCommitReview) { 'occurrence-commit' } elseif ($HomeRefreshReview) { if ($HomeRefreshReferencesOnly) { 'home-refresh-references' } else { 'home-refresh' } } elseif ($SettlementCommitReview) { 'settlement-commit' } elseif ($ReconciliationReview) { 'reconciliation' } elseif ($PlansAddHeaderReview) { 'plans-add-header' } elseif ($AccountTabRefreshReview) { 'account-tab-refresh' } elseif ($EcbReferenceReview) { 'ecb-reference' } elseif ($CsvInputReview -and $CsvQuotationOnly) { 'csv-quotation' } elseif ($CsvInputReview -and $CsvInputEmptyOnly) { 'csv-input-empty' } elseif ($CsvInputReview) { 'csv-input' } elseif ($ReportRowsReview) { 'report-rows' } elseif ($BudgetTitlesReview) { 'budget-titles' } elseif ($HomeLegendsReview) { 'home-legends' } elseif ($PlanRowsReview) { 'plan-rows' } elseif ($LedgerRowsReview) { 'ledger-rows' } elseif ($ReimbursementsReview) { 'reimbursements' } elseif ($HomeTemplatesReview) { 'home-templates' } elseif ($GettingStartedReview) { 'getting-started' } elseif ($HomeAddHeaderReview) { 'home-add-header' } elseif ($HomeReviewCountReview) { 'home-review-count' } elseif ($EditorTextReview) { 'editor-text' } else { '' }
    $env:VAFADAR_HOME_LEGEND_LANGUAGES = if ($HomeLegendsReview) { $HomeLegendsLanguages } else { '' }
    $env:VAFADAR_PLAN_ROWS_EDGE_PROBE = if (($ReimbursementsVisibilityOnly -or $ReimbursementsEmptyOnly) -and !$ReimbursementsReview) { throw 'Visible reimbursement checks require their original review mode.' }
if ($PlanRowsEdgeProbe) { '1' } else { '' }
    $env:VAFADAR_LEDGER_ROWS_LANGUAGES = if ($LedgerRowsReview) { $LedgerRowsLanguages } else { '' }
    $env:VAFADAR_LEDGER_ROWS_CONTEXTS = if ($LedgerRowsReview) { $LedgerRowsContexts } else { '' }
    $env:VAFADAR_REIMBURSEMENTS_LANGUAGES = if ($ReimbursementsReview) { $ReimbursementsLanguages } else { '' }
    $env:VAFADAR_REIMBURSEMENTS_VISIBILITY_ONLY = if ($ReimbursementsVisibilityOnly) { '1' } else { '' }
    $env:VAFADAR_REIMBURSEMENTS_EMPTY_ONLY = if ($ReimbursementsEmptyOnly) { '1' } else { '' }
    $env:VAFADAR_START_ROUTE = ''
    Add-Type -Path (Join-Path $PSScriptRoot '../modules/StoreDraftProcess.cs')
    $inputBefore = [Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName()
    $lease = [Vafadar.Build.StoreDraftProcess]::Start((Join-Path $directory 'Vafadar.Zanance.App.exe'), $root, $run)
    $process = $lease.Process
    [ordered]@{ Run=$run; Package=$identity; ProcessId=$process.Id; Output=$output; Desktop=$lease.DesktopName; InputDesktopBefore=$inputBefore } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $output 'process.json') -Encoding UTF8
    if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Observation expired for PID $($process.Id); retain that process and partial evidence at $output. Do not restart it."
    }
    if ($process.ExitCode -ne 0) { throw "Draft process ended with exit code $($process.ExitCode); preserve $output." }
    if (($OriginalAmountErrorReview -or $BudgetFinancialReadReview -or $BudgetPeriodCaptionReview -or $BudgetScopeCaptionReview) -and (Test-Path -LiteralPath (Join-Path $output 'cleanup-failure.txt'))) {
        throw 'Original financial observations completed but review cleanup failed; preserve the same source and evidence.'
    }
    if ($OccurrenceStateReview) { & (Join-Path $PSScriptRoot 'Test-OccurrenceStateReview.ps1') -OutputDirectory $output }
    elseif (Test-Path -LiteralPath (Join-Path $output 'error.txt')) { throw "Draft capture failed; preserve $output/error.txt." }
    $receiptPath = Join-Path $output 'complete.json'
    if (!(Test-Path -LiteralPath $receiptPath)) { throw 'No completed original draft receipt.' }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    function Count-QualifiedPlanMoney([string]$id) {
        if (!$id) { return 0 }
        return @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $id)) -Recurse -Filter observation.json | Where-Object {
            $row = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
            @($row.NativeReadings | Where-Object { $_.Text -eq '1.234' }).Count -gt 0
        }).Count
    }
    function Get-CorrectedNumericCount([string]$id) {
        if (!$id) { return 0 }
        $directory = Join-Path $root ('artifacts/store-drafts/' + $id)
        return @(Get-ChildItem -LiteralPath $directory -Recurse -File -Filter observation.json | Where-Object {
            $row = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
            $row.NumericCaptionVersion -eq 'physical-rate-value-v1' -and $row.Stage -match '-(physical-original|physical-partial|physical-invalid|rate-original|rate-partial|rate-pair)$'
        }).Count
    }
    $expectedCount = if ($OriginalAmountErrorReview) { 54 } elseif ($BudgetScopeCaptionReview) { 18 } elseif ($BudgetPeriodCaptionReview) { 54 } elseif ($BudgetFinancialReadReview) { 18 } elseif ($BudgetActionsLifetimeReview) { 6 } elseif ($GoalDetailEditFailureOnly) { 2 } elseif ($GoalDetailReview) { 144 } elseif ($GoalListReview) { 84 } elseif ($PhysicalRatesGuideOnly) { 1 } elseif ($PhysicalRatesOutcomesOnly) { 58 } elseif ($PhysicalRatesNotesOnly) { 108 - (Get-CorrectedNumericCount $PlanEditorSourceRun) - (Get-CorrectedNumericCount $PlanEditorObservationRun) } elseif ($PhysicalRatesReview) { 144 - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($SecondaryHouseholdReview) { if ($PlanEditorObservationRun) { 36 } else { 54 } } elseif ($SecondarySettingsReview) { 73 - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($SecondaryFieldsReview) { 149 - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter money.png).Count } else { 0 }) - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($SecondaryUnitsReview) { 91 - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter money.png).Count } else { 0 }) - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($HoldingMoneyReview) { 147 - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter money.png).Count } else { 0 }) - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($AccountCurrencyOnly) { 4 } elseif ($AccountDefaultOnly) { 2 } elseif ($AccountMoneyReview) { 110 - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter money.png).Count } else { 0 }) - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($EntryMoneyReview) { 112 - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter money.png).Count } else { 0 }) - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($GoalBudgetMoneyReview) { 72 - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter money.png).Count } else { 0 }) - $(if ($PlanEditorSourceRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter money.png).Count } else { 0 }) } elseif ($PlanMoneySubmissionOnly) { 2 } elseif ($PlanMoneyDebtOnly) { 2 } elseif ($PlanMoneyReview) { 54 - (Count-QualifiedPlanMoney $PlanEditorObservationRun) - (Count-QualifiedPlanMoney $PlanEditorSourceRun) } elseif ($PlanEditorArchivedOnly) { 1 } elseif ($PlanEditorCaptionsOnly) { 54 } elseif ($PlanEditorReview) { if ($PlanEditorSourceRun) { 112 - $(if ($PlanEditorObservationRun) { @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorObservationRun)) -Recurse -Filter plan.png).Count } else { 0 }) - @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $PlanEditorSourceRun)) -Recurse -Filter plan.png).Count } else { 112 } } elseif ($EntryWorkflowBlockedOnly) { 54 } elseif ($EntryWorkflowCaptionsOnly) { 36 } elseif ($EntryWorkflowAggregateOnly) { 1 } elseif ($EntryWorkflowReview) { 3 } elseif ($EntryFormReview) { 96 - $entryFormCompleted } elseif ($EntryDetailReview) { 24 } elseif ($EntrySaveReview) { 1 } elseif ($EntryAttachmentsSourceRun) { 18 } elseif ($EntryAttachmentsReview) { 54 } elseif ($HoldingEventPublicationReview) { 56 } elseif ($HoldingLocationReview) { 21 } elseif ($HoldingDetailQueryOnly) { 2 } elseif ($HoldingDetailReview) { 57 } elseif ($HoldingOverviewCaptionsOnly) { 36 } elseif ($HoldingOverviewContinuation) { 56 - @(Get-ChildItem -LiteralPath (Join-Path $root ('artifacts/store-drafts/' + $HoldingOverviewContinuation)) -Recurse -Filter overview.png | Where-Object { $_.FullName -notmatch '[\\/]build[\\/]' }).Count } elseif ($HoldingOverviewReview) { 56 } elseif ($HoldingTypeContinuation) { 54 } elseif ($HoldingTypeReview) { 57 } elseif ($HoldingArchiveContinuation) { 36 } elseif ($HoldingArchiveReview) { 40 } elseif ($HoldingHistoryContinuation) { 72 } elseif ($HoldingHistoryReview) { 78 } elseif ($HoldingPriceContinuation) { 18 } elseif ($HoldingPriceReview) { 21 } elseif ($HoldingQuantityContinuation) { 18 } elseif ($HoldingQuantityInputReview) { 22 } elseif ($HoldingFeeActionReview) { 26 } elseif ($HoldingFeeInputContinuation) { 12 } elseif ($HoldingFeeInputReview) { 22 } elseif ($HoldingRefundReview) { 40 } elseif ($HoldingFinancialReview -or $SplitFinancialReview) { 36 } elseif ($ImportFinancialReview) { 24 } elseif ($OccurrenceRefreshReview) { 42 } elseif ($OccurrenceStateReview) { 6 } elseif ($HomeRefreshReferencesOnly) { 1 } elseif ($HomeRefreshReview) { 6 } elseif ($AccountTabRefreshReview) { 6 } elseif ($EditorRetainedAggregateOnly) { 36 - @($EditorAggregateCompleted.Split(',') | Where-Object { $_ }).Count } elseif ($EcbReferenceReview) { 54 } elseif ($OccurrenceCommitReview -or $SettlementCommitReview -or $ReconciliationReview -or $PlansAddHeaderReview -or $CsvInputReview -or $HomeReviewCountReview -or $HomeAddHeaderReview -or $GettingStartedReview) { 18 } elseif ($EditorOnboardingContexts) { $EditorOnboardingContexts.Split(',').Count } elseif ($EditorTextReview) { 18 } elseif ($HomeTemplatesReview) { 6 } elseif ($ReimbursementsReview) { $ReimbursementsLanguages.Split(',').Count * 6 } elseif ($LedgerRowsReview -and $LedgerRowsContexts) { $LedgerRowsContexts.Split(',').Count } elseif ($LedgerRowsReview) { $LedgerRowsLanguages.Split(',').Count * 6 } elseif (($ReimbursementsVisibilityOnly -or $ReimbursementsEmptyOnly) -and !$ReimbursementsReview) { throw 'Visible reimbursement checks require their original review mode.' }
if ($PlanRowsEdgeProbe) { 1 } elseif ($HomeLegendsReview) { $HomeLegendsLanguages.Split(',').Count * 6 } elseif ($ReportRowsReview -or $BudgetTitlesReview -or $PlanRowsReview -or $ReimbursementsReview) { 18 } else { 48 }
    $financialInputsQualified = if ($OriginalAmountErrorReview) { $receipt.Scope -eq 'original-amount-errors' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.FinancialSaves -eq 0 } elseif ($BudgetScopeCaptionReview) { $receipt.Scope -eq 'budget-scope-captions' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.FinancialSaves -eq 0 } elseif ($BudgetPeriodCaptionReview) { $receipt.Scope -eq 'budget-period-captions' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.FinancialSaves -eq 0 } elseif ($BudgetFinancialReadReview) { $receipt.Scope -eq 'budget-financial-read' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.FinancialSaves -eq 0 } elseif ($BudgetActionsLifetimeReview) { $receipt.Scope -eq 'budget-action-lifetime' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.NonBudgetRowsUnchanged -and $receipt.NativeAcceptedWrites -eq $(if ($BudgetActionsObservationsOnly) { 0 } else { 3 }) -and $receipt.NativeRetries -eq $(if ($BudgetActionsObservationsOnly) { 0 } else { 3 }) -and $receipt.LedgerRows -eq 0 } elseif ($GoalDetailEditFailureOnly) { $receipt.Scope -eq 'goal-detail-edit-failure' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.OriginalSourceUnchanged -and $receipt.AcceptedWrites -eq 0 -and $receipt.NativeBacks -eq 1 -and $receipt.NativeRetries -eq 1 } elseif ($GoalDetailReview) { $receipt.Scope -eq 'goal-detail' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.OriginalSourceUnchanged -and $receipt.AcceptedWrites -eq 0 -and $receipt.NativeBacks -eq 36 -and $receipt.NativeRetries -eq 36 } elseif ($GoalListReview) { $receipt.Scope -eq 'goal-list' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.OriginalSourceUnchanged -and $receipt.AcceptedWrites -eq 0 -and $receipt.NativeDetailVisits -eq 6 -and $receipt.NativeRetries -eq 18 } elseif ($PhysicalRatesReview) {
        if ($PhysicalRatesOutcomesOnly) { $receipt.Scope -eq 'physical-rate-outcomes' -and $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeTypeSaves -eq 1 -and $receipt.NativeRateSaves -eq 1 -and $receipt.NativeConcurrentRateVersion -eq 1 -and $receipt.NativeRateDeletes -eq 1 -and $receipt.NativeLedgerEntries -eq 0 } else { $receipt.Scope -in @('physical-rate-inputs', 'physical-rate-notes', 'physical-rate-guide') -and $receipt.OriginalBaseStoreUnchanged -and $receipt.OriginalNumericRowsUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeTypeSaves -eq 0 -and $receipt.NativeRateSaves -eq 0 -and $receipt.NativeLedgerEntries -eq 0 }
    } elseif ($SecondaryHouseholdReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.OriginalProtectedRowsUnchanged -and $receipt.OriginalMembershipAuthority -and $receipt.NativeSharedSaves -eq 0
    } elseif ($SecondarySettingsReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeEstimateSaves -eq 1 -and $receipt.NativeLedgerEntries -eq 0
    } elseif ($SecondaryFieldsReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeGoalAllocations -eq 1 -and $receipt.NativeAssumedPriceSaves -eq 1 -and $receipt.NativeOccurrenceOverrides -eq 1 -and $receipt.NativePartialPayments -eq 1 -and $receipt.NativeBillSaves -eq 1 -and $receipt.NativeLedgerEntries -eq 3 -and $receipt.NativeNewLedgerEntries -eq 2
    } elseif ($SecondaryUnitsReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeUnitSaves -eq 1 -and $receipt.NativeLedgerEntries -eq 0
    } elseif ($HoldingMoneyReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeHoldingSaves -eq 2 -and $receipt.NativeManualPriceSaves -eq 1 -and $receipt.NativeLedgerEntries -eq 2
    } elseif ($AccountCurrencyOnly) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeAccountSaves -eq 0 -and $receipt.NativeAdjustments -eq 0
    } elseif ($AccountDefaultOnly) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeAccountSaves -eq 2 -and $receipt.NativeAdjustments -eq 0
    } elseif ($AccountMoneyReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeAccountSaves -eq 1 -and $receipt.NativeAdjustments -eq 1
    } elseif ($EntryMoneyReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeTransactionSaves -eq 2 -and $receipt.NativeSplitSaves -eq 1
    } elseif ($GoalBudgetMoneyReview) {
        $receipt.OriginalBaseStoreUnchanged -and $receipt.NoAcceptedCommandReplayed -and $receipt.NativeGoalSaves -eq 1 -and
            $receipt.NativeBudgetSaves -eq 1 -and $receipt.NativeLedgerPosts -eq 0
    } elseif ($PlanEditorReview -or $EntryWorkflowReview -or $EntryFormReview -or $EntryDetailReview -or $EntrySaveReview -or $HoldingEventPublicationReview -or $HoldingLocationReview -or $HoldingTypeReview -or $HoldingArchiveReview -or $HoldingHistoryReview -or $HoldingPriceReview -or $HoldingQuantityInputReview -or $HoldingFeeInputReview -or $HoldingRefundReview -or $HoldingFinancialReview -or $SplitFinancialReview -or $ImportFinancialReview -or $OccurrenceRefreshReview -or $OccurrenceStateReview -or $OccurrenceCommitReview -or $SettlementCommitReview -or $AccountTabRefreshReview -or $ReconciliationReview) {
        $receipt.OnlySeparateFictitiousLedgerChanged -and $receipt.OriginalBaseStoreUnchanged
    } else { $receipt.FinancialRowsUnchangedAfterSeeding }
    if ($receipt.Package -ne $identity -or $receipt.ProcessId -ne $process.Id -or
        !$financialInputsQualified -or $receipt.Captures.Count -ne $expectedCount) {
        throw 'Draft completion does not match the original run.'
    }
    foreach ($capture in $receipt.Captures) {
        $image = if ($OriginalAmountErrorReview) { Join-Path $output $capture.File } elseif ($GoalDetailReview) { Join-Path (Join-Path $output $capture.Stage) 'detail.png' } elseif ($GoalListReview) { Join-Path (Join-Path $output $capture.Stage) 'goals.png' } else { Join-Path (Join-Path $output $capture.Language) $capture.File }
        if (!(Test-Path -LiteralPath $image) -or (Get-Item -LiteralPath $image).Length -lt 1024) {
            throw 'An original native capture is missing or incomplete.'
        }
    }
    [ordered]@{ InputDesktopBefore=$inputBefore; InputDesktopAfter=[Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName(); ReviewDesktop=$lease.DesktopName } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'desktop-isolation.json') -Encoding UTF8
    "$expectedCount native Windows review contexts in $output. Review every image before updating tracked drafts."
}
finally {
    [Environment]::SetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_SOURCE_RUN', $workflowSourceBefore, 'Process')
    [Environment]::SetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_AGGREGATE_ONLY', $workflowAggregateBefore, 'Process')
    [Environment]::SetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_CAPTIONS_ONLY', $workflowCaptionsBefore, 'Process')
    [Environment]::SetEnvironmentVariable('VAFADAR_ENTRY_WORKFLOW_BLOCKED_ONLY', $workflowBlockedBefore, 'Process')
    if ($lease) { $lease.Dispose() }
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
