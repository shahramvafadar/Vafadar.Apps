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
if ($HomeRefreshReferencesOnly -and !$HomeRefreshReview) { throw 'Home reference qualification requires its original review mode.' }
if ($ReconciliationContinuation -and !$ReconciliationReview) { throw 'Reconciliation continuation requires the original reconciliation review.' }
if ($ReconciliationVisualOnly -and (!$ReconciliationReview -or !$ReconciliationContinuation)) { throw 'Visual reconciliation continuation requires the exact original financial fixture.' }
if ($AccountTabRefreshContinuation -and !$AccountTabRefreshReview) { throw 'Account continuation requires the original account review.' }
if ($EditorAggregateCompleted -and (!$EditorRetainedAggregateOnly -or @($EditorAggregateCompleted.Split(',') | Select-Object -Unique).Count -ne $EditorAggregateCompleted.Split(',').Count)) { throw 'Completed aggregate cases require a unique original review subset.' }
if ($EditorRetainedAggregateOnly -and !$EditorTextReview) { throw 'Retained aggregate review requires the original editor review.' }
$reviewCount = @($HomeRefreshReview.IsPresent, $SettlementCommitReview.IsPresent, $ReconciliationReview.IsPresent, $PlansAddHeaderReview.IsPresent, $AccountTabRefreshReview.IsPresent, $EcbReferenceReview.IsPresent, $ReportRowsReview.IsPresent, $BudgetTitlesReview.IsPresent, $HomeLegendsReview.IsPresent, $PlanRowsReview.IsPresent, $LedgerRowsReview.IsPresent, $ReimbursementsReview.IsPresent, $HomeTemplatesReview.IsPresent, $HomeReviewCountReview.IsPresent, $HomeAddHeaderReview.IsPresent, $GettingStartedReview.IsPresent, $CsvInputReview.IsPresent, $EditorTextReview.IsPresent | Where-Object { $_ }).Count
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
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $root
if ($UseExistingBuild -and !$RunId) { throw 'An existing build requires its exact original run id.' }
$run = if ($RunId) { $RunId } else { [Guid]::NewGuid().ToString('N') }
$identity = 'pro.vafadar.zanance.storepreview.' + $run.Substring(0, 12)
$output = Join-Path $root ('artifacts/store-drafts/' + $run)
if ((Test-Path -LiteralPath $output) -and (!$UseExistingBuild -or (Test-Path -LiteralPath (Join-Path $output 'process.json')))) {
    throw 'Preserve the existing draft run/process; never restart it after observation expiry.'
}
New-Item -ItemType Directory -Path $output -Force | Out-Null
$project = 'src/Apps/Zanance/Vafadar.Zanance.App'
$framework = 'net10.0-windows10.0.19041.0'
$log = Join-Path $output 'build.log'
$buildRoot = Join-Path $output 'build'
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
$variables = @('VAFADAR_RECONCILIATION_VISUAL_ONLY', 'VAFADAR_RECONCILIATION_CONTINUATION', 'VAFADAR_ACCOUNT_TAB_CONTINUATION','VAFADAR_RETAINED_AGGREGATE_COMPLETED','VAFADAR_EDITOR_RETAINED_AGGREGATE_ONLY','VAFADAR_EDITOR_ONBOARDING_CONTEXTS','VAFADAR_EDITOR_ONBOARDING_FOOTER_ONLY','VAFADAR_EDITOR_NARROW_ACTIONS_ONLY','VAFADAR_EDITOR_GOAL_CHOICES_ONLY','VAFADAR_EDITOR_DEBT_DETAILS_ONLY','VAFADAR_SNAPSHOTS','VAFADAR_SNAPSHOT_ONLY','VAFADAR_STORE_DRAFT_RUN',
    'VAFADAR_WINDOW_SIZE','VAFADAR_SNAPSHOT_FONT_SCALE','VAFADAR_START_ROUTE','VAFADAR_STORE_DRAFT_REVIEW','VAFADAR_HOME_LEGEND_LANGUAGES','VAFADAR_PLAN_ROWS_EDGE_PROBE','VAFADAR_LEDGER_ROWS_LANGUAGES','VAFADAR_LEDGER_ROWS_CONTEXTS','VAFADAR_REIMBURSEMENTS_LANGUAGES','VAFADAR_REIMBURSEMENTS_VISIBILITY_ONLY','VAFADAR_REIMBURSEMENTS_EMPTY_ONLY')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
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
    $env:VAFADAR_STORE_DRAFT_REVIEW = if ($HomeRefreshReview) { if ($HomeRefreshReferencesOnly) { 'home-refresh-references' } else { 'home-refresh' } } elseif ($SettlementCommitReview) { 'settlement-commit' } elseif ($ReconciliationReview) { 'reconciliation' } elseif ($PlansAddHeaderReview) { 'plans-add-header' } elseif ($AccountTabRefreshReview) { 'account-tab-refresh' } elseif ($EcbReferenceReview) { 'ecb-reference' } elseif ($CsvInputReview -and $CsvQuotationOnly) { 'csv-quotation' } elseif ($CsvInputReview -and $CsvInputEmptyOnly) { 'csv-input-empty' } elseif ($CsvInputReview) { 'csv-input' } elseif ($ReportRowsReview) { 'report-rows' } elseif ($BudgetTitlesReview) { 'budget-titles' } elseif ($HomeLegendsReview) { 'home-legends' } elseif ($PlanRowsReview) { 'plan-rows' } elseif ($LedgerRowsReview) { 'ledger-rows' } elseif ($ReimbursementsReview) { 'reimbursements' } elseif ($HomeTemplatesReview) { 'home-templates' } elseif ($GettingStartedReview) { 'getting-started' } elseif ($HomeAddHeaderReview) { 'home-add-header' } elseif ($HomeReviewCountReview) { 'home-review-count' } elseif ($EditorTextReview) { 'editor-text' } else { '' }
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
    if (Test-Path -LiteralPath (Join-Path $output 'error.txt')) { throw "Draft capture failed; preserve $output/error.txt." }
    $receiptPath = Join-Path $output 'complete.json'
    if (!(Test-Path -LiteralPath $receiptPath)) { throw 'No completed original draft receipt.' }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $expectedCount = if ($HomeRefreshReferencesOnly) { 1 } elseif ($HomeRefreshReview) { 6 } elseif ($AccountTabRefreshReview) { 6 } elseif ($EditorRetainedAggregateOnly) { 36 - @($EditorAggregateCompleted.Split(',') | Where-Object { $_ }).Count } elseif ($EcbReferenceReview) { 54 } elseif ($SettlementCommitReview -or $ReconciliationReview -or $PlansAddHeaderReview -or $CsvInputReview -or $HomeReviewCountReview -or $HomeAddHeaderReview -or $GettingStartedReview) { 18 } elseif ($EditorOnboardingContexts) { $EditorOnboardingContexts.Split(',').Count } elseif ($EditorTextReview) { 18 } elseif ($HomeTemplatesReview) { 6 } elseif ($ReimbursementsReview) { $ReimbursementsLanguages.Split(',').Count * 6 } elseif ($LedgerRowsReview -and $LedgerRowsContexts) { $LedgerRowsContexts.Split(',').Count } elseif ($LedgerRowsReview) { $LedgerRowsLanguages.Split(',').Count * 6 } elseif (($ReimbursementsVisibilityOnly -or $ReimbursementsEmptyOnly) -and !$ReimbursementsReview) { throw 'Visible reimbursement checks require their original review mode.' }
