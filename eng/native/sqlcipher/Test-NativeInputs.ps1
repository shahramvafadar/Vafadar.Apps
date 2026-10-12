<# .SYNOPSIS Exercises actual native input validation with independently owned malicious/invalid fixtures. #>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$results = [Collections.Generic.List[string]]::new()
function Assert-NativeInputRefused {
    <# .SYNOPSIS Requires the real restorer to refuse before any fixture executable can run. #>
    param([string]$Name,[scriptblock]$Action,[string]$Message)
    $refused = $false
    try { & $Action | Out-Null }
    catch {
        if ($_.Exception.Message -notlike $Message) { throw }
        $refused = $true
    }
    if (-not $refused) { throw ('Unsafe native input accepted: ' + $Name) }
    $results.Add($Name)
}
$tool = [pscustomobject]@{ uri='https://example.invalid/tool.zip'; sha256=('1' * 64); bytes=1 }
Assert-NativeInputRefused 'unofficial-tool-origin' { Restore-NativeZipTool $tool 'none.exe' } 'Unsupported native tool*'
$tool.uri = 'http://www.nasm.us/pub/nasm/releasebuilds/3.02/win64/nasm-3.02-win64.zip'
Assert-NativeInputRefused 'unencrypted-tool-origin' { Restore-NativeZipTool $tool 'none.exe' } 'Unsupported native tool*'
$tool.uri = 'https://www.nasm.us/pub/nasm/releasebuilds/3.02/win64/nasm-3.02-win64.zip'
$tool.sha256 = 'malformed'
Assert-NativeInputRefused 'malformed-tool-digest' { Restore-NativeZipTool $tool 'none.exe' } 'Unsupported native tool*'
$source = [pscustomobject]@{ repository='https://github.com/example/crypto.git'; commit=('1' * 40) }
Assert-NativeInputRefused 'unofficial-source-origin' { Restore-NativeSource $source 'sqlcipher' } 'Unsupported native source*'
$source.repository = 'https://github.com/sqlcipher/sqlcipher.git'; $source.commit = 'malformed'
Assert-NativeInputRefused 'malformed-source-commit' { Restore-NativeSource $source 'sqlcipher' } 'Unsupported native source*'

Add-Type -AssemblyName System.IO.Compression.FileSystem
$fixtureRoot = Join-Path (Get-NativeInputRoot) ('validation-fixture-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $fixtureRoot | Out-Null
$cache = Join-Path (Get-NativeInputRoot) 'tools'
New-Item -ItemType Directory -Force $cache | Out-Null
foreach ($case in @('parent-path','alternate-stream','symbolic-link')) {
    $file = Join-Path $fixtureRoot ($case + '.zip')
    $zip = [IO.Compression.ZipFile]::Open($file,[IO.Compression.ZipArchiveMode]::Create)
    try {
        $name = switch ($case) { 'parent-path' { '../escaped.exe' } 'alternate-stream' { 'tool.exe:stream' } default { 'link.exe' } }
        $entry = $zip.CreateEntry($name)
        if ($case -eq 'symbolic-link') { $entry.ExternalAttributes = -1610612736 }
        $stream = $entry.Open()
        try { $bytes = [Text.Encoding]::ASCII.GetBytes('independent-nonexecutable-fixture'); $stream.Write($bytes,0,$bytes.Length) }
        finally { $stream.Dispose() }
    }
    finally { $zip.Dispose() }
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    Copy-Item -LiteralPath $file -Destination (Join-Path $cache ($hash + '.zip'))
    $fixture = [pscustomobject]@{ uri='https://www.nasm.us/pub/nasm/releasebuilds/3.02/win64/nasm-3.02-win64.zip'; sha256=$hash; bytes=(Get-Item -LiteralPath $file).Length }
    Assert-NativeInputRefused $case { Restore-NativeZipTool $fixture 'none.exe' } 'Unsafe native archive*'
    $fixture.bytes++
    Assert-NativeInputRefused ($case + '-wrong-size') { Restore-NativeZipTool $fixture 'none.exe' } 'Cached native archive differs*'
}
[ordered]@{ passed=$results.Count; cases=$results.ToArray(); fictitiousOwnedArchivesOnly=$true; noExecutableOrSourceFetch=$true } |
    ConvertTo-Json -Depth 4
