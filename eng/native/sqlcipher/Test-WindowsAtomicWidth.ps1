<# .SYNOPSIS Reviews upstream conditional-intrinsic warnings with exact source, pinned MSVC and real original object disassembly. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-d')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$lock = Get-Content (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$manifest = Get-Content (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
if ((Get-FileHash $library -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.librarySha256) { throw 'Candidate changed.' }
$source = Join-Path $root 'openssl/include/internal/tsan_assist.h'
$gitRoot = Join-Path (Get-NativeInputRoot) ('source-git/openssl-' + $lock.sources.openssl.commit)
$official = & git -C $gitRoot show ($lock.sources.openssl.commit + ':include/internal/tsan_assist.h')
if ($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText($source).Replace("`r`n","`n") -ne (($official -join "`n") + "`n")) { throw 'Atomic header differs from locked official source.' }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$installation = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json | ConvertFrom-Json
if ($installation.installationVersion -ne $lock.windowsCompiler.installationVersion) { throw 'Pinned compiler is unavailable.' }
$vcvars = Join-Path $installation.installationPath 'VC/Auxiliary/Build/vcvarsall.bat'
$evidence = Join-Path $root ('atomic-width-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $evidence | Out-Null
$probe = Join-Path $PSScriptRoot 'Windows/atomic-width.c'
$command = @"
@echo off
call "$vcvars" x64 $($lock.windowsCompiler.sdkVersion) -vcvars_ver=$($lock.windowsCompiler.toolsVersion)
if errorlevel 1 exit /b 1
cd /d "$evidence"
cl /nologo /O2 /W3 /MT /guard:cf /I"$root\openssl\include" "$probe" /Fe:atomic-width.exe /Fo:atomic-width.obj
if errorlevel 1 exit /b 1
atomic-width.exe
if errorlevel 1 exit /b 1
dumpbin /NOLOGO /DISASM atomic-width.obj > probe-disassembly.txt
if errorlevel 1 exit /b 1
dumpbin /NOLOGO /DISASM "$root\openssl-build\crypto\objects\libcrypto-lib-obj_dat.obj" > object-disassembly.txt
if errorlevel 1 exit /b 1
dumpbin /NOLOGO /DISASM "$root\openssl-build\crypto\property\libcrypto-lib-property.obj" > property-disassembly.txt
exit /b %errorlevel%
"@
$commandPath = Join-Path $evidence 'run.cmd'
[IO.File]::WriteAllText($commandPath,$command,[Text.Encoding]::ASCII)
& cmd.exe /d /c $commandPath *> (Join-Path $evidence 'run.log')
if ($LASTEXITCODE -ne 0) { throw ('Atomic review failed; inspect ' + $evidence) }
$assembly = Get-Content (Join-Path $evidence 'probe-disassembly.txt') -Raw
if ($assembly -notmatch 'lock xadd\s+dword ptr' -or $assembly -notmatch 'lock xadd\s+qword ptr') { throw 'Original macro did not select both intended widths.' }
$assembly = Get-Content (Join-Path $evidence 'object-disassembly.txt') -Raw
if ($assembly -notmatch 'lock xadd\s+dword ptr \[\?new_nid') { throw 'Actual new_nid operation is not 32-bit.' }
# MSVC eliminates the unused fetch result at global_seed and emits lock add, with the same width.
$assembly = Get-Content (Join-Path $evidence 'property-disassembly.txt') -Raw
if ($assembly -notmatch 'lock (?:xadd|add)\s+dword ptr \[\?global_seed') { throw 'Actual global_seed operation is not 32-bit.' }
if ((Get-FileHash $library -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.librarySha256) { throw 'Review changed candidate.' }
[ordered]@{ OfficialHeaderUnchanged=$true; Compiler=$lock.windowsCompiler; NativeSha256=$manifest.librarySha256;
    OfficialHeaderSha256=(Get-FileHash $source -Algorithm SHA256).Hash.ToLowerInvariant();
    OriginalObjectSha256=(Get-FileHash (Join-Path $root 'openssl-build/crypto/objects/libcrypto-lib-obj_dat.obj') -Algorithm SHA256).Hash.ToLowerInvariant();
    OriginalPropertySha256=(Get-FileHash (Join-Path $root 'openssl-build/crypto/property/libcrypto-lib-property.obj') -Algorithm SHA256).Hash.ToLowerInvariant();
    CarrySentinelAndConcurrentUpdates=$true; Probe32And64Disassembly=$true; OriginalObjects32BitOperation=$true;
    Scope='C4133 conditional MSVC intrinsic branches only; not all native warnings or application acceptance' } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'proof.json') -Encoding UTF8
Write-Output (Join-Path $evidence 'proof.json')
