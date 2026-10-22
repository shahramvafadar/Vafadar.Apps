Set-StrictMode -Version Latest

function Resolve-ReviewedWindowsToolchain {
    <# .SYNOPSIS Qualifies supplied installation metadata against the shared compiler policy and actual executable bytes. #>
    param([Parameter(Mandatory)]$Installation, [Parameter(Mandatory)]$InputLock, [Parameter(Mandatory)]$Acceptance)
    if ($InputLock.schemaVersion -ne 1 -or $Acceptance.schemaVersion -ne 2 -or
        $Acceptance.toolsVersion -ne $InputLock.windowsCompiler.toolsVersion -or
        $Acceptance.sdkVersion -ne $InputLock.windowsCompiler.sdkVersion -or
        $Acceptance.toolsVersion -notmatch '^\d+\.\d+\.\d+$' -or
        $Acceptance.sdkVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or
        @($Acceptance.compilers).Count -eq 0 -or
        @($Acceptance.compilers | Where-Object { $_.sha256 -notmatch '^[a-f0-9]{64}$' -or $_.fileVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' }).Count -or
        @($Acceptance.compilers.sha256 | Select-Object -Unique).Count -ne @($Acceptance.compilers).Count -or
        @($Acceptance.installationVersions).Count -eq 0) {
        throw 'Unsupported Windows compiler acceptance.'
    }
    if ($Installation.installationVersion -notin $Acceptance.installationVersions -or
        -not [IO.Path]::IsPathRooted($Installation.installationPath)) {
        throw ('The installed compiler differs from the reviewed native toolchain. InstallationVersion={0}; InstallationPathRooted={1}' -f
            $Installation.installationVersion, [IO.Path]::IsPathRooted($Installation.installationPath))
    }
    $vcvars = Join-Path $Installation.installationPath 'VC/Auxiliary/Build/vcvarsall.bat'
    $compiler = Join-Path $Installation.installationPath ('VC/Tools/MSVC/' + $Acceptance.toolsVersion + '/bin/Hostx64/x64/cl.exe')
    if (-not (Test-Path -LiteralPath $vcvars -PathType Leaf) -or
        -not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
        throw 'Pinned native compiler or bootstrap is unavailable.'
    }
    # A servicing patch can change executable bytes without changing the MSVC folder version.
    $signature = Get-AuthenticodeSignature -LiteralPath $compiler
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($compiler)
    $observedHash = (Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant()
    $reviewed = @($Acceptance.compilers | Where-Object { $_.sha256 -eq $observedHash -and $_.fileVersion -eq $version.FileVersion })
    $signer = if ($signature.SignerCertificate) { $signature.SignerCertificate.Subject } else { '' }
    if ($reviewed.Count -ne 1 -or $signature.Status -ne 'Valid' -or
        $signer -cne 'CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US') {
        throw ('Installed compiler bytes differ from the reviewed native toolchain. SHA256={0}; FileVersion={1}; Signature={2}; Signer={3}' -f
            $observedHash, $version.FileVersion, $signature.Status, $signer)
    }
    [pscustomobject]@{ Installation = $Installation; Compiler = $compiler; VcVars = $vcvars;
        Acceptance = $Acceptance; CompilerSha256 = $observedHash; CompilerFileVersion = $version.FileVersion; ToolsVersion = $Acceptance.toolsVersion; SdkVersion = $Acceptance.sdkVersion }
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

function Get-ReviewedWindowsPackageContents {
    <# .SYNOPSIS Selects one immutable package variant, never a set of alternate entry hashes for one version. #>
    param([string]$Variant = $env:VafadarWindowsCipherVariant)
    if ($Variant -and $Variant -notin @('baseline','servicing','hosted20261021')) { throw 'Unsupported Windows cipher package variant.' }
    $name = switch ($Variant) {
        'servicing' { 'windows.servicing.package.contents.json' }
        'hosted20261021' { 'windows.hosted20261021.package.contents.json' }
        default { 'windows.package.contents.json' }
    }
    $contents = Get-Content -LiteralPath (Join-Path $PSScriptRoot $name) -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($contents.schemaVersion -ne 1 -or $contents.packageId -ne 'Vafadar.Native.SqlCipher.Windows' -or
        $contents.compilerExecutableSha256 -notmatch '^[a-f0-9]{64}$') { throw 'Unsupported native package contents.' }
    $contents
}

function Resolve-ReviewedWindowsProducer {
    <# .SYNOPSIS Keeps rolling hosted installations on separately qualified immutable native producers. #>
    param([Parameter(Mandatory)][string]$InstallationVersion, [Parameter(Mandatory)][string]$CompilerSha256)
    $policy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.compiler.acceptance.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($InstallationVersion -notin $policy.installationVersions -or $CompilerSha256 -notin $policy.compilers.sha256) {
        throw 'Unsupported Windows producer host.'
    }
    $current = Get-ReviewedWindowsPackageContents -Variant hosted20261021
    # The same reviewed cl.exe can produce different native bytes on a serviced host. Never
    # merge those outputs under one package identity or infer a compiler-internal cause.
    if ($InstallationVersion -in $current.hostedInstallationVersions) {
        if ($CompilerSha256 -cne $current.compilerExecutableSha256) { throw 'Unsupported Windows producer host.' }
        return 'hosted20261021'
    }
    foreach ($variant in @('baseline','servicing')) {
        $contents = Get-ReviewedWindowsPackageContents -Variant $variant
        if ($CompilerSha256 -ceq $contents.compilerExecutableSha256) { return $variant }
    }
    throw 'Unsupported Windows producer host.'
}

Export-ModuleMember -Function Resolve-ReviewedWindowsToolchain,Get-ReviewedWindowsToolchain,Get-ReviewedWindowsPackageContents,Resolve-ReviewedWindowsProducer
