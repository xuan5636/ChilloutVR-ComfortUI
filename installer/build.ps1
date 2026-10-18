param([string]$PayloadPath, [string]$OutFile)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $PayloadPath) { $PayloadPath = Join-Path $projectRoot 'artifacts/mod/CVRComfortUI.dll' }
if (-not $OutFile) { $OutFile = Join-Path $projectRoot 'artifacts/ComfortUI-Setup-0.3.2.exe' }
$resolvedPayload = (Resolve-Path -LiteralPath $PayloadPath).Path
$resolvedOutput = [IO.Path]::GetFullPath($OutFile)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler was not found.' }
$payloadHash = (Get-FileHash -LiteralPath $resolvedPayload -Algorithm SHA256).Hash
$originalSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Setup.cs'))
$hashPattern = 'public const string PayloadHash="[A-F0-9]{64}";'
if ([regex]::Matches($originalSource,$hashPattern).Count -ne 1) { throw 'Installer payload constant could not be located unambiguously.' }
$buildSource = [regex]::Replace($originalSource,$hashPattern,('public const string PayloadHash="' + $payloadHash + '";'))
$temporaryRoot = Join-Path $projectRoot ('artifacts/installer-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryRoot,(Split-Path $resolvedOutput -Parent) -Force | Out-Null
$temporarySource = Join-Path $temporaryRoot 'Setup.cs'
[IO.File]::WriteAllText($temporarySource,$buildSource,[Text.UTF8Encoding]::new($true))
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll ("/win32manifest:" + (Join-Path $PSScriptRoot 'app.manifest')) ("/resource:" + $resolvedPayload + ',ComfortUI.Payload') ("/out:" + $resolvedOutput) $temporarySource
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
Get-FileHash -LiteralPath $resolvedPayload,$resolvedOutput -Algorithm SHA256
