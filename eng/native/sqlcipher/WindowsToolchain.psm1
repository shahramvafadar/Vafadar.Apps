Set-StrictMode -Version Latest

function Resolve-ReviewedWindowsToolchain {
    <# .SYNOPSIS Qualifies supplied installation metadata against the shared compiler policy and actual executable bytes. #>
    param([Parameter(Mandatory)]$Installation, [Parameter(Mandatory)]$InputLock, [Parameter(Mandatory)]$Acceptance)
    if ($InputLock.schemaVersion -ne 1 -or $Acceptance.schemaVersion -ne 1 -or
        $Acceptance.toolsVersion -ne $InputLock.windowsCompiler.toolsVersion -or
        $Acceptance.sdkVersion -ne $InputLock.windowsCompiler.sdkVersion -or
        $Acceptance.toolsVersion -notmatch '^\d+\.\d+\.\d+$' -or
        $Acceptance.sdkVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or
        $Acceptance.compilerExecutableSha256 -notmatch '^[a-f0-9]{64}$' -or
        @($Acceptance.installationVersions).Count -eq 0) {
        throw 'Unsupported Windows compiler acceptance.'
    }
    if ($Installation.installationVersion -notin $Acceptance.installationVersions -or
        -not [IO.Path]::IsPathRooted($Installation.installationPath)) {
        throw 'The installed compiler differs from the reviewed native toolchain.'
    }
    $vcvars = Join-Path $Installation.installationPath 'VC/Auxiliary/Build/vcvarsall.bat'
    $compiler = Join-Path $Installation.installationPath ('VC/Tools/MSVC/' + $Acceptance.toolsVersion + '/bin/Hostx64/x64/cl.exe')
    if (-not (Test-Path -LiteralPath $vcvars -PathType Leaf) -or
        -not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
        throw 'Pinned native compiler or bootstrap is unavailable.'
    }
    if ((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Acceptance.compilerExecutableSha256) {
        throw 'Installed compiler bytes differ from the reviewed native toolchain.'
    }
    [pscustomobject]@{ Installation = $Installation; Compiler = $compiler; VcVars = $vcvars;
        Acceptance = $Acceptance; ToolsVersion = $Acceptance.toolsVersion; SdkVersion = $Acceptance.sdkVersion }
}

function Get-ReviewedWindowsToolchain {
    <# .SYNOPSIS Reads the installed native toolchain once and applies the same reviewed policy for builds and checks. #>
    param()
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $records = @(& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0 -or $records.Count -ne 1) { throw 'A unique installed native toolchain is unavailable.' }
    $lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $acceptance = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.compiler.acceptance.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Resolve-ReviewedWindowsToolchain -Installation $records[0] -InputLock $lock -Acceptance $acceptance
}

Export-ModuleMember -Function Resolve-ReviewedWindowsToolchain,Get-ReviewedWindowsToolchain
