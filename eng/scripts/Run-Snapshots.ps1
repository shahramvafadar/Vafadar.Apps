<#
.SYNOPSIS
    Builds the Windows app in Debug and walks through every screen, saving screenshots (and the report PDF).

.DESCRIPTION
    The Debug-only walk-through (src/Apps/Zanance/Vafadar.Zanance.App/Diagnostics/DebugSnapshots.cs) starts when
    VAFADAR_SNAPSHOTS points to a folder. It starts with an empty development database (the existing one is moved to a
    Data-before-snapshots-* folder next to it, never deleted), seeds fictitious data, sets Advanced mode
    and captures each route per language, plus "-end" shots of scrolled pages. Look at the Persian (right-to-left) and
    the dark variants after every UI change.

.EXAMPLE
    ./eng/scripts/Run-Snapshots.ps1 -Languages fa -Theme dark
    ./eng/scripts/Run-Snapshots.ps1 -Languages en -WindowSize 1280x820
#>
param(
    [string]$Languages = 'fa',
    [ValidateSet('light', 'dark')] [string]$Theme = 'light',
    [string]$Output = (Join-Path $PSScriptRoot '..\..\artifacts\snapshots'),
    [int]$TimeoutSeconds = 240,
    # e.g. 1280x820 to check wide windows (desktop, tablet); default: a phone-sized window.
    [string]$WindowSize = '',
    # The empty states of a new user (one account, nothing recorded) instead of the screens with sample data.
    [switch]$Empty)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $root

# Only the development build of this repository is stopped. Its database is moved aside, never deleted: the walk-through
# needs an empty start, but someone may have used the development build with their own data.
Get-Process Vafadar.Zanance.App -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$root\*" } | Stop-Process -Force
Start-Sleep 1
$data = Join-Path $env:LOCALAPPDATA 'Shahram Vafadar\pro.vafadar.zanance\Data'
if (Test-Path $data) {
    $files = Get-ChildItem $data -Filter '*.db*' | Where-Object Name -match '^(zanance|finance)(-[0-9a-f]{32})?\.db'
    if ($files) {
        $kept = Join-Path (Split-Path $data) "Data-before-snapshots-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        New-Item -ItemType Directory -Force $kept | Out-Null
        $files | Move-Item -Destination $kept
        "The development data was moved to $kept"
    }
}
New-Item -ItemType Directory -Force $Output | Out-Null
Get-ChildItem $Output -File | ForEach-Object { [IO.File]::Delete($_.FullName) }

dotnet build src\Apps\Zanance\Vafadar.Zanance.App -f net10.0-windows10.0.19041.0 -v q -nologo 2>&1 | Select-String ' error |warning CS' | Select-Object -Unique -First 15
$exe = Get-ChildItem 'src\Apps\Zanance\Vafadar.Zanance.App\bin\Debug\net10.0-windows10.0.19041.0' -Recurse -Filter 'Vafadar.Zanance.App.exe' | Select-Object -First 1
$env:VAFADAR_SNAPSHOTS = (Resolve-Path $Output)
$env:VAFADAR_SNAPSHOT_LANGUAGES = $Languages
$env:VAFADAR_SNAPSHOT_THEME = $Theme
$env:VAFADAR_WINDOW_SIZE = $WindowSize
$env:VAFADAR_SNAPSHOT_EMPTY = if ($Empty) { '1' } else { '' }
$process = Start-Process $exe.FullName -PassThru
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { Stop-Process -Id $process.Id -Force; 'timed out (the shots taken so far are kept)' }
if (Test-Path (Join-Path $Output 'error.txt')) { Get-Content (Join-Path $Output 'error.txt') -TotalCount 5 }
"$((Get-ChildItem $Output -Filter *.png).Count) screenshots in $Output"