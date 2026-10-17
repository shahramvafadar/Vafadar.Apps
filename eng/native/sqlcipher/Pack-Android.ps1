<#
.SYNOPSIS Packages matching independently validated Android slices without publishing or app activation.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64',
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$PackageName = 'candidate',
    [string]$CandidateVersion = '')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$candidate = Join-Path $root 'candidate'
$manifestPath = Join-Path $candidate 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1 -or $manifest.files.Count -ne 2 -or
    $manifest.cryptoProfileSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'windows.crypto.profile.json') -Algorithm SHA256).Hash.ToLowerInvariant() -or
    $manifest.sourceInputsSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json')).Hash.ToLowerInvariant() -or
    $manifest.status -ne 'candidate-not-app-or-release-accepted' -or $manifest.minimumApi -ne 24 -or
    $manifest.androidInputsSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'android.inputs.lock.json')).Hash.ToLowerInvariant()) {
    throw 'Android candidate differs from reviewed inputs.'
}
$runtimePath = Join-Path $root 'runtime-proof.json'
$reusedRuntimeEvidence = -not (Test-Path -LiteralPath $runtimePath)
$qualification = $null
if ($reusedRuntimeEvidence) {
    # An observed binary's exact bytes retain its prior scope; this is not a new emulator run.
    $qualification = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'android.runtime.qualification.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($qualification.schemaVersion -ne 1 -or $qualification.sourceInputsSha256 -ne $manifest.sourceInputsSha256 -or
        $qualification.androidInputsSha256 -ne $manifest.androidInputsSha256) { throw 'No matching observed native qualification.' }
    $runtime = $qualification.runtimeEvidence
} else { $runtime = Get-Content -LiteralPath $runtimePath -Raw -Encoding UTF8 | ConvertFrom-Json }
$entries = [ordered]@{}
foreach ($abi in @('arm64-v8a','x86_64')) {
    $file = $manifest.files | Where-Object abi -eq $abi
    $library = Join-Path $candidate ($abi + '/libsqlcipher.so')
    $proof = Get-Content -LiteralPath (Join-Path $candidate ($abi + '/elf-proof.json')) -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $file -or $proof.nativeSha256 -ne $file.sha256 -or
        (Get-FileHash -LiteralPath $library).Hash.ToLowerInvariant() -ne $file.sha256 -or
        $proof.abi -ne $abi -or -not $proof.requiredExportsPresent -or -not $proof.staticCryptoNotExposed -or
        -not $proof.loadAlignment16KiB -or -not $proof.relroImmediateBindingNonExecutableStack) {
        throw 'Native Android slice differs from its verified evidence.'
    }
    if ($runtime.abi -eq $abi -and $runtime.nativeSha256 -ne $file.sha256) { throw 'Runtime-tested bytes differ.' }
    if ($reusedRuntimeEvidence -and $qualification.nativeByAbi.$abi -ne $file.sha256) { throw 'Unobserved native bytes cannot reuse qualification.' }
    $rid = if ($abi -eq 'arm64-v8a') { 'android-arm64' } else { 'android-x64' }
    $entries['runtimes/' + $rid + '/native/libsqlcipher.so'] = [IO.File]::ReadAllBytes($library)
}
if ($runtime.abi -notin @('arm64-v8a','x86_64') -or -not $runtime.nativeProof.wrongMissingKeyTamperRefused -or
    -not $runtime.nativeProof.nativeCipherAndProviderMatched -or -not $runtime.nativeProof.correctKeyReopenMinorUnitsCurrencyDate) { throw 'Independent runtime proof missing.' }
