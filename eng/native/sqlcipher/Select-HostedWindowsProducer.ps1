<# .SYNOPSIS Selects an exact hosted native producer only after actual signed compiler qualification. #>
[CmdletBinding()]
param([string]$EnvironmentFile = '')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'WindowsToolchain.psm1') -Force
$actual = Get-ReviewedWindowsToolchain
$variant = Resolve-ReviewedWindowsProducer -InstallationVersion $actual.Installation.installationVersion -CompilerSha256 $actual.CompilerSha256
$env:VafadarWindowsCipherVariant = $variant
if ($EnvironmentFile) {
    if (-not [IO.Path]::IsPathRooted($EnvironmentFile) -or -not (Test-Path -LiteralPath $EnvironmentFile -PathType Leaf)) {
        throw 'An existing absolute environment file is required.'
    }
    Add-Content -LiteralPath $EnvironmentFile -Value ('VafadarWindowsCipherVariant=' + $variant) -Encoding UTF8
}
Write-Output ('Qualified hosted Windows producer: ' + $variant)
