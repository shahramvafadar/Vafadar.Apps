<# .SYNOPSIS Selects an exact hosted native producer only after actual signed compiler qualification. #>
[CmdletBinding()]
param([string]$EnvironmentFile = '',
    # Producer qualification only: the candidate version of the explicit request for a compiler that has no
    # immutable consumer identity yet. The qualified host must be exactly the requested one.
    [string]$ProspectiveCandidateVersion = '')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'WindowsToolchain.psm1') -Force
$actual = Get-ReviewedWindowsToolchain
try { $variant = Resolve-ReviewedWindowsProducer -InstallationVersion $actual.Installation.installationVersion -CompilerSha256 $actual.CompilerSha256 }
catch {
    $request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.producer.request.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($_.Exception.Message -cne 'Unsupported Windows producer host.' -or -not $ProspectiveCandidateVersion -or
        -not (Test-RequestedWindowsProducerCompiler -Toolchain $actual -Request $request -CandidateVersion $ProspectiveCandidateVersion)) { throw }
    # No consumer variant is selected or persisted: the export is packed for the request alone and pins nothing.
    Write-Output ('Qualified requested Windows producer without a consumer identity: ' + $ProspectiveCandidateVersion)
    return
}
$env:VafadarWindowsCipherVariant = $variant
if ($EnvironmentFile) {
    if (-not [IO.Path]::IsPathRooted($EnvironmentFile) -or -not (Test-Path -LiteralPath $EnvironmentFile -PathType Leaf)) {
        throw 'An existing absolute environment file is required.'
    }
    Add-Content -LiteralPath $EnvironmentFile -Value ('VafadarWindowsCipherVariant=' + $variant) -Encoding UTF8
}
Write-Output ('Qualified hosted Windows producer: ' + $variant)
