<#
.SYNOPSIS
    Runs one repository SDK command with project-owned state and installed workload discovery.
.DESCRIPTION
    Forwards arguments unchanged. A portable SDK selected by global.json can reuse existing
    workload packs without installing/copying them or changing persistent environment settings.
    Every environment value is restored when the command ends. Direct application DLL execution
    retains normal host/runtime semantics; sdk.paths only selects SDK commands.
.EXAMPLE
    ./eng/scripts/Invoke-ProjectDotNet.ps1 build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-android
#>
$ErrorActionPreference = 'Stop'
# Native SDK failures retain their exit code for the caller's existing retry/reporting logic.
$PSNativeCommandUseErrorActionPreference = $false
$forwardedArguments = [string[]]$args
if ($forwardedArguments.Count -eq 0) { throw 'Supply a dotnet SDK command and its arguments.' }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artifactRoot = Join-Path $repoRoot 'artifacts'
$stateRoot = Join-Path $artifactRoot 'project-sdk-state'
$pathComparison = [StringComparison]::Ordinal
if ($IsWindows) { $pathComparison = [StringComparison]::OrdinalIgnoreCase }
$hostCommand = Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1
$hostFile = Get-Item -LiteralPath $hostCommand.Source
if ($hostFile.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    $resolvedHost = $hostFile.ResolveLinkTarget($true)
    if ($resolvedHost) { $hostFile = $resolvedHost }
}
$hostExecutable = $hostFile.FullName
$hostRoot = Split-Path -Parent $hostExecutable
$savedEnvironment = @{}
$callerLocation = Get-Location

function Set-InvocationEnvironment([string]$Name, [string]$Value) {
    if (-not $savedEnvironment.ContainsKey($Name)) {
        $savedEnvironment[$Name] = [Environment]::GetEnvironmentVariable($Name, 'Process')
    }
    [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
}

function Set-OwnedState([string]$Name, [string]$RelativePath) {
    $current = [Environment]::GetEnvironmentVariable($Name, 'Process')
    $owned = $false
    if ($current) {
        try {
            $full = [IO.Path]::GetFullPath($current)
            $owned = $full.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, $pathComparison)
        } catch { $owned = $false }
    }
    if (-not $owned) { $current = Join-Path $stateRoot $RelativePath }
    [IO.Directory]::CreateDirectory($current) | Out-Null
    Set-InvocationEnvironment $Name $current
}

$exitCode = 1
try {
    Set-Location -LiteralPath $repoRoot
    Set-OwnedState 'DOTNET_CLI_HOME' 'cli-home'
    Set-OwnedState 'TEMP' 'temp'
    Set-OwnedState 'TMP' 'temp'
    Set-OwnedState 'NUGET_PACKAGES' 'packages'
    Set-OwnedState 'NUGET_HTTP_CACHE_PATH' 'http-cache'
    Set-OwnedState 'NUGET_PLUGINS_CACHE_PATH' 'plugin-cache'
    Set-InvocationEnvironment 'DOTNET_CLI_TELEMETRY_OPTOUT' '1'
    Set-InvocationEnvironment 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE' '1'
    Set-InvocationEnvironment 'DOTNET_GENERATE_ASPNET_CERTIFICATE' 'false'
    Set-InvocationEnvironment 'DOTNET_ADD_GLOBAL_TOOLS_TO_PATH' '0'
    Set-InvocationEnvironment 'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE' 'true'
    Set-InvocationEnvironment 'DOTNET_NOLOGO' '1'

    # SDK search paths do not make another installation's workload packs visible.
    # Respect explicit process roots; otherwise discover an existing MAUI installation.
    $candidates = @($hostRoot)
    if ($IsWindows) {
        $programFilesRoot = [Environment]::GetFolderPath('ProgramFiles')
        if ($programFilesRoot) { $candidates += Join-Path $programFilesRoot 'dotnet' }
    }
    $sharedRoot = $candidates | Select-Object -Unique | Where-Object {
        Test-Path -LiteralPath (Join-Path $_ 'packs/Microsoft.Maui.Sdk') -PathType Container
    } | Select-Object -First 1
    if ($sharedRoot) {
        if (-not [Environment]::GetEnvironmentVariable('DOTNETSDK_WORKLOAD_PACK_ROOTS', 'Process')) {
            Set-InvocationEnvironment 'DOTNETSDK_WORKLOAD_PACK_ROOTS' $sharedRoot
        }
        if (-not [Environment]::GetEnvironmentVariable('DOTNETSDK_WORKLOAD_MANIFEST_ROOTS', 'Process')) {
            Set-InvocationEnvironment 'DOTNETSDK_WORKLOAD_MANIFEST_ROOTS' (Join-Path $sharedRoot 'sdk-manifests')
        }
    }
    & $hostExecutable @forwardedArguments
    $exitCode = $LASTEXITCODE
} finally {
    foreach ($name in $savedEnvironment.Keys) {
        if ($null -eq $savedEnvironment[$name]) {
            # Empty and absent environment values differ on current .NET/PowerShell.
            Remove-Item -LiteralPath ('Env:' + $name) -ErrorAction SilentlyContinue
        } else {
            [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
        }
    }
    Set-Location -LiteralPath $callerLocation.Path
}
exit $exitCode
