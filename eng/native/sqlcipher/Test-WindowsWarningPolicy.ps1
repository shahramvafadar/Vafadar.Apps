<# .SYNOPSIS Exercises diagnostic drift, original-source bindings and resumed producer evidence without rebuilding. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'WindowsWarnings.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$baseline = Get-Content (Join-Path $PSScriptRoot 'windows.warnings.baseline.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$lines = @(Get-Content (Join-Path $root 'build.log') -Encoding UTF8)
$rows = @(Get-WindowsWarningRows -Lines $lines)
$cases = [Collections.Generic.List[string]]::new()
function Expect-Refusal([string]$Name, [string]$Reason, [scriptblock]$Action) {
    <# .SYNOPSIS Retains each expected refusal and rejects failures for an unrelated reason. #>
    try { & $Action | Out-Null } catch {
        if ($_.Exception.Message -notlike ('*' + $Reason + '*')) { throw ('Wrong refusal for ' + $Name + ': ' + $_.Exception.Message) }
        $cases.Add($Name); return
    }
    throw ('Unexpected acceptance: ' + $Name)
}
function Copy-Record($Value) {
    <# .SYNOPSIS Isolates modified diagnostic fixtures from the original evidence. #>
    return ($Value | ConvertTo-Json -Depth 12 | ConvertFrom-Json)
}
$valid = Test-WindowsWarningSet -Baseline $baseline -Rows $rows
if ($valid.occurrences -ne 508 -or $valid.sites -ne 503 -or $valid.securitySafetyAcceptance) { throw 'The actual complete baseline differs.' }
$cases.Add('actual-508-diagnostic-baseline')
if ((Test-WindowsWarningSources -Root $root -Baseline $baseline) -ne 160) { throw 'Original source coverage differs.' }
$cases.Add('160-official-source-bindings')
$changed = @(Copy-Record $rows); $changed[0].code = 'C9999'
Expect-Refusal 'new-code' 'added or changed' { Test-WindowsWarningSet $baseline $changed }
$changed = @(Copy-Record $rows); $changed[0].message += ' changed'
Expect-Refusal 'changed-meaning' 'added or changed' { Test-WindowsWarningSet $baseline $changed }
$changed = @(Copy-Record $rows); $changed[0].line++
Expect-Refusal 'moved-site' 'added or changed' { Test-WindowsWarningSet $baseline $changed }
$first = $rows[0]
$missing = @($rows | Where-Object { $_.path -ne $first.path -or $_.line -ne $first.line -or $_.message -ne $first.message -or $_.code -ne $first.code })
Expect-Refusal 'partial-log' 'coverage is incomplete' { Test-WindowsWarningSet $baseline $missing }
Expect-Refusal 'empty-log' 'coverage is incomplete' { Test-WindowsWarningSet -Baseline $baseline -Rows @() }
Expect-Refusal 'fresh-multiplicity-drift' 'multiplicity differs' { Test-WindowsWarningSet $baseline @($rows + $rows[0]) }
$resumed = Test-WindowsWarningSet -Baseline $baseline -Rows @($rows + $rows[0]) -ResumedHistory
if ($resumed.occurrences -ne 509 -or -not $resumed.resumedHistory) { throw 'Known repeated resume evidence was lost.' }
$cases.Add('known-resume-duplicates-retained')
$duplicate = @($baseline.diagnostics | Where-Object occurrences -gt 1)[0]
$at = 0
while ($rows[$at].path -ine $duplicate.path -or $rows[$at].line -ne $duplicate.line -or
    $rows[$at].code -ne $duplicate.code -or $rows[$at].message -cne $duplicate.message) { $at++ }
