<#
.SYNOPSIS Rejects native diagnostic/source drift while preserving unresolved upstream warnings.
.DESCRIPTION Reads owned producer evidence only. Does not suppress warnings, accept their safety or rebuild a binary.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'WindowsWarnings.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$baselinePath = Join-Path $PSScriptRoot 'windows.warnings.baseline.json'
$baseline = Get-Content -LiteralPath $baselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$sourceCommits = @{}; foreach ($source in $manifest.sources) { $sourceCommits[$source.name] = $source.commit }
if ($baseline.sourceCommit -ne $inputLock.sources.openssl.commit -or $baseline.sqlcipherCommit -ne $inputLock.sources.sqlcipher.commit -or
    $sourceCommits['openssl'] -ne $baseline.sourceCommit -or $sourceCommits['sqlcipher'] -ne $baseline.sqlcipherCommit -or
    $baseline.cryptoProfileSha256 -ne $manifest.cryptoProfileSha256 -or
    $baseline.cryptoProfileSha256 -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'windows.crypto.profile.json') -Algorithm SHA256).Hash.ToLowerInvariant()) {
    throw 'The diagnostic baseline does not belong to this source/profile; review the new dependency before qualification.'
}
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
if ((Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.librarySha256) { throw 'Native candidate differs from its producer manifest.' }
$sourceCount = Test-WindowsWarningSources -Root $root -Baseline $baseline
# Resume preserves earlier logs with opaque GUID suffixes. Require identical command recipes before
# combining them; never lose a warning merely because its object was compiled before interruption.
$logs = @(Get-WindowsWarningLogHistory -Root $root)
$rows = @(foreach ($log in $logs) { Get-WindowsWarningRows -Lines @(Get-Content -LiteralPath $log.FullName -Encoding UTF8) })
$result = Test-WindowsWarningSet -Baseline $baseline -Rows $rows -ResumedHistory:($logs.Count -gt 1)
$evidence = Join-Path $root ('warning-review-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $evidence | Out-Null
[ordered]@{ schemaVersion=1; scope='upstream diagnostic drift only'; securitySafetyAcceptance=$false;
    nativeSha256=$manifest.librarySha256; baselineSha256=(Get-FileHash -LiteralPath $baselinePath -Algorithm SHA256).Hash.ToLowerInvariant();
    sourceCommit=$baseline.sourceCommit; cryptoProfileSha256=$baseline.cryptoProfileSha256; sourceFiles=$sourceCount;
    diagnostics=$result; logs=@($logs | ForEach-Object { @{name=$_.Name;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()} })
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidence 'proof.json') -Encoding UTF8
Write-Output (Join-Path $evidence 'proof.json')
