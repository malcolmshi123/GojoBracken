#requires -Version 5.1
[CmdletBinding()]
param(
    [string] $UnityEditorPath = 'C:\Program Files\Unity 2022.3.62f2\Editor\Unity.exe',
    [switch] $VerifyOnly,
    [switch] $Reimport
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($VerifyOnly -and $Reimport) { throw 'Choose verification or reimport, not both.' }
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'artifacts\unity'
$project = Join-Path $output 'project'
$model = Join-Path $repo 'artifacts\blender\Gojo.blend'
$blender = Join-Path $repo 'artifacts\blender\tools\blender-4.5.9-windows-x64\blender.exe'
if (-not (Test-Path -LiteralPath $model -PathType Leaf)) { throw 'Approved artifacts/blender/Gojo.blend is required.' }
if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw 'Pass the Unity 2022.3.62f2 Editor path.' }
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($UnityEditorPath).ProductVersion
if ($version -notmatch '^2022\.3\.62f2(?=$|[\s_(])') { throw "Expected Unity 2022.3.62f2; found $version." }
foreach ($folder in @('Assets\Editor', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
$manifest = Join-Path $project 'Packages\manifest.json'
if (-not (Test-Path -LiteralPath $manifest)) {
    $packages = @{ dependencies = @{
        'com.unity.modules.animation' = '1.0.0'; 'com.unity.modules.assetbundle' = '1.0.0'
        'com.unity.modules.audio' = '1.0.0'; 'com.unity.modules.imageconversion' = '1.0.0'
        'com.unity.modules.physics' = '1.0.0'; 'com.unity.modules.particlesystem' = '1.0.0'
        'com.unity.modules.imgui' = '1.0.0'; 'com.unity.render-pipelines.high-definition' = '14.0.12'
    } }
    [IO.File]::WriteAllText($manifest, ($packages | ConvertTo-Json))
    Copy-Item -LiteralPath (Join-Path $repo 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings')
}
Copy-Item -Path (Join-Path $PSScriptRoot 'blender\*.cs') -Destination (Join-Path $project 'Assets\Editor') -Force
$runtime = Join-Path $project 'Assets\GojoRuntime'
New-Item -ItemType Directory -Force $runtime | Out-Null
foreach ($name in @('BrackenModelAttachment.cs', 'BrackenVisibilityReader.cs')) {
    Copy-Item -LiteralPath (Join-Path $repo "src\GojoBracken\$name") -Destination $runtime -Force
    $oldEditorCopy = Join-Path $project "Assets\Editor\$name"
    if (Test-Path -LiteralPath $oldEditorCopy) { Remove-Item -LiteralPath $oldEditorCopy -Force }
    if (Test-Path -LiteralPath ($oldEditorCopy + '.meta')) { Remove-Item -LiteralPath ($oldEditorCopy + '.meta') -Force }
}
[IO.File]::WriteAllText((Join-Path $runtime 'EditorVisibility.cs'), '[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]')
if (-not $VerifyOnly) {
    if (-not (Test-Path -LiteralPath $blender -PathType Leaf)) { throw 'The local Blender installation is required.' }
    & $blender -b $model --python-exit-code 1 --python (Join-Path $PSScriptRoot 'blender\export_model.py')
    if ($LASTEXITCODE -ne 0) { throw 'Blender validation/export failed.' }
}
$dataFile = Join-Path $output 'Gojo.json'
if (-not (Test-Path -LiteralPath $dataFile)) { throw 'Export Gojo.json before verification.' }
$data = Get-Content -LiteralPath $dataFile -Raw
if ($data -notmatch '"sourceSha256":"([a-f0-9]{64})"') { throw 'Export source hash is missing.' }
if ($Matches[1] -ine (Get-FileHash -LiteralPath $model -Algorithm SHA256).Hash) { throw 'The Unity data does not match Gojo.blend.' }
function Invoke-Unity([string] $Method, [string] $LogName, [string] $Marker) {
    $log = Join-Path $output $LogName
    $arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'), '-buildTarget', 'Win64',
        '-force-d3d11', '-executeMethod', $Method, '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern $Marker)) {
        throw "Unity failed during $Method. Read $log."
    }
}
if ($Reimport) {
    Invoke-Unity 'ImportGojo.Reimport' 'import.log' 'GOJO UNITY IMPORT PASS:'
} elseif (-not $VerifyOnly -and -not (Test-Path -LiteralPath (Join-Path $project 'Assets\Gojo'))) {
    Invoke-Unity 'ImportGojo.Run' 'import.log' 'GOJO UNITY IMPORT PASS:'
}
Invoke-Unity 'GojoModelChecks.VerifyBundle' 'verification.log' 'GOJO BUNDLE CHECKS PASS'
if (-not $VerifyOnly) { Invoke-Unity 'CaptureGojo.Run' 'previews.log' 'GOJO HDRP PREVIEWS PASS:' }
Write-Output "Native prefab: $project\Assets\Gojo\Gojo.prefab"
Get-FileHash -LiteralPath (Join-Path $output 'bundle\gojo-model') -Algorithm SHA256
