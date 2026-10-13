<#
.SYNOPSIS Builds independent Android native candidates from canonical official Git archives.
.DESCRIPTION Restores build tools only inside an owned container, verifies locked versions, and records native hashes.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$androidLockPath = Join-Path $PSScriptRoot 'android.inputs.lock.json'
$androidLock = Get-Content -LiteralPath $androidLockPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($androidLock.schemaVersion -ne 1 -or $androidLock.ndk.minimumApi -ne 24) { throw 'Unsupported reviewed Android inputs.' }
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
if (Test-Path -LiteralPath $root) { throw 'Use a fresh Android run name and preserve previous evidence.' }
if ((Get-NativeFreeSpace) -lt 5GB) { throw 'Native Android build requires at least 5 GB free.' }
$image = 'vafadar-native-sqlcipher-host:review-r30'
$imageId = & docker image ls --quiet --filter ('reference=' + $image)
if ($LASTEXITCODE -ne 0) { throw 'Container build engine unavailable.' }
if (-not $imageId) {
    & docker build --tag $image (Join-Path $PSScriptRoot 'Linux')
    if ($LASTEXITCODE -ne 0) { throw 'Reviewed Linux native tool restoration failed.' }
}
New-Item -ItemType Directory -Force (Join-Path $root 'inputs'),(Join-Path $root 'candidate') | Out-Null
$sources = @()
foreach ($name in @('openssl','sqlcipher')) {
    $source = $inputLock.sources.$name
    $archive = Restore-NativeSource -Source $source -Name $name
    Copy-Item -LiteralPath $archive -Destination (Join-Path $root ('inputs/' + $name + '.tar'))
    $epoch = & git -C (Join-Path (Get-NativeInputRoot) ('source-git/' + $name + '-' + $source.commit)) show -s --format=%ct $source.commit
    if ($LASTEXITCODE -ne 0 -or $epoch -notmatch '^\d+$') { throw 'Native source epoch unavailable.' }
    $sources += [ordered]@{ sourceDateEpoch=$epoch; name=$name; commit=$source.commit; tagObject=$source.tagObject; archiveSha256=(Get-FileHash -LiteralPath $archive).Hash.ToLowerInvariant() }
}
$sources | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'inputs/source-manifest.json') -Encoding UTF8
Copy-Item -LiteralPath $androidLockPath -Destination (Join-Path $root 'inputs/android.inputs.lock.json')
$imageId = & docker image inspect $image --format '{{.Id}}'
if ($LASTEXITCODE -ne 0) { throw 'Native host image identity unavailable.' }
$containerArguments = @(Get-NativeContainerRunArguments -RunRoot $root -OutputName candidate -WorkTarget '/home/nativebuild/work')
& docker run @containerArguments $image python3 /recipe/Linux/verify-host.py /inputs/android.inputs.lock.json *> (Join-Path $root 'host-proof.json')
if ($LASTEXITCODE -ne 0) { throw 'Restored Android build tools differ from reviewed versions.' }
& docker run @containerArguments $image bash /recipe/Linux/build-android.sh *> (Join-Path $root 'build.log')
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath (Join-Path $root 'build.log') -Tail 20 | Write-Host
    throw ('Native Android build failed; retain ' + (Join-Path $root 'build.log')) }
$files = @()
foreach ($abi in @('arm64-v8a','x86_64')) {
    $library = Join-Path $root ('candidate/' + $abi + '/libsqlcipher.so')
    $files += [ordered]@{ abi=$abi; bytes=(Get-Item -LiteralPath $library).Length; sha256=(Get-FileHash -LiteralPath $library).Hash.ToLowerInvariant() }
}
[ordered]@{ schemaVersion=1; status='candidate-not-app-or-release-accepted'; minimumApi=24;
    sourceInputsSha256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json')).Hash.ToLowerInvariant();
    androidInputsSha256=(Get-FileHash -LiteralPath $androidLockPath).Hash.ToLowerInvariant();
    hostImageId=$imageId; sources=$sources; files=$files } | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Encoding UTF8
Write-Output (Join-Path $root 'candidate')
