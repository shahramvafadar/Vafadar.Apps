<# .SYNOPSIS Qualifies actual Windows compiler bytes and exercises refused metadata before native execution. #>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'WindowsToolchain.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$actual = Get-ReviewedWindowsToolchain
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$acceptance = $actual.Acceptance
$results = [Collections.Generic.List[string]]::new()
function Copy-Policy {
    <# .SYNOPSIS Keeps each malicious-input fixture independent of the real reviewed policy. #>
    param($Value)
    $Value | ConvertTo-Json -Depth 20 | ConvertFrom-Json
}
function Require-Refused {
    <# .SYNOPSIS Requires qualification to fail before any compiler/bootstrap can be executed. #>
    param([string]$Name,[scriptblock]$Action,[string]$Message)
    $refused = $false
    try { & $Action | Out-Null } catch {
        if ($_.Exception.Message -notlike $Message) { throw }
        $refused = $true
    }
    if (-not $refused) { throw "Unsafe toolchain metadata accepted: $Name" }
    $results.Add($Name)
}
foreach ($version in $acceptance.installationVersions) {
    $metadata = Copy-Policy $actual.Installation
    $metadata.installationVersion = $version
    $qualified = Resolve-ReviewedWindowsToolchain $metadata $inputLock $acceptance
    if ($qualified.Compiler -ne $actual.Compiler) { throw 'Accepted metadata changed compiler selection.' }
    $results.Add('accepted-metadata-' + $version)
}
$foreign = Copy-Policy $actual.Installation; $foreign.installationVersion = '99.0.0.0'
Require-Refused 'unknown-installer' { Resolve-ReviewedWindowsToolchain $foreign $inputLock $acceptance } '*differs from the reviewed native toolchain*'
$relative = Copy-Policy $actual.Installation; $relative.installationPath = 'relative-fixture'
Require-Refused 'relative-installation' { Resolve-ReviewedWindowsToolchain $relative $inputLock $acceptance } '*differs from the reviewed native toolchain*'
$sdk = Copy-Policy $inputLock; $sdk.windowsCompiler.sdkVersion = '10.0.0.0'
Require-Refused 'unreviewed-sdk' { Resolve-ReviewedWindowsToolchain $actual.Installation $sdk $acceptance } '*Unsupported Windows compiler acceptance*'
$tools = Copy-Policy $inputLock; $tools.windowsCompiler.toolsVersion = '14.0.0'
Require-Refused 'unreviewed-tools' { Resolve-ReviewedWindowsToolchain $actual.Installation $tools $acceptance } '*Unsupported Windows compiler acceptance*'
$hash = Copy-Policy $acceptance
foreach ($item in $hash.compilers) { if ($item.sha256 -eq $actual.CompilerSha256) { $item.sha256 = ('0' * 64) } }
Require-Refused 'changed-compiler-bytes' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $hash } '*Installed compiler bytes differ*'
$malformed = Copy-Policy $acceptance; $malformed.compilers[0].sha256 = 'not-a-hash'
Require-Refused 'malformed-compiler-hash' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $malformed } '*Unsupported Windows compiler acceptance*'
$schema = Copy-Policy $acceptance; $schema.schemaVersion = 4
Require-Refused 'unknown-policy-schema' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $schema } '*Unsupported Windows compiler acceptance*'
# The earlier policy named one tool set for every compiler; it can no longer admit a host.
$legacy = Copy-Policy $acceptance; $legacy.schemaVersion = 2
$legacy | Add-Member -NotePropertyName toolsVersion -NotePropertyValue $actual.ToolsVersion
Require-Refused 'legacy-single-tool-set-policy' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $legacy } '*Unsupported Windows compiler acceptance*'
$unnamed = Copy-Policy $acceptance
$unnamed.compilers = @($unnamed.compilers | ForEach-Object { [pscustomobject]@{ sha256=$_.sha256; fileVersion=$_.fileVersion } })
Require-Refused 'compiler-without-tool-set' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $unnamed } '*Unsupported Windows compiler acceptance*'
$badTools = Copy-Policy $acceptance; $badTools.compilers[0].toolsVersion = '14.latest'
Require-Refused 'malformed-compiler-tool-set' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $badTools } '*Unsupported Windows compiler acceptance*'
# The exact reviewed bytes qualify only inside the tool set they were reviewed for.
$moved = Copy-Policy $acceptance
foreach ($item in $moved.compilers) { if ($item.sha256 -eq $actual.CompilerSha256) { $item.toolsVersion = '99.0.0' } }
$movedMessage = if (@($moved.compilers | Where-Object { $_.toolsVersion -eq $actual.ToolsVersion }).Count) { '*Installed compiler bytes differ*' } else { '*compiler or bootstrap is unavailable*' }
$movedLock = Copy-Policy $inputLock; $movedLock.windowsCompiler.toolsVersion = @($moved.compilers.toolsVersion | Where-Object { $_ -ne '99.0.0' } | Select-Object -First 1)
Require-Refused 'reviewed-bytes-in-another-tool-set' { Resolve-ReviewedWindowsToolchain $actual.Installation $movedLock $moved } $movedMessage
# The shared lock is not the source of the tool set: the qualified one is the signed compiler of this host.
if ($actual.ToolsVersion -notin @($acceptance.compilers.toolsVersion) -or
    @($acceptance.compilers | Where-Object { $_.sha256 -eq $actual.CompilerSha256 -and $_.toolsVersion -eq $actual.ToolsVersion -and $_.fileVersion -eq $actual.CompilerFileVersion }).Count -ne 1 -or
    $actual.Compiler -notlike ('*' + $actual.ToolsVersion + '*')) { throw 'The qualified tool set is not the reviewed tool set of the installed compiler.' }
