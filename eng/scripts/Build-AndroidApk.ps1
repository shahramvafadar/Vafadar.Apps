<#
.SYNOPSIS
    Builds an Android APK that can be copied to a phone and installed directly (side-loading).

.DESCRIPTION
    The APK in bin\Debug is made for Visual Studio's Fast Deployment: the app's assemblies are not inside it but are
    copied to the device while debugging. Installed on its own it stops at once ("No assemblies found"). This script
    builds a complete APK instead – by default the Release build, as users get it (trimmed, no network permission
    without cloud backup clients), signed with the local debug key unless signing properties are passed.

.EXAMPLE
    ./eng/scripts/Build-AndroidApk.ps1
    ./eng/scripts/Build-AndroidApk.ps1 -Configuration Debug
#>
param(
    [ValidateSet('Release', 'Debug')] [string]$Configuration = 'Release',
    [string]$Project = 'src\Apps\Zanance\Vafadar.Zanance.App\Vafadar.Zanance.App.csproj',
    [string]$Output = (Join-Path $PSScriptRoot '..\..\artifacts\android'))
$ErrorActionPreference = 'Stop'
Set-Location (Resolve-Path (Join-Path $PSScriptRoot '..\..'))
New-Item -ItemType Directory -Force $Output | Out-Null

# EmbedAssembliesIntoApk makes a Debug APK complete as well; Release always embeds them.
dotnet publish $Project -c $Configuration -f net10.0-android -p:VafadarMauiTargetFrameworks=net10.0-android `
    -p:AndroidPackageFormat=apk -p:EmbedAssembliesIntoApk=true -o $Output -nologo
if ($LASTEXITCODE -ne 0) { throw "The build failed ($LASTEXITCODE)." }

$apk = Get-ChildItem $Output -Filter '*-Signed.apk' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
"APK: $($apk.FullName) ($([math]::Round($apk.Length / 1MB, 1)) MB)"
'Copy it to the phone and open it there (allow installing from this source once), or: adb install -r "<path>"'
