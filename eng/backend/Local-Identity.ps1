<# .SYNOPSIS Operates only the isolated fictional local identity service; never a hosted deployment. #>
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Provision','Start','Stop','Backup','Restore','ConnectionInfo')][string]$Action,
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,40}$')][string]$Instance='local',
    [ValidateRange(1024,65535)][int]$DatabasePort=55474,
    [ValidateRange(1024,65535)][int]$ServicePort=5047,
    [ValidateRange(0,65535)][int]$ServiceTlsPort=0,
    [string]$Snapshot)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$owned=Join-Path $repo ('artifacts/zanance-identity/'+$Instance)
$private=Join-Path $owned 'private'
$container='zanance-backend-'+$Instance
$volume='zanance-identity-'+$Instance
$database='zanance_identity_'+$Instance.Replace('-','_')
$project=Join-Path $repo 'src/Apps/Zanance/Vafadar.Zanance.Api/Vafadar.Zanance.Api.csproj'
$runtimeRoot=Join-Path $repo 'artifacts/zanance-identity/runtime/win-x64'
$binary=Join-Path $runtimeRoot 'Vafadar.Zanance.Api.exe'
if ($ServiceTlsPort -eq 0) { $ServiceTlsPort=$ServicePort+1 }
if ($ServiceTlsPort -lt 1024 -or $ServiceTlsPort -gt 65535 -or $ServiceTlsPort -eq $ServicePort) { throw 'Distinct bounded local TLS port required.' }
if ($Action -eq 'ConnectionInfo') {
    # This file contains public connection information only, never a private certificate or credential.
    $publicPath=Join-Path $owned 'connection.json'
    $publicInfo=Get-Item -LiteralPath $publicPath
    if ($publicInfo.Length -gt 4096 -or ($publicInfo.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Public connection information is unsafe.' }
    Get-Content -LiteralPath $publicPath -Raw -Encoding UTF8
    return
}
function Require-Exit { if ($LASTEXITCODE -ne 0) { throw 'Owned local operation failed.' } }
function Require-OwnedContainer {
    $label=& docker inspect $container --format '{{index .Config.Labels "pro.vafadar.zanance.identity"}}'
    Require-Exit
    if ($label -ne $Instance) { throw 'Container ownership differs; mutation refused.' }
}
function Stop-OwnedService {
    $pidPath=Join-Path $private 'server.pid'
    if (-not (Test-Path -LiteralPath $pidPath)) { return }
    $servicePid=[int](Get-Content -LiteralPath $pidPath -Raw)
    $process=Get-Process -Id $servicePid -ErrorAction SilentlyContinue
    if (-not $process) { return }
    $command=(Get-CimInstance Win32_Process -Filter ('ProcessId='+$servicePid)).CommandLine
    if (-not $command -or -not $command.Contains($binary) -or -not $command.Contains('"'+$private+'"')) { throw 'PID/root ownership differs; stop refused.' }
    [IO.File]::WriteAllText((Join-Path $private 'shutdown.request'),'requested')
    if (-not $process.WaitForExit(45000)) { throw 'Graceful drain failed; snapshot refused.' }
}
function Resolve-OwnedSnapshot {
    # PowerShell 5.1's .NET Framework does not expose Path.IsPathFullyQualified.
    if (-not $Snapshot -or $Snapshot -notmatch '^(?:[A-Za-z]:[\\/]|\\\\[^\\]+\\[^\\]+\\)') { throw 'Absolute owned snapshot path required.' }
    $resolved=[IO.Path]::GetFullPath($Snapshot)
    $prefix=[IO.Path]::GetFullPath((Join-Path $repo 'artifacts/zanance-identity-snapshots'))+[IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Snapshot is outside the owned directory.' }
    return $resolved
}
function Protect-OwnedSnapshotDirectory([string]$Path) {
    $owner=[Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl=[Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true,$false)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($owner,
        [Security.AccessControl.FileSystemRights]::FullControl,
        [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit,
        [Security.AccessControl.PropagationFlags]::None,[Security.AccessControl.AccessControlType]::Allow))
    Set-Acl -LiteralPath $Path -AclObject $acl
}
if ($Action -eq 'Stop') { Stop-OwnedService; return }
if ($Action -eq 'Backup') {
    Require-OwnedContainer
    $destination=Resolve-OwnedSnapshot
    if (Test-Path -LiteralPath $destination) { throw 'Choose a fresh snapshot destination.' }
    Stop-OwnedService
    New-Item -ItemType Directory -Path $destination | Out-Null
    Protect-OwnedSnapshotDirectory $destination
    & docker exec $container pg_dump -U postgres -Fc -f /tmp/zanance-identity.dump $database
    Require-Exit
    & docker cp ($container+':/tmp/zanance-identity.dump') (Join-Path $destination 'identity.dump')
    Require-Exit
    Copy-Item -LiteralPath $private -Destination (Join-Path $destination 'private') -Recurse
    & docker exec $container rm /tmp/zanance-identity.dump
    Require-Exit
    $dumpHash=(Get-FileHash -LiteralPath (Join-Path $destination 'identity.dump') -Algorithm SHA256).Hash
    [ordered]@{ Schema=1; Quiesced=$true; Database=$database; DumpSha256=$dumpHash } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $destination 'complete.json') -Encoding UTF8
    Write-Output 'Private snapshot complete; source stays stopped.'
    return
}
if ($Action -eq 'Restore') {
    Require-OwnedContainer
    $source=Resolve-OwnedSnapshot
    $manifest=Get-Content -LiteralPath (Join-Path $source 'complete.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.Schema -ne 1 -or -not $manifest.Quiesced -or (Test-Path -LiteralPath $private)) { throw 'Complete snapshot and fresh private target required.' }
    if (-not $manifest.DumpSha256 -or (Get-FileHash -LiteralPath (Join-Path $source 'identity.dump') -Algorithm SHA256).Hash -ne $manifest.DumpSha256) { throw 'Snapshot dump integrity differs.' }
    $tables=& docker exec $container psql -U postgres -d $database -tAc "SELECT count(*) FROM information_schema.tables WHERE table_schema='public'"
    Require-Exit
    if ([int]$tables -ne 0) { throw 'Target schema must be empty; existing data is never overwritten.' }
    & docker cp (Join-Path $source 'identity.dump') ($container+':/tmp/zanance-identity.dump')
    Require-Exit
    # PostgreSQL dumps reference source roles. Restore schema/data without source grants; Start
    # creates the destination runtime role and grants only the intended application privileges.
    & docker exec $container pg_restore -U postgres --exit-on-error --no-owner --no-privileges -d $database /tmp/zanance-identity.dump
    Require-Exit
    New-Item -ItemType Directory -Path $owned -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $source 'private') -Destination $private -Recurse
    Remove-Item -LiteralPath (Join-Path $private 'server.pid') -ErrorAction SilentlyContinue
    & docker exec $container rm /tmp/zanance-identity.dump
    Require-Exit
    Write-Output 'Restored into a fresh stopped local target; verify before activation.'
    return
}
$existing=& docker ps -a --filter ('name=^/'+$container+'$') --format '{{.Names}}'
Require-Exit
if ($existing) { Require-OwnedContainer; & docker start $container | Out-Null; Require-Exit }
else {
    $oldVolume=& docker volume ls --filter ('name=^'+$volume+'$') --format '{{.Name}}'
    Require-Exit
    if ($oldVolume) { throw 'Existing unattached volume requires ownership reconciliation.' }
    & docker volume create --label ('pro.vafadar.zanance.identity='+$Instance) $volume | Out-Null
    Require-Exit
    & docker run --detach --pull never --name $container --label ('pro.vafadar.zanance.identity='+$Instance) --publish ('127.0.0.1:'+$DatabasePort+':5432') --env POSTGRES_HOST_AUTH_METHOD=trust --env ('POSTGRES_DB='+$database) --mount ('type=volume,source='+$volume+',target=/var/lib/postgresql') postgres:18.4-alpine | Out-Null
    Require-Exit
}
$ready=$false
for ($attempt=0;$attempt -lt 20;$attempt++) {
    & docker exec $container pg_isready -U postgres *> $null
    if ($LASTEXITCODE -eq 0) { $ready=$true; break }
    Start-Sleep -Milliseconds 500
}
if (-not $ready) { throw 'Owned PostgreSQL is not ready.' }
if ($Action -eq 'Provision') { Write-Output 'Empty local database provisioned; no service key or schema created.'; return }
if (Test-Path -LiteralPath (Join-Path $private 'server.pid')) {
    $running=Get-Process -Id ([int](Get-Content -LiteralPath (Join-Path $private 'server.pid') -Raw)) -ErrorAction SilentlyContinue
    if ($running) { throw 'Service PID is live; duplicate start refused.' }
}
$versions=[xml](Get-Content -LiteralPath (Join-Path $repo 'Directory.Packages.props') -Raw -Encoding UTF8)
$runtimeVersion=[string]$versions.Project.PropertyGroup.VafadarSecurityRuntimeVersion
if ($runtimeVersion -notmatch '^10\.0\.\d+$') { throw 'Reviewed service runtime version is missing.' }
# The installed shared runtime may lag the security patch. Publish the reviewed runtime as a
# build-restored private dependency; never install or modify global SDK/runtime configuration.
& dotnet publish $project --configuration Release -r win-x64 --self-contained true -p:ContinuousIntegrationBuild=true ('-p:RuntimeFrameworkVersion='+$runtimeVersion) --output $runtimeRoot
Require-Exit
New-Item -ItemType Directory -Path $owned -Force | Out-Null
$info=[Diagnostics.ProcessStartInfo]::new($binary)
$info.Arguments='--migrate'
$info.UseShellExecute=$false; $info.CreateNoWindow=$true
$info.EnvironmentVariables['ASPNETCORE_ENVIRONMENT']='Development'
$info.EnvironmentVariables['ConnectionStrings__Service']='Host=127.0.0.1;Port='+$DatabasePort+';Database='+$database+';Username=postgres'
$info.EnvironmentVariables['Service__Root']=$private
$migration=[Diagnostics.Process]::Start($info)
$migration.WaitForExit()
if ($migration.ExitCode -ne 0) { throw 'Explicit schema migration failed.' }
& docker exec $container psql -U postgres -d $database -v ON_ERROR_STOP=1 -c 'DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname=''zanance_runtime'') THEN CREATE ROLE zanance_runtime LOGIN; END IF; END $$; GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime;' | Out-Null
Require-Exit
$info.Arguments='--Service:Root "'+$private+'"'
$info.EnvironmentVariables['ConnectionStrings__Service']='Host=127.0.0.1;Port='+$DatabasePort+';Database='+$database+';Username=zanance_runtime'
$info.EnvironmentVariables['Service__Port']=[string]$ServicePort
$info.EnvironmentVariables['Service__TlsPort']=[string]$ServiceTlsPort
$info.EnvironmentVariables['Service__LocalMailbox']='true'
$server=[Diagnostics.Process]::Start($info)
$published=$false
for ($attempt=0;$attempt -lt 30;$attempt++) {
    if ($server.HasExited) { throw 'Local worker terminated before readiness.' }
    $pidPath=Join-Path $private 'server.pid'
    if ((Test-Path -LiteralPath $pidPath) -and [int](Get-Content -LiteralPath $pidPath -Raw) -eq $server.Id) {
        try {
            $response=Invoke-WebRequest -Uri ('http://127.0.0.1:'+$ServicePort+'/health/ready') -UseBasicParsing -TimeoutSec 1
            if ([int]$response.StatusCode -eq 200) { $published=$true; break }
        } catch { }
    }
    Start-Sleep -Milliseconds 500
}
if (-not $published) { throw 'Worker remains live but readiness was not observed; inspect this same PID before any retry.' }
$publicPinPath=Join-Path $private 'local-tls.fingerprint'
$publicPinInfo=Get-Item -LiteralPath $publicPinPath
if ($publicPinInfo.Length -gt 128 -or ($publicPinInfo.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Public certificate fingerprint is unsafe.' }
$publicPin=[IO.File]::ReadAllText($publicPinPath).Trim()
if ($publicPin -notmatch '^[a-f0-9]{64}$') { throw 'Public certificate fingerprint is invalid.' }
[ordered]@{ Schema=1; LocalAddress=('https://127.0.0.1:'+$ServiceTlsPort+'/'); EmulatorAddress=('https://10.0.2.2:'+$ServiceTlsPort+'/'); CertificateSha256=$publicPin } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $owned 'connection.json') -Encoding UTF8
Write-Output ('Ready hidden local service PID '+$server.Id+' at https://127.0.0.1:'+$ServiceTlsPort+'; public pin available through ConnectionInfo; fictional data only.')
