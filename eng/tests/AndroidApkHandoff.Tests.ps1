# Exercise the actual entry point without a restore, compilation, signing or device command.
# These tiny file fixtures qualify artifact selection, not APK contents or signatures.
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$entry = Join-Path $repo 'eng/scripts/Build-AndroidApk.ps1'
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryRoot ('Vafadar-AndroidApkHandoff-' + [Guid]::NewGuid().ToString('N'))
$buildCommands = [Collections.Generic.List[string]]::new()
$previousExitCode = $global:LASTEXITCODE
function dotnet {
    if ($args[0] -notin @('restore', 'publish')) { throw 'Unexpected build command in the isolated fixture.' }
    $buildCommands.Add([string]$args[0])
    $global:LASTEXITCODE = 0
}

Push-Location
try {
    New-Item -ItemType Directory -Path $fixture | Out-Null
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
    if ($buildCommands.Count -ne 8) { throw 'The isolated restore/publish boundary was not exercised exactly once per case.' }
    'Four APK handoff entry-point cases passed; no SDK build or device work executed.'
}
finally {
    Pop-Location
    Remove-Item Function:/dotnet
    $global:LASTEXITCODE = $previousExitCode
    # Remove only this new, fully resolved test fixture; never a caller-supplied path.
    $resolved = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($resolved) -ne $temporaryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) -or
        [IO.Path]::GetFileName($resolved) -notmatch '^Vafadar-AndroidApkHandoff-[0-9a-f]{32}$') {
        throw 'Owned test-fixture cleanup path refused.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
