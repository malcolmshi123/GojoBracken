#requires -Version 5.1
[CmdletBinding()]
param([string] $UnityEditorPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Resolve-TestUnity.ps1')
$run = Join-Path $repository ('artifacts\connected-cosmetic-tests\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\Assets\GojoBrackenPreview') -Destination (Join-Path $project 'Assets') -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ConnectedCosmeticChecks.cs') -Destination (Join-Path $project 'Assets\Editor\ConnectedCosmeticChecks.cs')
$package = (Join-Path $repository 'shared\GojoBracken.Presentation').Replace('\', '/')
$manifest = @{ dependencies = @{
    'ca.malcolmshi.gojobracken.presentation' = "file:$package"
    'com.unity.modules.animation' = '1.0.0'
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.particlesystem' = '1.0.0'
    'com.unity.modules.imgui' = '1.0.0'
    'com.unity.modules.physics' = '1.0.0'
} } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), $manifest)
$log = Join-Path $run 'unity-checks.log'
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'ConnectedCosmeticChecks.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Connected cosmetic checks failed. Read $log." }
$marker = 'CONNECTED COSMETICS PASS:'
if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern $marker)) {
    throw "Unity did not confirm the checks. Read $log."
}
Write-Output "Connected cosmetic checks passed in Unity $version. Log: $log"
