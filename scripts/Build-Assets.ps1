#requires -Version 5.1
<#
.SYNOPSIS
Builds the real Unity assets, or checks their file layout without launching Unity.
.EXAMPLE
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Build-Assets.ps1 -PreflightOnly
.EXAMPLE
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Build-Assets.ps1 -UnityEditorPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe'
#>
[CmdletBinding()]
param(
    [string] $UnityEditorPath,
    [switch] $PreflightOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryDirectory = Split-Path -Path $PSScriptRoot -Parent
$projectDirectory = Join-Path $repositoryDirectory 'UnityProject'
$versionFile = Join-Path $projectDirectory 'ProjectSettings\ProjectVersion.txt'
$versionText = Get-Content -LiteralPath $versionFile -Raw
if ($versionText -notmatch '(?m)^m_EditorVersion:\s*(\S+)') {
    throw 'Unity project has no pinned editor version.'
}
$requiredVersion = $Matches[1]

$requiredAssets = @(
    'Assets\GojoBracken\Prefabs\Gojo.prefab',
    'Assets\GojoBracken\Prefabs\Aura.prefab',
    'Assets\GojoBracken\Audio\DomainExpansion.wav',
    'Assets\GojoBracken\CREDITS.txt'
)
$missing = @($requiredAssets | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $projectDirectory $_) -PathType Leaf)
})
if ($missing.Count -gt 0) {
    throw ("Real assets are not ready. Missing:" + [Environment]::NewLine + ($missing -join [Environment]::NewLine))
}
if ($PreflightOnly) {
    Write-Output "Required asset files are present. Unity $requiredVersion must still validate their types, components, and contents."
    return
}

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$requiredVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw "Unity Editor was not found at $UnityEditorPath. Install Unity $requiredVersion or pass -UnityEditorPath."
}
$UnityEditorPath = (Resolve-Path -LiteralPath $UnityEditorPath).ProviderPath
$editorVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($UnityEditorPath).ProductVersion
if ($editorVersion -notmatch ('^' + [regex]::Escape($requiredVersion) + '(\s|\(|$)')) {
    throw "Expected Unity $requiredVersion; editor metadata reports '$editorVersion'."
}

$outputDirectory = Join-Path $repositoryDirectory 'artifacts\assets'
New-Item -ItemType Directory -Force $outputDirectory | Out-Null
$logFile = Join-Path $outputDirectory ('unity-build-' + [Guid]::NewGuid().ToString('N') + '.log')
# Start-Process joins arguments on Windows; quote path arguments explicitly for spaces.
$arguments = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', ('"' + $projectDirectory + '"'),
    '-buildTarget', 'Win64',
    '-executeMethod', 'GojoBracken.Editor.BuildGojoAssets.BuildWindowsBundle',
    '-logFile', ('"' + $logFile + '"')
)
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {
    throw "Unity failed with exit code $($process.ExitCode). Read $logFile."
}
$bundlePath = Join-Path $outputDirectory 'gojobracken'
if (-not (Test-Path -LiteralPath $bundlePath -PathType Leaf)) {
    throw "Unity exited without producing $bundlePath. Read $logFile."
}
if (-not (Test-Path -LiteralPath $logFile -PathType Leaf) -or
    -not (Select-String -LiteralPath $logFile -SimpleMatch -Quiet -Pattern "Built and reopened Windows x64 bundle: $bundlePath")) {
    throw "Unity did not confirm bundle validation. Read $logFile; an old output file is not evidence of success."
}
Write-Output "Bundle: $bundlePath"
Get-FileHash -LiteralPath $bundlePath -Algorithm SHA256
