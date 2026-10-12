<#
.SYNOPSIS Packages matching independently validated Android slices without publishing or app activation.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64',
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$PackageName = 'candidate')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$candidate = Join-Path $root 'candidate'
$manifestPath = Join-Path $candidate 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1 -or $manifest.files.Count -ne 2 -or
    $manifest.sourceInputsSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json')).Hash.ToLowerInvariant() -or
    $manifest.status -ne 'candidate-not-app-or-release-accepted' -or $manifest.minimumApi -ne 24 -or
    $manifest.androidInputsSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'android.inputs.lock.json')).Hash.ToLowerInvariant()) {
    throw 'Android candidate differs from reviewed inputs.'
}
$runtime = Get-Content -LiteralPath (Join-Path $root 'runtime-proof.json') -Raw -Encoding UTF8 | ConvertFrom-Json
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
    $rid = if ($abi -eq 'arm64-v8a') { 'android-arm64' } else { 'android-x64' }
    $entries['runtimes/' + $rid + '/native/libsqlcipher.so'] = [IO.File]::ReadAllBytes($library)
}
if ($runtime.abi -notin @('arm64-v8a','x86_64') -or -not $runtime.nativeProof.wrongMissingKeyTamperRefused -or
    -not $runtime.nativeProof.nativeCipherAndProviderMatched -or -not $runtime.nativeProof.correctKeyReopenMinorUnitsCurrencyDate) { throw 'Independent runtime proof missing.' }
$notices = 'Owner-written Zanance source remains all rights reserved. Component notices apply only to their components. Upstream NDK notices cover compiler-runtime support; the build tools are not shipped.'
foreach ($license in @('SQLCipher.txt','OpenSSL.txt','NDK-CompilerRuntime.txt')) {
    $notices += "`n`n" + (Get-Content -LiteralPath (Join-Path $candidate ('licenses/' + $license)) -Raw -Encoding UTF8)
}
$id = 'Vafadar.Native.SqlCipher.Android'; $version = $inputLock.sources.sqlcipher.tag.TrimStart('v') + '-candidate.1'
$nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>
<id>$id</id><version>$version</version><authors>Shahram Vafadar</authors>
<description>Independent Android SQLCipher candidate. Not accepted for whole-app integration or release.</description>
<license type="file">licenses/ThirdPartyNotices.txt</license>
<dependencies><group targetFramework="net10.0"><dependency id="SQLitePCLRaw.provider.sqlcipher" version="[$($inputLock.managedProviderReview.version)]" /></group></dependencies>
</metadata></package>
"@
$entries[$id + '.nuspec'] = [Text.Encoding]::UTF8.GetBytes($nuspec.Replace("`r`n","`n"))
$entries['licenses/ThirdPartyNotices.txt'] = [Text.Encoding]::UTF8.GetBytes($notices.Replace("`r`n","`n"))
$entries['manifest.json'] = [IO.File]::ReadAllBytes($manifestPath)
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
    runtimeAbi=$runtime.abi; otherAbiRuntimeAndWholeAppNotAccepted=$true } | ConvertTo-Json |
    Set-Content -LiteralPath ($path + '.proof.json') -Encoding UTF8
Write-Output $path
