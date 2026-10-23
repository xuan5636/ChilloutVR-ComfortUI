param([Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$resolvedGame = (Resolve-Path -LiteralPath $GameDir).Path
if (-not (Test-Path -LiteralPath (Join-Path $resolvedGame 'ChilloutVR.exe'))) { throw 'Select your installed ChilloutVR directory.' }
$modOutput = Join-Path $projectRoot 'artifacts/mod'
& dotnet build (Join-Path $projectRoot 'src/ComfortUI.csproj') -c Release "-p:GameDir=$resolvedGame" -o $modOutput
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }
& (Join-Path $projectRoot 'tools/check-json-compatibility.ps1') -GameDir $resolvedGame -ModPath (Join-Path $modOutput 'CVRComfortUI.dll')
& (Join-Path $projectRoot 'installer/build.ps1') -PayloadPath (Join-Path $modOutput 'CVRComfortUI.dll')
