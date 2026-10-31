<# .SYNOPSIS Checks requested-host refusals using public metadata fixtures, without native compilation or consumer admission. #>
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'WindowsToolchain.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$request=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.producer.request.json') -Raw | ConvertFrom-Json
$policy=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.compiler.acceptance.json') -Raw | ConvertFrom-Json
$observation=[pscustomobject]@{
 InstallationVersion=$request.installationVersion; ToolsVersion=$request.toolsVersion; SdkVersion=$request.sdkVersion
 CompilerPresent=$true; SdkPresent=$true; CompilerExecuted=$false; ReviewedAdmissionGranted=$false
 CompilerSha256=$request.compilerSha256; CompilerFileVersion=$request.compilerFileVersion; SignatureStatus='Valid'
 SignerSubject='CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US'
}
$original=@($request,$policy,$observation) | ConvertTo-Json -Depth 20
$results=[Collections.Generic.List[string]]::new()
function Copy-PublicMetadata($value) { $value | ConvertTo-Json -Depth 20 | ConvertFrom-Json }
function Require-Refused([string]$name,[scriptblock]$action,[string]$message) {
 $refused=$false
 try { & $action } catch { if($_.Exception.Message -notlike $message){throw}; $refused=$true }
 if(-not $refused){throw ('Unsafe requested producer metadata accepted: '+$name)}
 $results.Add($name)
}
Assert-RequestedWindowsProducer $observation $request $policy $request.candidateVersion
$results.Add('matching-public-metadata-fixture-not-actual-host-acceptance')
$old=Copy-PublicMetadata $observation;$old.InstallationVersion='18.11.12315.225'
Require-Refused 'older-image-before-native-build' { Assert-RequestedWindowsProducer $old $request $policy $request.candidateVersion } '*requested producer image is not available*'
$foreign=Copy-PublicMetadata $observation;$foreign.InstallationVersion='99.0.0.0'
Require-Refused 'foreign-image' { Assert-RequestedWindowsProducer $foreign $request $policy $request.candidateVersion } '*requested producer image is not available*'
foreach($field in @('CompilerPresent','SdkPresent')) {
 $wrong=Copy-PublicMetadata $observation;$wrong.$field=$false
 Require-Refused ('missing-'+$field) { Assert-RequestedWindowsProducer $wrong $request $policy $request.candidateVersion } '*Observed compiler evidence differs*'
}
foreach($field in @('CompilerExecuted','ReviewedAdmissionGranted')) {
 $wrong=Copy-PublicMetadata $observation;$wrong.$field=$true
 Require-Refused ('unexpected-'+$field) { Assert-RequestedWindowsProducer $wrong $request $policy $request.candidateVersion } '*Observed compiler evidence differs*'
}
foreach($field in @('ToolsVersion','SdkVersion','CompilerSha256','CompilerFileVersion','SignatureStatus','SignerSubject')) {
 $wrong=Copy-PublicMetadata $observation;$wrong.$field='unreviewed-fixture'
 Require-Refused ('changed-'+$field) { Assert-RequestedWindowsProducer $wrong $request $policy $request.candidateVersion } '*Observed compiler evidence differs*'
}
$schema=Copy-PublicMetadata $request;$schema.schemaVersion=2
Require-Refused 'unknown-request-schema' { Assert-RequestedWindowsProducer $observation $schema $policy $request.candidateVersion } '*Unsupported requested Windows producer identity*'
$unknown=Copy-PublicMetadata $request;$unknown.installationVersion='99.0.0.0'
Require-Refused 'unreviewed-request-installer' { Assert-RequestedWindowsProducer $observation $unknown $policy $request.candidateVersion } '*Unsupported requested Windows producer identity*'
$unknown=Copy-PublicMetadata $request;$unknown.compilerSha256='0'*64
Require-Refused 'unreviewed-request-compiler' { Assert-RequestedWindowsProducer $observation $unknown $policy $request.candidateVersion } '*Unsupported requested Windows producer identity*'
Require-Refused 'different-candidate' { Assert-RequestedWindowsProducer $observation $request $policy '4.19.0-candidate.9' } '*Unsupported requested Windows producer identity*'
if((@($request,$policy,$observation) | ConvertTo-Json -Depth 20) -cne $original){throw 'Request validation mutated original public evidence.'}
$directory=Join-Path (Get-NativeInputRoot) ('producer-request-review-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $directory | Out-Null
@{Cases=$results;MetadataFixturesOnly=$true;ActualHostedOutputQualified=$false;NativeCompilerExecuted=$false;ConsumerPinsChanged=$false;OriginalPublicMetadataUnchanged=$true} |
 ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $directory 'proof.json') -Encoding UTF8
Write-Output ('Producer request metadata checks passed: {0} cases; evidence {1}' -f $results.Count,$directory)
