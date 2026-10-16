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
    ./eng/scripts/Build-AndroidApk.ps1 -Offline -Output artifacts/android-offline
    ./eng/scripts/Build-AndroidApk.ps1 -PersonalSync -Output artifacts/android-personal-sync
    ./eng/scripts/Build-AndroidApk.ps1 -Billing -BillingSandbox -Output artifacts/android-billing-sandbox
#>
param(
    [ValidateSet('Release', 'Debug')] [string]$Configuration = 'Release',
    [string]$Project = 'src\Apps\Zanance\Vafadar.Zanance.App\Vafadar.Zanance.App.csproj',
    [string]$Output = (Join-Path $PSScriptRoot '..\..\artifacts\android'),
    [ValidateSet('', 'pro.vafadar.zanance.householdlifecycle')] [string]$ReviewApplicationId = '',
    [switch]$PersonalSync,
    [switch]$Billing,
    [switch]$BillingSandbox,
    [switch]$Offline)
$ErrorActionPreference = 'Stop'
if ($Offline -and ($PersonalSync -or $Billing)) {
    throw 'An offline APK cannot enable personal synchronization or store billing.'
}
if ($BillingSandbox -and -not $Billing) {
    throw 'The billing sandbox requires the explicit billing edition.'
}
if ($Offline -and $Configuration -ne 'Release') {
    throw 'Offline privacy verification requires Release; Debug includes debugger network access.'
}
# Empty global properties override local provider configuration without reading, logging or changing it.
# Explicit edition properties outrank ambient environment/MSBuild defaults. In particular, -Offline
# must not inherit a network-enabled sync edition from a previous local build.
$cloudProperties = @(
    ('-p:ZanancePersonalSyncEnabled=' + $PersonalSync.IsPresent.ToString().ToLowerInvariant()),
    ('-p:ZananceBillingEnabled=' + $Billing.IsPresent.ToString().ToLowerInvariant()),
    ('-p:ZananceBillingSandbox=' + $BillingSandbox.IsPresent.ToString().ToLowerInvariant()))
if ($ReviewApplicationId) {
    if ($Configuration -ne 'Debug') { throw 'Independent review identity is Debug-only; Release keeps the permanent product identity.' }
    $cloudProperties += ('-p:ApplicationId=' + $ReviewApplicationId)
}
if ($Offline) {
    $cloudProperties += @('-p:MicrosoftEntraClientId=', '-p:GoogleOAuthClientIdAndroid=',
        '-p:GoogleOAuthClientIdIos=', '-p:GoogleOAuthClientIdWindows=')
}
Set-Location (Resolve-Path (Join-Path $PSScriptRoot '..\..'))
New-Item -ItemType Directory -Force $Output | Out-Null

# Restore for Android alone first: after a build for all platforms (e.g. Windows from Visual Studio) the assets file
# lists other target frameworks, and publish would stop with NETSDK1005 instead of restoring again. Visual Studio with
# the solution open restores in the background too and can overwrite it meanwhile (NETSDK1005, APT2126): then the
# build is tried once more.
for ($attempt = 1; $attempt -le 2; $attempt++) {
    $log = dotnet restore $Project -p:VafadarMauiTargetFrameworks=net10.0-android -p:ContinuousIntegrationBuild=true @cloudProperties -nologo 2>&1
    if ($LASTEXITCODE -eq 0) {
        # EmbedAssembliesIntoApk makes a Debug APK complete as well; Release always embeds them.
        $log = dotnet publish $Project -c $Configuration -f net10.0-android -p:VafadarMauiTargetFrameworks=net10.0-android `
            -p:AndroidPackageFormat=apk -p:EmbedAssembliesIntoApk=true -p:ContinuousIntegrationBuild=true @cloudProperties -o $Output -nologo --no-restore 2>&1
    }

    if ($LASTEXITCODE -eq 0 -or $attempt -eq 2 -or -not ($log | Select-String -Pattern 'NETSDK1005|APT2126' -Quiet)) {
        break
    }

    Write-Host 'The restore was changed meanwhile (Visual Studio open?); building once more.'
}
if ($LASTEXITCODE -ne 0) {
    # Show the errors, not only the exit code, even when the caller keeps just the last lines.
    $log | Select-String -Pattern ': error ' | Select-Object -ExpandProperty Line -Unique | Write-Host
    throw "The build failed ($LASTEXITCODE). The errors are listed above."
}

# CI treats warnings as errors; list them so they are fixed before pushing.
$warnings = $log | Select-String -Pattern ': warning ' | Select-Object -ExpandProperty Line -Unique
if ($warnings) {
    throw "The APK build produced warnings; resolve them before handing it off."
}

$apk = Get-ChildItem $Output -Filter '*-Signed.apk' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
"APK: $($apk.FullName) ($([math]::Round($apk.Length / 1MB, 1)) MB)"
'Copy it to the phone and open it there (allow installing from this source once), or: adb install -r "<path>"'
