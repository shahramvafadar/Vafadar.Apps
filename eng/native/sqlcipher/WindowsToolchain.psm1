Set-StrictMode -Version Latest

$script:MicrosoftSigner = 'CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US'
# Immutable hosted package variants, newest first. Each binds its own installer versions to one compiler.
$script:HostedVariants = @('hosted20261116','hosted20261021')

function Test-ReviewedWindowsCompilerPolicy {
    <# .SYNOPSIS Accepts only the reviewed policy shape: every compiler names its own MSVC tool set. #>
    param([Parameter(Mandatory)]$Acceptance)
    if ($Acceptance.schemaVersion -ne 3 -or
        $Acceptance.sdkVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or
        @($Acceptance.compilers).Count -eq 0 -or
        @($Acceptance.compilers | Where-Object { $_.PSObject.Properties.Name -notcontains 'toolsVersion' }).Count -or
        @($Acceptance.compilers | Where-Object { $_.sha256 -notmatch '^[a-f0-9]{64}$' -or $_.fileVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or
            $_.toolsVersion -notmatch '^\d+\.\d+\.\d+$' }).Count -or
        @($Acceptance.compilers.sha256 | Select-Object -Unique).Count -ne @($Acceptance.compilers).Count -or
        @($Acceptance.installationVersions).Count -eq 0) {
        throw 'Unsupported Windows compiler acceptance.'
    }
}

