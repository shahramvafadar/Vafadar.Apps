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
$projectDotNet = Join-Path $PSScriptRoot 'Invoke-ProjectDotNet.ps1'

# Restore must not silently fall back to the original Core assembly when its density correction is required.
# Preparation verifies a completed package or builds its pinned source in repository-owned state.
& (Join-Path $PSScriptRoot '../maui/Ensure-ConfigurationMetrics.ps1')

# Restore for Android alone first: after a build for all platforms (e.g. Windows from Visual Studio) the assets file
# lists other target frameworks, and publish would stop with NETSDK1005 instead of restoring again. Visual Studio with
# the solution open restores in the background too and can overwrite it meanwhile (NETSDK1005, APT2126): then the
# build is tried once more.
$outputDirectory = (Get-Item -LiteralPath $Output).FullName
$publishAttempts = [Collections.Generic.List[string]]::new()
try {
    for ($attempt = 1; $attempt -le 2; $attempt++) {
        $log = & $projectDotNet restore $Project -p:Configuration=$Configuration -p:VafadarMauiTargetFrameworks=net10.0-android -p:ContinuousIntegrationBuild=true @cloudProperties -nologo 2>&1
        if ($LASTEXITCODE -eq 0) {
            # Each publish attempt must emit its own artifact, even when retrying a failed publish.
            $publishPath = Join-Path $outputDirectory ([IO.Path]::GetRandomFileName())
            New-Item -ItemType Directory -Path $publishPath | Out-Null
            $publishAttempts.Add($publishPath)
            # EmbedAssembliesIntoApk makes a Debug APK complete as well; Release always embeds them.
            $log = & $projectDotNet publish $Project -c $Configuration -f net10.0-android -p:VafadarMauiTargetFrameworks=net10.0-android `
                -p:AndroidPackageFormat=apk -p:EmbedAssembliesIntoApk=true -p:ContinuousIntegrationBuild=true @cloudProperties -o $publishPath -nologo --no-restore 2>&1
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

    # D-729: a successful publish exit is not a usable handoff. Never report a missing,
    # empty or arbitrarily selected package from a mixed application output directory.
    $apks = @(Get-ChildItem -LiteralPath $publishPath -Filter '*-Signed.apk' -File)
    if ($apks.Count -ne 1) {
        throw "Expected exactly one signed APK from this publish; found $($apks.Count)."
    }
    $apk = $apks[0]
    if ($apk.Length -eq 0) {
        throw 'The signed APK is empty; no installable handoff is available.'
    }
    $verified = & (Join-Path $PSScriptRoot 'Assert-AndroidApk.ps1') -ApkPath $apk.FullName -Project $Project `
        -Configuration $Configuration -BuildProperties $cloudProperties -Offline:$Offline

    $otherPackages = @(Get-ChildItem -LiteralPath $outputDirectory -Filter '*-Signed.apk' -File |
        Where-Object { $_.Name -ine $apk.Name })
    if ($otherPackages.Count -gt 0) {
        throw 'The output directory contains another signed APK. Use a separate output directory for this application.'
    }
    $handoffPath = Join-Path $outputDirectory $apk.Name
    [IO.File]::Move($apk.FullName, $handoffPath, $true)
    $apk = Get-Item -LiteralPath $handoffPath
}
finally {
    foreach ($publishPath in $publishAttempts) {
        $owned = Get-Item -LiteralPath $publishPath -ErrorAction SilentlyContinue
        if ($owned.PSIsContainer -and $owned.Parent.FullName -eq $outputDirectory -and
            -not ($owned.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            Remove-Item -LiteralPath $owned.FullName -Recurse -Force
        }
    }
}
"APK: $($apk.FullName) ($([math]::Round($apk.Length / 1MB, 1)) MB)"
"Package: $($verified.Package)"
"SHA-256: $($verified.Sha256)"
'Copy it to the phone and open it there (allow installing from this source once), or: adb install -r "<path>"'
