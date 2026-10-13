<# .SYNOPSIS Verifies actual protected startup using independent Windows keys, files and application migrations. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64',
    [switch]$UseExistingBuild)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.librarySha256 -ne (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()) {
    throw 'Startup input differs from reviewed native candidate.'
}
$project = Join-Path $PSScriptRoot 'Compatibility/Vafadar.SqlCipher.Compatibility.csproj'
if (-not $UseExistingBuild) {
    & dotnet build $project -c Release -p:ContinuousIntegrationBuild=true "-p:NativeSqlCipherPath=$library" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Independent startup caller build failed.' }
}
$copied = Join-Path $PSScriptRoot 'Compatibility/bin/Release/net10.0/sqlcipher.dll'
if ((Get-FileHash -LiteralPath $copied -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.librarySha256) {
    throw 'Startup caller carries another native candidate.'
}
$proofs = @()
foreach ($scenario in @('fresh-startup', 'startup')) {
    $folder = Join-Path $root ($scenario + '-proof-' + [Guid]::NewGuid().ToString('N'))
    $created = & dotnet run --project $project -c Release --no-build "-p:NativeSqlCipherPath=$library" -- ($scenario + '-create') $folder
    if ($LASTEXITCODE -ne 0) { throw 'Independent initial startup failed; retain its captured state.' }
    $reopened = & dotnet run --project $project -c Release --no-build "-p:NativeSqlCipherPath=$library" -- ($scenario + '-reopen') $folder
    if ($LASTEXITCODE -ne 0) { throw 'Independent cold startup failed; retain its captured state.' }
    $first = ($created | Out-String) | ConvertFrom-Json
    $cold = ($reopened | Out-String) | ConvertFrom-Json
    if ($scenario -eq 'fresh-startup') {
        if (-not $first.ActualFreshStartup -or -not $first.NoPlaintextOriginal -or -not $first.NoManufacturedAccount -or
            $cold.RetainedOpeningBalance -ne 321 -or -not $cold.MissingIdentityRefused) { throw 'Fresh startup evidence differs.' }
    } elseif (-not $first.AllProfilesBeforeSinglePublication -or $first.SelectedWorkAmount -ne 880 -or
        -not $cold.ColdExistingKeysOnly -or -not $cold.NoRepeatedKeyResolution) { throw 'Existing profile startup evidence differs.' }
    $proofs += [ordered]@{ Scenario = $scenario; Directory = $folder; Created = $first; Reopened = $cold }
}
$proof = Join-Path $root ('windows-startup-proof-' + [Guid]::NewGuid().ToString('N') + '.json')
$proofs | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $proof -Encoding UTF8
Write-Output $proof
