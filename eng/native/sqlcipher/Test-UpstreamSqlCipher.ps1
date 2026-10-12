<#
.SYNOPSIS Runs stock SQLCipher tests with the original source-built static crypto provider.
.DESCRIPTION Uses only independent container/files and reports the exact upstream suite scope.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'sqlcipher-upstream')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$androidLockPath = Join-Path $PSScriptRoot 'android.inputs.lock.json'
$androidLock = Get-Content -LiteralPath $androidLockPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($androidLock.schemaVersion -ne 1) { throw 'Unsupported reviewed native host inputs.' }
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
if (Test-Path -LiteralPath $root) { throw 'Use a fresh upstream run name and preserve previous evidence.' }
if ((Get-NativeFreeSpace) -lt 5GB) { throw 'Native Android build requires at least 5 GB free.' }
$image = 'vafadar-native-sqlcipher-host:review-r30'
$imageId = & docker image ls --quiet --filter ('reference=' + $image)
if ($LASTEXITCODE -ne 0) { throw 'Container build engine unavailable.' }
if (-not $imageId) {
    & docker build --tag $image (Join-Path $PSScriptRoot 'Linux')
    if ($LASTEXITCODE -ne 0) { throw 'Reviewed Linux native tool restoration failed.' }
}
New-Item -ItemType Directory -Force (Join-Path $root 'inputs'),(Join-Path $root 'evidence') | Out-Null
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
$mounts = @('--mount',('type=bind,source=' + (Join-Path $root 'inputs') + ',target=/inputs,readonly'),
    '--mount',('type=bind,source=' + $PSScriptRoot + ',target=/recipe,readonly'),
    '--mount',('type=bind,source=' + (Join-Path $root 'evidence') + ',target=/output'))
& docker run --rm --network none @mounts $image python3 /recipe/Linux/verify-host.py /inputs/android.inputs.lock.json *> (Join-Path $root 'host-proof.json')
if ($LASTEXITCODE -ne 0) { throw 'Restored Android build tools differ from reviewed versions.' }
& docker run --rm --network none @mounts $image bash /recipe/Linux/test-sqlcipher.sh *> (Join-Path $root 'build.log')
if ($LASTEXITCODE -ne 0) { throw ('Upstream SQLCipher failed; retain ' + (Join-Path $root 'build.log')) }
$proofPath = Join-Path $root 'evidence/upstream-proof.json'
$proof = Get-Content -LiteralPath $proofPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($proof.errors -ne 0 -or $proof.tests -le 0 -or -not $proof.reviewedNativeIdentityMatched) {
    throw 'Required stock SQLCipher observations missing.'
}
Write-Output $proofPath
