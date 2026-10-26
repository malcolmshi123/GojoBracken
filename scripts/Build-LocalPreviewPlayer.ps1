#requires -Version 5.1
[CmdletBinding()]
param([string] $UnityEditorPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$version = '2022.3.62f2'
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $standalone = Join-Path $env:ProgramFiles "Unity $version\Editor\Unity.exe"
    $hub = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
    $UnityEditorPath = if (Test-Path -LiteralPath $standalone) { $standalone } else { $hub }
}
if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw 'Pass the pinned Unity Editor path.' }
$metadata = [Diagnostics.FileVersionInfo]::GetVersionInfo($UnityEditorPath).ProductVersion
if ($metadata -notmatch ('^' + [regex]::Escape($version) + '(?=$|[\s_(])')) { throw "Expected Unity $version; found $metadata." }
$native = Join-Path $repository 'UnityProject\Assets\LocalModelStudy\NativeGojo'
if (-not (Test-Path -LiteralPath (Join-Path $native 'Gojo.prefab'))) { throw 'Bake the local native Gojo prefab first.' }
$run = Join-Path $repository ('artifacts\local-preview-player\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Assets\Runtime', 'Assets\Resources', 'Assets\LocalModelStudy', 'Assets\GojoBracken\Prefabs', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
Copy-Item -LiteralPath $native -Destination (Join-Path $project 'Assets\LocalModelStudy') -Recurse
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\Assets\GojoBracken\Effects') -Destination (Join-Path $project 'Assets\GojoBracken') -Recurse
foreach ($name in @('Aura.prefab', 'Aura.prefab.meta')) {
    Copy-Item -LiteralPath (Join-Path $repository ('UnityProject\Assets\GojoBracken\Prefabs\' + $name)) -Destination (Join-Path $project 'Assets\GojoBracken\Prefabs')
}
foreach ($name in @('BrackenCosmeticController.cs', 'BrackenVisibilityReader.cs', 'GojoLocomotion.cs')) {
    Copy-Item -LiteralPath (Join-Path $repository ('src\GojoBracken\' + $name)) -Destination (Join-Path $project 'Assets\Runtime')
}
Get-ChildItem -LiteralPath (Join-Path $repository 'shared\GojoBracken.Presentation\Runtime') -Filter '*.cs' | Copy-Item -Destination (Join-Path $project 'Assets\Runtime')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'local-preview\LocalPrefabPreview.cs') -Destination (Join-Path $project 'Assets\Runtime')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'local-preview\BuildLocalPreview.cs') -Destination (Join-Path $project 'Assets\Editor')
$records = Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Recurse -File | Sort-Object FullName | ForEach-Object {
    $_.FullName.Substring($project.Length) + ':' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
}
$hash = [Security.Cryptography.SHA256]::Create()
try { $digest = [BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes(($records -join "`n")))).Replace('-', '') }
finally { $hash.Dispose() }
$info = @{ kind = 'local-prefab-preview'; sourceDigest = $digest; builtUtc = [DateTime]::UtcNow.ToString('o'); unityVersion = $version } | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $project 'Assets\Resources\build-info.json'), $info)
$manifest = Get-Content -LiteralPath (Join-Path $repository 'UnityProject\Packages\manifest.json') -Raw | ConvertFrom-Json
$manifest.dependencies.'ca.malcolmshi.gojobracken.presentation' = 'file:' + (Join-Path $repository 'shared\GojoBracken.Presentation').Replace('\', '/')
foreach ($module in @('com.unity.modules.jsonserialize', 'com.unity.modules.screencapture')) {
    $manifest.dependencies | Add-Member -NotePropertyName $module -NotePropertyValue '1.0.0' -Force
}
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), ($manifest | ConvertTo-Json -Depth 8))
$log = Join-Path $run 'unity-build.log'
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'BuildLocalPreview.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern 'LOCAL PREVIEW BUILD PASS:')) {
    throw "Local preview build failed. Read $log."
}
$player = Join-Path $run 'Player'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'local-preview\Run-PreviewChecks.ps1') -Destination $player
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'local-preview\README.txt') -Destination $player
$zip = Join-Path $run 'GojoBrackenLocomotionPreview.zip'
Compress-Archive -Path (Join-Path $player '*') -DestinationPath $zip
Write-Output "Built local preview ZIP: $zip"
Write-Output "Source digest: $digest"
