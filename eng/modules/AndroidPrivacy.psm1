# Permission policy is separate from APK inspection so forbidden capabilities can be regression-tested.
$permissionPolicy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'zanance-android-permissions.json') -Raw -ErrorAction Stop |
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
        [bool]$Debuggable)
    if ($Package -ne 'pro.vafadar.zanance') { throw 'The Zanance application package is required.' }
    if ($Debuggable) { throw 'A non-debuggable Release APK is required.' }
    $edition = switch ($Variant) {
        Offline { 'offline' }
        Cloud { 'cloud-backup' }
        Billing { 'billing' }
    }
    $required = $permissionPolicy.required + $permissionPolicy.editions[$edition]
    $allowed = $required + $permissionPolicy.optional
    $unexpected = @($Permissions | Where-Object { $_ -cnotin $allowed })
    if ($unexpected.Count -gt 0) { throw ('Unexpected permissions: ' + ($unexpected -join ', ')) }
    $missing = @($required | Where-Object { $_ -cnotin $Permissions })
    if ($missing.Count -gt 0) { throw ('Missing required permissions: ' + ($missing -join ', ')) }
}
Export-ModuleMember -Function Assert-ZananceAndroidPrivacy
