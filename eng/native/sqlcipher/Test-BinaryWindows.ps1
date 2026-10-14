<# .SYNOPSIS Verifies actual production assemblies against the independently built Windows cipher candidate. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-d')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$inputRoot = Get-NativeInputRoot
$native = Join-Path $inputRoot ('build/' + $RunName + '/candidate/runtimes/win-x64/native/sqlcipher.dll')
$project = Join-Path $PSScriptRoot 'BinaryCompatibility'
$proofDirectory = Join-Path $inputRoot ('binary-host/' + [Guid]::NewGuid().ToString('N'))
$fixture = Join-Path $proofDirectory 'fixture'
New-Item -ItemType Directory -Force $proofDirectory | Out-Null
& dotnet build $project -c Release -p:ContinuousIntegrationBuild=true "-p:NativeSqlCipherPath=$native" *> (Join-Path $proofDirectory 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Actual binary host build failed; do not invoke stale output.' }
$program = Join-Path $project 'bin/Release/net10.0/Vafadar.SqlCipher.BinaryCompatibility.dll'
if ((Get-FileHash $native).Hash -ne (Get-FileHash (Join-Path (Split-Path $program) 'sqlcipher.dll')).Hash) { throw 'Copied native candidate differs.' }
foreach ($phase in @('create','reopen')) {
    & dotnet $program $phase $fixture *> (Join-Path $proofDirectory ($phase + '.log'))
    if ($LASTEXITCODE -ne 0) { throw 'Actual binary behavior failed; preserve same fixture and logs.' }
    $report = Get-Content (Join-Path $proofDirectory ($phase + '.log')) -Tail 1 | ConvertFrom-Json
    if (-not $report.ActualPersistenceAssemblies -or -not $report.Original32Migrations -or -not $report.NegativeMoneyPreserved -or -not $report.ActualBinaryPortableSource -or -not $report.WrongKeyRefused) { throw 'Actual binary proof fields missing.' }
}
Write-Output $proofDirectory
