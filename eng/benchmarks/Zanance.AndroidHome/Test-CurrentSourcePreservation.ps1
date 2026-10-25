<#
.SYNOPSIS Verifies the actual diagnostic file-preservation predicate without opening a database or UI.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'AndroidHomeReview.CurrentStages.cs'
$text = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
$startMarker = '    private static bool CurrentHomeFilesPreserved('
$endMarker = '    /// <summary>Rechecks only existing encrypted source/control rows'
$start = $text.IndexOf($startMarker, [StringComparison]::Ordinal)
$end = $text.IndexOf($endMarker, [StringComparison]::Ordinal)
if ($start -lt 0 -or $start -ne $text.LastIndexOf($startMarker, [StringComparison]::Ordinal) -or $end -le $start) {
    throw 'One exact original diagnostic predicate is required.'
}
$method = $text.Substring($start, $end - $start).Trim()
# Compile the actual source body, changing only its visibility inside a non-UI host wrapper.
# No copied predicate, database algorithm, MAUI control or financial source belongs to this contract check.
$visible = $method.Replace('private static bool', 'public static bool')
Add-Type -TypeDefinition ('using System; using System.Linq; using System.Collections.Generic; namespace Vafadar.Build { public static class CurrentHomeSourceContract { ' + $visible + ' } }')
$comparison = '11111111111111111111111111111111'
$baseRecords = [string[]]@(('fixture.cipher.db:' + ('a' * 64)), ('keys/existing.key:' + ('b' * 64)),
    ('presentation-' + $comparison + '-en.json:' + ('c' * 64)))
if ($baseRecords.Count -ne 3) { throw 'Three distinct synthetic source records required.' }
$cases = [Collections.Generic.List[object]]::new()
function Check-Contract([string]$Name, [string[]]$Before, [string[]]$After, [bool]$Expected) {
    $actual = [Vafadar.Build.CurrentHomeSourceContract]::CurrentHomeFilesPreserved($Before, $After, $comparison)
    if ($actual -ne $Expected) { throw ('Preservation contract differs: ' + $Name) }
    $cases.Add(@{ Case = $Name; Expected = $Expected; Passed = $true })
}
Check-Contract 'Unchanged complete source' $baseRecords $baseRecords $true
Check-Contract 'Exact new current presentation' $baseRecords ($baseRecords + @('presentation-' + $comparison + '-fa.json:' + ('d' * 64))) $true
$changed = [string[]]$baseRecords.Clone(); $changed[0] = 'fixture.cipher.db:' + ('e' * 64)
Check-Contract 'Changed ciphertext refused' $baseRecords $changed $false
$changed = [string[]]$baseRecords.Clone(); $changed[1] = 'keys/existing.key:' + ('f' * 64)
Check-Contract 'Changed key-envelope digest refused' $baseRecords $changed $false
Check-Contract 'Missing original key file refused' $baseRecords @($baseRecords[0], $baseRecords[2]) $false
$changed = [string[]]$baseRecords.Clone(); $changed[2] = 'presentation-' + $comparison + '-en.json:' + ('0' * 64)
Check-Contract 'Changed retained presentation refused' $baseRecords $changed $false
Check-Contract 'Unrelated added output refused' $baseRecords ($baseRecords + @('unrelated.json:' + ('d' * 64))) $false
Check-Contract 'Different-comparison presentation refused' $baseRecords ($baseRecords + @('presentation-22222222222222222222222222222222-fa.json:' + ('d' * 64))) $false
@{ ActualSourcePredicateCompiled = $true; Cases = $cases; DatabaseOrUiOpened = $false;
    SourceFileSha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash } | ConvertTo-Json -Depth 5
