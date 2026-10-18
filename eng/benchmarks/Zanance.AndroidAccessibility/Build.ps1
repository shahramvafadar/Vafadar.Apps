[CmdletBinding()]
param(
    [string]$AndroidSdk = "${env:ProgramFiles(x86)}\Android\android-sdk",
    [string]$JavaHome = "${env:ProgramFiles(x86)}\Android\openjdk\jdk-21.0.8",
    [string]$OutputDirectory = 'artifacts/android-accessibility-observer'
)
$ErrorActionPreference = 'Stop'
# Shell-only DEX packaging uses installed tools, with no restore or app mutation.
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Use a fresh output directory.' }
$androidJar = Join-Path $AndroidSdk 'platforms/android-36/android.jar'
$compiler = Join-Path $JavaHome 'bin/javac.exe'
$dexer = Join-Path $AndroidSdk 'build-tools/36.0.0/d8.bat'
foreach ($path in @($androidJar, $compiler, $dexer)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing installed tool: $path" }
}
$classes = Join-Path $OutputDirectory 'classes'
New-Item -ItemType Directory -Path $classes -Force | Out-Null
& $compiler --release 11 -Xlint:all -Werror -cp $androidJar -d $classes (Join-Path $PSScriptRoot 'AccessibilityObserver.java')
if ($LASTEXITCODE -ne 0) { throw 'Java compilation failed.' }
$classFiles = @(Get-ChildItem -LiteralPath $classes -Recurse -Filter '*.class' | ForEach-Object FullName)
$jar = Join-Path $OutputDirectory 'observer.jar'
& $dexer --release --min-api 24 --lib $androidJar --output $jar $classFiles
if ($LASTEXITCODE -ne 0) { throw 'DEX packaging failed.' }
Get-FileHash -LiteralPath $jar -Algorithm SHA256
