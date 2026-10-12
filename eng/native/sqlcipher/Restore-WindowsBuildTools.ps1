<#
.SYNOPSIS
    Restores the checksum-pinned portable native Windows Perl used by the SQLCipher build.
.DESCRIPTION
    Uses official release metadata pinned in inputs.lock.json. No installer, registry, global PATH,
    account or system configuration is changed. Restored inputs stay inside this repository's artifacts.
    MSYS Perl does not match OpenSSL's documented VC build environment.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($inputLock.schemaVersion -ne 1) { throw 'Unsupported native input schema.' }
$drive = Get-PSDrive -Name ([IO.Path]::GetPathRoot($repoRoot).Substring(0,1))
if ($drive.Free -lt 3GB) { throw 'Native input restoration requires at least 3 GB free.' }
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$tool = $inputLock.windowsPerl
$destination = Restore-NativeZipTool -Tool $tool -Executable 'perl/bin/perl.exe'
$executable = Join-Path $destination 'perl/bin/perl.exe'
$previousNativeLang = $env:LANG
$previousNativeLocale = $env:LC_ALL
try {
    # Native Windows Perl does not accept the MSYS C.UTF-8 locale. Scope C to this build process.
    $env:LANG = 'C'
    $env:LC_ALL = 'C'
    $identity = & $executable -e 'print "$^O $^V"'
    $identityExit = $LASTEXITCODE
}
finally {
    $env:LANG = $previousNativeLang
    $env:LC_ALL = $previousNativeLocale
}
if ($identityExit -ne 0 -or $identity -ne ('MSWin32 v' + ($tool.version -replace '\.\d+$',''))) {
    throw 'The restored interpreter does not match the locked native Windows Perl version.'
}
Write-Output $destination