if ($PlanRowsEdgeProbe) { 1 } elseif ($HomeLegendsReview) { $HomeLegendsLanguages.Split(',').Count * 6 } elseif ($ReportRowsReview -or $BudgetTitlesReview -or $PlanRowsReview -or $ReimbursementsReview) { 18 } else { 48 }
    $financialInputsQualified = if ($SettlementCommitReview -or $AccountTabRefreshReview -or $ReconciliationReview) {
        $receipt.OnlySeparateFictitiousLedgerChanged -and $receipt.OriginalBaseStoreUnchanged
    } else { $receipt.FinancialRowsUnchangedAfterSeeding }
    if ($receipt.Package -ne $identity -or $receipt.ProcessId -ne $process.Id -or
        !$financialInputsQualified -or $receipt.Captures.Count -ne $expectedCount) {
        throw 'Draft completion does not match the original run.'
    }
    foreach ($capture in $receipt.Captures) {
        $image = Join-Path (Join-Path $output $capture.Language) $capture.File
        if (!(Test-Path -LiteralPath $image) -or (Get-Item -LiteralPath $image).Length -lt 1024) {
            throw 'An original native capture is missing or incomplete.'
        }
    }
    [ordered]@{ InputDesktopBefore=$inputBefore; InputDesktopAfter=[Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName(); ReviewDesktop=$lease.DesktopName } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'desktop-isolation.json') -Encoding UTF8
    "$expectedCount native Windows review contexts in $output. Review every image before updating tracked drafts."
}
finally {
    if ($lease) { $lease.Dispose() }
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
