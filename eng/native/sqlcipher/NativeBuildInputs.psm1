<# Shared restoration of reviewed native build inputs, always inside owned repository artifacts. #>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-NativeInputRoot {
    <# .SYNOPSIS Returns the owned native artifact root without changing the caller's working directory. #>
    [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../artifacts/native-sqlcipher'))
}

function Get-NativeFreeSpace {
    <# .SYNOPSIS Reads available space using a filesystem root on both Windows and Linux hosts. #>
    [IO.DriveInfo]::new([IO.Path]::GetPathRoot((Get-NativeInputRoot))).AvailableFreeSpace
}

function Get-NativeContainerHostIdentity {
    <# .SYNOPSIS Uses the Linux bind-mount owner's numeric identity; Docker Desktop retains its reviewed default user. #>
    if ([IO.Path]::DirectorySeparatorChar -eq '/') {
        $nativeUid = & id -u
        if ($LASTEXITCODE -ne 0) { throw 'Native host user identity unavailable.' }
        $nativeGid = & id -g
        if ($LASTEXITCODE -ne 0) { throw 'Native host group identity unavailable.' }
        if ($nativeUid -notmatch '^[1-9][0-9]*$' -or $nativeGid -notmatch '^[0-9]+$') {
            throw 'Native builds require a non-root numeric host identity.'
        }
        return ($nativeUid + ':' + $nativeGid)
    }
    return ''
}

function Get-NativeContainerRunArguments {
    <# .SYNOPSIS Binds fresh owned output/work directories, read-only sources/recipe and a non-root Linux owner. #>
    param([Parameter(Mandatory)][string]$RunRoot,
        [Parameter(Mandatory)][ValidateSet('candidate','evidence')][string]$OutputName,
        [Parameter(Mandatory)][ValidateSet('/home/nativebuild/work','/home/nativebuild/upstream')][string]$WorkTarget)
    $absolute = [IO.Path]::GetFullPath($RunRoot)
    $prefix = [IO.Path]::GetFullPath((Join-Path (Get-NativeInputRoot) 'build')).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $absolute.StartsWith($prefix, [StringComparison]::Ordinal) -or
        [IO.Path]::GetDirectoryName($absolute) -ne $prefix.TrimEnd('\','/')) {
        throw 'Native container work must belong to one direct owned run directory.'
    }
    $workspace = Join-Path $absolute 'work'
    if (Test-Path -LiteralPath $workspace) { throw 'Use a fresh native container workspace.' }
    New-Item -ItemType Directory $workspace | Out-Null
    $identity = Get-NativeContainerHostIdentity
    # Linux bind mounts retain host ownership. Match it rather than running root or granting world-write.
    $arguments = @('--rm','--network','none')
    if ($identity) { $arguments += @('--user',$identity) }
    $arguments += @('--mount',('type=bind,source=' + (Join-Path $absolute 'inputs') + ',target=/inputs,readonly'),
        '--mount',('type=bind,source=' + $PSScriptRoot + ',target=/recipe,readonly'),
        '--mount',('type=bind,source=' + (Join-Path $absolute $OutputName) + ',target=/output'),
        '--mount',('type=bind,source=' + $workspace + ',target=' + $WorkTarget))
    return $arguments
}

