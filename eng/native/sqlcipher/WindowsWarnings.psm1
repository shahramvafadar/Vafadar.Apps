Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-WindowsWarningRows {
    <# .SYNOPSIS Normalizes complete compiler diagnostics without accepting unknown warning formats. #>
    param([Parameter(Mandatory=$true)][AllowEmptyCollection()][AllowEmptyString()][string[]]$Lines)
    foreach ($line in $Lines) {
        if ($line -notmatch '(?i)\bwarning\b') { continue }
        if ($line -notmatch '^(?<path>.+?)\((?<line>[1-9][0-9]*)\)\s*:\s*warning\s+(?<code>[A-Z]+[0-9]+):\s*(?<message>.+)$') {
            throw 'Unknown native warning format; retain the complete producer log for review.'
        }
        $lineNumber = [int]$Matches.line; $code = $Matches.code; $message = $Matches.message.Trim()
        $path = $Matches.path.Replace('\','/')
        $at = $path.LastIndexOf('/openssl/', [StringComparison]::OrdinalIgnoreCase)
        if ($at -lt 0) { throw 'Native warning belongs to an unreviewed source tree.' }
        $path = $path.Substring($at + 1).ToLowerInvariant()
        if ($path -notmatch '^openssl/[a-z0-9_./-]+\.(c|h|inc)$' -or @($path.Split('/') | Where-Object { $_ -in @('','..','.') }).Count) {
            throw 'Native warning source path is unsafe or outside the reviewed grammar.'
        }
        [pscustomobject]@{ path=$path; line=$lineNumber; code=$code; message=$message }
    }
}

function Get-WindowsWarningKey {
    <# .SYNOPSIS Preserves the source location, code and exact diagnostic meaning in an ordinal key. #>
    param([Parameter(Mandatory=$true)]$Row)
    return ($Row.path.ToLowerInvariant() + '|' + $Row.line + '|' + $Row.code + '|' + $Row.message)
}

function Test-WindowsWarningSet {
    <# .SYNOPSIS Refuses added, changed or missing upstream diagnostic sites; repeated resume evidence stays explicit. #>
    param([Parameter(Mandatory=$true)]$Baseline,
        [Parameter(Mandatory=$true)][AllowEmptyCollection()][object[]]$Rows,
        [switch]$ResumedHistory)
    if ($Baseline.schemaVersion -ne 1 -or $Baseline.securitySafetyAcceptance -ne $false -or
        -not $Baseline.diagnostics.Count -or -not $Baseline.sourceFiles.Count) { throw 'Unsupported diagnostic baseline.' }
    $expected = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    $boundSources = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($source in $Baseline.sourceFiles) {
        if (-not $boundSources.Add($source.path)) { throw 'Duplicate diagnostic source binding.' }
    }
    $expectedCount = 0
    foreach ($row in $Baseline.diagnostics) {
        $key = Get-WindowsWarningKey $row
        if ($expected.ContainsKey($key) -or $row.occurrences -lt 1 -or $row.line -lt 1 -or
            -not $boundSources.Contains($row.path)) { throw 'Duplicate, empty or unbound baseline diagnostic.' }
        $expected.Add($key, $row); $expectedCount += [int]$row.occurrences
    }
    if ($expectedCount -ne $Baseline.expectedOccurrences) { throw 'Baseline diagnostic total is inconsistent.' }
    $actual = [Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal)
    foreach ($row in $Rows) {
        $key = Get-WindowsWarningKey $row
        if (-not $expected.ContainsKey($key)) { throw 'A native warning was added or changed; review it before packaging.' }
        if (-not $actual.ContainsKey($key)) { $actual.Add($key, 0) }
        $actual[$key]++
    }
    if ($actual.Count -ne $expected.Count) { throw 'Native warning coverage is incomplete; do not qualify a partial log.' }
    foreach ($key in $expected.Keys) {
        if ($actual[$key] -lt $expected[$key].occurrences -or
            (-not $ResumedHistory -and $actual[$key] -ne $expected[$key].occurrences)) {
            throw 'Native diagnostic multiplicity differs from the baseline.'
        }
    }
    [pscustomobject]@{ occurrences=$Rows.Count; sites=$actual.Count; resumedHistory=[bool]$ResumedHistory;
        baselineOccurrences=$expectedCount; securitySafetyAcceptance=$false }
}

function Get-WindowsWarningLogHistory {
    <# .SYNOPSIS Requires the current producer log and matching preserved recipes before joining resumed evidence. #>
    param([Parameter(Mandatory=$true)][string]$Root)
    $current = Join-Path $Root 'build.log'
    if (-not (Test-Path -LiteralPath $current)) { throw 'Complete native producer log is missing.' }
    $logs = @((Get-Item -LiteralPath $current)) + @(Get-ChildItem -LiteralPath $Root -File | Where-Object { $_.Name -match '^build\.log\.[0-9a-f]{32}$' } | Sort-Object Name)
    $command = [IO.File]::ReadAllText((Join-Path $Root 'build.cmd')).Replace("`r`n","`n")
    foreach ($log in $logs | Select-Object -Skip 1) {
        $previousCommand = Join-Path $Root ('build.cmd.' + $log.Name.Substring(10))
        if (-not (Test-Path -LiteralPath $previousCommand) -or [IO.File]::ReadAllText($previousCommand).Replace("`r`n","`n") -cne $command) {
            throw 'Resumed diagnostic history has a different or missing producer recipe.'
        }
    }
    return $logs
}

function Test-WindowsWarningSources {
    <# .SYNOPSIS Binds every diagnostic source to its reviewed official-source bytes without following redirections. #>
    param([Parameter(Mandatory=$true)][string]$Root, [Parameter(Mandatory=$true)]$Baseline)
    $absoluteRoot = [IO.Path]::GetFullPath($Root)
    $checked = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($source in $Baseline.sourceFiles) {
        if ($source.path -notmatch '^openssl/[a-z0-9_./-]+\.(c|h|inc)$' -or
            @($source.path.Split('/') | Where-Object { $_ -in @('','..','.') }).Count -or
            $source.sha256 -notmatch '^[0-9a-f]{64}$' -or $source.gitBlob -notmatch '^[0-9a-f]{40}$') {
            throw 'Invalid reviewed diagnostic source binding.'
        }
        $path = [IO.Path]::GetFullPath((Join-Path $absoluteRoot $source.path))
        if (-not $path.StartsWith($absoluteRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Diagnostic source escaped the native producer root.'
        }
        for ($item = Get-Item -LiteralPath $path; $null -ne $item; $item = $item.Parent) {
            if (-not $checked.Add($item.FullName)) { break }
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Redirected native diagnostic source refused.' }
            # FileInfo uses Directory; DirectoryInfo uses Parent. Check the complete ancestor chain.
            if ($item -is [IO.FileInfo]) {
                $directory = $item.Directory
                while ($null -ne $directory) {
                    if (-not $checked.Add($directory.FullName)) { break }
                    if (($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Redirected native source ancestor refused.' }
                    $directory = $directory.Parent
                }
                break
            }
        }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $source.sha256) {
            throw 'A warning-bearing source differs from its reviewed official bytes.'
        }
    }
    return $Baseline.sourceFiles.Count
}

Export-ModuleMember -Function Get-WindowsWarningRows,Test-WindowsWarningSet,Test-WindowsWarningSources,Get-WindowsWarningLogHistory
