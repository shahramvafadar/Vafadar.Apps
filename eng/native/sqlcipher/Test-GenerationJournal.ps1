<# .SYNOPSIS Checks actual native hot-journal rollback and committed selection after external process termination. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.librarySha256 -ne (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()) {
    throw 'Journal input differs from reviewed native candidate.'
}
$projectRoot = Join-Path $PSScriptRoot 'Compatibility'
& dotnet build (Join-Path $projectRoot 'Vafadar.SqlCipher.Compatibility.csproj') -c Release -p:ContinuousIntegrationBuild=true "-p:NativeSqlCipherPath=$library" *> (Join-Path $root 'journal-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Independent journal build failed.' }
$dll = Join-Path $projectRoot 'bin/Release/net10.0/Vafadar.SqlCipher.Compatibility.dll'
$evidence = Join-Path $root ('journal-proof-' + [Guid]::NewGuid().ToString('N'))
$dotnet = (Get-Command dotnet).Source
$reports = @()
foreach ($phase in @('journal-create','journal-uncommitted','journal-reopen-rollback','journal-committed','journal-reopen-selected')) {
    $stdout = Join-Path $root ($phase + '-stdout.log')
    $stderr = Join-Path $root ($phase + '-stderr.log')
    $process = Start-Process -FilePath $dotnet -ArgumentList @(('"' + $dll + '"'),$phase,('"' + $evidence + '"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    try {
        if ($phase -in @('journal-uncommitted','journal-committed')) {
            $ready = Join-Path $evidence ($(if ($phase -eq 'journal-uncommitted') { 'ready-uncommitted.json' } else { 'ready-committed.json' }))
            for ($attempt=0; $attempt -lt 100 -and -not (Test-Path -LiteralPath $ready); $attempt++) {
                if ($process.HasExited) { throw 'Native writer exited before the requested kill barrier.' }
                Start-Sleep -Milliseconds 200
            }
            if (-not (Test-Path -LiteralPath $ready) -or $process.HasExited) { throw 'Live native writer did not reach its kill barrier.' }
            $report = Get-Content -LiteralPath $ready -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($phase -eq 'journal-uncommitted' -and -not $report.HotJournalFlushed) { throw 'No actual hot journal at kill barrier.' }
            if ($phase -eq 'journal-committed' -and -not $report.SelectionCommitted) { throw 'No actual committed selection at kill barrier.' }
            $process.Kill()
            if (-not $process.WaitForExit(10000)) { throw 'Owned native writer did not terminate.' }
            $reports += $report
        } else {
            if (-not $process.WaitForExit(30000)) { throw 'Owned native reader timed out; do not restart it.' }
            if ($process.ExitCode -ne 0) { throw 'Native journal phase failed; inspect its owned stderr log.' }
            $report = Get-Content -LiteralPath $stdout -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($phase -eq 'journal-create' -and -not $report.JournalCreated) { throw 'Native journal initialization missing.' }
            if ($phase -ne 'journal-create' -and (-not $report.JournalReopened -or $report.CandidateSelected -ne ($phase -eq 'journal-reopen-selected'))) {
                throw 'Native journal restart selected the wrong generation.'
            }
            $reports += $report
        }
    } finally {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit(10000) | Out-Null }
        $process.Dispose()
    }
}
$reports | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence 'journal-process-proof.json') -Encoding UTF8
Write-Output (Join-Path $evidence 'journal-process-proof.json')
