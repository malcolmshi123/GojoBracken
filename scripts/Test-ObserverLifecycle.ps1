#requires -Version 5.1
[CmdletBinding()]
param([string] $UnityEditorPath, [string] $BepInExCoreDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt'
if ((Get-Content $versionFile -Raw) -notmatch '(?m)^m_EditorVersion:\s*(\S+)') { throw 'Pinned Unity version missing.' }
$version = $Matches[1]
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $standalone = Join-Path $env:ProgramFiles "Unity $version\Editor\Unity.exe"
    $hub = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
    $UnityEditorPath = if (Test-Path -LiteralPath $standalone) { $standalone } else { $hub }
}
if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw 'Pass the pinned Unity Editor path.' }
$metadata = [Diagnostics.FileVersionInfo]::GetVersionInfo($UnityEditorPath).ProductVersion
if ($metadata -notmatch ('^' + [regex]::Escape($version) + '(?=$|[\s_(])')) { throw "Expected Unity $version; found $metadata." }
if ([string]::IsNullOrWhiteSpace($BepInExCoreDirectory)) {
    $props = Join-Path $repository 'local.props'
    if (Test-Path $props) {
        [xml] $local = Get-Content $props
        $BepInExCoreDirectory = [string]$local.Project.PropertyGroup.BepInExCoreDir
    }
}
$dependencies = @('BepInEx.dll', '0Harmony.dll', 'Mono.Cecil.dll', 'MonoMod.Utils.dll', 'MonoMod.RuntimeDetour.dll')
foreach ($name in $dependencies) {
    if ([string]::IsNullOrWhiteSpace($BepInExCoreDirectory) -or -not (Test-Path -LiteralPath (Join-Path $BepInExCoreDirectory $name))) {
        throw "Pass -BepInExCoreDirectory containing $name."
    }
}
$plugin = Join-Path $repository 'src\GojoBracken\bin\Release\netstandard2.1\GojoBracken.dll'
if (-not (Test-Path $plugin)) { throw 'Build the Release plugin before running this check.' }
$run = Join-Path $repository ('artifacts\observer-lifecycle\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Assets\Fixtures', 'Assets\Plugins', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item $versionFile -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
foreach ($name in $dependencies) {
    Copy-Item (Join-Path $BepInExCoreDirectory $name) -Destination (Join-Path $project 'Assets\Plugins')
}
Copy-Item $plugin -Destination (Join-Path $project 'Assets\Plugins\GojoBracken.dll')
foreach ($name in @('FlowermanAI.cs', 'ObserverLifecycleProbe.cs')) {
    Copy-Item (Join-Path $PSScriptRoot ('tests\observer\' + $name)) -Destination (Join-Path $project 'Assets\Fixtures')
}
Copy-Item (Join-Path $PSScriptRoot 'tests\observer\ObserverLifecycleDriver.cs') -Destination (Join-Path $project 'Assets\Editor')
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), '{"dependencies":{"com.unity.modules.animation":"1.0.0","com.unity.modules.assetbundle":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.particlesystem":"1.0.0"}}')
$log = Join-Path $run 'unity-lifecycle.log'
# The Play-mode fixture exits Unity itself and has an Editor watchdog; do not pass -quit.
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'ObserverLifecycleDriver.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Observer lifecycle check failed. Read $log." }
foreach ($marker in @('OBSERVER LIFECYCLE PASS: real Plugin Awake, paused-time coroutine, pipeline, report, no spawn',
    'OBSERVER LIFECYCLE PASS: disable logged', 'OBSERVER LIFECYCLE PASS: disabled options do not start observer',
    'OBSERVER LIFECYCLE PASS: effects opt-in rejects wrong module without starting scans')) {
    if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern $marker)) {
        throw "Unity did not confirm all lifecycle checks. Read $log."
    }
}
Write-Output "Release-DLL observer lifecycle checks passed. Log: $log"
