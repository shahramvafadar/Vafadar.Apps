<# .SYNOPSIS Verifies original protected key code and encrypted data reopen across two Windows processes with fictitious files. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'windows-x64')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$library = Join-Path $root 'candidate/runtimes/win-x64/native/sqlcipher.dll'
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.librarySha256 -ne (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()) {
    throw 'Protected-key input differs from reviewed native candidate.'
}
$evidence = Join-Path $root ('key-proof-' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $PSScriptRoot 'Compatibility/Vafadar.SqlCipher.Compatibility.csproj'
& dotnet run --project $project -c Release -p:ContinuousIntegrationBuild=true "-p:NativeSqlCipherPath=$library" -- --create-key-proof $evidence
if ($LASTEXITCODE -ne 0) { throw 'Fictitious protected-key creation failed.' }
& dotnet run --project $project -c Release --no-build "-p:NativeSqlCipherPath=$library" -- --reopen-key-proof $evidence
if ($LASTEXITCODE -ne 0) { throw 'Protected-key data reopen in a new process failed.' }
$files = @(Get-ChildItem -LiteralPath $evidence -Filter '*.key')
if ($files.Count -ne 1) { throw 'Expected one exact independently owned key envelope.' }
$acl = Get-Acl -LiteralPath $files[0].FullName
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
try {
    $rules = $acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])
    if (-not $acl.AreAccessRulesProtected -or $rules.Count -ne 1 -or
        $rules[0].IdentityReference -ne $identity.User -or $rules[0].IsInherited -or
        $rules[0].AccessControlType -ne [Security.AccessControl.AccessControlType]::Allow -or
        $rules[0].FileSystemRights -ne [Security.AccessControl.FileSystemRights]::FullControl) {
        throw 'Actual envelope ACL does not match the captured current-user policy.'
    }
} finally { $identity.Dispose() }
[ordered]@{ ProtectedAcl = $true; RuleCount = 1; OnlyCurrentUser = $true; FullControl = $true } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'acl-proof.json') -Encoding UTF8
Write-Output (Join-Path $evidence 'key-reopen-proof.json')
