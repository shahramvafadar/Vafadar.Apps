#requires -PSEdition Core
#requires -Version 7.3
<#
.SYNOPSIS Checks the actual retained-byte identity policy without opening a device, database or UI.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'AndroidHomeReview.CurrentStages.cs'
$text = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
$startMarker = '    private static bool CurrentHomeByteIdentityMatches('
$endMarker = '    /// <summary>Preserves every prior byte'
$start = $text.IndexOf($startMarker, [StringComparison]::Ordinal)
$end = $text.IndexOf($endMarker, [StringComparison]::Ordinal)
if ($start -lt 0 -or $start -ne $text.LastIndexOf($startMarker, [StringComparison]::Ordinal) -or $end -le $start) {
    throw 'One original source-byte identity predicate required.'
}
$method = $text.Substring($start, $end - $start).Trim().Replace('private static bool', 'public static bool')
# Compile the actual policy body; only its visibility and non-UI wrapper differ from the native source.
$references = @(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -File -Filter '*.dll' | ForEach-Object FullName)
Add-Type -ReferencedAssemblies $references -TypeDefinition ('using System; using System.Text.Json; namespace Vafadar.Build { public static class CurrentHomeByteIdentityContract { ' + $method + ' } }')
$comparison = '11111111111111111111111111111111'
$day = '2026-10-25'
$data = [guid]'11111111-1111-1111-1111-111111111111'
$core = [guid]'22222222-2222-2222-2222-222222222222'
$caller = [guid]'33333333-3333-3333-3333-333333333333'
$other = [guid]'44444444-4444-4444-4444-444444444444'
$cases = [Collections.Generic.List[object]]::new()
function New-IdentityPacket([hashtable]$Changes) {
    $fields = @{ Comparison=$comparison; Day=$day; CapturedBeforeNativePublication=$true;
        DataModule=$data.ToString(); CoreModule=$core.ToString(); CallerModule=$caller.ToString();
        LedgerTransport='blocks'; ValidationMode='retained'; RuntimeProfile='concurrent-default' }
    foreach ($key in $Changes.Keys) {
        if ($null -eq $Changes[$key]) { $fields.Remove($key) } else { $fields[$key]=$Changes[$key] }
    }
    return [System.Text.Json.JsonDocument]::Parse(($fields | ConvertTo-Json -Compress))
}
function Check-Identity([string]$Name, [hashtable]$Changes, [bool]$ReportOnly, [bool]$Expected, [bool]$MissingField=$false) {
    $packet = New-IdentityPacket $Changes
    try {
        $threw = $false
        try { $actual = [Vafadar.Build.CurrentHomeByteIdentityContract]::CurrentHomeByteIdentityMatches($packet.RootElement, $comparison, $day, $data, $core, $caller, $ReportOnly) }
        catch {
            $failure = $_.Exception
            while ($failure.InnerException) { $failure = $failure.InnerException }
            if (!$MissingField -or $failure -isnot [Collections.Generic.KeyNotFoundException]) { throw }
            $threw = $true
        }
        if ($threw -ne $MissingField -or (!$MissingField -and $actual -ne $Expected)) { throw ('Identity contract differs: '+$Name) }
        $cases.Add(@{ Case=$Name; Passed=$true; Expected=$Expected; MissingFieldRefused=$MissingField })
    }
    finally { $packet.Dispose() }
}
Check-Identity 'Original source/day/code resumes' @{} $false $true
Check-Identity 'Different comparison refuses' @{Comparison='22222222222222222222222222222222'} $false $false
Check-Identity 'Different validation mode refuses' @{ValidationMode='separate'} $false $false
Check-Identity 'Missing validation mode refuses' @{ValidationMode=$null} $false $false
Check-Identity 'Unsupported validation mode refuses' @{ValidationMode='cached'} $false $false
Check-Identity 'Different runtime profile refuses' @{RuntimeProfile='concurrent-nursery32'} $false $false
Check-Identity 'Historical report still refuses changed runtime profile' @{RuntimeProfile='concurrent-nursery32'} $true $false
Check-Identity 'Missing runtime profile refuses' @{RuntimeProfile=$null} $false $false
Check-Identity 'Unsupported runtime profile refuses' @{RuntimeProfile='unknown'} $false $false
Check-Identity 'Post-observation fingerprint refuses' @{CapturedBeforeNativePublication=$false} $false $false
Check-Identity 'Different data module refuses' @{DataModule=$other.ToString()} $false $false
Check-Identity 'Different Core module refuses' @{CoreModule=$other.ToString()} $false $false
Check-Identity 'Different publication caller refuses new metrics' @{CallerModule=$other.ToString()} $false $false
Check-Identity 'Different actual day refuses new metrics' @{Day='2026-10-26'} $false $false
Check-Identity 'Historical report may change observer/day without new UI' @{CallerModule=$other.ToString();Day='2026-10-26'} $true $true
Check-Identity 'Historical report still refuses different Core' @{CoreModule=$other.ToString()} $true $false
Check-Identity 'Missing actual day refuses new metrics' @{Day=$null} $false $false $true
@{ ActualSourceByteIdentityCompiled=$true; Cases=$cases; DeviceDatabaseOrUiOpened=$false;
    SourceFileSha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash } | ConvertTo-Json -Depth 5
