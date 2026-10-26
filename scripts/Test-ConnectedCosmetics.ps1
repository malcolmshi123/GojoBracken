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
$run = Join-Path $repository ('artifacts\connected-cosmetic-tests\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\Assets\GojoBrackenPreview') -Destination (Join-Path $project 'Assets') -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\ConnectedCosmeticChecks.cs') -Destination (Join-Path $project 'Assets\Editor\ConnectedCosmeticChecks.cs')
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
