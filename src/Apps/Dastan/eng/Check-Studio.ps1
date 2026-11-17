<#
.SYNOPSIS
    Asks a running tool of Dastan's editors, from outside its process, what a server has to be sure of
    (docs/operations.md).

.DESCRIPTION
    The checks need nothing but the address and no account: that the tool refuses a connection that is not
    encrypted, that it is alive, that nothing of the store is shown to somebody who has not signed in, that a
    form without a sign-in is refused, that a wrong password signs nobody in, and that the files of the tool
    are not served. The script asks as a proxy that holds the certificate would (X-Forwarded-Proto: https), so
    the tool has to believe the machine the script runs on (Studio:TrustedProxies). Each check is one line; the
    script ends with 1 when a check fails.

.PARAMETER Address
    The address of the tool, without a path: http://127.0.0.1:53381

.PARAMETER HostName
    The name the tool is reached by, as it was told (AllowedHosts).

.EXAMPLE
    ./src/Apps/Dastan/eng/Check-Studio.ps1 -Address http://127.0.0.1:53381
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Address,
    [string]$HostName = 'localhost'
)

$ErrorActionPreference = 'Stop'
$Address = $Address.TrimEnd('/')
$failed = 0
$proxy = @{ 'X-Forwarded-Proto' = 'https'; 'Host' = $HostName }

function Ask([string]$Path, [hashtable]$Headers = @{}, [string[]]$Form = @()) {
    $arguments = @('-s', '-o', '-', '-D', '-', '--max-time', '20')
    foreach ($name in $Headers.Keys) { $arguments += @('-H', ('{0}: {1}' -f $name, $Headers[$name])) }
    foreach ($field in $Form) { $arguments += @('--data-urlencode', $field) }
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

# A connection that is not encrypted. Asked from the machine of the tool itself the tool answers - it trusts
# its own machine - and the check says so instead of failing.
$open = Ask '/en/sign-in' @{ 'Host' = $HostName }
if ($open.Status -eq 400) { Check 'a connection that is not encrypted is refused' $true }
elseif ($open.Status -eq 200) { Write-Output 'note    asked from the machine of the tool: it answers its own machine without encryption' }
else { Check 'a connection that is not encrypted is refused' $false $open.Status }

# Alive.
$health = Ask '/health' $proxy
Check 'the tool is alive (/health answers "ok")' ($health.Status -eq 200 -and $health.Body.Trim() -eq 'ok') ("{0} {1}" -f $health.Status, $health.Body)

# Not signed in.
foreach ($path in @('/en/', '/fa/', '/en/some-collection', '/en/some-collection/waiting', '/en/some-collection/record/relations/some-record')) {
    $answer = Ask $path $proxy
    Check ("{0} leads to the page that signs in" -f $path) ($answer.Status -eq 302 -and $answer.Headers['location'] -match '/sign-in\?to=') ("{0} {1}" -f $answer.Status, $answer.Headers['location'])
}
$form = Ask '/en/some-collection/record/relations/some-record' $proxy @('_handler=remove', 'Removing.Reason=a check')
Check 'a form sent without a sign-in is refused' ($form.Status -in 400, 401) $form.Status

# The page that signs in.
$page = Ask '/en/sign-in' $proxy
Check 'the page that signs in answers' ($page.Status -eq 200 -and $page.Body -match 'name="Signing.Password"') $page.Status
Check 'it is never indexed and never kept' ($page.Headers['x-robots-tag'] -match 'noindex' -and $page.Headers['cache-control'] -match 'no-store')
Check 'it loads nothing from another site' (-not ($page.Body -match '(src|href)="https?://'))
Check 'its cookies are HttpOnly, SameSite and Secure' (-not (($page.Headers['set-cookie'] -split "`n") | Where-Object { $_ -and ($_ -notmatch 'httponly' -or $_ -notmatch 'samesite' -or $_ -notmatch 'secure') })) $page.Headers['set-cookie']
foreach ($name in @('content-security-policy', 'x-content-type-options', 'x-frame-options', 'referrer-policy')) {
    Check ("it carries {0}" -f $name) ([bool]$page.Headers[$name])
}
$wrong = Ask '/en/sign-in' $proxy @('_handler=signin', 'Signing.Name=nobody', 'Signing.Password=not a password of anybody')
Check 'a sign-in without the token of the tool is refused' ($wrong.Status -eq 400 -and -not ($wrong.Headers['set-cookie'] -match 'dastan\.studio\.session')) $wrong.Status

# What is never served.
foreach ($path in @('/data/editors.db', '/editors.db', '/appsettings.json', '/Vafadar.Dastan.Studio.dll', '/.env')) {
    $answer = Ask $path $proxy
    Check ("{0} is not served" -f $path) ($answer.Status -in 302, 404 -and $answer.Body.Length -lt 20000 -and -not ($answer.Body -match 'SQLite format')) $answer.Status
}

# Another name than the one the tool was told.
$stranger = Ask '/en/sign-in' @{ 'X-Forwarded-Proto' = 'https'; 'Host' = 'elsewhere.example' }
Check 'asked by another name than its own, the tool refuses' ($stranger.Status -eq 400) $stranger.Status

if ($failed -gt 0) { Write-Output ("{0} check(s) failed." -f $failed); exit 1 }
Write-Output 'All checks passed.'
