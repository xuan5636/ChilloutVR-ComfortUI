$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$cases = @(
    @{Name='CohtmlLifetimeTests0210';Arguments=@()},
    @{Name='ChatModeTests0210';Arguments=@((Join-Path $projectRoot 'assets/locales.json'))},
    @{Name='TranslationTests029';Arguments=@()},
    @{Name='CameraPolicyTests029';Arguments=@((Join-Path $projectRoot 'assets/camera-zh.json'),(Join-Path $projectRoot 'assets/locales.json'))},
    @{Name='NameplateTests030';Arguments=@()},
    @{Name='LocalModelTests031';Arguments=@()},
    @{Name='HudMicrophoneTests033';Arguments=@()}
)
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'artifacts/tests') -Force | Out-Null
Push-Location $projectRoot
try {
    foreach ($case in $cases) {
        $testOutput = Join-Path $projectRoot ('artifacts/tests/' + $case.Name)
        & dotnet build (Join-Path $projectRoot ('tests/' + $case.Name + '.csproj')) -c Release -o $testOutput
        if ($LASTEXITCODE -ne 0) { throw ('Test build failed: ' + $case.Name) }
        $testArguments = $case.Arguments
        & (Join-Path $testOutput ($case.Name + '.exe')) @testArguments
        if ($LASTEXITCODE -ne 0) { throw ('Test failed: ' + $case.Name) }
    }
} finally { Pop-Location }
