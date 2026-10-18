<#
.SYNOPSIS Renders six-language store drafts in a fresh independent Windows Debug installation.
.DESCRIPTION Never moves, resets or reads the main installation's data. A timeout retains the original live process.
#>
[CmdletBinding()]
param(
    [ValidateRange(30, 1800)][int]$TimeoutSeconds = 600,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$RunId,
    [switch]$UseExistingBuild,
    [switch]$ReportRowsReview,
    [switch]$BudgetTitlesReview,
    [switch]$HomeLegendsReview,
    [ValidateSet('en,fa,de', 'fa,de')][string]$HomeLegendsLanguages = 'en,fa,de')
$ErrorActionPreference = 'Stop'
$reviewCount = @($ReportRowsReview.IsPresent, $BudgetTitlesReview.IsPresent, $HomeLegendsReview.IsPresent | Where-Object { $_ }).Count
if ($reviewCount -gt 1) { throw 'Choose one original review matrix.' }
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
if (!$UseExistingBuild) {
    dotnet build $project -f $framework -p:ContinuousIntegrationBuild=true "-p:ApplicationId=$identity" -nologo *> $log
    if ($LASTEXITCODE -ne 0) {
        Get-Content -LiteralPath $log -Tail 20 | Write-Host
        throw "Independent store draft build failed; retain $output."
    }
}
if (Select-String -LiteralPath $log -Pattern ': warning ' -Quiet) { throw 'Resolve draft build warnings first.' }
$directory = Join-Path $root ($project + '/bin/Debug/' + $framework + '/win-x64')
$manifest = Join-Path $root ($project + '/obj/Debug/' + $framework + '/win-x64/resizetizer/m/Package.appxmanifest')
if (!(Test-Path -LiteralPath $manifest)) { throw 'Independent compiled manifest is missing.' }
[xml]$metadata = Get-Content -LiteralPath $manifest -Raw
if ($metadata.Package.Identity.Name -ne $identity) { throw 'Compiled draft identity does not match the requested run.' }
$variables = @('VAFADAR_SNAPSHOTS','VAFADAR_SNAPSHOT_ONLY','VAFADAR_STORE_DRAFT_RUN',
    'VAFADAR_WINDOW_SIZE','VAFADAR_SNAPSHOT_FONT_SCALE','VAFADAR_START_ROUTE','VAFADAR_STORE_DRAFT_REVIEW','VAFADAR_HOME_LEGEND_LANGUAGES')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $env:VAFADAR_SNAPSHOTS = $output
    $env:VAFADAR_SNAPSHOT_ONLY = 'store-draft'
    $env:VAFADAR_STORE_DRAFT_RUN = $run
    $env:VAFADAR_WINDOW_SIZE = '412x800'
    $env:VAFADAR_SNAPSHOT_FONT_SCALE = if ($ReportRowsReview -or $BudgetTitlesReview -or $HomeLegendsReview) { '2' } else { '' }
    $env:VAFADAR_STORE_DRAFT_REVIEW = if ($ReportRowsReview) { 'report-rows' } elseif ($BudgetTitlesReview) { 'budget-titles' } elseif ($HomeLegendsReview) { 'home-legends' } else { '' }
    $env:VAFADAR_HOME_LEGEND_LANGUAGES = if ($HomeLegendsReview) { $HomeLegendsLanguages } else { '' }
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
    $expectedCount = if ($HomeLegendsReview) { $HomeLegendsLanguages.Split(',').Count * 6 } elseif ($ReportRowsReview -or $BudgetTitlesReview) { 18 } else { 48 }
    if ($receipt.Package -ne $identity -or $receipt.ProcessId -ne $process.Id -or
        !$receipt.FinancialRowsUnchangedAfterSeeding -or $receipt.Captures.Count -ne $expectedCount) {
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
