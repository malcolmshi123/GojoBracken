#requires -Version 5.1
[CmdletBinding()]
param([string] $UnityEditorPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Resolve-TestUnity.ps1')
$run = Join-Path $repository ('artifacts\baked-aura-tests\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Assets\GojoBracken\Prefabs', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\Assets\GojoBracken\Effects') -Destination (Join-Path $project 'Assets\GojoBracken') -Recurse
foreach ($name in @('Aura.prefab', 'Aura.prefab.meta')) {
    Copy-Item -LiteralPath (Join-Path $repository ('UnityProject\Assets\GojoBracken\Prefabs\' + $name)) -Destination (Join-Path $project 'Assets\GojoBracken\Prefabs')
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\Assets\GojoBrackenPreview') -Destination (Join-Path $project 'Assets') -Recurse
foreach ($name in @('BuildGojoAssets.cs', 'BakeNativeAura.cs')) {
    Copy-Item -LiteralPath (Join-Path $repository ('UnityProject\Assets\Editor\' + $name)) -Destination (Join-Path $project 'Assets\Editor')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BakedAuraChecks.cs') -Destination (Join-Path $project 'Assets\Editor')
$package = (Join-Path $repository 'shared\GojoBracken.Presentation').Replace('\', '/')
$manifest = @{ dependencies = @{
    'ca.malcolmshi.gojobracken.presentation' = "file:$package"
    'com.unity.modules.animation' = '1.0.0'
    'com.unity.modules.assetbundle' = '1.0.0'
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.particlesystem' = '1.0.0'
    'com.unity.modules.imgui' = '1.0.0'
    'com.unity.modules.physics' = '1.0.0'
} } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), $manifest)
$log = Join-Path $run 'unity-checks.log'
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'BakedAuraChecks.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Baked aura checks failed. Read $log." }
if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern 'BAKED AURA PASS:')) {
    throw "Unity did not confirm the checks. Read $log."
}
Write-Output "Baked aura Windows bundle checks passed in Unity $version. Log: $log"