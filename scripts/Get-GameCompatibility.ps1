#requires -Version 5.1
<#
.SYNOPSIS
Reports installed game and mod-loader file versions without loading game assemblies.
.DESCRIPTION
Compatible with Windows PowerShell 5.1. No PowerShell upgrade is required.
The examples set RemoteSigned only for the child process; no persistent policy is changed.
.EXAMPLE
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Get-GameCompatibility.ps1 -GameDirectory 'C:\Games\Lethal Company'
.EXAMPLE
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Get-GameCompatibility.ps1 -GameDirectory 'C:\Games\Lethal Company' -BepInExCoreDirectory 'C:\ModProfiles\GojoTest\BepInEx\core'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $GameDirectory,

    [string] $BepInExCoreDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$gameRoot = (Resolve-Path -LiteralPath $GameDirectory).ProviderPath
$managedDirectory = Join-Path $gameRoot 'Lethal Company_Data\Managed'
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'Lethal Company.exe') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $managedDirectory 'Assembly-CSharp.dll') -PathType Leaf)) {
    throw 'GameDirectory must contain Lethal Company.exe and Lethal Company_Data\Managed\Assembly-CSharp.dll. Use the game installation, not a mod profile.'
}

if ([string]::IsNullOrWhiteSpace($BepInExCoreDirectory)) {
    $coreDirectory = Join-Path $gameRoot 'BepInEx\core'
} else {
    $coreDirectory = [System.IO.Path]::GetFullPath($BepInExCoreDirectory)
}

function Get-FileDetails {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{ present = $false; fileVersion = $null; productVersion = $null; sha256 = $null }
    }

    $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($Path)
    return [ordered]@{
        present = $true
        fileVersion = $version.FileVersion
        productVersion = $version.ProductVersion
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    }
}

$files = [ordered]@{
    game = Get-FileDetails (Join-Path $gameRoot 'Lethal Company.exe')
    unityPlayer = Get-FileDetails (Join-Path $gameRoot 'UnityPlayer.dll')
    gameAssembly = Get-FileDetails (Join-Path $managedDirectory 'Assembly-CSharp.dll')
    unityCore = Get-FileDetails (Join-Path $managedDirectory 'UnityEngine.CoreModule.dll')
    hdrpAssembly = Get-FileDetails (Join-Path $managedDirectory 'Unity.RenderPipelines.HighDefinition.Runtime.dll')
    bepinex = Get-FileDetails (Join-Path $coreDirectory 'BepInEx.dll')
    harmony = Get-FileDetails (Join-Path $coreDirectory '0Harmony.dll')
}

# Unity player version metadata is evidence to inspect, not a guessed editor pin.
$unityVersion = $null
if ($files.unityPlayer.productVersion -match '(\d+\.\d+\.\d+[abfp]\d+)') {
    $unityVersion = $Matches[1]
}

# Steam's build ID identifies the installed content; it is not the in-game version number.
$steamAppsDirectory = Split-Path -Path (Split-Path -Path $gameRoot -Parent) -Parent
$steamManifest = Join-Path $steamAppsDirectory 'appmanifest_1966720.acf'
$steamBuildId = $null
if (Test-Path -LiteralPath $steamManifest -PathType Leaf) {
    $manifestText = Get-Content -LiteralPath $steamManifest -Raw
    if ($manifestText -match '"buildid"\s+"(\d+)"') {
        $steamBuildId = $Matches[1]
    }
}

$notes = @(
    'File versions and hashes identify this installation; they do not prove in-game compatibility.',
    'HDRP assembly presence is evidence of an available pipeline, not proof of the active render settings.',
    'Bracken state mapping, anger range, corpse handling, and network replication still require code/runtime inspection.'
)
if ($null -eq $unityVersion) {
    $notes += 'Unity editor version could not be determined exactly. Inspect UnityPlayer productVersion and the game Player.log before pinning Unity.'
}
if (-not $files.bepinex.present -or -not $files.harmony.present) {
    $notes += 'BepInEx or Harmony is missing. For a mod manager, pass the test profile BepInEx\core directory explicitly.'
}

[ordered]@{
    reportVersion = 1
    powershellVersion = $PSVersionTable.PSVersion.ToString()
    collectedAtUtc = [DateTime]::UtcNow.ToString('o')
    steamBuildId = $steamBuildId
    unityVersionFromPlayer = $unityVersion
    files = $files
    notes = $notes
} | ConvertTo-Json -Depth 6
