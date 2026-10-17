<#
.SYNOPSIS Builds and checks the independent original-schema Android Release probe.
.DESCRIPTION Never starts or changes the main Zanance package. Uses the existing private emulator by default.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64',
    [ValidatePattern('^[a-zA-Z0-9.:-]+$')][string]$DeviceSerial = '127.0.0.1:5571',
    [ValidateRange(1,65535)][int]$AdbPort = 5038)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$candidate = Join-Path $root 'candidate'
$projectRoot = Join-Path $PSScriptRoot 'Compatibility.Android'
$project = Join-Path $projectRoot 'Vafadar.SqlCipher.Compatibility.Android.csproj'
$managedBuildLog = Join-Path $root ('managed-android-build-' + [Guid]::NewGuid().ToString('N') + '.log')
& dotnet build $project -c Release -p:ContinuousIntegrationBuild=true "-p:NativeCandidateDirectory=$candidate" *> $managedBuildLog
if ($LASTEXITCODE -ne 0) { throw ('Independent managed Android build failed; retain ' + $managedBuildLog) }
$apk = Join-Path $projectRoot 'bin/Release/net10.0-android/pro.vafadar.zanance.nativecompatibility-Signed.apk'
$manifest = Get-Content -LiteralPath (Join-Path $candidate 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($apk)
try {
    foreach ($slice in $manifest.files) {
        $name = 'lib/' + $slice.abi + '/libsqlcipher.so'
        $entries = @($archive.Entries | Where-Object FullName -CEQ $name)
        if ($entries.Count -ne 1) { throw 'Independent APK has missing or duplicate native slices.' }
        $stream = $entries[0].Open(); $hash = [Security.Cryptography.SHA256]::Create()
        try { $observed = ([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $hash.Dispose(); $stream.Dispose() }
        if ($observed -cne $slice.sha256) { throw 'Independent APK carries another native candidate.' }
    }
} finally { $archive.Dispose() }
$adb = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk/platform-tools/adb.exe'
} else { (Get-Command adb -ErrorAction Stop).Source }
$package = 'pro.vafadar.zanance.nativecompatibility'
& $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
& $adb -P $AdbPort -s $DeviceSerial install -r $apk | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Independent package installation failed.' }
$activity = (& $adb -P $AdbPort -s $DeviceSerial shell cmd package resolve-activity --brief $package | Select-Object -Last 1).Trim()
if ($activity -notmatch '^pro\.vafadar\.zanance\.nativecompatibility/[A-Za-z0-9_.]+$') { throw 'Unexpected independent activity identity.' }
$schemaRun = [Guid]::NewGuid().ToString('N')
$resultPath = 'files/schema-result-' + $schemaRun + '.json'
& $adb -P $AdbPort -s $DeviceSerial shell am start -W -n $activity --es schema_run $schemaRun | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Independent activity launch failed.' }
$observedPid = (& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()
[ordered]@{ schemaRun=$schemaRun; resultPath=$resultPath; originalProcessId=$observedPid; deviceSerial=$DeviceSerial; adbPort=$AdbPort } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root ('managed-observation-' + $schemaRun + '.json')) -Encoding UTF8
$report = $null
for ($attempt=0; $attempt -lt 30; $attempt++) {
    $text = & $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat $resultPath 2>$null
    if ($LASTEXITCODE -eq 0) { $report = ($text | Out-String) | ConvertFrom-Json; break }
    $pidValue = (& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()
    if ($pidValue -notmatch '^\d+$') { throw 'Independent process ended without a report.' }
    Start-Sleep -Seconds 2
}
if (-not $report -or $report.Failed -or -not $report.ActualCompiledModelUsed -or
    -not $report.WrongMissingKeyAndTamperRefused -or $report.FirstSchemaToCurrentMigrations -le 0) {
    throw 'Independent managed schema proof failed or did not complete; retain its scoped diagnostic evidence.'
}
$report | Add-Member -NotePropertyName qualificationRun -NotePropertyValue $schemaRun
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root ('managed-android-proof-' + $schemaRun + '.json')) -Encoding UTF8
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'managed-android-proof.json') -Encoding UTF8
& $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
Write-Output (Join-Path $root 'managed-android-proof.json')
