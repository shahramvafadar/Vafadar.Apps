<#
.SYNOPSIS
    Adds or updates UI strings in all languages of a .resx set (neutral/en, fa, de) in one step.

.DESCRIPTION
    Every UI string must exist in every language (a test enforces it). The input is a UTF-8 JSON file:

    {
      "target": "src/Apps/Finance/Vafadar.Finance.App/Resources/Strings/AppResources",
      "remove": ["Old_Key"],
      "entries": [ { "key": "Goal_Title", "en": "Goal", "fa": "هدف", "de": "Ziel", "comment": "optional" } ]
    }

    "target" is the repository-relative path without ".resx"; shared strings live in
    src/Libraries/Vafadar.Localization/Resources/SharedStrings. Each file is written to a temporary file first and then
    moved into place, so a failure (e.g. a full disk) never leaves a truncated resource file.

.EXAMPLE
    ./eng/scripts/Add-Strings.ps1 -JsonPath strings.json
#>
param([Parameter(Mandatory)] [string]$JsonPath)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$spec = [System.IO.File]::ReadAllText((Resolve-Path $JsonPath), [System.Text.Encoding]::UTF8) | ConvertFrom-Json
foreach ($lang in @('en', 'fa', 'de')) {
    $suffix = if ($lang -eq 'en') { '' } else { ".$lang" }
    $path = Join-Path $root ($spec.target + $suffix + '.resx')
    $doc = New-Object System.Xml.XmlDocument
    $doc.PreserveWhitespace = $false
    $doc.Load($path)
    foreach ($key in @($spec.remove)) {
        if (-not $key) { continue }
        $node = $doc.SelectSingleNode("/root/data[@name='$key']")
        if ($node) { [void]$node.ParentNode.RemoveChild($node) }
    }
    foreach ($entry in @($spec.entries)) {
        $value = $entry.$lang
        if ($null -eq $value) { throw "Missing '$lang' for $($entry.key)" }
        $node = $doc.SelectSingleNode("/root/data[@name='$($entry.key)']")
        if (-not $node) {
            $node = $doc.CreateElement('data')
            [void]$node.SetAttribute('name', $entry.key)
            $space = $doc.CreateAttribute('xml', 'space', 'http://www.w3.org/XML/1998/namespace')
            $space.Value = 'preserve'
            [void]$node.Attributes.Append($space)
            [void]$doc.DocumentElement.AppendChild($node)
        }
        $valueNode = $node.SelectSingleNode('value')
        if (-not $valueNode) { $valueNode = $doc.CreateElement('value'); [void]$node.AppendChild($valueNode) }
        $valueNode.InnerText = $value
        if ($lang -eq 'en' -and $entry.comment) {
            $commentNode = $node.SelectSingleNode('comment')
            if (-not $commentNode) { $commentNode = $doc.CreateElement('comment'); [void]$node.AppendChild($commentNode) }
            $commentNode.InnerText = $entry.comment
        }
    }
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = '  '
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $settings.NewLineChars = "`r`n"
    $temp = "$path.tmp"
    $writer = [System.Xml.XmlWriter]::Create($temp, $settings)
    try { $doc.Save($writer) } finally { $writer.Dispose() }
    Move-Item -LiteralPath $temp -Destination $path -Force
    "updated $path ($(@($spec.entries).Count) entries)"
}