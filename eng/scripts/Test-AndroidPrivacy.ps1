<#
.SYNOPSIS
    Verifies the declared Android permissions of a complete Zanance APK, without reading app secrets.
.DESCRIPTION
    Checks the shipped binary rather than its source manifest. Select Offline or Cloud explicitly: Debug,
    unknown permissions and an unexpected network permission fail. This is a static boundary check, not a
    runtime traffic audit, OAuth acceptance or a store privacy declaration.
.EXAMPLE
    ./eng/scripts/Test-AndroidPrivacy.ps1 -Path artifacts/android-offline/pro.vafadar.zanance-Signed.apk -Variant Offline
#>
param(
    [Parameter(Mandatory = $true)] [string]$Path,
    [Parameter(Mandatory = $true)] [ValidateSet('Offline', 'Cloud')] [string]$Variant,
    [string]$SdkRoot)
$ErrorActionPreference = 'Stop'
$apkFile = (Resolve-Path -LiteralPath $Path).Path
if (-not $apkFile.EndsWith('.apk', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'A complete APK is required.'
}
if (-not $SdkRoot) {
    $SdkRoot = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT,
        "${env:ProgramFiles(x86)}\Android\android-sdk", "$env:LOCALAPPDATA\Android\Sdk") |
        Where-Object { $_ -and (Test-Path -LiteralPath (Join-Path $_ 'build-tools')) } |
        Select-Object -First 1
}
if (-not $SdkRoot) { throw 'No installed Android SDK found; pass -SdkRoot.' }
$buildTools = Get-ChildItem -LiteralPath (Join-Path $SdkRoot 'build-tools') -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'aapt2.exe') } |
    Sort-Object { [version]($_.Name -replace '[^0-9.].*$', '') } -Descending | Select-Object -First 1
if (-not $buildTools) { throw 'No installed aapt2 found.' }
$aapt = Join-Path $buildTools.FullName 'aapt2.exe'
$permissionLines = & $aapt dump permissions $apkFile 2>&1
if ($LASTEXITCODE -ne 0) { throw 'The APK permissions could not be read.' }
$badging = & $aapt dump badging $apkFile 2>&1
if ($LASTEXITCODE -ne 0) { throw 'The APK package information could not be read.' }
# Only extract package, permission and debug flags; do not dump arbitrary manifest metadata.
$package = [regex]::Match(($permissionLines -join "`n"), '(?m)^package: (\S+)\s*$').Groups[1].Value
$permissions = @($permissionLines | ForEach-Object {
    if ($_ -match "^uses-permission: name='([^']+)'") { $Matches[1] }
} | Sort-Object -Unique)
Import-Module (Join-Path $PSScriptRoot '../modules/AndroidPrivacy.psm1') -Force
Assert-ZananceAndroidPrivacy -Package $package -Variant $Variant -Permissions $permissions -Debuggable (($badging -join "`n") -match '(?m)^application-debuggable')
"PASS: $Variant Zanance APK permission boundary verified."
"Permissions: $($permissions -join ', ')"
"APK SHA-256: $((Get-FileHash -LiteralPath $apkFile -Algorithm SHA256).Hash.ToLowerInvariant())"
