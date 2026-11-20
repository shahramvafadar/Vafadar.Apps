# Exercise the actual entry point without a restore, compilation, signing or device command.
# The tiny file fixtures qualify selection and the verifier boundary without an Android SDK.
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sourceEntry = Join-Path $repo 'eng/scripts/Build-AndroidApk.ps1'
$temporaryRoot = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts'))
$fixture = Join-Path $temporaryRoot ('Vafadar-AndroidApkHandoff-' + [Guid]::NewGuid().ToString('N'))
$previousExitCode = $global:LASTEXITCODE
$previousPath = $env:PATH
$previousCommandLog = $env:VAFADAR_HANDOFF_COMMAND_LOG
$previousPayload = $env:VAFADAR_HANDOFF_PAYLOAD

Push-Location
try {
    New-Item -ItemType Directory -Path $fixture | Out-Null
    $commandLog = Join-Path $fixture 'commands.txt'
    # Copy the actual entry/wrapper bytes into the owned fixture so their relative preparation
    # dependency can be inert too. Production has no flag that bypasses Core preparation.
    $scripts = Join-Path $fixture 'eng/scripts'
    $maui = Join-Path $fixture 'eng/maui'
    New-Item -ItemType Directory -Path $scripts, $maui | Out-Null
    $entry = Join-Path $scripts 'Build-AndroidApk.ps1'
    Copy-Item -LiteralPath $sourceEntry -Destination $entry
    Copy-Item -LiteralPath (Join-Path $repo 'eng/scripts/Invoke-ProjectDotNet.ps1') -Destination $scripts
    if ((Get-FileHash -LiteralPath $sourceEntry).Hash -ne (Get-FileHash -LiteralPath $entry).Hash) {
        throw 'The isolated entry-point bytes differ from the actual script.'
    }
    $preparation = Join-Path $maui 'Ensure-ConfigurationMetrics.ps1'
    [IO.File]::WriteAllText($preparation, 'Add-Content -LiteralPath $env:VAFADAR_HANDOFF_COMMAND_LOG -Value prepare')
    [IO.File]::WriteAllText((Join-Path $scripts 'Assert-AndroidApk.ps1'), @'
param([string]$ApkPath, [string]$Project, [string]$Configuration, [string[]]$BuildProperties, [switch]$Offline)
Add-Content -LiteralPath $env:VAFADAR_HANDOFF_COMMAND_LOG -Value verify
if (!$Offline -or $Configuration -cne 'Release' -or !$Project -or '-p:MicrosoftEntraClientId=' -cnotin $BuildProperties) {
    throw 'The verifier did not receive the build configuration.'
}
if ($ApkPath -like '*verifier-refused*') { throw 'The fixture verifier refused the signature.' }
[pscustomobject]@{ Package = 'pro.vafadar.zanance'; Sha256 = ('a' * 64) }
'@)
    # The project wrapper deliberately resolves an Application, not a PowerShell
    # function. Put an inert executable first on PATH so the real wrapper is tested
    # without accidentally reaching an installed SDK or restoring native packages.
    if ($IsWindows) {
        $fakeHost = Join-Path $fixture 'dotnet.cmd'
        [IO.File]::WriteAllText($fakeHost, @'
@echo off
echo %1>>"%VAFADAR_HANDOFF_COMMAND_LOG%"
if "%~1" neq "publish" exit /b 0
:next
if "%~1" equ "" exit /b 96
if "%~1" equ "-o" goto emit
shift
goto next
:emit
if exist "%VAFADAR_HANDOFF_PAYLOAD%\attempted.flag" exit /b 0
if exist "%VAFADAR_HANDOFF_PAYLOAD%\*.apk" copy /y "%VAFADAR_HANDOFF_PAYLOAD%\*.apk" "%~2\" >nul
if exist "%VAFADAR_HANDOFF_PAYLOAD%\retry.flag" (
    ren "%VAFADAR_HANDOFF_PAYLOAD%\retry.flag" attempted.flag
    echo NETSDK1005 simulated retry
    exit /b 1
)
exit /b 0
'@)
    } else {
        $fakeHost = Join-Path $fixture 'dotnet'
        [IO.File]::WriteAllText($fakeHost, @'
#!/bin/sh
printf '%s\n' "$1" >> "$VAFADAR_HANDOFF_COMMAND_LOG"
[ "$1" = publish ] || exit 0
while [ "$#" -gt 0 ]; do
    if [ "$1" = -o ]; then
        [ ! -f "$VAFADAR_HANDOFF_PAYLOAD/attempted.flag" ] || exit 0
        for file in "$VAFADAR_HANDOFF_PAYLOAD"/*.apk; do
            if [ -f "$file" ]; then cp "$file" "$2/" || exit $?; fi
        done
        if [ -f "$VAFADAR_HANDOFF_PAYLOAD/retry.flag" ]; then
            mv "$VAFADAR_HANDOFF_PAYLOAD/retry.flag" "$VAFADAR_HANDOFF_PAYLOAD/attempted.flag"
            printf '%s\n' 'NETSDK1005 simulated retry'
            exit 1
        fi
        exit 0
    fi
    shift
done
exit 96
'@)
        [IO.File]::SetUnixFileMode($fakeHost, [IO.UnixFileMode]::UserRead -bor [IO.UnixFileMode]::UserWrite -bor [IO.UnixFileMode]::UserExecute)
    }
    $env:VAFADAR_HANDOFF_COMMAND_LOG = $commandLog
    $env:PATH = $fixture + [IO.Path]::PathSeparator + $previousPath
    $resolvedHost = Get-Command dotnet -CommandType Application | Select-Object -First 1
    if ($resolvedHost.Source -ne $fakeHost) { throw 'The isolated SDK executable was not selected.' }
    foreach ($case in @('missing', 'stale', 'empty', 'ambiguous', 'unique', 'verifier-refused', 'other-app', 'retry-no-file')) {
        $output = Join-Path $fixture $case
        $payload = Join-Path $fixture "$case-payload"
        New-Item -ItemType Directory -Path $output, $payload | Out-Null
        $env:VAFADAR_HANDOFF_PAYLOAD = $payload
        $publishedApk = Join-Path $output 'fixture-Signed.apk'
        if ($case -in @('stale', 'unique')) {
            [IO.File]::WriteAllText($publishedApk, 'Previous handoff.')
        }
        if ($case -eq 'other-app') {
            [IO.File]::WriteAllText((Join-Path $output 'other-Signed.apk'), 'Another existing package.')
        }
        if ($case -eq 'empty') {
            [IO.File]::WriteAllBytes((Join-Path $payload 'fixture-Signed.apk'), [byte[]]@())
        }
        if ($case -in @('ambiguous', 'unique', 'verifier-refused', 'other-app', 'retry-no-file')) {
            [IO.File]::WriteAllText((Join-Path $payload 'fixture-Signed.apk'), 'New handoff fixture.')
        }
        if ($case -eq 'retry-no-file') {
            [IO.File]::WriteAllText((Join-Path $payload 'retry.flag'), '')
        }
        if ($case -eq 'ambiguous') {
            [IO.File]::WriteAllText((Join-Path $payload 'other-Signed.apk'), 'Second candidate from the same build.')
        }
        $accepted = $false
        $lines = @()
        try {
            & $entry -Offline -Output $output | ForEach-Object { $lines += $_ }
            $accepted = $true
        }
        catch {
            if ($case -eq 'unique') { throw }
            if ($_.Exception.Message -notmatch 'Expected exactly one signed APK|signed APK is empty|fixture verifier refused|another signed APK') { throw }
        }
        if ($accepted -ne ($case -eq 'unique')) { throw "Unexpected artifact admission for $case." }
        if ($case -eq 'unique' -and (-not ($lines -like 'APK:*fixture-Signed.apk*') -or
            'Package: pro.vafadar.zanance' -cnotin $lines -or "SHA-256: $('a' * 64)" -cnotin $lines)) {
            throw 'The verified artifact identity was not reported.'
        }
        if (!$accepted -and $lines -match '^APK:|^Package:|^SHA-256:|adb install') {
            throw "An unverified handoff was reported for $case."
        }
        if ($case -eq 'stale' -and [IO.File]::ReadAllText($publishedApk) -cne 'Previous handoff.') {
            throw 'A failed publish changed the prior handoff.'
        }
        if ($case -eq 'unique' -and [IO.File]::ReadAllText($publishedApk) -cne 'New handoff fixture.') {
            throw 'The fresh artifact did not replace the previous handoff.'
        }
        if ($case -eq 'other-app' -and (Test-Path -LiteralPath $publishedApk)) {
            throw 'The mixed output gained another signed package.'
        }
        if (Get-ChildItem -LiteralPath $output -Directory) { throw 'An owned publish directory was left behind.' }
    }
    # A preparation refusal must stop before restore/publish, even when an inert SDK would succeed.
    [IO.File]::WriteAllText($preparation, @'
Add-Content -LiteralPath $env:VAFADAR_HANDOFF_COMMAND_LOG -Value prepare-refused
throw 'Private Core preparation fixture refusal.'
'@)
    $preparationRefused = $false
    try { & $entry -Offline -Output (Join-Path $fixture 'preparation-refused') | Out-Null }
    catch {
        if ($_.Exception.Message -ne 'Private Core preparation fixture refusal.') { throw }
        $preparationRefused = $true
    }
    if (-not $preparationRefused) { throw 'The entry point accepted a refused Core preparation.' }
    $buildCommands = @([IO.File]::ReadAllLines($commandLog))
    $expectedCommands = @('prepare,restore,publish') * 4 +
        @('prepare,restore,publish,verify') * 3 + @('prepare,restore,publish,restore,publish', 'prepare-refused')
    if (($buildCommands -join ',') -ne ($expectedCommands -join ',')) {
        throw 'The isolated preparation, build and verification boundaries changed.'
    }
    'Eight APK handoff cases and one preparation refusal passed with isolated build and verifier commands.'
}
finally {
    Pop-Location
    $env:PATH = $previousPath
    $env:VAFADAR_HANDOFF_PAYLOAD = $previousPayload
    if ($null -eq $previousCommandLog) {
        Remove-Item Env:/VAFADAR_HANDOFF_COMMAND_LOG -ErrorAction SilentlyContinue
    } else {
        $env:VAFADAR_HANDOFF_COMMAND_LOG = $previousCommandLog
    }
    $global:LASTEXITCODE = $previousExitCode
    # Remove only this new, fully resolved test fixture; never a caller-supplied path.
    $resolved = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($resolved) -ne $temporaryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) -or
        [IO.Path]::GetFileName($resolved) -notmatch '^Vafadar-AndroidApkHandoff-[0-9a-f]{32}$') {
        throw 'Owned test-fixture cleanup path refused.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
