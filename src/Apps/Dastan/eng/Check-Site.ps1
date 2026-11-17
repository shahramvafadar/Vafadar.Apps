<#
.SYNOPSIS
    Asks a running Dastan site, from outside its process, what a host has to be sure of (docs/operations.md).

.DESCRIPTION
    The checks need nothing but the address: that the site is alive, that every answer carries the headers that
    protect a visitor, that the files of a collection are not served, that a page that does not exist says so,
    and that the site believes a proxy that says the visitor came by https. Each check is one line; the script
    ends with 1 when a check fails.

.PARAMETER Address
    The address of the site, without a path: http://127.0.0.1:53380

.PARAMETER Collection
    The name of the collection in the addresses of the site. A site that shows a published revision answers its
    page; a site with nothing published answers "not found" for it, which is reported and is not a failure.

.EXAMPLE
    ./src/Apps/Dastan/eng/Check-Site.ps1 -Address http://127.0.0.1:53380
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Address,
    [string]$Collection = 'story-rostam-sohrab'
)

$ErrorActionPreference = 'Stop'
$Address = $Address.TrimEnd('/')
$failed = 0

function Ask([string]$Path, [hashtable]$Headers = @{}) {
    $arguments = @('-s', '-o', '-', '-D', '-', '--max-time', '20')
    foreach ($name in $Headers.Keys) { $arguments += @('-H', ('{0}: {1}' -f $name, $Headers[$name])) }
    $arguments += ($Address + $Path)
    $raw = (& curl.exe @arguments) -join "`n"
    $head, $body = $raw -split "`n`r?`n", 2
    $lines = $head -split "`r?`n"
    $answer = @{ Status = [int](($lines[0] -split ' ')[1]); Body = [string]$body; Headers = @{} }
    foreach ($line in $lines | Select-Object -Skip 1) {
        $name, $value = $line -split ':\s*', 2
        if ($name) { $answer.Headers[$name.ToLowerInvariant()] = ((@($answer.Headers[$name.ToLowerInvariant()]) + $value.Trim()) | Where-Object { $_ }) -join "`n" }
    }
    return $answer
}

function Check([string]$What, [bool]$Holds, [string]$Found = '') {
    if ($Holds) { Write-Output ("ok      {0}" -f $What) }
    else { $script:failed++; Write-Output ("FAILED  {0}{1}" -f $What, $(if ($Found) { " - found: $Found" })) }
}

# Alive.
$health = Ask '/health'
Check 'the site is alive (/health answers "ok")' ($health.Status -eq 200 -and $health.Body.Trim() -eq 'ok') ("{0} {1}" -f $health.Status, $health.Body)
Check 'the answer about being alive is not kept and not indexed' ($health.Headers['cache-control'] -eq 'no-store' -and $health.Headers['x-robots-tag'] -match 'noindex')

# A page, and the headers every answer carries.
$start = Ask '/en/'
Check 'the start page answers' ($start.Status -eq 200) $start.Status
$expected = [ordered]@{
    'content-security-policy'    = "default-src 'self'"
    'x-content-type-options'     = 'nosniff'
    'x-frame-options'            = 'DENY'
    'referrer-policy'            = 'strict-origin-when-cross-origin'
    'permissions-policy'         = 'camera=\(\)'
    'cross-origin-opener-policy' = 'same-origin'
}
foreach ($answer in @(@{ Name = 'a page'; Answer = $start }, @{ Name = 'the answer about being alive'; Answer = $health })) {
    foreach ($name in $expected.Keys) {
        Check ("{0} carries {1}" -f $answer.Name, $name) ($answer.Answer.Headers[$name] -match $expected[$name]) $answer.Answer.Headers[$name]
    }
}
Check 'a page is not kept by a cache between visitors' ($start.Headers['cache-control'] -match 'no-store')
Check 'a page loads nothing from another site' (-not ($start.Body -match '(src|href)="https?://(?!www\.metmuseum\.org|umma\.umich\.edu)[^"]+\.(js|css|woff2?)"'))
Check 'the cookies of a page are HttpOnly and SameSite' (-not (($start.Headers['set-cookie'] -split "`n") | Where-Object { $_ -and ($_ -notmatch 'httponly' -or $_ -notmatch 'samesite') }))

# The collection.
$story = Ask "/en/$Collection"
if ($story.Status -eq 200) { Write-Output 'note    the site shows a published revision' }
elseif ($story.Status -eq 404) { Write-Output 'note    the site shows nothing: no published revision is mounted' }
else { Check 'the page of the collection answers 200 or 404' $false $story.Status }
if ($story.Status -eq 200) {
    Check 'a public site is not marked "noindex" on its pages' (-not $story.Headers['x-robots-tag']) $story.Headers['x-robots-tag']
    Check 'a public site does not call itself a demonstration' (-not ($story.Body -match 'Local demonstration'))
}

# What is never served.
foreach ($path in @('/content/samples/shahnameh-pilot.json', '/samples/shahnameh-pilot.json', '/content/media/dastan-family-connections.png', '/appsettings.json', '/Vafadar.Dastan.Web.dll', '/.env')) {
    $answer = Ask $path
    Check ("{0} is not served" -f $path) ($answer.Status -eq 404) $answer.Status
}
$missing = Ask '/en/no-such-page'
Check 'a page that does not exist answers 404 with a page of the site' ($missing.Status -eq 404 -and $missing.Body -match '<html')

# Behind a proxy that holds the certificate.
$proxied = Ask '/en/' @{ 'X-Forwarded-Proto' = 'https'; 'X-Forwarded-For' = '203.0.113.7'; 'Host' = 'dastan.test' }
Check 'told by the proxy that the visitor came by https, the site asks for https from then on (HSTS)' ($proxied.Headers['strict-transport-security'] -match 'max-age=\d+') $proxied.Headers['strict-transport-security']
Check 'told so, its cookies are Secure' (-not (($proxied.Headers['set-cookie'] -split "`n") | Where-Object { $_ -and $_ -notmatch 'secure' }))
Check 'not told so, it does not claim https' (-not $start.Headers['strict-transport-security'])

if ($failed -gt 0) { Write-Output ("{0} check(s) failed." -f $failed); exit 1 }
Write-Output 'All checks passed.'
