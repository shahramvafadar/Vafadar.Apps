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
$hash = Copy-Policy $acceptance; $hash.compilers = @([pscustomobject]@{ sha256=('0' * 64); fileVersion=$actual.CompilerFileVersion })
Require-Refused 'changed-compiler-bytes' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $hash } '*Installed compiler bytes differ*'
$malformed = Copy-Policy $acceptance; $malformed.compilers[0].sha256 = 'not-a-hash'
Require-Refused 'malformed-compiler-hash' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $malformed } '*Unsupported Windows compiler acceptance*'
$schema = Copy-Policy $acceptance; $schema.schemaVersion = 3
Require-Refused 'unknown-policy-schema' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $schema } '*Unsupported Windows compiler acceptance*'
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