function Resolve-ReviewedWindowsToolchain {
    <# .SYNOPSIS Qualifies supplied installation metadata against the shared compiler policy and actual executable bytes. #>
    param([Parameter(Mandatory)]$Installation, [Parameter(Mandatory)]$InputLock, [Parameter(Mandatory)]$Acceptance)
    Test-ReviewedWindowsCompilerPolicy $Acceptance
    # The shared input lock is bound into qualified packages of other platforms and keeps naming the
    # tool set of the first reviewed producer. It must stay one of the reviewed tool sets; the tool set
    # that is used comes from the signed compiler found on this host, never from the lock.
    if ($InputLock.schemaVersion -ne 1 -or
        $Acceptance.sdkVersion -ne $InputLock.windowsCompiler.sdkVersion -or
        $InputLock.windowsCompiler.toolsVersion -notin @($Acceptance.compilers.toolsVersion)) {
        throw 'Unsupported Windows compiler acceptance.'
    }
    if ($Installation.installationVersion -notin $Acceptance.installationVersions -or
        -not [IO.Path]::IsPathRooted($Installation.installationPath)) {
        throw ('The installed compiler differs from the reviewed native toolchain. InstallationVersion={0}; InstallationPathRooted={1}' -f
            $Installation.installationVersion, [IO.Path]::IsPathRooted($Installation.installationPath))
    }
    $vcvars = Join-Path $Installation.installationPath 'VC/Auxiliary/Build/vcvarsall.bat'
    $present = @(@($Acceptance.compilers.toolsVersion | Select-Object -Unique) | ForEach-Object {
        $candidate = Join-Path $Installation.installationPath ('VC/Tools/MSVC/' + $_ + '/bin/Hostx64/x64/cl.exe')
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { [pscustomobject]@{ ToolsVersion = $_; Compiler = $candidate } }
    })
    if (-not (Test-Path -LiteralPath $vcvars -PathType Leaf) -or $present.Count -eq 0) {
        throw 'Pinned native compiler or bootstrap is unavailable.'
    }
    # A servicing patch can change executable bytes without changing the MSVC folder version, so a
    # tool set qualifies only with the exact bytes, file version and signer reviewed for that tool set.
    $qualified = [Collections.Generic.List[object]]::new()
    $observed = [Collections.Generic.List[string]]::new()
    foreach ($item in $present) {
        $signature = Get-AuthenticodeSignature -LiteralPath $item.Compiler
        $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($item.Compiler)
        $observedHash = (Get-FileHash -LiteralPath $item.Compiler -Algorithm SHA256).Hash.ToLowerInvariant()
        $signer = if ($signature.SignerCertificate) { $signature.SignerCertificate.Subject } else { '' }
        $reviewed = @($Acceptance.compilers | Where-Object { $_.toolsVersion -eq $item.ToolsVersion -and
            $_.sha256 -eq $observedHash -and $_.fileVersion -eq $version.FileVersion })
        $observed.Add(('Tools={0}; SHA256={1}; FileVersion={2}; Signature={3}; Signer={4}' -f
            $item.ToolsVersion, $observedHash, $version.FileVersion, $signature.Status, $signer))
        if ($reviewed.Count -eq 1 -and $signature.Status -eq 'Valid' -and $signer -ceq $script:MicrosoftSigner) {
            $qualified.Add([pscustomobject]@{ ToolsVersion = $item.ToolsVersion; Compiler = $item.Compiler;
                Sha256 = $observedHash; FileVersion = $version.FileVersion })
        }
    }
    if ($qualified.Count -eq 0) {
        throw ('Installed compiler bytes differ from the reviewed native toolchain. ' + ($observed -join ' | '))
    }
    # Two reviewed tool sets on one host would make the producer a matter of chance.
    if ($qualified.Count -ne 1) { throw 'More than one reviewed native tool set is installed; the producer would be ambiguous.' }
    [pscustomobject]@{ Installation = $Installation; Compiler = $qualified[0].Compiler; VcVars = $vcvars;
        Acceptance = $Acceptance; CompilerSha256 = $qualified[0].Sha256; CompilerFileVersion = $qualified[0].FileVersion;
        ToolsVersion = $qualified[0].ToolsVersion; SdkVersion = $Acceptance.sdkVersion }
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

function Get-WindowsProducerCompilerRecord {
    <# .SYNOPSIS The compiler description of a producer manifest: the tool set that built it, independent of the build machine. #>
    param([Parameter(Mandatory)]$InputLock, [Parameter(Mandatory)][string]$ToolsVersion, [Parameter(Mandatory)][string]$SdkVersion)
    if ($ToolsVersion -notmatch '^\d+\.\d+\.\d+$' -or $SdkVersion -cne $InputLock.windowsCompiler.sdkVersion) {
        throw 'Unsupported producer compiler record.'
    }
    # Producers of the first reviewed tool set keep their original record, so their immutable manifests stay reproducible.
    if ($ToolsVersion -ceq $InputLock.windowsCompiler.toolsVersion) { return $InputLock.windowsCompiler }
    [pscustomobject][ordered]@{ toolsVersion = $ToolsVersion; sdkVersion = $SdkVersion }
}

function Get-ReviewedWindowsWarningBaselinePath {
    <# .SYNOPSIS Selects the diagnostic inventory recorded for one compiler tool set; an inventory never serves another. #>
    param([Parameter(Mandatory)]$InputLock, [Parameter(Mandatory)][string]$ToolsVersion)
    if ($ToolsVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Unsupported compiler tool set for a diagnostic baseline.' }
    $name = if ($ToolsVersion -ceq $InputLock.windowsCompiler.toolsVersion) { 'windows.warnings.baseline.json' }
        else { 'windows.warnings.msvc-' + $ToolsVersion + '.baseline.json' }
    $path = Join-Path $PSScriptRoot $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw ('No diagnostic baseline is recorded for compiler tool set ' + $ToolsVersion + '; review its diagnostics before qualification.')
    }
    $path
}

function Get-ReviewedWindowsPackageContents {
    <# .SYNOPSIS Selects one immutable package variant, never a set of alternate entry hashes for one version. #>
    param([string]$Variant = $env:VafadarWindowsCipherVariant)
    if ($Variant -and $Variant -notin (@('baseline','servicing') + $script:HostedVariants)) { throw 'Unsupported Windows cipher package variant.' }
    $name = switch ($Variant) {
        'servicing' { 'windows.servicing.package.contents.json' }
        'hosted20261021' { 'windows.hosted20261021.package.contents.json' }
        'hosted20261116' { 'windows.hosted20261116.package.contents.json' }
        default { 'windows.package.contents.json' }
    }
    $contents = Get-Content -LiteralPath (Join-Path $PSScriptRoot $name) -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($contents.schemaVersion -ne 1 -or $contents.packageId -ne 'Vafadar.Native.SqlCipher.Windows' -or
        $contents.compilerExecutableSha256 -notmatch '^[a-f0-9]{64}$') { throw 'Unsupported native package contents.' }
    $contents
}

function Get-KnownWindowsPackageContents {
    <# .SYNOPSIS Every immutable consumer identity, so that a producer can never reuse one of them. #>
    param()
    foreach ($variant in (@('baseline','servicing') + $script:HostedVariants)) {
        $path = switch ($variant) {
            'baseline' { 'windows.package.contents.json' }
            default { 'windows.' + $variant + '.package.contents.json' }
        }
        # A hosted variant exists only after its actual producer output was reviewed and pinned.
        if (Test-Path -LiteralPath (Join-Path $PSScriptRoot $path) -PathType Leaf) { Get-ReviewedWindowsPackageContents -Variant $variant }
        elseif ($variant -in @('baseline','servicing','hosted20261021')) { throw 'A reviewed native package inventory is missing.' }
    }
}

function Resolve-ReviewedWindowsProducer {
    <# .SYNOPSIS Keeps rolling hosted installations on separately qualified immutable native producers. #>
    param([Parameter(Mandatory)][string]$InstallationVersion, [Parameter(Mandatory)][string]$CompilerSha256)
    $policy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.compiler.acceptance.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($InstallationVersion -notin $policy.installationVersions -or $CompilerSha256 -notin $policy.compilers.sha256) {
        throw 'Unsupported Windows producer host.'
    }
    # The same reviewed cl.exe can produce different native bytes on a serviced host. Never
    # merge those outputs under one package identity or infer a compiler-internal cause.
    foreach ($variant in $script:HostedVariants) {
        if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot ('windows.' + $variant + '.package.contents.json')) -PathType Leaf)) { continue }
        $current = Get-ReviewedWindowsPackageContents -Variant $variant
        if ($InstallationVersion -in $current.hostedInstallationVersions) {
            if ($CompilerSha256 -cne $current.compilerExecutableSha256) { throw 'Unsupported Windows producer host.' }
            return $variant
        }
    }
    foreach ($variant in @('baseline','servicing')) {
        $contents = Get-ReviewedWindowsPackageContents -Variant $variant
        if ($CompilerSha256 -ceq $contents.compilerExecutableSha256) { return $variant }
    }
    throw 'Unsupported Windows producer host.'
}

