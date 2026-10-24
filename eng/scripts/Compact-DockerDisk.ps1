<#
.SYNOPSIS
Compacts an existing Docker Desktop data disk during an explicitly approved outage.
.DESCRIPTION
Run manually in an elevated Windows PowerShell session. Records container identities and
restarts only the containers that were running. Never deletes images, volumes or a disk.
The owner approved the cross-project outage on 2026-10-24 (D-505).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DiskPath,
    [Parameter(Mandatory = $true)]
    [switch]$ApprovedSharedOutage
)
$ErrorActionPreference = 'Stop'
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script manually as Administrator; no services have been stopped.'
}
if (-not $ApprovedSharedOutage) { throw 'Explicit shared-service outage approval is required.' }
$disk = Get-Item -LiteralPath $DiskPath
if ($disk.Name -ne 'docker_data.vhdx' -or ($disk.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw 'Expected the original Docker data VHDX, not a link or another virtual disk.'
}
# Confirm this is the configured data root rather than accepting an arbitrary VHDX path.
$settingsPath = Join-Path $env:APPDATA 'Docker\settings-store.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$expected = [IO.Path]::GetFullPath((Join-Path $settings.CustomWslDistroDir 'disk\docker_data.vhdx'))
if ($disk.FullName -ne $expected) { throw 'Disk differs from the configured Docker data disk.' }
function Invoke-Docker {
    param([string[]]$DockerArguments)
    $output = & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw ('Docker operation failed: ' + $DockerArguments[0]) }
    return $output
}
$running = @(Invoke-Docker -DockerArguments @('ps', '-q', '--no-trunc'))
$allIds = @(Invoke-Docker -DockerArguments @('ps', '-aq', '--no-trunc'))
foreach ($id in $running) {
    $autoRemove = Invoke-Docker -DockerArguments @('inspect', $id, '--format', '{{.HostConfig.AutoRemove}}')
    if ($autoRemove -ne 'false') { throw 'A running disposable container cannot be restored; outage refused.' }
}
$receiptRoot = Join-Path $env:LOCALAPPDATA ('Temp\Zanance-DockerCompact-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $receiptRoot | Out-Null
$beforeBytes = $disk.Length
@{ Disk = $expected; Bytes = $beforeBytes; Running = $running; All = $allIds } |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $receiptRoot 'before.json') -Encoding UTF8
$diskpartScript = Join-Path $receiptRoot 'compact.txt'
@(
    ('select vdisk file="' + $expected + '"')
    'compact vdisk'
    'exit'
) |
    Set-Content -LiteralPath $diskpartScript -Encoding ASCII
$outageStarted = $false
try {
    $outageStarted = $true
    # Graceful shutdown flushes application/database data before detaching Docker's disk.
    if ($running.Count) { Invoke-Docker -DockerArguments (@('stop', '--time', '60') + $running) | Out-Null }
    Invoke-Docker -DockerArguments @('desktop', 'stop', '--timeout', '120') | Out-Null
    $runningDistros = @(& wsl.exe --list --running --quiet) -replace "`0", ''
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect WSL attachment; disk remains untouched.' }
    if ($runningDistros.Trim() -contains 'docker-desktop') {
        & wsl.exe --terminate docker-desktop
        if ($LASTEXITCODE -ne 0) { throw 'Docker WSL shutdown was not confirmed; disk remains untouched.' }
    }
    # An exclusive read confirms detachment. Never force-detach or terminate other WSL distros.
    $handle = [IO.File]::Open($expected, 'Open', 'Read', 'None')
    $handle.Dispose()
    & "$env:SystemRoot\System32\diskpart.exe" /s $diskpartScript |
        Set-Content -LiteralPath (Join-Path $receiptRoot 'diskpart.log') -Encoding UTF8
    if ($LASTEXITCODE -ne 0) { throw 'DiskPart failed; retain its diagnostic log.' }
}
finally {
    if ($outageStarted) {
        Invoke-Docker -DockerArguments @('desktop', 'start', '--timeout', '120') | Out-Null
        if ($running.Count) { Invoke-Docker -DockerArguments (@('start') + $running) | Out-Null }
    }
}
$afterIds = @(Invoke-Docker -DockerArguments @('ps', '-aq', '--no-trunc'))
$afterRunning = @(Invoke-Docker -DockerArguments @('ps', '-q', '--no-trunc'))
$afterBytes = (Get-Item -LiteralPath $expected).Length
$result = @{
    BeforeBytes = $beforeBytes; AfterBytes = $afterBytes; ReducedBytes = $beforeBytes - $afterBytes
    IdentitiesPreserved = -not [bool](Compare-Object $allIds $afterIds)
    RunningSetPreserved = -not [bool](Compare-Object $running $afterRunning)
    ReceiptDirectory = $receiptRoot
}
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $receiptRoot 'after.json') -Encoding UTF8
$result | ConvertTo-Json
if (-not $result.IdentitiesPreserved -or -not $result.RunningSetPreserved) {
    throw 'Container identity/running-set restoration differs; inspect the retained receipt.'
}
if ($afterBytes -ge $beforeBytes) {
    Write-Warning 'No VHDX size reduction was measured. DiskPart exit alone is not proof of reclaimed space.'
}
# A matching running set is not application health acceptance; inspect original health checks afterward.