$notices = 'Owner-written Zanance source remains all rights reserved. Component notices apply only to their components. Upstream NDK notices cover compiler-runtime support; the build tools are not shipped.'
foreach ($license in @('SQLCipher.txt','OpenSSL.txt','NDK-CompilerRuntime.txt')) {
    $notices += "`n`n" + (Get-Content -LiteralPath (Join-Path $candidate ('licenses/' + $license)) -Raw -Encoding UTF8)
}
$contents = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'android.package.contents.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$id = 'Vafadar.Native.SqlCipher.Android'
$version = Resolve-NativeProducerVersion -InputLock $inputLock -Contents $contents -KnownContents @($contents) -CandidateVersion $CandidateVersion
if (-not $CandidateVersion) {
    foreach ($slice in $manifest.files) {
        $rid = if ($slice.abi -eq 'arm64-v8a') { 'android-arm64' } else { 'android-x64' }
        if ($contents.entries.('runtimes/' + $rid + '/native/libsqlcipher.so') -cne $slice.sha256) {
            throw 'Unpinned Android bytes require a fresh producer version; never reuse an immutable consumer identity.'
        }
    }
}
$nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>
<id>$id</id><version>$version</version><authors>Shahram Vafadar</authors>
<description>Independent Android SQLCipher candidate. Not accepted for whole-app integration or release.</description>
<license type="file">licenses/ThirdPartyNotices.txt</license>
<dependencies><group targetFramework="net10.0"><dependency id="SQLitePCLRaw.provider.sqlcipher" version="[$($inputLock.managedProviderReview.version)]" /></group></dependencies>
</metadata></package>
"@
# Consumers receive exact ABI items and bundled notices from the package itself.
$targets = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'android-package.targets') -Raw -Encoding UTF8
$entries['buildTransitive/net10.0-android/' + $id + '.targets'] = [Text.Encoding]::UTF8.GetBytes($targets.Replace("`r`n","`n"))
$entries[$id + '.nuspec'] = [Text.Encoding]::UTF8.GetBytes($nuspec.Replace("`r`n","`n"))
$entries['licenses/ThirdPartyNotices.txt'] = [Text.Encoding]::UTF8.GetBytes($notices.Replace("`r`n","`n"))
# Package identity is stable across independently reproduced hosts. Keep hostImageId in the
# producer manifest/evidence, rather than changing immutable package bytes with each Docker image.
$packageManifest = [ordered]@{ schemaVersion=$manifest.schemaVersion; status=$manifest.status; minimumApi=$manifest.minimumApi;
    cryptoProfileSha256=$manifest.cryptoProfileSha256;
    sourceInputsSha256=$manifest.sourceInputsSha256; androidInputsSha256=$manifest.androidInputsSha256;
    sources=$manifest.sources; files=$manifest.files }
$entries['manifest.json'] = [Text.Encoding]::UTF8.GetBytes(($packageManifest | ConvertTo-Json -Depth 8).Replace("`r`n","`n"))
if (-not $CandidateVersion) {
    if ((@($entries.Keys | Sort-Object) -join '|') -cne (@($contents.entries.PSObject.Properties.Name | Sort-Object) -join '|')) {
        throw 'Normal Android packaging cannot change immutable consumer entry names.'
    }
    foreach ($name in $entries.Keys) {
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $actual = ([BitConverter]::ToString($hash.ComputeHash($entries[$name]))).Replace('-','').ToLowerInvariant() }
        finally { $hash.Dispose() }
        if ($actual -cne $contents.entries.($name)) { throw 'Changed Android consumer entries require a fresh producer identity.' }
    }
}
$packageDirectory = Join-Path $root ('packages/' + $PackageName)
New-Item -ItemType Directory -Force $packageDirectory | Out-Null
$path = Join-Path $packageDirectory ($id + '.' + $version + '.nupkg')
if (Test-Path -LiteralPath $path) { throw 'Preserve prior Android packages; use a fresh package name.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create)
$epoch = ($manifest.sources | Where-Object name -eq 'openssl').sourceDateEpoch
try {
    foreach ($name in ($entries.Keys | Sort-Object)) {
        $entry = $zip.CreateEntry($name,[IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = [DateTimeOffset]::FromUnixTimeSeconds([long]$epoch)
        $stream = $entry.Open()
        try { $stream.Write($entries[$name],0,$entries[$name].Length) } finally { $stream.Dispose() }
    }
}
finally { $zip.Dispose() }
[ordered]@{ packageSha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant(); candidateOnly=$true;
    runtimeAbi=$runtime.abi; priorRuntimeEvidenceReused=$reusedRuntimeEvidence; producerManifestSha256=(Get-FileHash -LiteralPath $manifestPath).Hash.ToLowerInvariant();
    otherAbiRuntimeAndWholeAppNotAccepted=$true } | ConvertTo-Json |
    Set-Content -LiteralPath ($path + '.proof.json') -Encoding UTF8
Write-Output $path
