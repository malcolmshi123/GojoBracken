#requires -Version 5.1
[CmdletBinding()]
param([string] $UnityEditorPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$versionText = Get-Content (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Raw
if ($versionText -notmatch '(?m)^m_EditorVersion:\s*(\S+)') { throw 'Pinned Unity version missing.' }
$version = $Matches[1]
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $standalone = Join-Path $env:ProgramFiles "Unity $version\Editor\Unity.exe"
    $hub = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
    $UnityEditorPath = if (Test-Path -LiteralPath $standalone) { $standalone } else { $hub }
}
if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw 'Pass the pinned Unity Editor path.' }
$metadata = [Diagnostics.FileVersionInfo]::GetVersionInfo($UnityEditorPath).ProductVersion
if ($metadata -notmatch ('^' + [regex]::Escape($version) + '(?=$|[\s_(])')) { throw "Expected Unity $version; found $metadata." }
$run = Join-Path $repository ('artifacts\cosmetic-controller-tests\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Assets\Runtime', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
New-Item -ItemType Directory -Force (Join-Path $project 'Assets\GojoBracken\Prefabs') | Out-Null
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\Assets\GojoBracken\Effects') -Destination (Join-Path $project 'Assets\GojoBracken') -Recurse
foreach ($name in @('Aura.prefab', 'Aura.prefab.meta')) {
    Copy-Item -LiteralPath (Join-Path $repository ('UnityProject\Assets\GojoBracken\Prefabs\' + $name)) -Destination (Join-Path $project 'Assets\GojoBracken\Prefabs')
}
Copy-Item -LiteralPath (Join-Path $repository 'src\GojoBracken\BrackenCosmeticController.cs') -Destination (Join-Path $project 'Assets\Runtime')
foreach ($name in @('BrackenGameBinding.cs', 'DevelopmentEffects.cs', 'GojoAssetBundle.cs')) {
    Copy-Item -LiteralPath (Join-Path $repository ('src\GojoBracken\' + $name)) -Destination (Join-Path $project 'Assets\Runtime')
}
Get-ChildItem -LiteralPath (Join-Path $repository 'shared\GojoBracken.Presentation\Runtime') -Filter '*.cs' | Copy-Item -Destination (Join-Path $project 'Assets\Runtime')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\cosmetics\CosmeticControllerProbe.cs') -Destination (Join-Path $project 'Assets\Runtime')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\cosmetics\GameBindingChecks.cs') -Destination (Join-Path $project 'Assets\Runtime')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\cosmetics\CosmeticControllerDriver.cs') -Destination (Join-Path $project 'Assets\Editor')
$manifest = @{ dependencies = @{
    'com.unity.modules.assetbundle' = '1.0.0'
    'com.unity.modules.animation' = '1.0.0'
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.particlesystem' = '1.0.0'
    'com.unity.modules.physics' = '1.0.0'
} } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), $manifest)
$log = Join-Path $run 'unity-checks.log'
# The Play-mode probe exits Unity; the Editor driver enforces a watchdog.
$arguments = @('-batchmode', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'CosmeticControllerDriver.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Cosmetic controller checks failed. Read $log." }
if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern 'COSMETIC CONTROLLER PASS:')) {
    throw "Unity did not confirm the checks. Read $log."
}
if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern 'GAME BINDING PASS:')) {
    throw "Unity did not confirm the binding checks. Read $log."
}
Write-Output "Cosmetic controller Play-mode checks passed in Unity $version. Log: $log"
