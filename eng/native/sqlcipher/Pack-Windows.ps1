<#
.SYNOPSIS Packages a verified Windows candidate, with its manifest and required component notices.
.DESCRIPTION Creates a local prerelease NuGet package with stable ordering and timestamps. Does not publish.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64',
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$PackageName = 'package',
    [string]$CandidateVersion = '')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'WindowsToolchain.psm1') -Force
$contents = Get-ReviewedWindowsPackageContents
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$candidate = Join-Path $root 'candidate'
$manifest = Get-Content -LiteralPath (Join-Path $candidate 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$lockPath = Join-Path $PSScriptRoot 'inputs.lock.json'
$inputLock = Get-Content -LiteralPath $lockPath -Raw -Encoding UTF8 | ConvertFrom-Json
$profilePath = Join-Path $PSScriptRoot 'windows.crypto.profile.json'
if ($manifest.schemaVersion -ne 1 -or $manifest.runtimeIdentifier -ne 'win-x64' -or
    $manifest.status -ne 'candidate-not-app-or-release-accepted' -or
    $manifest.cryptoProfileSha256 -ne (Get-FileHash -LiteralPath $profilePath -Algorithm SHA256).Hash.ToLowerInvariant() -or
    $manifest.inputLockSha256 -ne (Get-FileHash -LiteralPath $lockPath -Algorithm SHA256).Hash.ToLowerInvariant()) {
    throw 'The candidate manifest does not match the current reviewed inputs.'
}
$library = Join-Path $candidate 'runtimes/win-x64/native/sqlcipher.dll'
# Recheck retained evidence even when the candidate predates this packaging invocation.
& (Join-Path $PSScriptRoot 'Test-WindowsWarnings.ps1') -RunName $RunName | Out-Host
if ((Get-Item -LiteralPath $library).Length -ne $manifest.libraryBytes -or
    (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.librarySha256) {
    throw 'Candidate native library differs from its build manifest.'
}
$compatibility = Get-Content -LiteralPath (Join-Path $root 'compatibility-proof.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($compatibility.nativeSha256 -ne $manifest.librarySha256 -or -not $manifest.nativeDependencies.Count) {
    throw 'Run the matching original-schema compatibility check and native dependency inventory before packaging.'
}
$bytes = [IO.File]::ReadAllBytes($library)
$pe = [BitConverter]::ToInt32($bytes,0x3c)
if ($pe -lt 0 -or $pe + 96 -ge $bytes.Length -or [BitConverter]::ToUInt32($bytes,$pe) -ne 0x4550 -or
    [BitConverter]::ToUInt16($bytes,$pe+4) -ne 0x8664 -or
    ([BitConverter]::ToUInt16($bytes,$pe+24+70) -band 0x4160) -ne 0x4160) {
    throw 'Candidate is not a guarded x64 PE library with the required memory protections.'
}
$notices = 'Owner-written Zanance source remains all rights reserved. The following notices apply only to the bundled components.' + "`n`n" +
    (Get-Content -LiteralPath (Join-Path $candidate 'licenses/SQLCipher.txt') -Raw -Encoding UTF8) + "`n`n" +
    (Get-Content -LiteralPath (Join-Path $candidate 'licenses/OpenSSL.txt') -Raw -Encoding UTF8)
# Publish recipe provenance independently of the observed build machine. Exact DLL bytes and all
# package entries remain pinned; the unmodified candidate manifest retains the actual producer.
$compilerPolicy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.compiler.acceptance.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($compilerPolicy.schemaVersion -ne 2 -or $manifest.compilerExecutableSha256 -ne $contents.compilerExecutableSha256 -or $manifest.compilerExecutableSha256 -notin @($compilerPolicy.compilers.sha256)) {
    throw 'Candidate compiler is outside the reviewed producer policy.'
}
$packageManifest = $manifest | ConvertTo-Json -Depth 8 | ConvertFrom-Json
$packageManifest.PSObject.Properties.Remove('compilerExecutableSha256')
$packageManifest | Add-Member -NotePropertyName reviewedCompilerExecutables -NotePropertyValue @($compilerPolicy.compilers | Sort-Object sha256)
$packageManifestBytes = [Text.Encoding]::UTF8.GetBytes((($packageManifest | ConvertTo-Json -Depth 8).Replace("`r`n","`n")) + "`n")
# Producer qualification exports only to this owned artifact directory, never the consumer feed.
# It keeps all source, diagnostic, native, compatibility and compiler gates before proposing new immutable bytes.
$knownContents = @('windows.package.contents.json','windows.servicing.package.contents.json','windows.hosted20261021.package.contents.json') | ForEach-Object {
    Get-Content -LiteralPath (Join-Path $PSScriptRoot $_) -Raw -Encoding UTF8 | ConvertFrom-Json
}
$version = Resolve-NativeProducerVersion -InputLock $inputLock -Contents $contents -KnownContents $knownContents -CandidateVersion $CandidateVersion
if (-not $CandidateVersion -and $contents.entries.'runtimes/win-x64/native/sqlcipher.dll' -cne $manifest.librarySha256) {
    throw 'Unpinned native bytes require a fresh explicit producer version; never reuse an immutable consumer identity.'
}
$packageId = 'Vafadar.Native.SqlCipher.Windows'
$nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>$packageId</id><version>$version</version><authors>Shahram Vafadar</authors>
    <description>Source-built Windows x64 SQLCipher candidate. Not accepted for app integration or product release.</description>
    <license type="file">licenses/ThirdPartyNotices.txt</license>
    <dependencies><group targetFramework="net10.0"><dependency id="SQLitePCLRaw.provider.sqlcipher" version="[$($inputLock.managedProviderReview.version)]" /></group></dependencies>
  </metadata>
</package>
"@
$entries = [ordered]@{
    ($packageId + '.nuspec')=[Text.Encoding]::UTF8.GetBytes($nuspec.Replace("`r`n","`n"))
    'licenses/ThirdPartyNotices.txt'=[Text.Encoding]::UTF8.GetBytes($notices.Replace("`r`n","`n"))
    'manifest.json'=$packageManifestBytes
    ('buildTransitive/' + $packageId + '.targets')=[IO.File]::ReadAllBytes((Join-Path $PSScriptRoot 'windows-package.targets'))
    'runtimes/win-x64/native/sqlcipher.dll'=$bytes
}
if (-not $CandidateVersion) {
    if ((@($entries.Keys | Sort-Object) -join '|') -cne (@($contents.entries.PSObject.Properties.Name | Sort-Object) -join '|')) {
        throw 'Normal packaging cannot change immutable consumer entry names.'
    }
    foreach ($name in $entries.Keys) {
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $actual = ([BitConverter]::ToString($hash.ComputeHash($entries[$name]))).Replace('-','').ToLowerInvariant() }
        finally { $hash.Dispose() }
        if ($actual -cne $contents.entries.($name)) { throw 'Changed consumer entry bytes require a fresh producer identity.' }
    }
}
$output = Join-Path $root $PackageName
if (Test-Path -LiteralPath $output) { throw 'Use a fresh package directory; preserve earlier evidence.' }
New-Item -ItemType Directory $output | Out-Null
$package = Join-Path $output ($packageId + '.' + $version + '.nupkg')
if (Test-Path -LiteralPath $package) { throw 'Preserve previous candidate packages; use a fresh run or remove only your own package deliberately.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($package,[IO.Compression.ZipArchiveMode]::Create)
$timestamp = [DateTimeOffset]::FromUnixTimeSeconds([long]$manifest.sourceDateEpoch)
try {
    foreach ($name in $entries.Keys) {
        $entry = $archive.CreateEntry($name,[IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = $timestamp
        $stream = $entry.Open()
        try { $stream.Write($entries[$name],0,$entries[$name].Length) }
        finally { $stream.Dispose() }
    }
}
finally { $archive.Dispose() }
[ordered]@{ package=$package; packageSha256=(Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant(); nativeSha256=$manifest.librarySha256; candidateOnly=$true } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'package-proof.json') -Encoding UTF8
Write-Output $package
