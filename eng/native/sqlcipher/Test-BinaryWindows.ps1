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
$packageName = 'consumer-' + [Guid]::NewGuid().ToString('N')
$packed = & (Join-Path $PSScriptRoot 'Pack-Windows.ps1') -RunName $RunName -PackageName $packageName
$feed = Join-Path $inputRoot 'feed'
New-Item -ItemType Directory -Force $feed | Out-Null
$destination = Join-Path $feed ([IO.Path]::GetFileName($packed))
if (Test-Path -LiteralPath $destination) {
    if ((Get-FileHash -LiteralPath $packed).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw 'Immutable Windows package differs; bump its version.' }
} else { Copy-Item -LiteralPath $packed -Destination $destination }
& dotnet restore $project --configfile (Join-Path $PSScriptRoot 'native-package.nuget.config') *> (Join-Path $proofDirectory 'restore.log')
if ($LASTEXITCODE -ne 0) { throw 'Windows source-built package restore failed.' }
& dotnet build $project -c Release -p:ContinuousIntegrationBuild=true -p:TreatWarningsAsErrors=true --no-restore *> (Join-Path $proofDirectory 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Actual binary host build failed; do not invoke stale output.' }
$program = Join-Path $project 'bin/Release/net10.0/win-x64/Vafadar.SqlCipher.BinaryCompatibility.dll'
if ((Get-FileHash $native).Hash -ne (Get-FileHash (Join-Path (Split-Path $program) 'sqlcipher.dll')).Hash) { throw 'Copied native candidate differs.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($packed)
try {
    foreach ($file in @('licenses/ThirdPartyNotices.txt','manifest.json')) {
        $entry = $archive.GetEntry($file)
        if (-not $entry) { throw 'Native package lacks notices/provenance.' }
        $relative = if ($file.StartsWith('licenses/')) { 'native/sqlcipher/ThirdPartyNotices.txt' } else { 'native/sqlcipher/manifest.json' }
        $stream = $entry.Open(); $hash = [Security.Cryptography.SHA256]::Create()
        try { $expected = ([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-','') }
        finally { $hash.Dispose(); $stream.Dispose() }
        if ((Get-FileHash -LiteralPath (Join-Path (Split-Path $program) $relative)).Hash -ne $expected) { throw 'Actual host package notices/provenance differ.' }
    }
} finally { $archive.Dispose() }
foreach ($phase in @('create','reopen')) {
    & dotnet $program $phase $fixture *> (Join-Path $proofDirectory ($phase + '.log'))
    if ($LASTEXITCODE -ne 0) { throw 'Actual binary behavior failed; preserve same fixture and logs.' }
    $report = Get-Content (Join-Path $proofDirectory ($phase + '.log')) -Tail 1 | ConvertFrom-Json
    if (-not $report.ActualPersistenceAssemblies -or -not $report.Original32Migrations -or -not $report.NegativeMoneyPreserved -or -not $report.ActualBinaryPortableSource -or -not $report.WrongKeyRefused) { throw 'Actual binary proof fields missing.' }
}
Write-Output $proofDirectory
