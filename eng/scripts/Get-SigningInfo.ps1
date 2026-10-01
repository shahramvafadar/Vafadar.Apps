<#
.SYNOPSIS
    Prints the identifiers that Google Cloud, Microsoft Entra and Play Console ask for: package name, version, the
    SHA-1 and SHA-256 fingerprints of the signing certificate and the Entra signature hash (Base64 of the SHA-1).

.DESCRIPTION
    Reads a built APK or AAB (no password needed), or a keystore (keytool asks for its password itself; the script
    never sees it). Builds made with .NET are signed with the .NET debug key
    (%LOCALAPPDATA%\Xamarin\Mono for Android\debug.keystore), not with Android Studio's ~\.android\debug.keystore, so
    the command Microsoft shows for Android Studio projects gives a different hash. Release builds are signed with the
    upload key; after the first upload, Play Console shows the fingerprints of the app signing key under
    Test and release > App integrity > App signing – register both.

.EXAMPLE
    ./eng/scripts/Get-SigningInfo.ps1
    ./eng/scripts/Get-SigningInfo.ps1 -Path artifacts\android\Zanance.aab
    ./eng/scripts/Get-SigningInfo.ps1 -Keystore D:\Keys\vafadar-upload.keystore -Alias vafadar-upload
#>
param(
    [string]$Path = (Join-Path $PSScriptRoot '..\..\artifacts\android\pro.vafadar.zanance-Signed.apk'),
    [string]$Keystore,
    [string]$Alias)
$ErrorActionPreference = 'Stop'

$sdk = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, "${env:ProgramFiles(x86)}\Android\android-sdk", "$env:LOCALAPPDATA\Android\Sdk") |
    Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
$jdk = @($env:JAVA_HOME) + @(Get-ChildItem "$env:ProgramFiles\Android\openjdk", "$env:ProgramFiles\Microsoft" -Directory -Filter 'jdk-*' -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending | ForEach-Object FullName) | Where-Object { $_ -and (Test-Path "$_\bin\keytool.exe") } | Select-Object -First 1
if (-not $jdk) { throw 'No JDK found (keytool). Install the Android workload of Visual Studio or set JAVA_HOME.' }
$env:JAVA_HOME = $jdk
$keytool = "$jdk\bin\keytool.exe"

function Show-Fingerprints([string]$sha1, [string]$sha256) {
    $pairs = { param($hex) (($hex.ToUpperInvariant() -split '(..)' | Where-Object { $_ }) -join ':') }
    $bytes = [byte[]]@(for ($i = 0; $i -lt $sha1.Length; $i += 2) { [Convert]::ToByte($sha1.Substring($i, 2), 16) })
    "SHA-1                 : $(& $pairs $sha1)"
    "SHA-256               : $(& $pairs $sha256)"
    "Entra signature hash  : $([Convert]::ToBase64String($bytes))"
}

if ($Keystore) {
    # keytool prompts for the password; nothing is passed on or stored.
    $list = & $keytool -list -v -keystore $Keystore $(if ($Alias) { '-alias'; $Alias })
    $sha1 = ([regex]::Match(($list -join "`n"), 'SHA1:\s*([0-9A-F:]+)')).Groups[1].Value -replace ':', ''
    $sha256 = ([regex]::Match(($list -join "`n"), 'SHA256:\s*([0-9A-F:]+)')).Groups[1].Value -replace ':', ''
    "Keystore              : $Keystore"
    Show-Fingerprints $sha1 $sha256
    return
}

$file = Resolve-Path $Path
"File                  : $file"
"File SHA-256          : $((Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant())"
if ($file.Path.EndsWith('.apk', [StringComparison]::OrdinalIgnoreCase)) {
    $tools = Get-ChildItem "$sdk\build-tools" -Directory | Sort-Object { [version]($_.Name -replace '[^0-9.].*$', '') } -Descending | Select-Object -First 1
    $badging = & "$($tools.FullName)\aapt2.exe" dump badging $file
    $package = [regex]::Match(($badging -join "`n"), "package: name='([^']+)' versionCode='([^']+)' versionName='([^']+)'")
    "Package name          : $($package.Groups[1].Value)"
    "Version               : $($package.Groups[3].Value) (versionCode $($package.Groups[2].Value))"
    "Min / target SDK      : $([regex]::Match(($badging -join "`n"), "minSdkVersion:'(\d+)'").Groups[1].Value) / $([regex]::Match(($badging -join "`n"), "targetSdkVersion:'(\d+)'").Groups[1].Value)"
    $certs = & "$($tools.FullName)\apksigner.bat" verify --print-certs $file
    "Certificate           : $(([regex]::Match(($certs -join "`n"), 'certificate DN: (.+)')).Groups[1].Value.Trim())"
    Show-Fingerprints ([regex]::Match(($certs -join "`n"), 'SHA-1 digest: ([0-9a-f]+)').Groups[1].Value) ([regex]::Match(($certs -join "`n"), 'SHA-256 digest: ([0-9a-f]+)').Groups[1].Value)
}
else {
    # An AAB carries a JAR signature that keytool can read.
    $print = & $keytool -printcert -jarfile $file
    "Certificate           : $(([regex]::Match(($print -join "`n"), 'Owner: (.+)')).Groups[1].Value.Trim())"
    Show-Fingerprints (([regex]::Match(($print -join "`n"), 'SHA1:\s*([0-9A-F:]+)')).Groups[1].Value -replace ':', '') (([regex]::Match(($print -join "`n"), 'SHA256:\s*([0-9A-F:]+)')).Groups[1].Value -replace ':', '')
}
