# AT-76: test forbidden capabilities and configuration errors without a device, secrets or extra test modules.
param([switch]$BillingOnly)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '../modules/AndroidPrivacy.psm1') -Force
$base = @('android.permission.POST_NOTIFICATIONS', 'android.permission.RECEIVE_BOOT_COMPLETED',
    'android.permission.USE_BIOMETRIC', 'android.permission.USE_FINGERPRINT',
    'pro.vafadar.zanance.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION')
$cases = @(
    @{ Name = 'Offline permits only local capabilities'; Variant = 'Offline'; Permissions = $base; Pass = $true },
    @{ Name = 'Cloud adds network access'; Variant = 'Cloud'; Permissions = $base + 'android.permission.INTERNET'; Pass = $true },
    @{ Name = 'Offline rejects online APK'; Variant = 'Offline'; Permissions = $base + 'android.permission.INTERNET'; Pass = $false },
    @{ Name = 'Cloud rejects missing network permission'; Variant = 'Cloud'; Permissions = $base; Pass = $false },
    @{ Name = 'Location is forbidden'; Variant = 'Cloud'; Permissions = $base + 'android.permission.INTERNET' + 'android.permission.ACCESS_FINE_LOCATION'; Pass = $false },
    @{ Name = 'Advertising id is forbidden'; Variant = 'Offline'; Permissions = $base + 'com.google.android.gms.permission.AD_ID'; Pass = $false },
    @{ Name = 'Camera permission is forbidden'; Variant = 'Offline'; Permissions = $base + 'android.permission.CAMERA'; Pass = $false },
    @{ Name = 'Network state remains forbidden online'; Variant = 'Cloud'; Permissions = $base + 'android.permission.INTERNET' + 'android.permission.ACCESS_NETWORK_STATE'; Pass = $false },
    @{ Name = 'Fixture package is not the finance app'; Variant = 'Offline'; Permissions = $base; Package = 'pro.vafadar.zanance.encryptionproof'; Pass = $false },
    @{ Name = 'Debug is not a Release handoff'; Variant = 'Offline'; Permissions = $base; Debuggable = $true; Pass = $false },
    @{ Name = 'Missing reminder permission fails'; Variant = 'Offline'; Permissions = @('android.permission.RECEIVE_BOOT_COMPLETED'); Pass = $false }
)
$billing = $base + 'android.permission.INTERNET' + 'com.android.vending.BILLING'
$newCases = @(
    @{ Name = 'Billing declares exact store and network permissions'; Variant = 'Billing'; Permissions = $billing; Pass = $true },
    @{ Name = 'Billing refuses missing store permission'; Variant = 'Billing'; Permissions = $base + 'android.permission.INTERNET'; Pass = $false },
    @{ Name = 'Billing refuses missing network permission'; Variant = 'Billing'; Permissions = $base + 'com.android.vending.BILLING'; Pass = $false },
    @{ Name = 'Billing refuses location'; Variant = 'Billing'; Permissions = $billing + 'android.permission.ACCESS_FINE_LOCATION'; Pass = $false },
    @{ Name = 'Billing refuses advertising id'; Variant = 'Billing'; Permissions = $billing + 'com.google.android.gms.permission.AD_ID'; Pass = $false },
    @{ Name = 'Billing refuses network state'; Variant = 'Billing'; Permissions = $billing + 'android.permission.ACCESS_NETWORK_STATE'; Pass = $false },
    @{ Name = 'Cloud still refuses a billing APK'; Variant = 'Cloud'; Permissions = $billing; Pass = $false },
    @{ Name = 'Offline still refuses store permission'; Variant = 'Offline'; Permissions = $base + 'com.android.vending.BILLING'; Pass = $false },
    @{ Name = 'Billing refuses debugger packaging'; Variant = 'Billing'; Permissions = $billing; Debuggable = $true; Pass = $false }
)
$cases = if ($BillingOnly) { $newCases } else { $cases + $newCases }
foreach ($case in $cases) {
    $package = 'pro.vafadar.zanance'
    if ($case.Package) { $package = $case.Package }
    $passed = $true
    try { Assert-ZananceAndroidPrivacy -Package $package -Variant $case.Variant -Permissions $case.Permissions -Debuggable ([bool]$case.Debuggable) }
    catch { $passed = $false }
    if ($passed -ne $case.Pass) { throw ('Privacy policy regression: ' + $case.Name) }
}
"PASS: $($cases.Count) Android privacy policy cases (AT-76)."
