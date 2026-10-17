<# .SYNOPSIS Refuses immutable package identity reuse and unreviewed source-version publication. #>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$lock = [pscustomobject]@{ sources=[pscustomobject]@{ sqlcipher=[pscustomobject]@{ tag='v4.19.0' } } }
$known = @([pscustomobject]@{ version='4.19.0-candidate.5' },[pscustomobject]@{ version='4.19.0-candidate.6' })
$passed = 0
if ((Resolve-NativeProducerVersion -InputLock $lock -Contents $known[0] -KnownContents $known) -cne $known[0].version) {
    throw 'Ordinary consumer packaging changed its immutable identity.'
}
$passed++
if ((Resolve-NativeProducerVersion -InputLock $lock -Contents $known[0] -KnownContents $known -CandidateVersion '4.19.0-candidate.7') -cne '4.19.0-candidate.7') {
    throw 'A fresh prospective identity was lost.'
}
$passed++
foreach ($version in @('4.19.0-candidate.5','4.19.0-candidate.6','4.19.0-candidate.4',
    '4.19.0-candidate.07','4.18.0-candidate.7','4.20.0-candidate.7','4.19.0-candidate.7/other',
    '4.19.0-candidate.9223372036854775808')) {
    $refused = $false
    try { Resolve-NativeProducerVersion -InputLock $lock -Contents $known[0] -KnownContents $known -CandidateVersion $version | Out-Null }
    catch { $refused = $true }
    if (-not $refused) { throw ('Unsafe producer identity was accepted: ' + $version) }
    $passed++
}
$newSource = [pscustomobject]@{ sources=[pscustomobject]@{ sqlcipher=[pscustomobject]@{ tag='v4.20.0' } } }
if ((Resolve-NativeProducerVersion -InputLock $newSource -Contents $known[0] -KnownContents $known -CandidateVersion '4.20.0-candidate.1') -cne '4.20.0-candidate.1') {
    throw 'A separately reviewed stable source cannot qualify its own fresh candidate.'
}
$passed++
$beta = [pscustomobject]@{ sources=[pscustomobject]@{ sqlcipher=[pscustomobject]@{ tag='v5.0.0-beta' } } }
$refused = $false
try { Resolve-NativeProducerVersion -InputLock $beta -Contents $known[0] -KnownContents $known -CandidateVersion '5.0.0-beta-candidate.1' | Out-Null }
catch { $refused = $true }
if (-not $refused) { throw 'A beta source was accepted as stable production qualification.' }
$passed++
[ordered]@{ passed=$passed; immutableIdentityReuseRefused=$true; consumerFeedChanged=$false } | ConvertTo-Json
