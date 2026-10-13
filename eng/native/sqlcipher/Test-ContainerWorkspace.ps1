<# .SYNOPSIS Reproduces Linux bind ownership refusal and verifies non-root writable work/output with read-only recipes. #>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$image = 'vafadar-native-sqlcipher-host:review-r30'
& docker image inspect $image --format '{{.Id}}' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Restore the reviewed native build image before the workspace check.' }
$fixture = 'vafadar-native-workspace-test-' + [Guid]::NewGuid().ToString('N')
& docker volume create $fixture | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Owned Linux permission fixture unavailable.' }
$mount = 'type=volume,source=' + $fixture + ',target=/owned'
try {
    # Only this independently owned volume is initialized as root. Actual build containers remain non-root.
    & docker run --rm --network none --user 0:0 --mount $mount $image bash -c 'mkdir /owned/output /owned/work; chown 1001:1001 /owned/output /owned/work; chmod 755 /owned/output /owned/work'
    if ($LASTEXITCODE -ne 0) { throw 'Owned permission fixture preparation failed.' }
    & docker run --rm --network none --user 10001:10001 --mount $mount $image bash -c 'test ! -w /owned/output && test ! -w /owned/work'
    if ($LASTEXITCODE -ne 0) { throw 'Original Linux owner mismatch was not reproduced.' }
    & docker run --rm --network none --user 1001:1001 --mount $mount --mount ('type=bind,source=' + $PSScriptRoot + ',target=/recipe,readonly') $image bash -c 'test "$(id -u)" = 1001 && touch /owned/output/proof /owned/work/proof && test ! -w /recipe/Linux/build-android.sh'
    if ($LASTEXITCODE -ne 0) { throw 'Non-root matched-owner output/work or read-only recipe failed.' }
    # Exercise actual argument construction without compiling unchanged native libraries.
    $run = Join-Path (Get-NativeInputRoot) ('build/workspace-check-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory (Join-Path $run 'inputs'),(Join-Path $run 'candidate') -Force | Out-Null
    $arguments = @(Get-NativeContainerRunArguments -RunRoot $run -OutputName candidate -WorkTarget '/home/nativebuild/work')
    & docker run @arguments $image bash -c 'test "$(id -u)" != 0 && touch /output/workspace-proof /home/nativebuild/work/workspace-proof && test ! -w /recipe/Linux/build-android.sh && test ! -w /inputs'
    if ($LASTEXITCODE -ne 0) { throw 'Actual native mount arguments failed.' }
    $upstreamRun = Join-Path (Get-NativeInputRoot) ('build/workspace-upstream-check-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory (Join-Path $upstreamRun 'inputs'),(Join-Path $upstreamRun 'evidence') -Force | Out-Null
    $upstreamArguments = @(Get-NativeContainerRunArguments -RunRoot $upstreamRun -OutputName evidence -WorkTarget '/home/nativebuild/upstream')
    & docker run @upstreamArguments $image bash -c 'test "$(id -u)" != 0 && touch /output/workspace-proof /home/nativebuild/upstream/workspace-proof && test ! -w /recipe/Linux/test-sqlcipher.sh && test ! -w /inputs'
    if ($LASTEXITCODE -ne 0) { throw 'Actual upstream mount arguments failed.' }
    $refused = $false
    try { Get-NativeContainerRunArguments -RunRoot $run -OutputName candidate -WorkTarget '/home/nativebuild/work' | Out-Null }
    catch { if ($_.Exception.Message -notlike 'Use a fresh*') { throw }; $refused = $true }
    if (-not $refused) { throw 'Existing native workspace was accepted.' }
    $refused = $false
    try { Get-NativeContainerRunArguments -RunRoot (Join-Path $run 'nested') -OutputName candidate -WorkTarget '/home/nativebuild/work' | Out-Null }
    catch { if ($_.Exception.Message -notlike 'Native container work must*') { throw }; $refused = $true }
    if (-not $refused) { throw 'Nested unowned native workspace was accepted.' }
    Write-Output 'Six native workspace checks passed: original refusal, matched non-root owner, Android/upstream actual mounts, existing and nested run refusal.'
}
finally {
    # Remove only the exact freshly created volume; never prune or touch unrelated machine resources.
    & docker volume rm $fixture | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Owned workspace fixture cleanup failed.' }
}