$results.Add('tool-set-from-qualified-compiler-' + $actual.ToolsVersion)
if ((Get-WindowsProducerCompilerRecord -InputLock $inputLock -ToolsVersion $inputLock.windowsCompiler.toolsVersion -SdkVersion $inputLock.windowsCompiler.sdkVersion | ConvertTo-Json -Compress) -cne ($inputLock.windowsCompiler | ConvertTo-Json -Compress)) {
    throw 'The first reviewed tool set lost its original producer record.'
}
$results.Add('original-producer-record-preserved')
Require-Refused 'producer-record-unreviewed-sdk' { Get-WindowsProducerCompilerRecord -InputLock $inputLock -ToolsVersion $actual.ToolsVersion -SdkVersion '10.0.0.0' } '*Unsupported producer compiler record*'
Require-Refused 'diagnostic-baseline-of-unknown-tool-set' { Get-ReviewedWindowsWarningBaselinePath -InputLock $inputLock -ToolsVersion '99.0.0' } '*No diagnostic baseline is recorded*'
$empty = Copy-Policy $acceptance; $empty.installationVersions = @()
Require-Refused 'empty-installer-allowlist' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $empty } '*Unsupported Windows compiler acceptance*'
$wrongVersion = Copy-Policy $acceptance
foreach ($item in $wrongVersion.compilers) { $item.fileVersion = '0.0.0.0' }
Require-Refused 'changed-executable-version' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $wrongVersion } '*Installed compiler bytes differ*'
$duplicate = Copy-Policy $acceptance; $duplicate.compilers = @($duplicate.compilers[0], $duplicate.compilers[0])
Require-Refused 'duplicate-compiler-policy' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $duplicate } '*Unsupported Windows compiler acceptance*'
# Package variants must be explicit and immutable; never accept alternate native bytes in one version.
$originalVariant = $env:VafadarWindowsCipherVariant
try {
    $env:VafadarWindowsCipherVariant = 'baseline'; $baseline = Get-ReviewedWindowsPackageContents
    $env:VafadarWindowsCipherVariant = 'servicing'; $servicing = Get-ReviewedWindowsPackageContents
    if ($baseline.version -eq $servicing.version -or $baseline.compilerExecutableSha256 -eq $servicing.compilerExecutableSha256 -or
        $baseline.entries.'runtimes/win-x64/native/sqlcipher.dll' -eq $servicing.entries.'runtimes/win-x64/native/sqlcipher.dll') {
        throw 'Servicing native output lost its distinct immutable identity.'
    }
    $results.Add('distinct-immutable-servicing-package')
    $current = Get-ReviewedWindowsPackageContents -Variant hosted20261021
    if ($current.version -in @($baseline.version,$servicing.version) -or
        $current.entries.'runtimes/win-x64/native/sqlcipher.dll' -in @($baseline.entries.'runtimes/win-x64/native/sqlcipher.dll',$servicing.entries.'runtimes/win-x64/native/sqlcipher.dll') -or
        $current.compilerExecutableSha256 -cne $servicing.compilerExecutableSha256) {
        throw 'Hosted servicing output lost its separately reviewed immutable identity.'
    }
    $results.Add('distinct-immutable-hosted-package')
    # The producer of another compiler tool set: its own version, compiler and native bytes, and a manifest of its own.
    $newest = Get-ReviewedWindowsPackageContents -Variant hosted20261116
    $earlier = @($baseline,$servicing,$current)
    if ($newest.version -in $earlier.version -or $newest.compilerExecutableSha256 -in $earlier.compilerExecutableSha256 -or
        $newest.entries.'runtimes/win-x64/native/sqlcipher.dll' -in @($earlier | ForEach-Object { $_.entries.'runtimes/win-x64/native/sqlcipher.dll' }) -or
        $newest.entries.'manifest.json' -in @($earlier | ForEach-Object { $_.entries.'manifest.json' }) -or
        @($acceptance.compilers | Where-Object { $_.sha256 -ceq $newest.compilerExecutableSha256 -and $_.toolsVersion -cne $inputLock.windowsCompiler.toolsVersion }).Count -ne 1 -or
        @($newest.hostedInstallationVersions | Where-Object { $_ -in $current.hostedInstallationVersions -or $_ -notin $acceptance.installationVersions }).Count) {
        throw 'The producer of the newer compiler tool set lost its separately reviewed immutable identity.'
    }
    $results.Add('distinct-immutable-newer-tool-set-package')
    if ((@(Get-KnownWindowsPackageContents).version -join '|') -cne (@($baseline,$servicing,$newest,$current).version -join '|')) { throw 'A reviewed immutable identity is missing from the known inventories.' }
    $results.Add('known-immutable-identities-complete')
    Require-Refused 'producer-current-identity-reuse' { Resolve-NativeProducerVersion -InputLock $inputLock -Contents $current -KnownContents @($baseline,$servicing,$current) -CandidateVersion $current.version } '*newer than every immutable consumer candidate*'
    Require-Refused 'producer-newest-identity-reuse' { Resolve-NativeProducerVersion -InputLock $inputLock -Contents $newest -KnownContents @(Get-KnownWindowsPackageContents) -CandidateVersion $newest.version } '*newer than every immutable consumer candidate*'
    Require-Refused 'producer-skipped-identity-reuse' { Resolve-NativeProducerVersion -InputLock $inputLock -Contents $newest -KnownContents @(Get-KnownWindowsPackageContents) -CandidateVersion '4.19.0-candidate.10' } '*newer than every immutable consumer candidate*'
    foreach ($row in @(
        @('18.10.12201.205',$baseline.compilerExecutableSha256,'baseline'),
        @('18.11.12306.390',$servicing.compilerExecutableSha256,'servicing'),
        @('18.11.12315.225',$current.compilerExecutableSha256,'hosted20261021'),
        @('18.11.12328.437',$current.compilerExecutableSha256,'hosted20261021'),
        @('18.12.12405.243',$newest.compilerExecutableSha256,'hosted20261116')
    )) {
        if ((Resolve-ReviewedWindowsProducer -InstallationVersion $row[0] -CompilerSha256 $row[1]) -cne $row[2]) {
            throw 'Hosted compiler metadata selected a different immutable producer.'
        }
        $results.Add('producer-selection-' + $row[2])
    }
    Require-Refused 'producer-unknown-installation' { Resolve-ReviewedWindowsProducer -InstallationVersion '99.0.0.0' -CompilerSha256 $current.compilerExecutableSha256 } '*Unsupported Windows producer host*'
    Require-Refused 'producer-unknown-compiler' { Resolve-ReviewedWindowsProducer -InstallationVersion '18.11.12315.225' -CompilerSha256 ('0' * 64) } '*Unsupported Windows producer host*'
    Require-Refused 'producer-wrong-reviewed-compiler' { Resolve-ReviewedWindowsProducer -InstallationVersion '18.11.12315.225' -CompilerSha256 $baseline.compilerExecutableSha256 } '*Unsupported Windows producer host*'
    Require-Refused 'producer-newer-tool-set-host-earlier-compiler' { Resolve-ReviewedWindowsProducer -InstallationVersion '18.12.12405.243' -CompilerSha256 $current.compilerExecutableSha256 } '*Unsupported Windows producer host*'
    Require-Refused 'producer-earlier-host-newer-compiler' { Resolve-ReviewedWindowsProducer -InstallationVersion '18.11.12328.437' -CompilerSha256 $newest.compilerExecutableSha256 } '*Unsupported Windows producer host*'
    Require-Refused 'producer-baseline-host-newer-compiler' { Resolve-ReviewedWindowsProducer -InstallationVersion '18.10.12201.205' -CompilerSha256 $newest.compilerExecutableSha256 } '*Unsupported Windows producer host*'
    Require-Refused 'producer-new-host-wrong-reviewed-compiler' { Resolve-ReviewedWindowsProducer -InstallationVersion '18.11.12328.437' -CompilerSha256 $baseline.compilerExecutableSha256 } '*Unsupported Windows producer host*'
    $env:VafadarWindowsCipherVariant = 'unknown-fixture'
    Require-Refused 'unknown-package-variant' { Get-ReviewedWindowsPackageContents } '*Unsupported Windows cipher package variant*'
} finally { $env:VafadarWindowsCipherVariant = $originalVariant }
$fixture = Join-Path (Get-NativeInputRoot) ('toolchain-review-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
$missing = Copy-Policy $actual.Installation; $missing.installationPath = $fixture
Require-Refused 'missing-native-tools' { Resolve-ReviewedWindowsToolchain $missing $inputLock $acceptance } '*compiler or bootstrap is unavailable*'
foreach ($name in @('Build-Windows.ps1','Test-UpstreamWindows.ps1','Test-WindowsAtomicWidth.ps1')) {
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $name),[ref]$tokens,[ref]$errors)
    if ($errors.Count -ne 0 -or -not $ast.Find({ param($node) $node -is [Management.Automation.Language.CommandAst] -and $node.GetCommandName() -eq 'Get-ReviewedWindowsToolchain' },$true)) {
        throw "Native consumer lost shared qualification: $name"
    }
}
[ordered]@{ Cases=$results; ActualCompilerBytesQualified=$true; ObservedInstallationVersion=$actual.Installation.installationVersion;
    CompilerSha256=$actual.CompilerSha256; CompilerFileVersion=$actual.CompilerFileVersion; SdkVersion=$actual.SdkVersion; ToolsVersion=$actual.ToolsVersion;
    ApprovedPatchVersionsTestedAsMetadataOnly=$true; NoCompilerOrBootstrapExecutedByFixtures=$true } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fixture 'proof.json') -Encoding UTF8
"Toolchain qualification passed: $($results.Count) cases; evidence $fixture"
