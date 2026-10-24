<# Device-local observation of one explicitly owned independent export; no product data or keys. #>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-OwnedExportObservationText([string]$Path) {
    <# .SYNOPSIS Reads an active redirected report without excluding its existing writer. #>
    $stream = [IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    try {
        $reader = [IO.StreamReader]::new($stream)
        try { $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally { $stream.Dispose() }
}

function Start-OwnedAndroidExportObservation {
    <# .SYNOPSIS Arms a device-local candidate observer before Activity launch can hide the export window. #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$AdbPath,
        [ValidateRange(1,65535)][int]$AdbPort = 5038,
        [Parameter(Mandatory)][ValidatePattern('^[a-zA-Z0-9.:-]+$')][string]$DeviceSerial,
        [ValidateSet('pro.vafadar.zanance.nativecompatibility')][string]$Package = 'pro.vafadar.zanance.nativecompatibility',
        [Parameter(Mandatory)][ValidatePattern('^files/native-compatibility/migration-proofs/[0-9a-f]{32}/profile\.db\.[0-9a-f]{32}\.cipher\.db$')][string]$CandidatePath,
        [Parameter(Mandatory)][string]$ReportPath)
    if (-not (Test-Path -LiteralPath $AdbPath -PathType Leaf)) { throw 'Use the existing reviewed adb executable.' }
    $report = [IO.Path]::GetFullPath($ReportPath)
    if (-not (Test-Path -LiteralPath ([IO.Path]::GetDirectoryName($report)) -PathType Container) -or
        (Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report + '.stderr.txt')) -or
        (Test-Path -LiteralPath ($report + '.observer.json'))) {
        throw 'A fresh caller-owned export observation report is required.'
    }
    $presenceCommand = "run-as $Package sh -c 'if [ -e $CandidatePath ]; then echo PRESENT; else echo ABSENT; fi'"
    $presence = (& $AdbPath -P $AdbPort -s $DeviceSerial shell $presenceCommand | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $presence -cne 'ABSENT') {
        throw 'The exact new candidate must be absent before arming; do not stop an existing phase.'
    }
    # D-521: am start -W and host round trips can outlive native export/verification. Arm first.
    # Only the validated independent package may be stopped, after its exact candidate appears.
    $script = 'echo ARMED; i=0; while [ $i -lt 6000 ]; do if run-as {0} sh -c ''test -e {1}'' 2>/dev/null; then p=$(pidof {0}); case $p in ''''|*[!0-9]*) echo INVALID_PID; exit 2;; esac; echo OBSERVED_PID:$p; am force-stop {0}; if pidof {0} >/dev/null; then echo STILL_LIVE; exit 3; fi; echo STOPPED; exit 0; fi; i=$((i+1)); sleep 0.01; done; echo EXPIRED; exit 4' -f $Package,$CandidatePath
    $arguments = '-P {0} -s {1} shell "{2}"' -f $AdbPort,$DeviceSerial,$script
    $process = Start-Process -FilePath $AdbPath -ArgumentList $arguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $report -RedirectStandardError ($report + '.stderr.txt')
    [pscustomobject]@{ ObserverPid = $process.Id; DeviceSerial = $DeviceSerial; CandidatePath = $CandidatePath } |
        ConvertTo-Json | Set-Content -LiteralPath ($report + '.observer.json') -Encoding UTF8
    $armed = $false
    for ($attempt = 0; $attempt -lt 100; $attempt++) {
        if ((Test-Path -LiteralPath $report) -and
            (Read-OwnedExportObservationText $report).Split([char]10).Where({ $_.Trim() -eq 'ARMED' }).Count -eq 1) {
            $armed = $true; break
        }
        $process.Refresh()
        if ($process.HasExited) { throw 'Owned export observer ended before arming; retain its report.' }
        Start-Sleep -Milliseconds 50
    }
    if (-not $armed) { throw 'Observer arming is unconfirmed; preserve its live handle and do not launch.' }
    [pscustomobject]@{ Process = $process; ReportPath = $report; DeviceSerial = $DeviceSerial; Armed = $true }
}

function Complete-OwnedAndroidExportObservation {
    <# .SYNOPSIS Reads the same armed observer; a timeout leaves the existing process intact. #>
    [CmdletBinding()]
    param([Parameter(Mandatory)]$Observation, [ValidateRange(1,60000)][int]$WaitMilliseconds = 60000)
    if (-not $Observation.Armed) { throw 'An actually armed independent export observer is required.' }
    $process = $Observation.Process
    if (-not $process.WaitForExit($WaitMilliseconds)) {
        return [pscustomobject]@{ Pending = $true; Process = $process; ReportPath = $Observation.ReportPath }
    }
    $process.WaitForExit()
    $lines = [IO.File]::ReadAllLines($Observation.ReportPath) | ForEach-Object { $_.Trim() }
    $observed = @($lines | Where-Object { $_ -match '^OBSERVED_PID:[0-9]+$' })
    # A reattached Windows process handle can lack an exit code. Keep that fact explicit;
    # actual terminal state and the exact candidate/PID/STOPPED report remain mandatory.
    $exitAvailable = $null -ne $process.ExitCode
    if (($exitAvailable -and $process.ExitCode -ne 0) -or $observed.Count -ne 1 -or @($lines | Where-Object { $_ -eq 'STOPPED' }).Count -ne 1) {
        throw 'Actual candidate observation and owned process death are unproven; retain original state.'
    }
    $result = [pscustomobject]@{ Pending = $false; ActualCandidateCreationObserved = $true;
        ObservedPid = [int]$observed[0].Substring('OBSERVED_PID:'.Length);
        ActualBoundaryProcessDeathProved = $true; ObserverExitCodeAvailable = $exitAvailable;
        ReportPath = $Observation.ReportPath }
    $process.Dispose()
    $result
}

Export-ModuleMember -Function Start-OwnedAndroidExportObservation,Complete-OwnedAndroidExportObservation
