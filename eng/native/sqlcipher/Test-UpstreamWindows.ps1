<#
.SYNOPSIS AT-181: Runs the official OpenSSL test suite in an existing independent Windows candidate tree.
.DESCRIPTION Preserves the candidate configuration and output hash. Never opens application data or changes machine settings.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'WindowsToolchain.psm1') -Force
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Upstream requires tests under an unprivileged account; do not elevate this process.'
}
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$toolchain = Get-ReviewedWindowsToolchain
$installation = $toolchain.Installation
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
if ($manifest.inputLockSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json')).Hash.ToLowerInvariant() -or
    $manifest.librarySha256 -ne (Get-FileHash -LiteralPath $library).Hash.ToLowerInvariant()) {
    throw 'Upstream test input differs from the candidate lock or native manifest.'
}
$stamp = [Guid]::NewGuid().ToString('N')
$command = Join-Path $root ('upstream-' + $stamp + '.cmd')
$log = Join-Path $root ('upstream-' + $stamp + '.log')
$vcvars = $toolchain.VcVars
$perlRoot = & (Join-Path $PSScriptRoot 'Restore-WindowsBuildTools.ps1')
$nasmRoot = Restore-NativeZipTool -Tool $inputLock.windowsNasm -Executable $inputLock.windowsNasm.executable
$nasmDirectory = Split-Path (Join-Path $nasmRoot $inputLock.windowsNasm.executable)
$cmd = @"
@echo off
set LANG=C
set LC_ALL=C
set SOURCE_DATE_EPOCH=$($manifest.sourceDateEpoch)
set "PATH=$nasmDirectory;$perlRoot\perl\bin;%PATH%"
call "$vcvars" x64 $($inputLock.windowsCompiler.sdkVersion) -vcvars_ver=$($inputLock.windowsCompiler.toolsVersion)
if errorlevel 1 exit /b 1
cd /d "$root\openssl-build"
nmake /NOLOGO test VFP=1
exit /b %errorlevel%
"@
[IO.File]::WriteAllText($command,$cmd,[Text.Encoding]::ASCII)
& cmd.exe /d /c $command *> $log
$exitCode = $LASTEXITCODE
if ((Get-FileHash -LiteralPath $library).Hash.ToLowerInvariant() -ne $manifest.librarySha256) {
    throw 'Upstream tests changed the candidate native library.'
}
[ordered]@{ suite='official-openssl'; exitCode=$exitCode; log=$log; nativeSha256=$manifest.librarySha256;
    candidateUnchanged=$true; appAndMobileNotAccepted=$true } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root ('upstream-' + $stamp + '.json')) -Encoding UTF8
if ($exitCode -ne 0) { throw ('Upstream suite failed; retain negative evidence: ' + $log) }
Write-Output $log
