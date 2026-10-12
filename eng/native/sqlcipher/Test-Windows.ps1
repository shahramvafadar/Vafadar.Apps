<#
.SYNOPSIS Runs the original schema and key/tamper checks against an independently built candidate.
.DESCRIPTION Never opens an app database. Evidence and fictitious encrypted files remain under owned artifacts.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.librarySha256 -ne (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()) {
    throw 'Compatibility input differs from the candidate manifest.'
}
$project = Join-Path $PSScriptRoot 'Compatibility/Vafadar.SqlCipher.Compatibility.csproj'
$evidence = Join-Path $root ('compatibility-' + [Guid]::NewGuid().ToString('N'))
& dotnet run --project $project -c Release -p:ContinuousIntegrationBuild=true "-p:NativeSqlCipherPath=$library" -- $evidence
if ($LASTEXITCODE -ne 0) { throw 'Original schema/native compatibility failed.' }
$proof = Get-Content -LiteralPath (Join-Path $evidence 'ef-proof.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $proof.WrongMissingKeyAndTamperRefused -or -not $proof.ActualCompiledModelUsed -or
    -not $proof.ForeignKeysRemainEnabled -or -not $proof.CorrectKeyReopenUnicodeAndOriginalMoneyDate) {
    throw 'Required original schema/native observations are missing.'
}
[ordered]@{ nativeSha256=$manifest.librarySha256; evidenceDirectory=$evidence; candidateOnly=$true } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'compatibility-proof.json') -Encoding UTF8