$fewer = @($rows[0..($at - 1)] + $rows[($at + 1)..($rows.Count - 1)])
if ($at -eq 0) { $fewer = @($rows | Select-Object -Skip 1) }
Expect-Refusal 'missing-resumed-occurrence' 'multiplicity differs' { Test-WindowsWarningSet $baseline $fewer -ResumedHistory }
$blankLines = @(Get-WindowsWarningRows -Lines @('', 'ordinary compiler output'))
if ($blankLines.Count) { throw 'Non-diagnostics became warning rows.' }
$cases.Add('blank-and-ordinary-lines')
Expect-Refusal 'linker-warning' 'Unknown native warning format' { Get-WindowsWarningRows -Lines @('LINK : warning LNK4099: missing PDB') }
Expect-Refusal 'unknown-format' 'Unknown native warning format' { Get-WindowsWarningRows -Lines @('clang: warning: new diagnostic') }
Expect-Refusal 'foreign-tree' 'unreviewed source tree' { Get-WindowsWarningRows -Lines @('..\sqlcipher\sqlite3.c(1): warning C4244: changed') }
Expect-Refusal 'traversal-diagnostic' 'unsafe' { Get-WindowsWarningRows -Lines @('..\openssl\..\outside.c(1): warning C4244: changed') }
$bad = Copy-Record $baseline; $bad.securitySafetyAcceptance = $true
Expect-Refusal 'no-safety-waiver' 'Unsupported' { Test-WindowsWarningSet $bad $rows }
$bad = Copy-Record $baseline; $bad.expectedOccurrences++
Expect-Refusal 'inconsistent-baseline-total' 'total is inconsistent' { Test-WindowsWarningSet $bad $rows }
$bad = Copy-Record $baseline; $bad.sourceFiles += $bad.sourceFiles[0]
Expect-Refusal 'duplicate-source-binding' 'Duplicate diagnostic source binding' { Test-WindowsWarningSet $bad $rows }
$bad = Copy-Record $baseline; $bad.sourceFiles = @($bad.sourceFiles | Select-Object -Skip 1)
Expect-Refusal 'unbound-diagnostic-source' 'unbound baseline' { Test-WindowsWarningSet $bad $rows }
$bad = Copy-Record $baseline; $bad.sourceFiles[0].sha256 = ('0' * 64)
Expect-Refusal 'changed-source-bytes' 'differs from its reviewed' { Test-WindowsWarningSources $root $bad }
$bad = Copy-Record $baseline; $bad.sourceFiles[0].path = 'openssl/../../outside.c'
Expect-Refusal 'traversal-source-binding' 'Invalid reviewed' { Test-WindowsWarningSources $root $bad }
$evidence = Join-Path (Get-NativeInputRoot) ('warning-policy-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $evidence | Out-Null
[IO.File]::WriteAllText((Join-Path $evidence 'build.log'), '')
[IO.File]::WriteAllText((Join-Path $evidence 'build.cmd'), 'same recipe')
$suffix = [Guid]::NewGuid().ToString('N')
[IO.File]::WriteAllText((Join-Path $evidence ('build.log.' + $suffix)), '')
[IO.File]::WriteAllText((Join-Path $evidence ('build.cmd.' + $suffix)), 'different recipe')
Expect-Refusal 'changed-resume-recipe' 'different or missing' { Get-WindowsWarningLogHistory $evidence }
[IO.File]::WriteAllText((Join-Path $evidence ('build.cmd.' + $suffix)), 'same recipe')
if (@(Get-WindowsWarningLogHistory $evidence).Count -ne 2) { throw 'Matching resume history missing.' }
$cases.Add('matching-resume-recipe')
# Exercise the actual packaging boundary on an isolated copy of public producer material.
# Never modify the original producer, package, key fixtures or source checkout.
$fixtureName = 'warning-refusal-' + [Guid]::NewGuid().ToString('N')
$fixture = Join-Path (Get-NativeInputRoot) ('build/' + $fixtureName)
New-Item -ItemType Directory -Path $fixture | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'candidate') -Destination (Join-Path $fixture 'candidate') -Recurse
foreach ($file in @('build.log','build.cmd','compatibility-proof.json')) {
    Copy-Item -LiteralPath (Join-Path $root $file) -Destination (Join-Path $fixture $file)
}
foreach ($source in $baseline.sourceFiles) {
    $target = Join-Path $fixture $source.path
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
    Copy-Item -LiteralPath (Join-Path $root $source.path) -Destination $target
}
$originalLog = [IO.File]::ReadAllText((Join-Path $fixture 'build.log'))
$unknown = [regex]::new('warning C[0-9]+:').Replace($originalLog, 'warning C9999:', 1)
[IO.File]::WriteAllText((Join-Path $fixture 'build.log'), $unknown)
Expect-Refusal 'packaging-refuses-new-warning' 'added or changed' {
    & (Join-Path $PSScriptRoot 'Pack-Windows.ps1') -RunName $fixtureName
}
[IO.File]::WriteAllText((Join-Path $fixture 'build.log'), $originalLog)
[IO.File]::AppendAllText((Join-Path $fixture $baseline.sourceFiles[0].path), "`n")
Expect-Refusal 'packaging-refuses-source-drift' 'differs from its reviewed' {
    & (Join-Path $PSScriptRoot 'Pack-Windows.ps1') -RunName $fixtureName
}
if (Test-Path -LiteralPath (Join-Path $fixture 'package')) { throw 'Refused producer created a package directory.' }
$cases.Add('no-package-published-after-refusal')
[ordered]@{ schemaVersion=1; cases=$cases.ToArray(); passed=$cases.Count; securitySafetyAcceptance=$false } |
    ConvertTo-Json -Depth 8 | Set-Content (Join-Path $evidence 'proof.json') -Encoding UTF8
Write-Output (Join-Path $evidence 'proof.json')