function Assert-RequestedWindowsProducer {
    <# .SYNOPSIS Refuses a different rolling image before repeating native work for a specific producer request. #>
    param([Parameter(Mandatory)]$Observation, [Parameter(Mandatory)]$Request,
        [Parameter(Mandatory)]$Policy, [Parameter(Mandatory)][string]$CandidateVersion)
    if ($Request.schemaVersion -ne 1 -or $Policy.schemaVersion -ne 3 -or
        $Request.candidateVersion -cne $CandidateVersion -or
        $CandidateVersion -cnotmatch '^\d+\.\d+\.\d+-candidate\.[1-9][0-9]*$' -or
        $Request.installationVersion -notin $Policy.installationVersions -or
        $Request.sdkVersion -cne $Policy.sdkVersion -or
        @($Policy.compilers | Where-Object { $_.sha256 -ceq $Request.compilerSha256 -and $_.fileVersion -ceq $Request.compilerFileVersion -and
            $_.toolsVersion -ceq $Request.toolsVersion }).Count -ne 1) {
        throw 'Unsupported requested Windows producer identity.'
    }
    if ($Observation.InstallationVersion -cne $Request.installationVersion) {
        throw ('The requested producer image is not available on this runner. Required={0}; Observed={1}; no native build is admitted.' -f
            $Request.installationVersion, $Observation.InstallationVersion)
    }
    if ($Observation.ToolsVersion -cne $Request.toolsVersion -or $Observation.SdkVersion -cne $Request.sdkVersion -or
        -not $Observation.CompilerPresent -or -not $Observation.SdkPresent -or
        $Observation.CompilerSha256 -cne $Request.compilerSha256 -or $Observation.CompilerFileVersion -cne $Request.compilerFileVersion -or
        $Observation.SignatureStatus -cne 'Valid' -or
        $Observation.SignerSubject -cne $script:MicrosoftSigner -or
        $Observation.CompilerExecuted -ne $false -or $Observation.ReviewedAdmissionGranted -ne $false) {
        throw 'Observed compiler evidence differs from the requested Windows producer.'
    }
    # This admits only the requested observation to the existing compiler/native/source gates.
    # It does not qualify native outputs or select a new consumer package identity.
}

function Test-RequestedWindowsProducerCompiler {
    <# .SYNOPSIS Whether a qualified host is the one an explicit producer request names, before any consumer identity exists for it. #>
    param([Parameter(Mandatory)]$Toolchain, [Parameter(Mandatory)]$Request, [Parameter(Mandatory)][string]$CandidateVersion)
    return ($Request.schemaVersion -eq 1 -and $CandidateVersion -and $Request.candidateVersion -ceq $CandidateVersion -and
        $Request.installationVersion -ceq $Toolchain.Installation.installationVersion -and
        $Request.compilerSha256 -ceq $Toolchain.CompilerSha256 -and $Request.compilerFileVersion -ceq $Toolchain.CompilerFileVersion -and
        $Request.toolsVersion -ceq $Toolchain.ToolsVersion -and $Request.sdkVersion -ceq $Toolchain.SdkVersion)
}

Export-ModuleMember -Function Test-ReviewedWindowsCompilerPolicy,Resolve-ReviewedWindowsToolchain,Get-ReviewedWindowsToolchain,Get-WindowsProducerCompilerRecord,Get-ReviewedWindowsWarningBaselinePath,Get-ReviewedWindowsPackageContents,Get-KnownWindowsPackageContents,Resolve-ReviewedWindowsProducer,Assert-RequestedWindowsProducer,Test-RequestedWindowsProducerCompiler
