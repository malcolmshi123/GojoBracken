#requires -Version 5.1
[CmdletBinding()]
param([string] $UnityEditorPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repo 'UnityProject'
$version = '2022.3.62f2'
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = Join-Path $env:ProgramFiles "Unity $version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $UnityEditorPath)) {
        $UnityEditorPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
    }
}
$native = Join-Path $source 'Assets\LocalModelStudy\NativeGojo'
if (-not (Test-Path -LiteralPath (Join-Path $native 'Gojo.prefab'))) { throw 'Bake the local native Gojo prefab first.' }
$run = Join-Path $repo ('artifacts\hdrp\' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$project = Join-Path $run 'u'
foreach ($folder in @('Assets\Editor', 'Assets\LocalModelStudy', 'Assets\GojoBracken\Prefabs', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $source 'ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings')
Copy-Item -LiteralPath $native -Destination (Join-Path $project 'Assets\LocalModelStudy') -Recurse
Copy-Item -LiteralPath (Join-Path $source 'Assets\GojoBracken\Effects') -Destination (Join-Path $project 'Assets\GojoBracken') -Recurse
foreach ($name in @('Aura.prefab', 'Aura.prefab.meta')) {
    Copy-Item -LiteralPath (Join-Path $source ('Assets\GojoBracken\Prefabs\' + $name)) -Destination (Join-Path $project 'Assets\GojoBracken\Prefabs')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'hdrp\BuildHdrpMaterials.cs') -Destination (Join-Path $project 'Assets\Editor')
Copy-Item -Path (Join-Path $PSScriptRoot 'hdrp\*.shader') -Destination (Join-Path $project 'Assets')
$manifest = Get-Content -LiteralPath (Join-Path $source 'Packages\manifest.json') -Raw | ConvertFrom-Json
$manifest.dependencies.PSObject.Properties.Remove('ca.malcolmshi.gojobracken.presentation')
$manifest.dependencies | Add-Member -NotePropertyName 'com.unity.render-pipelines.high-definition' -NotePropertyValue '14.0.12'
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), ($manifest | ConvertTo-Json -Depth 8))
$log = Join-Path $run 'unity-build.log'
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-buildTarget', 'Win64', '-force-d3d11', '-executeMethod', 'BuildHdrpMaterials.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern 'HDRP MATERIALS PASS:')) {
    throw "HDRP material build failed. Read $log."
}
Write-Output "HDRP material check passed. Prefabs, bundle and log: $run"
