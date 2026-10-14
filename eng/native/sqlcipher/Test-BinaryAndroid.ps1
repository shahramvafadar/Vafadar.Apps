<# .SYNOPSIS Checks actual production persistence assemblies in independent signed Release Android processes. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-d',
    [ValidatePattern('^[a-zA-Z0-9.:-]+$')][string]$DeviceSerial = '127.0.0.1:5571',
    [ValidateRange(1,65535)][int]$AdbPort = 5038, [switch]$UseExistingBuild,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$ResumeRun = '')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$candidate = Join-Path $root 'candidate'
$project = Join-Path $PSScriptRoot 'BinaryCompatibility.Android'
if (-not $UseExistingBuild) {
    $packageName = 'consumer-' + [Guid]::NewGuid().ToString('N')
    $packed = & (Join-Path $PSScriptRoot 'Pack-Android.ps1') -RunName $RunName -PackageName $packageName
    $feed = Join-Path (Get-NativeInputRoot) 'feed'
    New-Item -ItemType Directory -Force $feed | Out-Null
    $destination = Join-Path $feed ([IO.Path]::GetFileName($packed))
    if (Test-Path -LiteralPath $destination) {
        if ((Get-FileHash -LiteralPath $packed).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw 'Immutable local package version differs; bump its version.' }
    } else { Copy-Item -LiteralPath $packed -Destination $destination }
    & dotnet restore $project --configfile (Join-Path $PSScriptRoot 'native-package.nuget.config') *> (Join-Path $root 'binary-package-restore.log')
    if ($LASTEXITCODE -ne 0) { throw 'Local source-built package restore failed.' }
    & dotnet build $project -c Release -p:ContinuousIntegrationBuild=true -p:TreatWarningsAsErrors=true --no-restore *> (Join-Path $root 'binary-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Actual-assembly Android build failed.' }
}
$apk = Join-Path $project 'bin/Release/net10.0-android/pro.vafadar.zanance.binarycompatibility-Signed.apk'
$manifest = Get-Content (Join-Path $candidate 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($apk)
try {
    if (-not $zip.GetEntry('assets/native/sqlcipher/ThirdPartyNotices.txt') -or -not $zip.GetEntry('assets/native/sqlcipher/manifest.json')) { throw 'Source-built package notices or provenance are missing from APK.' }
    if ($zip.Entries.FullName -match '/libe_sqlite3\.so$') { throw 'Default native engine leaked into actual-assembly caller.' }
    foreach ($slice in $manifest.files) {
        $entry = $zip.GetEntry('lib/' + $slice.abi + '/libsqlcipher.so')
        if (-not $entry) { throw 'Reviewed native slice missing from APK.' }
        $stream = $entry.Open(); $hash = [Security.Cryptography.SHA256]::Create()
        try { $actual = ([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $hash.Dispose(); $stream.Dispose() }
        if ($actual -ne $slice.sha256) { throw 'Embedded actual-assembly native bytes differ.' }
    }
} finally { $zip.Dispose() }
$adb = Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk/platform-tools/adb.exe'
$package = 'pro.vafadar.zanance.binarycompatibility'
& $adb -P $AdbPort -s $DeviceSerial install -r $apk | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Independent binary package installation failed.' }
$activity = (& $adb -P $AdbPort -s $DeviceSerial shell cmd package resolve-activity --brief $package | Select-Object -Last 1).Trim()
if ($activity -notmatch '^pro\.vafadar\.zanance\.binarycompatibility/[A-Za-z0-9_.]+$') { throw 'Unexpected binary caller activity.' }
& $adb -P $AdbPort -s $DeviceSerial shell run-as $package true 2>$null
if ($LASTEXITCODE -ne 0) { throw 'Independent binary caller must permit inspection of its own fictitious sandbox.' }
$identity = if ($ResumeRun) { $ResumeRun } else { [Guid]::NewGuid().ToString('N') }
$results = @()
$phases = if ($ResumeRun) { @('reopen') } else { @('create','reopen') }
foreach ($phase in $phases) {
    & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
    if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Independent binary process did not stop before cold launch.' }
    & $adb -P $AdbPort -s $DeviceSerial shell am start -W -n $activity --es phase $phase --es fixture $identity | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Independent binary launch failed.' }
    $report = $null
    for ($attempt=0; $attempt -lt 600; $attempt++) {
        $value = & $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat files/result.json 2>$null
        if ($LASTEXITCODE -eq 0) { $report = ($value | Out-String) | ConvertFrom-Json; break }
        Start-Sleep -Milliseconds 200
    }
    if (-not $report -or $report.Failed) { throw 'Binary phase incomplete/failed; inspect same owned process and retained error, never recreate to hide failure.' }
    if (-not $report.ActualPersistenceAssemblies -or -not $report.Original32Migrations -or -not $report.NegativeMoneyPreserved -or -not $report.ActualBinaryPortableSource -or -not $report.WrongKeyRefused) { throw 'Actual binary behavior proof missing.' }
    $results += [pscustomobject]@{ Phase = $phase; Result = $report }
}
& $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
$proof = Join-Path $root ('binary-runtime-proof-' + [Guid]::NewGuid().ToString('N') + '.json')
[pscustomobject]@{ Run = $identity; Results = $results } | ConvertTo-Json -Depth 5 | Set-Content $proof -Encoding UTF8
Write-Output $proof
