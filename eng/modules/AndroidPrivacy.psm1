# Permission policy is separate from APK inspection so forbidden capabilities can be regression-tested.
function Assert-ZananceAndroidPrivacy {
    <#
    .SYNOPSIS
        Rejects an APK permission set that violates its explicitly selected Release variant.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Package,
        [Parameter(Mandatory = $true)] [ValidateSet('Offline', 'Cloud')] [string]$Variant,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]]$Permissions,
        [bool]$Debuggable)
    if ($Package -ne 'pro.vafadar.zanance') { throw 'The Zanance application package is required.' }
    if ($Debuggable) { throw 'A non-debuggable Release APK is required.' }
    $allowed = @('android.permission.POST_NOTIFICATIONS', 'android.permission.RECEIVE_BOOT_COMPLETED',
        'android.permission.USE_BIOMETRIC', 'android.permission.USE_FINGERPRINT',
        'pro.vafadar.zanance.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION')
    if ($Variant -eq 'Cloud') { $allowed += 'android.permission.INTERNET' }
    $unexpected = @($Permissions | Where-Object { $_ -notin $allowed })
    if ($unexpected.Count -gt 0) { throw ('Unexpected permissions: ' + ($unexpected -join ', ')) }
    if ($Variant -eq 'Cloud' -and 'android.permission.INTERNET' -notin $Permissions) {
        throw 'The Cloud variant must declare INTERNET; check the build configuration.'
    }
    foreach ($required in @('android.permission.POST_NOTIFICATIONS', 'android.permission.RECEIVE_BOOT_COMPLETED')) {
        if ($required -notin $Permissions) { throw ('Missing reminder permission: ' + $required) }
    }
}
Export-ModuleMember -Function Assert-ZananceAndroidPrivacy
