<# Verify the exact APK with the Android and Java tools resolved for the producing project. #>
param(
    [Parameter(Mandatory)] [string]$ApkPath,
    [Parameter(Mandatory)] [string]$Project,
    [ValidateSet('Release', 'Debug')] [string]$Configuration = 'Release',
    [string[]]$BuildProperties = @(),
    [switch]$Offline)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$apk = Get-Item -LiteralPath $ApkPath
$projectDotNet = Join-Path $PSScriptRoot 'Invoke-ProjectDotNet.ps1'
$metadata = & $projectDotNet msbuild $Project -nologo -v:q "-p:Configuration=$Configuration" `
    -p:TargetFramework=net10.0-android -p:VafadarMauiTargetFrameworks=net10.0-android `
    -p:EmbedAssembliesIntoApk=true -t:_ResolveMonoAndroidSdks `
    '-getProperty:ApplicationId,AndroidSdkBuildToolsPath,ApkSignerJar,JavaSdkDirectory,_AndroidUseAssemblyStore' @BuildProperties
if ($LASTEXITCODE -ne 0) { throw 'Could not resolve the Android package identity and verification tools.' }
$properties = ($metadata -join "`n" | ConvertFrom-Json).Properties
if (!$properties.ApplicationId -or !$properties.AndroidSdkBuildToolsPath -or
    !$properties.ApkSignerJar -or !$properties.JavaSdkDirectory) {
    throw 'The Android project did not resolve a package identity and verification tools.'
}

$toolSuffix = if ($IsWindows) { '.exe' } else { '' }
$aapt2 = Join-Path $properties.AndroidSdkBuildToolsPath "aapt2$toolSuffix"
$java = Join-Path $properties.JavaSdkDirectory "bin/java$toolSuffix"
foreach ($tool in @($aapt2, $java, $properties.ApkSignerJar)) {
    if (!(Test-Path -LiteralPath $tool -PathType Leaf)) { throw "APK verification tool unavailable: $tool" }
}

$null = & $java -jar $properties.ApkSignerJar verify $apk.FullName 2>&1
if ($LASTEXITCODE -ne 0) { throw 'The Android SDK could not verify the APK signature.' }

# Signature validation does not guarantee the placement of uncompressed native libraries.
# Check the exact signed Release archive without modifying its bytes.
if ($Configuration -eq 'Release') {
    $zipalign = Join-Path $properties.AndroidSdkBuildToolsPath "zipalign$toolSuffix"
    if (!(Test-Path -LiteralPath $zipalign -PathType Leaf)) { throw "APK verification tool unavailable: $zipalign" }
    $null = & $zipalign -c -P 16 4 $apk.FullName 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'The Release APK failed the Android SDK 16 KiB archive alignment check.' }
}

$badging = @(& $aapt2 dump badging $apk.FullName 2>&1)
if ($LASTEXITCODE -ne 0) { throw 'The Android SDK could not read the APK manifest.' }
$identity = @($badging | Select-String -Pattern "^package: name='([^']+)'")
if ($identity.Count -ne 1 -or $identity[0].Matches[0].Groups[1].Value -cne $properties.ApplicationId) {
    throw 'The APK manifest package differs from the producing Android project.'
}
if ($badging -cmatch '^testOnly=') {
    throw 'The APK is marked testOnly and requires a test installation.'
}
if ($Configuration -eq 'Release' -and ($badging -cmatch '^application-debuggable(?:\s|$)')) {
    throw 'The Release APK is debuggable.'
}
if ($Offline -and ($badging -cmatch "^uses-permission(?:-sdk-\d+)?: name='android\.permission\.(INTERNET|ACCESS_NETWORK_STATE)'")) {
    throw 'The offline APK requests a network permission.'
}

# The current Android SDK embeds one managed assembly store per packaged ABI.
# A valid APK signature also covers packages from which a required store was omitted.
if ($properties._AndroidUseAssemblyStore -eq 'true') {
    $archive = [IO.Compression.ZipFile]::OpenRead($apk.FullName)
    try {
        $nativeAbis = @($archive.Entries | ForEach-Object {
            if ($_.FullName -cmatch '^lib/([^/]+)/[^/]+\.so$') { $Matches[1] }
        } | Sort-Object -Unique)
        if ($nativeAbis.Count -eq 0) { throw 'The APK contains no native Android ABI.' }
        foreach ($name in @('classes.dex') + @($nativeAbis | ForEach-Object { "lib/$_/libassembly-store.so" })) {
            $members = @($archive.Entries | Where-Object FullName -CEQ $name)
            if ($members.Count -ne 1 -or $members[0].Length -eq 0) {
                throw "The APK must contain exactly one nonempty $name."
            }
        }
    }
    finally { $archive.Dispose() }
}

[pscustomobject]@{
    Package = $properties.ApplicationId
    Sha256 = (Get-FileHash -LiteralPath $apk.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
