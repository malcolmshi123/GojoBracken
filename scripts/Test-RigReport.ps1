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
$run = Join-Path $repository ('artifacts\rig-report-tests\' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'UnityProject'
foreach ($folder in @('Assets\Editor', 'Assets\Fixtures', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Force (Join-Path $project $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repository 'UnityProject\ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $project 'ProjectSettings\ProjectVersion.txt')
Copy-Item -LiteralPath (Join-Path $repository 'src\GojoBracken\BrackenRigReport.cs') -Destination (Join-Path $project 'Assets\Editor\BrackenRigReport.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\RigReportFixtureDriver.cs') -Destination (Join-Path $project 'Assets\Editor\RigReportFixtureDriver.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\RigReportFixtureAI.cs') -Destination (Join-Path $project 'Assets\Fixtures\RigReportFixtureAI.cs')
[IO.File]::WriteAllText((Join-Path $project 'Packages\manifest.json'), '{"dependencies":{"com.unity.modules.animation":"1.0.0"}}')
$log = Join-Path $run 'unity-checks.log'
$arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'RigReportFixtureDriver.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Rig checks failed. Read $log." }
foreach ($marker in @('RIG CHECK PASS: scene exclusion and bounded timeout',
    'RIG CHECK PASS: loaded prefab, bones, states, clips, explicit unavailable parameters, single export, no mutation',
    'RIG CHECK PASS: unavailable type and I/O failure')) {
    if (-not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern $marker)) {
        throw "Unity did not confirm all checks. Read $log."
    }
}
Write-Output "Rig report checks passed in Unity $version. Log: $log"
