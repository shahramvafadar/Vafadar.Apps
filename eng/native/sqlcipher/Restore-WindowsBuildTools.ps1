<#
.SYNOPSIS
    Restores the checksum-pinned portable native Windows Perl used by the SQLCipher build.
.DESCRIPTION
    Uses official release metadata pinned in inputs.lock.json. No installer, registry, global PATH,
    account or system configuration is changed. Restored inputs stay inside this repository's artifacts.
    MSYS Perl does not match OpenSSL's documented VC build environment.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$cacheRoot = Join-Path $repoRoot 'artifacts/native-sqlcipher/tools'
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$tool = $inputLock.windowsPerl
if ($inputLock.schemaVersion -ne 1 -or $tool.sha256 -notmatch '^[0-9a-f]{64}$' -or
    ([Uri]$tool.uri).Scheme -ne 'https' -or ([Uri]$tool.uri).Host -ne 'github.com' -or
    ([Uri]$tool.uri).AbsolutePath -notlike '/StrawberryPerl/Perl-Dist-Strawberry/releases/download/*') {
    throw 'The native build-tool lock has an unsupported format or origin.'
}
$drive = Get-PSDrive -Name ([IO.Path]::GetPathRoot($repoRoot).Substring(0,1))
if ($drive.Free -lt 3GB) { throw 'Native input restoration requires at least 3 GB free.' }
New-Item -ItemType Directory -Force $cacheRoot | Out-Null
$archive = Join-Path $cacheRoot ($tool.sha256 + '.zip')
if (-not (Test-Path -LiteralPath $archive)) {
    $partial = Join-Path $cacheRoot ($tool.sha256 + '.partial-' + [Guid]::NewGuid().ToString('N'))
    # Use the exact locked official release, not a changing latest download or an unofficial mirror.
    Invoke-WebRequest -Uri $tool.uri -OutFile $partial -UseBasicParsing
    if ((Get-Item -LiteralPath $partial).Length -ne $tool.bytes -or
        (Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant() -ne $tool.sha256) {
        throw 'The downloaded native build tool does not match its locked size/checksum.'
    }
    Move-Item -LiteralPath $partial -Destination $archive
}
if ((Get-Item -LiteralPath $archive).Length -ne $tool.bytes -or
    (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $tool.sha256) {
    throw 'The cached native build archive differs from its locked release.'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$destination = Join-Path $cacheRoot $tool.sha256
$marker = Join-Path $destination 'verified-tool.json'
if (-not (Test-Path -LiteralPath $destination)) {
    $staging = Join-Path $cacheRoot ($tool.sha256 + '.extract-' + [Guid]::NewGuid().ToString('N'))
    $stagingPrefix = [IO.Path]::GetFullPath($staging).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        foreach ($entry in $zip.Entries) {
            $target = [IO.Path]::GetFullPath((Join-Path $staging $entry.FullName))
            if (-not $target.StartsWith($stagingPrefix, [StringComparison]::OrdinalIgnoreCase) -or
                $entry.FullName.Contains(':') -or (($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000) {
                throw 'The locked build archive contains an unsafe extraction path or symbolic link.'
            }
        }
    }
    finally { $zip.Dispose() }
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $staging)
    # Check both computed absolute paths before moving this newly owned directory.
    $ownedPrefix = [IO.Path]::GetFullPath($cacheRoot).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    foreach ($path in @($staging, $destination)) {
        if (-not [IO.Path]::GetFullPath($path).StartsWith($ownedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Native tool extraction leaves the owned artifact directory.'
        }
    }
    [IO.Directory]::Move($staging, $destination)
    $executable = Join-Path $destination 'perl/bin/perl.exe'
    [ordered]@{ archiveSha256 = $tool.sha256; executableSha256 = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant() } |
        ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding UTF8
}
if (-not (Test-Path -LiteralPath $marker)) { throw 'The native tool cache has no completed verification record.' }
$verified = Get-Content -LiteralPath $marker -Raw -Encoding UTF8 | ConvertFrom-Json
$executable = Join-Path $destination 'perl/bin/perl.exe'
if ($verified.archiveSha256 -ne $tool.sha256 -or
    (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant() -ne $verified.executableSha256) {
    throw 'The restored native interpreter differs from its verified release.'
}
$previousNativeLang = $env:LANG
$previousNativeLocale = $env:LC_ALL
try {
    # Native Windows Perl does not accept the MSYS C.UTF-8 locale. Scope C to this build process.
    $env:LANG = 'C'
    $env:LC_ALL = 'C'
    $identity = & $executable -e 'print "$^O $^V"'
    $identityExit = $LASTEXITCODE
}
finally {
    $env:LANG = $previousNativeLang
    $env:LC_ALL = $previousNativeLocale
}
if ($identityExit -ne 0 -or $identity -ne ('MSWin32 v' + ($tool.version -replace '\.\d+$',''))) {
    throw 'The restored interpreter does not match the locked native Windows Perl version.'
}
Write-Output $destination
