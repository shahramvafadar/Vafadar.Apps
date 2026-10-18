# Exercise the real build target with isolated fake assemblies, without a device or an application build.
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path $repo ('artifacts/compression-header-tests/' + [Guid]::NewGuid().ToString('N'))
$cache = Join-Path $fixture 'android/lz4'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$source = Join-Path $fixture 'source.dll'
[IO.File]::WriteAllBytes($source, [byte[]](0..31))
function Write-Header([string]$Name, [uint32]$Magic, [uint32]$Index, [uint32]$Size) {
    $stream = [IO.File]::Create((Join-Path $cache $Name))
    $writer = New-Object IO.BinaryWriter($stream)
    try { $writer.Write($Magic); $writer.Write($Index); $writer.Write($Size) }
    finally { $writer.Dispose() }
}
Write-Header 'valid.lz4' 0x5A4C4158 2 32
Write-Header 'index.lz4' 0x5A4C4158 3 32
Write-Header 'size.lz4' 0x5A4C4158 2 33
Write-Header 'magic.lz4' 1 2 32
[IO.File]::WriteAllBytes((Join-Path $cache 'short.lz4'), [byte[]](1,2,3))
$stamp = (Get-Item -LiteralPath (Join-Path $cache 'valid.lz4')).LastWriteTimeUtc
$items = foreach ($name in @('valid','index','size','magic','short','missing')) {
    '<_AssembliesToCompress Include="' + $source + '"><DestinationPath>' + (Join-Path $cache ($name + '.lz4')) + '</DestinationPath><DescriptorIndex>2</DescriptorIndex></_AssembliesToCompress>'
}
$targetPath = Join-Path $repo 'eng/AndroidAssemblyCompression.targets'
$project = @"
<Project>
  <PropertyGroup><Configuration>Release</Configuration><AndroidEnableAssemblyCompression>True</AndroidEnableAssemblyCompression><IntermediateOutputPath>$fixture/</IntermediateOutputPath></PropertyGroup>
  <Import Project="$targetPath" />
  <Target Name="_CollectAssembliesToCompress"><ItemGroup>$items</ItemGroup></Target>
  <Target Name="_CompressAssemblies" DependsOnTargets="_CollectAssembliesToCompress" />
</Project>
"@
$projectPath = Join-Path $fixture 'check.proj'
[IO.File]::WriteAllText($projectPath, $project)
dotnet msbuild $projectPath -t:_CompressAssemblies -nologo -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Compression invalidation target failed.' }
if (!(Test-Path -LiteralPath (Join-Path $cache 'valid.lz4'))) { throw 'Valid cache was removed.' }
if ((Get-Item -LiteralPath (Join-Path $cache 'valid.lz4')).LastWriteTimeUtc -ne $stamp) { throw 'Valid cache was rewritten.' }
foreach ($name in @('index','size','magic','short','missing')) {
    if (Test-Path -LiteralPath (Join-Path $cache ($name + '.lz4'))) { throw "Incompatible cache retained: $name" }
}
# A caller cannot use the invalidator to remove a file outside this intermediate cache.
$outside = Join-Path $fixture 'outside.lz4'
[IO.File]::WriteAllBytes($outside, [byte[]](1,2,3))
$unsafeProject = $project.Replace((Join-Path $cache 'index.lz4'), $outside)
$unsafePath = Join-Path $fixture 'outside.proj'
[IO.File]::WriteAllText($unsafePath, $unsafeProject)
$refusal = dotnet msbuild $unsafePath -t:_CompressAssemblies -nologo -v:minimal 2>&1
if ($LASTEXITCODE -eq 0 -or !($refusal -match 'outside the current intermediate cache')) { throw 'Outside-path refusal was not observed.' }
if (([IO.File]::ReadAllBytes($outside)).Length -ne 3) { throw 'Outside file was changed.' }
'PASS: valid cache preserved; stale index, size, magic and truncated headers invalidated; missing cache accepted; outside path refused.'
