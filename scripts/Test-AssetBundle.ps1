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
$run = Join-Path $repository ('artifacts\asset-bundle-tests\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
Copy-Item -LiteralPath (Join-Path $repository 'src\GojoBracken\GojoAssetBundle.cs') -Destination (Join-Path $project 'Assets\Editor\GojoAssetBundle.cs')
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\Assets\Editor\BuildGojoAssets.cs') -Destination (Join-Path $project 'Assets\Editor\BuildGojoAssets.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\AssetBundleChecks.cs') -Destination (Join-Path $project 'Assets\Editor\AssetBundleChecks.cs')
$package = (Join-Path $repository 'shared\GojoBracken.Presentation').Replace('\', '/')
$manifest = @{ dependencies = @{
    'ca.malcolmshi.gojobracken.presentation' = "file:$package"
    'com.unity.modules.assetbundle' = '1.0.0'
    'com.unity.modules.animation' = '1.0.0'
    'com.unity.modules.audio' = '1.0.0'
    'com.unity.modules.particlesystem' = '1.0.0'
    'com.unity.modules.imgui' = '1.0.0'
    'com.unity.modules.physics' = '1.0.0'
} } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), $manifest)
$log = Join-Path $run 'unity-checks.log'
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'AssetBundleChecks.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Asset bundle checks failed. Read $log." }
$marker = 'ASSET BUNDLE PASS:'
if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern $marker)) {
    throw "Unity did not confirm the checks. Read $log."
}
if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern 'ASSET EXPORTER PASS:')) {
    throw "Unity did not confirm the exporter round-trip. Read $log."
}
Write-Output "Asset bundle checks passed in Unity $version. Log: $log"
