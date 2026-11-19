<#
.SYNOPSIS Raises the public results of a producer qualification as notices of the hosted job.
.DESCRIPTION Reads owned producer evidence only: hashes of fresh native outputs, the entries of the unpublished
package and, when the diagnostics of a compiler have no equal recorded inventory, the proposed inventory.
It builds nothing, changes no baseline and no consumer pin, and accepts nothing: the notices are what a reviewer
reads before any pin is changed. No key, database or source byte is raised.
#>
[CmdletBinding()]
param([string[]]$RunNames = @('windows-ci-a','windows-ci-b'),
    # A hosted step shows only so many notices: each kind of evidence is raised by a step of its own.
    [Parameter(Mandatory)][ValidateSet('Outputs','Diagnostics','Proposal','Package')][string]$Section,
    # The proposal is raised in groups of eight parts, one group per step.
    [ValidateRange(0,7)][int]$Group = 0)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'WindowsWarnings.psm1') -Force
function Write-Notice {
    <# .SYNOPSIS One line of a hosted job that everybody who can see the job can read. #>
    param([string]$Title,[string]$Message)
    $text = ($Message -replace "`r?`n",' ').Replace('%','%25')
    Write-Host ('::notice title=' + $Title + '::' + $text)
}
function Get-Sha256 {
    <# .SYNOPSIS The lower-case SHA-256 of bytes held in memory. #>
    param([byte[]]$Bytes)
    $hash = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($hash.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant() } finally { $hash.Dispose() }
}
$evidence = Join-Path (Get-NativeInputRoot) 'producer-evidence'
$partSize = 3000
$native = [ordered]@{}
foreach ($run in $RunNames) {
    $root = Join-Path (Get-NativeInputRoot) ('build/' + $run)
    $library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
    $native[$run] = if (Test-Path -LiteralPath $library -PathType Leaf) { (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant() } else { 'absent' }
    if ($Section -ne 'Outputs') { continue }
    $proof = Join-Path $root 'compiler-host-proof.json'
    if (Test-Path -LiteralPath $proof -PathType Leaf) {
        Write-Notice ('Producer host ' + $run) ((Get-Content -LiteralPath $proof -Raw -Encoding UTF8 | ConvertFrom-Json) | ConvertTo-Json -Depth 5 -Compress)
    }
    $log = Join-Path $root 'build.log'
    if ($native[$run] -eq 'absent' -and (Test-Path -LiteralPath $log -PathType Leaf)) {
        # A failed compilation: the compiler's own error lines about public official sources, nothing else.
        $lines = @(Get-Content -LiteralPath $log -Encoding UTF8 | Where-Object { $_ -match '(?i)\b(fatal )?error\b|NMAKE : ' } | Select-Object -Last 6)
        if (-not $lines.Count) { $lines = @(Get-Content -LiteralPath $log -Encoding UTF8 | Select-Object -Last 6) }
        Write-Notice ('Producer build failure ' + $run) (($lines | ForEach-Object { $_.Substring(0,[Math]::Min(400,$_.Length)) }) -join ' || ')
    }
}
if ($Section -eq 'Outputs') { Write-Notice 'Producer fresh native outputs' ($native | ConvertTo-Json -Compress) }

# The diagnostics of the first complete fresh output against the recorded inventory of its compiler. A difference
# is reported and the proposed inventory of the existing export is kept for the proposal steps; nothing is accepted.
$first = $RunNames | Where-Object { $native[$_] -ne 'absent' } | Select-Object -First 1
if ($first -and $Section -eq 'Diagnostics') {
    $drift = $null
    try { & (Join-Path $PSScriptRoot 'Test-WindowsWarnings.ps1') -RunName $first | Out-Null } catch { $drift = $_.Exception.Message }
    if (-not $drift) { Write-Notice 'Producer diagnostics' 'Equal to the recorded inventory of this compiler.' }
    else {
        Write-Notice 'Producer diagnostics' ('No equal recorded inventory: ' + $drift)
        $root = Join-Path (Get-NativeInputRoot) ('build/' + $first)
        # Warning lines outside the reviewed grammar (a linker or command-line warning, another source tree)
        # stop the export; they are shown as they are so that they can be reviewed.
        $foreign = @(foreach ($line in @(Get-Content -LiteralPath (Join-Path $root 'build.log') -Encoding UTF8 | Where-Object { $_ -match '(?i)\bwarning\b' })) {
            try { Get-WindowsWarningRows -Lines @($line) | Out-Null } catch { $line.Substring(0,[Math]::Min(300,$line.Length)) }
        })
        if ($foreign.Count) { Write-Notice 'Producer diagnostics outside the reviewed grammar' (('count=' + $foreign.Count + ' || ') + (($foreign | Select-Object -First 8) -join ' || ')) }
        try {
            $path = & (Join-Path $PSScriptRoot 'Export-WindowsWarningProposal.ps1') -RunName $first | Select-Object -Last 1
            $bytes = [IO.File]::ReadAllBytes($path)
            $packed = [IO.MemoryStream]::new()
            $gzip = [IO.Compression.GZipStream]::new($packed,[IO.Compression.CompressionLevel]::Optimal)
            try { $gzip.Write($bytes,0,$bytes.Length) } finally { $gzip.Dispose() }
            $text = [Convert]::ToBase64String($packed.ToArray())
            New-Item -ItemType Directory -Force $evidence | Out-Null
            [IO.File]::WriteAllText((Join-Path $evidence 'proposal.gz.b64'),$text,[Text.Encoding]::ASCII)
            $proposal = [Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
            Write-Notice 'Producer diagnostic proposal' ('sha256=' + (Get-Sha256 $bytes) + '; bytes=' + $bytes.Length + '; occurrences=' + $proposal.expectedOccurrences +
                '; sites=' + @($proposal.diagnostics).Count + '; sources=' + @($proposal.sourceFiles).Count + '; tools=' + $proposal.compilerToolsVersion +
                '; gzip-base64 length=' + $text.Length + '; parts=' + [Math]::Ceiling($text.Length / $partSize) + ' of ' + $partSize)
        } catch { Write-Notice 'Producer diagnostic proposal' ('Not exported: ' + $_.Exception.Message) }
    }
}
if ($Section -eq 'Proposal') {
    $file = Join-Path $evidence 'proposal.gz.b64'
    if (Test-Path -LiteralPath $file -PathType Leaf) {
        $text = [IO.File]::ReadAllText($file)
        $parts = [Math]::Ceiling($text.Length / $partSize)
        for ($index = $Group * 8; $index -lt [Math]::Min($parts, ($Group + 1) * 8); $index++) {
            Write-Notice ('Producer diagnostic proposal part ' + ($index + 1).ToString('00') + ' of ' + $parts) $text.Substring($index * $partSize,[Math]::Min($partSize,$text.Length - $index * $partSize))
        }
    }
}

# The entries of the unpublished package, as a consumer pin would name them.
foreach ($run in $(if ($Section -eq 'Package') { $RunNames } else { @() })) {
    $directory = Join-Path (Get-NativeInputRoot) ('build/' + $run + '/producer-qualification')
    $package = if (Test-Path -LiteralPath $directory) { Get-ChildItem -LiteralPath $directory -Filter '*.nupkg' -File | Select-Object -First 1 }
    if (-not $package) { continue }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $entries = [ordered]@{}
        foreach ($entry in $archive.Entries | Sort-Object FullName) {
            $stream = $entry.Open(); $copy = [IO.MemoryStream]::new()
            try { $stream.CopyTo($copy) } finally { $stream.Dispose() }
            $entries[$entry.FullName] = Get-Sha256 $copy.ToArray()
            # The package manifest is public recipe provenance; a reviewer reads it before pinning its hash.
            if ($entry.FullName -ceq 'manifest.json') { Write-Notice ('Producer package manifest ' + $run) ([Text.Encoding]::UTF8.GetString($copy.ToArray())) }
        }
    } finally { $archive.Dispose() }
    Write-Notice ('Producer package ' + $run) ([ordered]@{ name=$package.Name; sha256=(Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash.ToLowerInvariant(); bytes=$package.Length; entries=$entries } | ConvertTo-Json -Compress)
}
