# APK and Play bundle checks share the qualified SDK range and permission policy.
$androidPolicy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'zanance-android-permissions.json') -Raw -ErrorAction Stop |
    ConvertFrom-Json -AsHashtable

function Assert-ZananceAndroidPrivacy {
    <#
    .SYNOPSIS
        Rejects an APK permission set that violates its explicitly selected Release variant.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Package,
        [Parameter(Mandatory = $true)] [ValidateSet('Offline', 'Cloud', 'Billing')] [string]$Variant,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]]$Permissions,
        [bool]$Debuggable,
        [AllowEmptyCollection()] [string[]]$UnrestrictedPermissions = $Permissions)
    if ($Package -ne 'pro.vafadar.zanance') { throw 'The Zanance application package is required.' }
    if ($Debuggable) { throw 'A non-debuggable Release APK is required.' }
    $edition = switch ($Variant) {
        Offline { 'offline' }
        Cloud { 'cloud-backup' }
        Billing { 'billing' }
    }
    $required = $androidPolicy.required + $androidPolicy.editions[$edition]
    $allowed = $required + $androidPolicy.optional
    $unexpected = @($Permissions | Where-Object { $_ -cnotin $allowed })
    if ($unexpected.Count -gt 0) { throw ('Unexpected permissions: ' + ($unexpected -join ', ')) }
    $missing = @($required | Where-Object { $_ -cnotin $UnrestrictedPermissions })
    if ($missing.Count -gt 0) { throw ('Missing required permissions: ' + ($missing -join ', ')) }
}

function Assert-ZananceAndroidReleaseBadging {
    <#
    .SYNOPSIS
        Checks the qualified SDK range and expected edition permissions on the signed handoff.
    #>
    param(
        [Parameter(Mandatory = $true)] [string[]]$Badging,
        [Parameter(Mandatory = $true)] [ValidateSet('Offline', 'Cloud', 'Billing')] [string]$Variant)
    $minimum = @($Badging -cmatch '^minSdkVersion:')
    if ($minimum.Count -ne 1 -or $minimum[0] -cne "minSdkVersion:'$($androidPolicy.minimumSdk)'") {
        throw "The Zanance Release APK must declare Android minimum API $($androidPolicy.minimumSdk)."
    }
    if ($Badging -cmatch '^maxSdkVersion:') { throw 'The Zanance Release APK must not declare a maximum SDK.' }
    $target = @($Badging -cmatch '^targetSdkVersion:')
    if ($target.Count -ne 1 -or $target[0] -cnotmatch "^targetSdkVersion:'([0-9]+)'$" -or
        [int]$Matches[1] -lt $androidPolicy.playMinimumTargetSdk) {
        throw "The Zanance Release APK must target Android API $($androidPolicy.playMinimumTargetSdk) or higher."
    }
    $permissions = [Collections.Generic.List[string]]::new()
    $unrestricted = [Collections.Generic.List[string]]::new()
    foreach ($line in $Badging) {
        if ($line -cmatch "^(uses-permission(?:-sdk-[0-9]+)?): name='([^']+)'(.*)$") {
            $name = $Matches[2]
            $permissions.Add($name)
            if ($Matches[1] -ceq 'uses-permission' -and $Matches[3] -cnotmatch '\bmaxSdkVersion=') {
                $unrestricted.Add($name)
            }
        }
    }
    Assert-ZananceAndroidPrivacy -Package 'pro.vafadar.zanance' -Variant $Variant -Permissions $permissions -UnrestrictedPermissions $unrestricted
}

Export-ModuleMember -Function Assert-ZananceAndroidPrivacy, Assert-ZananceAndroidReleaseBadging
