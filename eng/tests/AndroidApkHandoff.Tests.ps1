# Exercise the actual entry point without a restore, compilation, signing or device command.
# These tiny file fixtures qualify artifact selection, not APK contents or signatures.
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sourceEntry = Join-Path $repo 'eng/scripts/Build-AndroidApk.ps1'
$temporaryRoot = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts'))
$fixture = Join-Path $temporaryRoot ('Vafadar-AndroidApkHandoff-' + [Guid]::NewGuid().ToString('N'))
$previousExitCode = $global:LASTEXITCODE
$previousPath = $env:PATH
$previousCommandLog = $env:VAFADAR_HANDOFF_COMMAND_LOG

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
    # The project wrapper deliberately resolves an Application, not a PowerShell
    # function. Put an inert executable first on PATH so the real wrapper is tested
    # without accidentally reaching an installed SDK or restoring native packages.
    if ($IsWindows) {
        $fakeHost = Join-Path $fixture 'dotnet.cmd'
        [IO.File]::WriteAllText($fakeHost, "@echo off`r`necho %1>>`"%VAFADAR_HANDOFF_COMMAND_LOG%`"`r`nexit /b 0`r`n")
    } else {
        $fakeHost = Join-Path $fixture 'dotnet'
        [IO.File]::WriteAllText($fakeHost, "#!/bin/sh`nprintf '%s\n' `"`$1`" >> `"`$VAFADAR_HANDOFF_COMMAND_LOG`"`nexit 0`n")
        [IO.File]::SetUnixFileMode($fakeHost, [IO.UnixFileMode]::UserRead -bor [IO.UnixFileMode]::UserWrite -bor [IO.UnixFileMode]::UserExecute)
    }
    $env:VAFADAR_HANDOFF_COMMAND_LOG = $commandLog
    $env:PATH = $fixture + [IO.Path]::PathSeparator + $previousPath
    $resolvedHost = Get-Command dotnet -CommandType Application | Select-Object -First 1
    if ($resolvedHost.Source -ne $fakeHost) { throw 'The isolated SDK executable was not selected.' }
    foreach ($case in @('missing', 'empty', 'ambiguous', 'unique')) {
        $output = Join-Path $fixture $case
        New-Item -ItemType Directory -Path $output | Out-Null
        if ($case -eq 'empty') {
            [IO.File]::WriteAllBytes((Join-Path $output 'fixture-Signed.apk'), [byte[]]@())
        }
        if ($case -in @('ambiguous', 'unique')) {
            [IO.File]::WriteAllText((Join-Path $output 'fixture-Signed.apk'), 'Not an Android package; artifact-selection fixture only.')
        }
        if ($case -eq 'ambiguous') {
            [IO.File]::WriteAllText((Join-Path $output 'other-Signed.apk'), 'Second artifact-selection fixture only.')
        }
        $accepted = $false
        $lines = @()
        try {
            $lines = @(& $entry -Offline -Output $output)
            $accepted = $true
        }
        catch {
            if ($case -eq 'unique') { throw }
            if ($_.Exception.Message -notmatch 'Expected exactly one signed APK|signed APK is empty') { throw }
        }
        if ($accepted -ne ($case -eq 'unique')) { throw "Unexpected artifact admission for $case." }
        if ($case -eq 'unique' -and -not ($lines | Where-Object { $_ -like 'APK:*fixture-Signed.apk*' })) {
            throw 'The unique artifact was not reported.'
        }
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
    if (($buildCommands -join ',') -ne 'prepare,restore,publish,prepare,restore,publish,prepare,restore,publish,prepare,restore,publish,prepare-refused') {
        throw 'The isolated preparation/restore/publish boundary was not exercised exactly once per case.'
    }
    'Four APK handoff cases and one preparation refusal passed; no real preparation, SDK build or device work executed.'
}
finally {
    Pop-Location
    $env:PATH = $previousPath
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
