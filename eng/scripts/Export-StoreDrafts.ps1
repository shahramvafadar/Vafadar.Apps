<#
.SYNOPSIS Encodes a completed isolated native capture as lossless opaque RGB store drafts.
#>
[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$RunId, [switch]$RepairEnglishBudget)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = Join-Path $root ('artifacts/store-drafts/' + $RunId)
$receipt = Get-Content -LiteralPath (Join-Path $source 'complete.json') -Raw | ConvertFrom-Json
$isolation = Get-Content -LiteralPath (Join-Path $source 'isolation.json') -Raw | ConvertFrom-Json
if ($receipt.Package -ne ('pro.vafadar.zanance.storepreview.' + $RunId.Substring(0, 12)) -or
    $receipt.Package -ne $isolation.Package -or $receipt.ProcessId -ne $isolation.ProcessId -or
    !$receipt.NativeWindowsDraftOnly -or !$receipt.FinancialRowsUnchangedAfterSeeding -or !$isolation.EmptyBeforeStartup -or
    $isolation.Desktop -ne ('ZananceStore_' + $RunId) -or $receipt.Captures.Count -ne 48) { throw 'Completed original isolated capture required.' }
if (Test-Path -LiteralPath (Join-Path $source 'error.txt')) { throw 'Refuse a capture with observed failures.' }
$drawing = [System.Drawing.Bitmap].Assembly.Location
$references = @($drawing, [System.Drawing.Rectangle].Assembly.Location, [System.Security.Cryptography.SHA256].Assembly.Location) + @(
    Get-ChildItem -LiteralPath (Split-Path $drawing) -Filter 'System.Private.Windows*.dll' | Select-Object -ExpandProperty FullName)
Add-Type -Path (Join-Path $PSScriptRoot '../modules/StoreDraftPng.cs') -ReferencedAssemblies @($references | Select-Object -Unique)
$output = Join-Path $source 'rgb24'
if ($RepairEnglishBudget) {
    $repair = Get-Content -LiteralPath (Join-Path $source 'repair-en-budget/complete.json') -Raw | ConvertFrom-Json
    $process = Get-Content -LiteralPath (Join-Path $source 'repair-process.json') -Raw | ConvertFrom-Json
    if ($repair.Package -ne $receipt.Package -or $repair.ProcessId -ne $process.ProcessId -or
        !$repair.FinancialRowsUnchanged -or $repair.Language -ne 'en' -or $repair.File -ne '05-budget.png') { throw 'Verified original repair required.' }
    $manifestPath = Join-Path $output 'manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.SourceRun -ne $RunId -or $manifest.Captures.Count -ne 48) { throw 'Original encoded manifest required.' }
    $row = @($manifest.Captures | Where-Object { $_.Language -eq 'en' -and $_.File -eq '05-budget.png' })
    if ($row.Count -ne 1 -or (Test-Path -LiteralPath (Join-Path $output 'manifest-before-repair.json'))) { throw 'Preserve previous repair evidence.' }
    $original = Join-Path $source 'repair-en-budget/05-budget.png'
    $destination = Join-Path $output 'repaired-en-budget.png'
    $encoded = [Vafadar.Build.StoreDraftPng]::Encode($original,$destination)
    if ([int]$encoded[0] -ne $row[0].Width -or [int]$encoded[1] -ne $row[0].Height) { throw 'Repair dimensions changed.' }
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $output 'manifest-before-repair.json')
    Move-Item -LiteralPath (Join-Path $output 'en/05-budget.png') -Destination (Join-Path $output 'rejected-en-budget-loading.png')
    Move-Item -LiteralPath $destination -Destination (Join-Path $output 'en/05-budget.png')
    $row[0].RgbSha256 = $encoded[2]
    $row[0].SourceSha256 = (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash.ToLowerInvariant()
    $row[0].Sha256 = (Get-FileHash -LiteralPath (Join-Path $output 'en/05-budget.png') -Algorithm SHA256).Hash.ToLowerInvariant()
    $row[0] | Add-Member -NotePropertyName SourceOverride -NotePropertyValue 'repair-en-budget/05-budget.png'
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
    'PASS: one repaired English budget encoded; previous image and manifest retained.'
    return
}
if (Test-Path -LiteralPath $output) { throw 'Preserve the existing encoded evidence.' }
New-Item -ItemType Directory -Path $output | Out-Null
$files = @('01-home.png','02-transactions.png','03-new-entry.png','04-plans.png','05-budget.png','06-reports.png','07-accounts.png','08-home-dark.png')
$rows = @()
foreach ($language in @('en','fa','de','es','fr','it')) {
    $selected = @($receipt.Captures | Where-Object Language -eq $language)
    if ($selected.Count -ne 8 -or (@($selected.File | Sort-Object -Unique)).Count -ne 8) { throw 'Incomplete original language capture.' }
    New-Item -ItemType Directory -Path (Join-Path $output $language) | Out-Null
    foreach ($file in $files) {
        if ($selected.File -notcontains $file) { throw 'Unexpected original file mapping.' }
        $original = Join-Path (Join-Path $source $language) $file
        $destination = Join-Path (Join-Path $output $language) $file
        $encoded = [Vafadar.Build.StoreDraftPng]::Encode($original, $destination)
        $bytes = [IO.File]::ReadAllBytes($destination)
        if ($bytes[24] -ne 8 -or $bytes[25] -ne 2) { throw 'Export is not an 8-bit truecolor RGB PNG.' }
        $width = [int]$encoded[0]; $height = [int]$encoded[1]
        if ([math]::Min($width,$height) -lt 320 -or [math]::Max($width,$height) -gt 3840 -or
            [math]::Max($width,$height) -gt 2*[math]::Min($width,$height)) { throw 'Draft dimensions do not meet the basic screenshot envelope.' }
        $rows += [ordered]@{ Language=$language; File=$file; Width=$width; Height=$height; RgbSha256=$encoded[2]; SourceSha256=(Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash.ToLowerInvariant(); Sha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
}
[ordered]@{ Origin='Windows development render - not Android/iOS device acceptance'; SourceRun=$RunId; ReviewPackage=$receipt.Package;
    ReviewDesktop=$isolation.Desktop; Encoding='24-bit RGB PNG, no alpha'; ExactOriginalRgbPixels=$true; Resized=$false; Captures=$rows } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'manifest.json') -Encoding UTF8
"48 exact opaque RGB PNG drafts in $output. Review before updating the repository; no store upload is performed."
