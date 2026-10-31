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
if ($policy.toolsVersion -notmatch '^\d+\.\d+\.\d+$' -or $policy.sdkVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw 'Only the existing pinned tool and SDK paths may be observed.'
}
$compiler = Join-Path $records[0].installationPath ('VC/Tools/MSVC/' + $policy.toolsVersion + '/bin/Hostx64/x64/cl.exe')
$sdk = Join-Path ${env:ProgramFiles(x86)} ('Windows Kits/10/Include/' + $policy.sdkVersion)
$proof = [ordered]@{
    InstallationVersion = $records[0].installationVersion
    ToolsVersion = $policy.toolsVersion
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
# These are public executable metadata only. No environment, credential, user path or source data is exported.
$proof | ConvertTo-Json
