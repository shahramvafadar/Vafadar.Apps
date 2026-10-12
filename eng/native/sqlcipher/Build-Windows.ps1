<#
.SYNOPSIS
    Builds a candidate Windows x64 SQLCipher slice from reviewed official sources.
.DESCRIPTION
    Requires the pinned installed MSVC/SDK. Restores portable build tools and immutable source
    archives, enables upstream OpenSSL assembly, and records exact inputs/flags/output hashes.
    No app database, app dependency or global machine setting is changed. Run names must be fresh.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64', [switch]$Resume)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$inputLockPath = Join-Path $PSScriptRoot 'inputs.lock.json'
$inputLock = Get-Content -LiteralPath $inputLockPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($inputLock.schemaVersion -ne 1) { throw 'Unsupported native input schema.' }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$installation = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json | ConvertFrom-Json
if (-not $installation -or $installation.installationVersion -ne $inputLock.windowsCompiler.installationVersion) {
    throw 'The installed compiler differs from the reviewed native toolchain; review the lock before adoption.'
}
$vcvars = Join-Path $installation.installationPath 'VC/Auxiliary/Build/vcvarsall.bat'
$compiler = Join-Path $installation.installationPath ('VC/Tools/MSVC/' + $inputLock.windowsCompiler.toolsVersion + '/bin/Hostx64/x64/cl.exe')
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Pinned native compiler is unavailable.' }
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
if ((Test-Path -LiteralPath $root) -and -not $Resume) { throw 'Use a fresh native run name; preserve previous build evidence.' }
if ($Resume -and -not (Test-Path -LiteralPath $root)) { throw 'No owned native run exists to resume.' }
$drive = Get-PSDrive -Name ([IO.Path]::GetPathRoot($root).Substring(0,1))
if ($drive.Free -lt 5GB) { throw 'Native compilation requires at least 5 GB free.' }
$perlRoot = & (Join-Path $PSScriptRoot 'Restore-WindowsBuildTools.ps1')
$nasmRoot = Restore-NativeZipTool -Tool $inputLock.windowsNasm -Executable $inputLock.windowsNasm.executable
$nasm = Join-Path $nasmRoot $inputLock.windowsNasm.executable
$identity = & $nasm -v
if ($LASTEXITCODE -ne 0 -or $identity -notlike ('NASM version ' + $inputLock.windowsNasm.version + ' *')) { throw 'Native assembler version differs from the lock.' }
New-Item -ItemType Directory -Force $root | Out-Null
$sourceRecords = @()
foreach ($name in @('openssl','sqlcipher')) {
    $source = $inputLock.sources.$name
    $archive = Restore-NativeSource -Source $source -Name $name
    $destination = Join-Path $root $name
    New-Item -ItemType Directory -Force $destination | Out-Null
    # Archives are generated exclusively from verified official Git objects, with no checkout conversions.
    & tar -xf $archive -C $destination
    if ($LASTEXITCODE -ne 0) { throw 'Native source extraction failed.' }
    $sourceRecords += [ordered]@{ name=$name; commit=$source.commit; tagObject=$source.tagObject; archiveSha256=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$opensslSource = Join-Path $root 'openssl'
$sqlcipherSource = Join-Path $root 'sqlcipher'
$opensslBuild = Join-Path $root 'openssl-build'
$cipherBuild = Join-Path $root 'sqlcipher-build'
New-Item -ItemType Directory -Force $opensslBuild,$cipherBuild | Out-Null
$opensslGit = Join-Path (Get-NativeInputRoot) ('source-git/openssl-' + $inputLock.sources.openssl.commit)
$sourceEpoch = & git -C $opensslGit show -s --format=%ct $inputLock.sources.openssl.commit
if ($LASTEXITCODE -ne 0 -or $sourceEpoch -notmatch '^\d+$') { throw 'Pinned native source epoch unavailable.' }
$perl = Join-Path $perlRoot 'perl/bin/perl.exe'
$nasmDirectory = Split-Path $nasm
$cmd = @"
@echo off
set LANG=C
set LC_ALL=C
set SOURCE_DATE_EPOCH=$sourceEpoch
set "PATH=$nasmDirectory;%PATH%"
call "$vcvars" x64 $($inputLock.windowsCompiler.sdkVersion) -vcvars_ver=$($inputLock.windowsCompiler.toolsVersion)
if errorlevel 1 exit /b 1
cd /d "$opensslBuild"
"$perl" "$opensslSource\Configure" VC-WIN64A no-shared no-module no-autoload-config no-filenames --prefix=C:\Vafadar\Sqlcipher /guard:cf
if errorlevel 1 exit /b 1
nmake /NOLOGO build_generated libcrypto.lib
if errorlevel 1 exit /b 1
cd /d "$cipherBuild"
nmake /NOLOGO /f "$sqlcipherSource\Makefile.msc" "TOP=$sqlcipherSource" sqlite3.c
if errorlevel 1 exit /b 1
cl /nologo /O2 /MT /LD /GS /guard:cf /DSQLITE_API=__declspec(dllexport) /DSQLITE_HAS_CODEC /DSQLITE_TEMP_STORE=3 /DSQLITE_THREADSAFE=1 /DSQLITE_DEFAULT_FOREIGN_KEYS=1 /DSQLITE_EXTRA_INIT=sqlcipher_extra_init /DSQLITE_EXTRA_SHUTDOWN=sqlcipher_extra_shutdown /DSQLCIPHER_CRYPTO_OPENSSL /DSQLITE_ENABLE_COLUMN_METADATA /DSQLITE_ENABLE_FTS5 /I"$opensslBuild\include" /I"$opensslSource\include" sqlite3.c "$opensslBuild\libcrypto.lib" /link /OUT:sqlcipher.dll /Brepro /guard:cf /DYNAMICBASE /NXCOMPAT /HIGHENTROPYVA /CETCOMPAT advapi32.lib crypt32.lib ws2_32.lib user32.lib gdi32.lib bcrypt.lib
if errorlevel 1 exit /b 1
dumpbin /nologo /exports sqlcipher.dll > "$root\exports.txt"
if errorlevel 1 exit /b 1
dumpbin /nologo /dependents sqlcipher.dll > "$root\dependents.txt"
if errorlevel 1 exit /b 1
"@
$commandPath = Join-Path $root 'build.cmd'
if ($Resume -and (Test-Path -LiteralPath $commandPath) -and
    [IO.File]::ReadAllText($commandPath).Replace("`r`n","`n") -ne $cmd.Replace("`r`n","`n")) {
    # Changed configuration may leave stale upstream objects: require a fresh directory instead.
    throw 'Native build recipe changed; use a fresh run name rather than reusing configured objects.'
}
if ($Resume) {
    # Keep failed evidence. An identical recipe permits nmake to resume an interrupted build; preserve its prior evidence.
    $stamp = [Guid]::NewGuid().ToString('N')
    foreach ($file in @('build.cmd','build.log')) {
        $previous = Join-Path $root $file
        if (Test-Path -LiteralPath $previous) { Copy-Item -LiteralPath $previous -Destination ($previous + '.' + $stamp) }
    }
}
[IO.File]::WriteAllText($commandPath,$cmd,[Text.Encoding]::ASCII)
& cmd.exe /d /c $commandPath *> (Join-Path $root 'build.log')
if ($LASTEXITCODE -ne 0) { throw ('Native compilation failed; inspect ' + (Join-Path $root 'build.log')) }
$exports = Get-Content -LiteralPath (Join-Path $root 'exports.txt') -Raw
foreach ($entry in @('sqlite3_open_v2','sqlite3_key','sqlite3_rekey','sqlite3_backup_init','sqlite3_serialize','sqlite3_deserialize')) {
    if ($exports -notmatch ('(?m)\s' + [regex]::Escape($entry) + '\s*$')) { throw ('Required native export missing: ' + $entry) }
}
$dependencies = @(Get-Content -LiteralPath (Join-Path $root 'dependents.txt') | ForEach-Object {
    if ($_ -match '^\s+([A-Za-z0-9_.-]+\.dll)\s*$') { $Matches[1].ToLowerInvariant() }
})
if (-not $dependencies.Count) { throw 'Native dependency inventory is empty.' }
foreach ($dependency in $dependencies) {
    if ($dependency -notin @('kernel32.dll','advapi32.dll','crypt32.dll','ws2_32.dll','user32.dll','gdi32.dll','bcrypt.dll')) {
        throw ('Unexpected native dependency: ' + $dependency)
    }
}
$library = Join-Path $cipherBuild 'sqlcipher.dll'
$packageRoot = Join-Path $root 'candidate'
New-Item -ItemType Directory -Force (Join-Path $packageRoot 'runtimes/win-x64/native'),(Join-Path $packageRoot 'licenses') | Out-Null
Copy-Item -LiteralPath $library -Destination (Join-Path $packageRoot 'runtimes/win-x64/native/sqlcipher.dll')
Copy-Item -LiteralPath (Join-Path $sqlcipherSource 'LICENSE.md') -Destination (Join-Path $packageRoot 'licenses/SQLCipher.txt')
Copy-Item -LiteralPath (Join-Path $opensslSource 'LICENSE.txt') -Destination (Join-Path $packageRoot 'licenses/OpenSSL.txt')
$manifest = [ordered]@{ schemaVersion=1; status='candidate-not-app-or-release-accepted'; runtimeIdentifier='win-x64'; sources=$sourceRecords;
    inputLockSha256=(Get-FileHash -LiteralPath $inputLockPath -Algorithm SHA256).Hash.ToLowerInvariant();
    nativeDependencies=$dependencies; compiler=$inputLock.windowsCompiler; compilerExecutableSha256=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant();
    perlVersion=$inputLock.windowsPerl.version; nasmVersion=$inputLock.windowsNasm.version; sourceDateEpoch=$sourceEpoch;
    opensslOptions=@('VC-WIN64A','no-shared','no-module','no-autoload-config','no-filenames','assembly-enabled','/guard:cf');
    sqliteOptions=@('SQLITE_HAS_CODEC','SQLITE_TEMP_STORE=3','SQLITE_THREADSAFE=1','SQLITE_DEFAULT_FOREIGN_KEYS=1','SQLITE_EXTRA_INIT=sqlcipher_extra_init','SQLITE_EXTRA_SHUTDOWN=sqlcipher_extra_shutdown','SQLCIPHER_CRYPTO_OPENSSL','SQLITE_ENABLE_COLUMN_METADATA','SQLITE_ENABLE_FTS5');
    linkerOptions=@('/Brepro','/guard:cf','/DYNAMICBASE','/NXCOMPAT','/HIGHENTROPYVA','/CETCOMPAT');
    libraryBytes=(Get-Item -LiteralPath $library).Length; librarySha256=(Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant() }
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding UTF8
Write-Output $packageRoot
