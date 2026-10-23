param(
    [Parameter(Mandatory=$true)][string]$GameDir,
    [Parameter(Mandatory=$true)][string]$ModPath
)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath $GameDir).Path
$modFile = (Resolve-Path -LiteralPath $ModPath).Path
Add-Type -Path (Join-Path $gameRoot 'MelonLoader/net35/Mono.Cecil.dll')
$jsonFiles = @(
    (Join-Path $gameRoot 'ChilloutVR_Data/Managed/Newtonsoft.Json.dll'),
    (Join-Path $gameRoot 'MelonLoader/net35/Newtonsoft.Json.dll')
)
$failures = @()
foreach ($jsonFile in $jsonFiles) {
    $resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
    # Different assemblies share version 13.0.0.0. Resolve in this exact
    # directory first rather than inferring compatibility from the version.
    foreach ($searchDirectory in @($resolver.GetSearchDirectories())) { $resolver.RemoveSearchDirectory($searchDirectory) }
    $resolver.AddSearchDirectory((Split-Path $jsonFile))
    $parameters = New-Object Mono.Cecil.ReaderParameters
    $parameters.AssemblyResolver = $resolver
    $module = [Mono.Cecil.ModuleDefinition]::ReadModule($modFile, $parameters)
    try {
        $types = @($module.GetTypeReferences() | Where-Object { $_.Scope.Name -eq 'Newtonsoft.Json' })
        $members = @($module.GetMemberReferences() | Where-Object { $_.DeclaringType.Scope.Name -eq 'Newtonsoft.Json' })
        if (-not $types.Count -or -not $members.Count) { throw 'No Json.NET references found in mod.' }
        foreach ($reference in @($types) + @($members)) {
            try {
                $resolved = $reference.Resolve()
                if ($null -eq $resolved) { throw 'Member absent' }
                $owner = if ($resolved -is [Mono.Cecil.TypeDefinition]) { $resolved.Module } else { $resolved.DeclaringType.Module }
                if ([IO.Path]::GetFullPath($owner.FileName) -ne [IO.Path]::GetFullPath($jsonFile)) { throw 'Unexpected dependency was resolved' }
            } catch { $failures += ('MISSING in ' + $jsonFile + ': ' + $reference.FullName) }
        }
        Write-Output ('Checked Json.NET: ' + $jsonFile + ' | types=' + $types.Count + ' members=' + $members.Count)
    } finally { $module.Dispose(); $resolver.Dispose() }
}
if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
Write-Output 'PASS both runtime Json.NET API surfaces; no game files changed.'
