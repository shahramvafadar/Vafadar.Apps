# Exercise the real SDK-style incremental inputs with public, isolated overlay files.
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path $repo ('artifacts/manifest-overlay-tests/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
$overlay = Join-Path $fixture 'optional.xml'
$first = Join-Path $fixture 'first.xml'
$second = Join-Path $fixture 'second.xml'
$output = Join-Path $fixture 'merged.txt'
[IO.File]::WriteAllText($overlay, '<application><profileable shell="true" /></application>')
[IO.File]::WriteAllText($first, '<application label="first" />')
[IO.File]::WriteAllText($second, '<application label="second" />')
$project = Join-Path $fixture 'incremental.proj'
$targetPath = Join-Path $repo 'eng/AndroidManifestOverlays.targets'
$escapedFixture = [Security.SecurityElement]::Escape($fixture.Replace('\', '/'))
$escapedTarget = [Security.SecurityElement]::Escape($targetPath)
$xml = @'
<Project>
  <PropertyGroup>
    <AndroidApplication>true</AndroidApplication>
    <AndroidManifestMerger>manifestmerger.jar</AndroidManifestMerger>
    <IntermediateOutputPath>FIXTURE/</IntermediateOutputPath>
  </PropertyGroup>
  <Import Project="TARGET" />
  <ItemGroup>
    <AndroidManifestOverlay Include="optional.xml" Condition="'$(Optional)' == 'true'" />
    <AndroidManifestOverlay Include="FIXTURE/first.xml;FIXTURE/second.xml" Condition="'$(Order)' == 'forward'" />
    <AndroidManifestOverlay Include="FIXTURE/second.xml;FIXTURE/first.xml" Condition="'$(Order)' == 'reverse'" />
  </ItemGroup>
  <!-- Match the SDK's input omission: the imported target must supply the missing dependency. -->
  <Target Name="_ManifestMerger" Inputs="$(MSBuildProjectFullPath);@(_AndroidMSBuildAllProjects)" Outputs="FIXTURE/merged.txt">
    <ReadLinesFromFile File="%(AndroidManifestOverlay.Identity)" Condition="'@(AndroidManifestOverlay)' != ''">
      <Output TaskParameter="Lines" ItemName="OverlayContent" />
    </ReadLinesFromFile>
    <WriteLinesToFile File="FIXTURE/merged.txt" Lines="merged;@(OverlayContent)" Overwrite="true" />
  </Target>
</Project>
'@
[IO.File]::WriteAllText($project, $xml.Replace('FIXTURE', $escapedFixture).Replace('TARGET', $escapedTarget))
function Merge([string]$Optional = 'false', [string]$Order = '') {
    $log = dotnet msbuild $project -t:_ManifestMerger -nologo -v:minimal "-p:Optional=$Optional" "-p:Order=$Order" 2>&1
    if ($LASTEXITCODE -ne 0) { throw ('Manifest fixture failed: ' + ($log -join [Environment]::NewLine)) }
    [IO.File]::ReadAllText($output)
}
if ((Merge) -match 'profileable') { throw 'An absent overlay was applied.' }
if ((Merge 'true') -notmatch 'profileable') { throw 'Adding an old overlay did not invalidate the merger.' }
$stamp = (Get-Item -LiteralPath $output).LastWriteTimeUtc
Start-Sleep -Milliseconds 100
$null = Merge 'true'
if ((Get-Item -LiteralPath $output).LastWriteTimeUtc -ne $stamp) { throw 'An unchanged merge was repeated.' }
$oldStamp = (Get-Item -LiteralPath $overlay).LastWriteTimeUtc
[IO.File]::WriteAllText($overlay, '<application><profileable shell="false" /></application>')
(Get-Item -LiteralPath $overlay).LastWriteTimeUtc = $oldStamp
if ((Merge 'true') -notmatch 'shell="false"') { throw 'Content changes with a preserved timestamp were ignored.' }
if ((Merge) -match 'profileable') { throw 'Removing the last overlay did not invalidate the merger.' }
$forward = Merge 'false' 'forward'
$reverse = Merge 'false' 'reverse'
if ($forward.IndexOf('first') -ge $forward.IndexOf('second') -or $reverse.IndexOf('second') -ge $reverse.IndexOf('first')) {
    throw 'Overlay precedence order was not preserved.'
}
Remove-Item -LiteralPath $first
$refusal = dotnet msbuild $project -t:_ManifestMerger -nologo -v:minimal -p:Order=forward 2>&1
if ($LASTEXITCODE -eq 0 -or !(($refusal -join '') -match 'first.xml')) { throw 'A missing requested overlay was not refused.' }
'PASS: absent, added, unchanged, same-timestamp edit, removed, reordered and missing manifest overlays.'
# The final negative child is expected to fail; assertion failures above must still fail the runner.
exit 0