function Restore-NativeZipTool {
    <# .SYNOPSIS Verifies a locked official ZIP and restores its executable without an installer. #>
    param([Parameter(Mandatory)]$Tool, [Parameter(Mandatory)][string]$Executable)
    $uri = [Uri]$Tool.uri
    if ($Tool.sha256 -notmatch '^[0-9a-f]{64}$' -or $uri.Scheme -ne 'https' -or
        -not (($uri.Host -eq 'github.com' -and $uri.AbsolutePath -like '/StrawberryPerl/Perl-Dist-Strawberry/releases/download/*') -or
              ($uri.Host -eq 'www.nasm.us' -and $uri.AbsolutePath -like '/pub/nasm/releasebuilds/*/win64/*-win64.zip'))) {
        throw 'Unsupported native tool format or official origin.'
    }
    $cache = Join-Path (Get-NativeInputRoot) 'tools'
    New-Item -ItemType Directory -Force $cache | Out-Null
    $archive = Join-Path $cache ($Tool.sha256 + '.zip')
    if (-not (Test-Path -LiteralPath $archive)) {
        $partial = $archive + '.partial-' + [Guid]::NewGuid().ToString('N')
        Invoke-WebRequest -Uri $uri -OutFile $partial -UseBasicParsing
        if ((Get-Item -LiteralPath $partial).Length -ne $Tool.bytes -or
            (Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Tool.sha256) {
            throw 'Downloaded native archive differs from the reviewed lock.'
        }
        Move-Item -LiteralPath $partial -Destination $archive
    }
    if ((Get-Item -LiteralPath $archive).Length -ne $Tool.bytes -or
        (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Tool.sha256) {
        throw 'Cached native archive differs from the reviewed lock.'
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $destination = Join-Path $cache $Tool.sha256
    $marker = Join-Path $destination 'verified-tool.json'
    if (-not (Test-Path -LiteralPath $destination)) {
        $staging = $destination + '.extract-' + [Guid]::NewGuid().ToString('N')
        $prefix = [IO.Path]::GetFullPath($staging).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
        $zip = [IO.Compression.ZipFile]::OpenRead($archive)
        try {
            foreach ($entry in $zip.Entries) {
                $target = [IO.Path]::GetFullPath((Join-Path $staging $entry.FullName))
                if (-not $target.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or
                    $entry.FullName.Contains(':') -or (($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000) {
                    throw 'Unsafe native archive extraction path or symbolic link.'
                }
            }
        }
        finally { $zip.Dispose() }
        [IO.Compression.ZipFile]::ExtractToDirectory($archive,$staging)
        $ownedPrefix = [IO.Path]::GetFullPath($cache).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
        foreach ($path in @($staging,$destination)) {
            if (-not [IO.Path]::GetFullPath($path).StartsWith($ownedPrefix,[StringComparison]::OrdinalIgnoreCase)) {
                throw 'Native directory move leaves the owned artifact root.'
            }
        }
        [IO.Directory]::Move($staging,$destination)
        [ordered]@{ archiveSha256=$Tool.sha256; executableSha256=(Get-FileHash -LiteralPath (Join-Path $destination $Executable) -Algorithm SHA256).Hash.ToLowerInvariant() } |
            ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding UTF8
    }
    if (-not (Test-Path -LiteralPath $marker)) { throw 'Incomplete native tool restoration.' }
    $record = Get-Content -LiteralPath $marker -Raw -Encoding UTF8 | ConvertFrom-Json
    $executablePath = Join-Path $destination $Executable
    if ($record.archiveSha256 -ne $Tool.sha256 -or
        (Get-FileHash -LiteralPath $executablePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $record.executableSha256) {
        throw 'Restored native executable differs from its verified input.'
    }
    $destination
}

function Restore-NativeSource {
    <# .SYNOPSIS Restores immutable official Git objects and returns a clean source archive for a build. #>
    param([Parameter(Mandatory)]$Source, [Parameter(Mandatory)][ValidateSet('sqlcipher','openssl')][string]$Name)
    $expected = if ($Name -eq 'sqlcipher') { 'https://github.com/sqlcipher/sqlcipher.git' } else { 'https://github.com/openssl/openssl.git' }
    if ($Source.repository -ne $expected -or $Source.commit -notmatch '^[0-9a-f]{40}$') { throw 'Unsupported native source origin or commit.' }
    $cache = Join-Path (Get-NativeInputRoot) 'source-git'
    New-Item -ItemType Directory -Force $cache | Out-Null
    $checkout = Join-Path $cache ($Name + '-' + $Source.commit)
    if (-not (Test-Path -LiteralPath $checkout)) {
        # Public source fetches do not use another repository/account's credential helpers or extra headers.
        & git -c credential.helper= -c http.extraHeader= -c core.autocrlf=false clone --no-checkout --depth 1 --branch $Source.tag $Source.repository $checkout 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Official native source fetch failed.' }
    }
    $commit = & git -C $checkout rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $commit -ne $Source.commit) { throw 'Native source checkout differs from its reviewed commit.' }
    $tag = & git -C $checkout rev-parse ('refs/tags/' + $Source.tag)
    if ($LASTEXITCODE -ne 0 -or $tag -ne $Source.tagObject) { throw 'Native source tag object differs from its reviewed identity.' }
    $archive = Join-Path $cache ($Name + '-' + $Source.commit + '.tar')
    & git -c core.autocrlf=false -c core.eol=lf -C $checkout archive --format=tar --output=$archive $Source.commit
    if ($LASTEXITCODE -ne 0) { throw 'Immutable native source archiving failed.' }
    $archive
}

function Resolve-NativeProducerVersion {
    <# .SYNOPSIS Selects an explicit unpublished candidate version without reusing an immutable consumer identity. #>
    param([Parameter(Mandatory)]$InputLock, [Parameter(Mandatory)]$Contents,
        [Parameter(Mandatory)][object[]]$KnownContents, [AllowEmptyString()][string]$CandidateVersion = '')
    if (-not $CandidateVersion) { return $Contents.version }
    $sourceText = $InputLock.sources.sqlcipher.tag.TrimStart('v')
    if ($sourceText -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+$') { throw 'Producer qualification requires a reviewed stable source version.' }
    $sourceVersion = [Version]$sourceText
    $prefix = $sourceText + '-candidate.'
    $pattern = '^' + [regex]::Escape($prefix) + '(?<number>[1-9][0-9]*)$'
    [long]$requested = 0
    if ($CandidateVersion -cnotmatch $pattern -or -not [long]::TryParse($Matches.number, [ref]$requested)) {
        throw 'Producer version must name a fresh candidate of the currently reviewed stable source.'
    }
    foreach ($known in $KnownContents) {
        [long]$existing = 0
        if ($known.version -cnotmatch '^(?<source>[0-9]+\.[0-9]+\.[0-9]+)-candidate\.(?<number>[1-9][0-9]*)$') {
            throw 'Immutable consumer version has an unsupported identity.'
        }
        $knownSource = [Version]$Matches.source
        if (-not [long]::TryParse($Matches.number, [ref]$existing) -or $knownSource -gt $sourceVersion -or
            ($knownSource -eq $sourceVersion -and $requested -le $existing)) {
            throw 'Producer version must be newer than every immutable consumer candidate.'
        }
    }
    return $CandidateVersion
}

Export-ModuleMember -Function Get-NativeInputRoot,Get-NativeFreeSpace,Get-NativeContainerHostIdentity,Get-NativeContainerRunArguments,Restore-NativeZipTool,Restore-NativeSource,Resolve-NativeProducerVersion
