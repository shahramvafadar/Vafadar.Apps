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
$hash = Copy-Policy $acceptance; $hash.compilerExecutableSha256 = '0' * 64
Require-Refused 'changed-compiler-bytes' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $hash } '*Installed compiler bytes differ*'
$malformed = Copy-Policy $acceptance; $malformed.compilerExecutableSha256 = 'not-a-hash'
Require-Refused 'malformed-compiler-hash' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $malformed } '*Unsupported Windows compiler acceptance*'
$schema = Copy-Policy $acceptance; $schema.schemaVersion = 2
Require-Refused 'unknown-policy-schema' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $schema } '*Unsupported Windows compiler acceptance*'
$empty = Copy-Policy $acceptance; $empty.installationVersions = @()
Require-Refused 'empty-installer-allowlist' { Resolve-ReviewedWindowsToolchain $actual.Installation $inputLock $empty } '*Unsupported Windows compiler acceptance*'
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
    CompilerSha256=$acceptance.compilerExecutableSha256; SdkVersion=$actual.SdkVersion; ToolsVersion=$actual.ToolsVersion;
    ApprovedPatchVersionsTestedAsMetadataOnly=$true; NoCompilerOrBootstrapExecutedByFixtures=$true } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fixture 'proof.json') -Encoding UTF8
"Toolchain qualification passed: $($results.Count) cases; evidence $fixture"
