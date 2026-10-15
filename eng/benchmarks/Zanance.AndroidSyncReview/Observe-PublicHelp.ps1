param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$Run,
    [Parameter(Mandatory)][ValidateRange(1,2147483647)][int]$ProcessId,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(1,240)][int]$ObserveSeconds=180
)
$ErrorActionPreference='Stop'
$adb='C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$serial='emulator-5570'
$package='pro.vafadar.zanance.androidsyncpublicreview'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$allowed=[IO.Path]::GetFullPath((Join-Path $repo 'artifacts'))+[IO.Path]::DirectorySeparatorChar
$root=[IO.Path]::GetFullPath($OutputDirectory)
if (-not $root.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Owned artifact output required.' }
$avd=& $adb -P 5038 -s $serial emu avd name
if ($LASTEXITCODE -ne 0 -or $avd[0].Trim() -ne 'Zanance_Storage_Review') { throw 'Owned review emulator required.' }
New-Item -ItemType Directory -Force $root | Out-Null
$statePath=Join-Path $root 'help-observed.json'
$cases=@()
if (Test-Path -LiteralPath $statePath) {
    $state=Get-Content -LiteralPath $statePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($state.Run -ne $Run -or $state.ProcessId -ne $ProcessId) { throw 'Original observer authority differs; preserve it.' }
    $cases=@($state.Cases)
}
function Require-Live {
    $actual=(& $adb -P 5038 -s $serial shell pidof $package) -join ''
    if ($LASTEXITCODE -ne 0 -or $actual.Trim() -ne [string]$ProcessId) { throw 'Original process is not live; never restart on observation expiry.' }
}
function Require-OwnFocus {
    Require-Live
    $focus=(& $adb -P 5038 -s $serial shell dumpsys window) | Select-String 'mFocusedApp=' | Select-Object -First 1
    if (-not $focus -or -not $focus.Line.Contains($package)) { throw 'Native action refused outside the original independent window.' }
}
$deadline=[DateTime]::UtcNow.AddSeconds($ObserveSeconds)
while ($cases.Count -lt 6 -and [DateTime]::UtcNow -lt $deadline) {
    Require-Live
    $lines=& $adb -P 5038 -s $serial logcat -d -v raw "--pid=$ProcessId" -s 'ZananceSyncHelp:I' '*:S'
    if ($LASTEXITCODE -ne 0) { throw 'Observation failed; original live process is preserved.' }
    foreach ($line in $lines) {
        if ($line -notmatch ('^'+$Run+'\|(\{.*\})$')) { continue }
        $message=$Matches[1] | ConvertFrom-Json
        if ($message.Language -notin @('en','fa','de') -or $message.Theme -notin @('Light','Dark') -or $message.ExpectedSha256 -notmatch '^[0-9a-f]{64}$') { throw 'Unexpected public help receipt.' }
        $key=$message.Language+'-'+$message.Theme
        if (@($cases | Where-Object { $_.Key -eq $key }).Count -gt 0) { continue }
        Require-OwnFocus
        $remote='/data/local/tmp/zanance-sync-public-'+$Run+'.xml'
        $capture=Join-Path $root ($key+'-'+[Guid]::NewGuid().ToString('N')+'.xml')
        & $adb -P 5038 -s $serial shell uiautomator dump --compressed $remote | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Native dialog read failed; original process remains.' }
        & $adb -P 5038 -s $serial pull $remote $capture 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Owned dialog copy failed; do not restart.' }
        [xml]$tree=Get-Content -LiteralPath $capture -Raw -Encoding UTF8
        $body=$tree.SelectSingleNode("//node[@resource-id='android:id/message']")
        $button=$tree.SelectSingleNode("//node[@class='android.widget.Button' and (@resource-id='android:id/button1' or @resource-id='android:id/button2')]")
        if (-not $body -or -not $button -or $body.GetAttribute('package') -ne $package -or $button.GetAttribute('package') -ne $package) { throw 'Original help dialog targets are unavailable.' }
        $hash=[Security.Cryptography.SHA256]::Create()
        try { $actual=([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($body.GetAttribute('text'))))).Replace('-','').ToLowerInvariant() } finally { $hash.Dispose() }
        if ($actual -ne $message.ExpectedSha256) { throw 'Actual native complete help text differs; preserve the dialog and evidence.' }
        if ($button.GetAttribute('bounds') -notmatch '^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$') { throw 'Native OK geometry is missing.' }
        $left=[int]$Matches[1]; $top=[int]$Matches[2]; $right=[int]$Matches[3]; $bottom=[int]$Matches[4]
        if ($right -le $left -or $bottom -le $top -or $right -gt 5000 -or $bottom -gt 5000) { throw 'Native OK target is outside bounded geometry.' }
        Require-OwnFocus
        & $adb -P 5038 -s $serial shell input tap ([int](($left+$right)/2)) ([int](($top+$bottom)/2))
        if ($LASTEXITCODE -ne 0) { throw 'Original native OK invocation failed.' }
        $cases+=@([ordered]@{Key=$key; NativeBodyMatched=$true; NativeOkBounds=$button.GetAttribute('bounds'); Capture=$capture})
        [ordered]@{Run=$Run; ProcessId=$ProcessId; Cases=$cases} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath -Encoding UTF8
        Write-Output ('Observed original native help: '+$key)
    }
    if ($cases.Count -lt 6) { Start-Sleep -Milliseconds 700 }
}
if ($cases.Count -lt 6) { throw 'Observation deadline ended; continue the same live process and observer state without reinstalling or restarting.' }
Write-Output 'All six original native help dialogs matched and closed through their actual targets.'
