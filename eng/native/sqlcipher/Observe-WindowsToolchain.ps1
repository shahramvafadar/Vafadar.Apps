<#
.SYNOPSIS Records public installed compiler evidence without executing or admitting that compiler.
.DESCRIPTION Keeps hosted-image drift diagnosable when the independent reviewed toolchain gate refuses it.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$records = @(& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json | ConvertFrom-Json)
if ($LASTEXITCODE -ne 0 -or $records.Count -ne 1 -or -not [IO.Path]::IsPathRooted($records[0].installationPath)) {
    throw 'A unique absolute compiler installation is required for observation.'
}
$policy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.compiler.acceptance.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$reviewedTools = @($policy.compilers.toolsVersion | Select-Object -Unique | Sort-Object { [Version]$_ } -Descending)
if ($policy.schemaVersion -ne 3 -or -not $reviewedTools.Count -or @($reviewedTools | Where-Object { $_ -notmatch '^\d+\.\d+\.\d+$' }).Count -or
    $policy.sdkVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw 'Only the existing pinned tool and SDK paths may be observed.'
}
# The record describes the newest reviewed tool set that is installed; without one, the newest reviewed one as absent.
$observedTools = @(@($reviewedTools | Where-Object { Test-Path -LiteralPath (Join-Path $records[0].installationPath ('VC/Tools/MSVC/' + $_ + '/bin/Hostx64/x64/cl.exe')) -PathType Leaf }) + $reviewedTools)[0]
$compiler = Join-Path $records[0].installationPath ('VC/Tools/MSVC/' + $observedTools + '/bin/Hostx64/x64/cl.exe')
$sdk = Join-Path ${env:ProgramFiles(x86)} ('Windows Kits/10/Include/' + $policy.sdkVersion)
$proof = [ordered]@{
    InstallationVersion = $records[0].installationVersion
    ToolsVersion = $observedTools
    SdkVersion = $policy.sdkVersion
    CompilerPresent = (Test-Path -LiteralPath $compiler -PathType Leaf)
    SdkPresent = (Test-Path -LiteralPath $sdk -PathType Container)
    CompilerExecuted = $false
    ReviewedAdmissionGranted = $false
}
if ($proof.CompilerPresent) {
    $signature = Get-AuthenticodeSignature -LiteralPath $compiler
    $proof.CompilerSha256 = (Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant()
    $proof.CompilerFileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($compiler).FileVersion
    $proof.SignatureStatus = $signature.Status.ToString()
    $proof.SignerSubject = if ($signature.SignerCertificate) { $signature.SignerCertificate.Subject } else { '' }
}
# Every installed MSVC tool set, by its public folder name and signed executable metadata. A hosted image
# that no longer carries the pinned tool set is then diagnosable from this record alone; nothing here
# executes or admits a compiler.
$toolsRoot = Join-Path $records[0].installationPath 'VC/Tools/MSVC'
$proof.InstalledTools = @(if (Test-Path -LiteralPath $toolsRoot -PathType Container) {
    Get-ChildItem -LiteralPath $toolsRoot -Directory | Sort-Object Name | ForEach-Object {
        $candidate = Join-Path $_.FullName 'bin/Hostx64/x64/cl.exe'
        $entry = [ordered]@{ ToolsVersion = $_.Name; CompilerPresent = (Test-Path -LiteralPath $candidate -PathType Leaf) }
        if ($entry.CompilerPresent) {
            $candidateSignature = Get-AuthenticodeSignature -LiteralPath $candidate
            $entry.CompilerSha256 = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToLowerInvariant()
            $entry.CompilerFileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($candidate).FileVersion
            $entry.SignatureStatus = $candidateSignature.Status.ToString()
            $entry.SignerSubject = if ($candidateSignature.SignerCertificate) { $candidateSignature.SignerCertificate.Subject } else { '' }
        }
        $entry
    }
})
$sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/Include'
$proof.InstalledSdks = @(if (Test-Path -LiteralPath $sdkRoot -PathType Container) { (Get-ChildItem -LiteralPath $sdkRoot -Directory | Sort-Object Name).Name })
# These are public executable metadata only. No environment, credential, user path or source data is exported.
# On a hosted runner the same record is also raised as a notice of the job, so that hosted-image drift
# can be read from the public check of a commit. It goes to the host, not to the output a caller parses.
if ($env:GITHUB_ACTIONS -eq 'true') {
    Write-Host ('::notice title=Windows toolchain observation::' + ($proof | ConvertTo-Json -Depth 5 -Compress))
}
$proof | ConvertTo-Json -Depth 5
