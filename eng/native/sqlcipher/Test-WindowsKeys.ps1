<# .SYNOPSIS Verifies original protected key code and encrypted data reopen across two Windows processes with fictitious files. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.librarySha256 -ne (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()) {
    throw 'Protected-key input differs from reviewed native candidate.'
}
$evidence = Join-Path $root ('key-proof-' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $PSScriptRoot 'Compatibility/Vafadar.SqlCipher.Compatibility.csproj'
& dotnet run --project $project -c Release -p:ContinuousIntegrationBuild=true "-p:NativeSqlCipherPath=$library" -- --create-key-proof $evidence
if ($LASTEXITCODE -ne 0) { throw 'Fictitious protected-key creation failed.' }
& dotnet run --project $project -c Release --no-build "-p:NativeSqlCipherPath=$library" -- --reopen-key-proof $evidence
if ($LASTEXITCODE -ne 0) { throw 'Protected-key data reopen in a new process failed.' }
Write-Output (Join-Path $evidence 'key-reopen-proof.json')
