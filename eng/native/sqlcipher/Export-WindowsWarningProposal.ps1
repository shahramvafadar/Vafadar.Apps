<#
.SYNOPSIS Exports a proposed diagnostic inventory from a fresh producer without changing the reviewed baseline.
.DESCRIPTION Requires the already restored official Git objects; never fetches, modifies upstream or approves warning safety.
#>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'WindowsWarnings.psm1') -Force
$inputLock = Get-Content (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$manifest = Get-Content (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$profileHash = (Get-FileHash (Join-Path $PSScriptRoot 'windows.crypto.profile.json') -Algorithm SHA256).Hash.ToLowerInvariant()
foreach ($name in @('openssl','sqlcipher')) {
    $records = @($manifest.sources | Where-Object name -eq $name)
    if ($records.Count -ne 1 -or $records[0].commit -cne $inputLock.sources.$name.commit) {
        throw 'Proposal producer does not match the currently locked official source.'
    }
}
if ($manifest.cryptoProfileSha256 -cne $profileHash -or
    (Get-FileHash (Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll') -Algorithm SHA256).Hash.ToLowerInvariant() -cne $manifest.librarySha256) {
    throw 'Proposal producer profile or native bytes differ from its manifest.'
}
$logs = @(Get-WindowsWarningLogHistory -Root $root)
if ($logs.Count -ne 1) { throw 'Export a proposal from a complete fresh producer, not resumed partial compilations.' }
$rows = @(Get-WindowsWarningRows -Lines @(Get-Content -LiteralPath $logs[0].FullName -Encoding UTF8))
if (-not $rows.Count) { throw 'No diagnostics found; review a warning-free producer separately rather than replacing the baseline.' }
$gitRoot = Join-Path (Get-NativeInputRoot) ('source-git/openssl-' + $inputLock.sources.openssl.commit)
if (-not (Test-Path -LiteralPath $gitRoot -PathType Container)) { throw 'Restore the locked official source through the native build first.' }
$tree = @(& git -C $gitRoot ls-tree -r $inputLock.sources.openssl.commit)
if ($LASTEXITCODE -ne 0) { throw 'Cannot read the locked official OpenSSL tree.' }
$blobs = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($line in $tree) {
    if ($line -match '^100644 blob ([0-9a-f]{40})\t(.+)$') { $blobs.Add(('openssl/' + $Matches[2]), $Matches[1]) }
}
$files = @(foreach ($path in @($rows.path | Sort-Object -Unique)) {
    if (-not $blobs.ContainsKey($path)) { throw 'Diagnostic source is absent from the locked official tree.' }
    $file = Join-Path $root $path
    # Hash actual source bytes without Git line-ending conversion or checkout filters.
    $blob = & git hash-object --no-filters -- $file
    if ($LASTEXITCODE -ne 0 -or $blob -cne $blobs[$path]) { throw 'Diagnostic source differs from the official Git object.' }
    [ordered]@{ path=$path; gitBlob=$blob; sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() }
})
$groups = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
foreach ($row in $rows) {
    $key = $row.path + '|' + $row.line + '|' + $row.code + '|' + $row.message
    if ($groups.ContainsKey($key)) { $groups[$key].occurrences++ }
    else { $groups.Add($key, [pscustomobject]@{path=$row.path; line=$row.line; code=$row.code; message=$row.message; occurrences=1}) }
}
$proposal = [pscustomobject][ordered]@{ schemaVersion=1; reviewedOn=[DateTime]::UtcNow.ToString('yyyy-MM-dd');
    scope='Proposed upstream diagnostic drift inventory only; requires explicit source and diagnostic review';
    securitySafetyAcceptance=$false; sourceCommit=$inputLock.sources.openssl.commit; sqlcipherCommit=$inputLock.sources.sqlcipher.commit;
    cryptoProfileSha256=$profileHash;
    expectedOccurrences=$rows.Count; sourceFiles=$files; diagnostics=@($groups.Values | Sort-Object path,line,code,message) }
Test-WindowsWarningSources -Root $root -Baseline $proposal | Out-Null
Test-WindowsWarningSet -Baseline $proposal -Rows $rows | Out-Null
$output = Join-Path (Get-NativeInputRoot) ('warning-proposal-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$path = Join-Path $output 'proposed-baseline.json'
$proposal | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $path -Encoding UTF8
Write-Output $path
