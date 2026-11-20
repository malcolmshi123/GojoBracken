#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $GameDirectory,
    [Parameter(Mandatory)] [string] $ProfileDirectory,
    [ValidateSet('LiveEnemy', 'RenderPreview')] [string] $Mode = 'LiveEnemy',
    [string] $ExpectedManifest = (Join-Path $PSScriptRoot 'expected.json'),
    [string] $OutputPath = (Join-Path $PSScriptRoot 'preflight-report.json')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), '{"preflightPassed":false,"gameplayValidated":false,"status":"Preflight has not completed."}')
$expected = Get-Content -LiteralPath $ExpectedManifest -Raw | ConvertFrom-Json
$game = (Resolve-Path -LiteralPath $GameDirectory).Path
$profile = (Resolve-Path -LiteralPath $ProfileDirectory).Path
$managed = Join-Path $game 'Lethal Company_Data\Managed'
foreach ($file in @('Lethal Company.exe', 'UnityPlayer.dll', 'Lethal Company_Data\Managed\Assembly-CSharp.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $game $file) -PathType Leaf)) { throw "Missing game input: $file" }
}
if (Get-Process -Name 'Lethal Company' -ErrorAction SilentlyContinue) { throw 'Close the game before checking the test profile.' }
$playerVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $game 'UnityPlayer.dll')).ProductVersion
if ($playerVersion -notmatch '^2022\.3\.62f2(?=$|[\s_(])') { throw "Unsupported Unity player: $playerVersion" }
$cecil = Join-Path $profile 'BepInEx\core\Mono.Cecil.dll'
if (-not (Test-Path -LiteralPath $cecil)) { throw 'A separate test profile with BepInEx core is required.' }
Add-Type -Path $cecil
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'Assembly-CSharp.dll'))
try { $mvid = $assembly.MainModule.Mvid.ToString() } finally { $assembly.Dispose() }
if ($mvid -ne $expected.gameMvid) { throw "Unsupported game assembly MVID: $mvid. Do not bypass the version guard." }
$plugins = Join-Path $profile 'BepInEx\plugins'
$copies = @(Get-ChildItem -LiteralPath $plugins -Recurse -File -Filter GojoBracken.dll)
if ($copies.Count -ne 1) { throw "Expected one GojoBracken DLL in the test profile; found $($copies.Count)." }
$plugin = $copies[0].FullName
$bundleName = if ($Mode -eq 'RenderPreview') { 'gojobracken-hdrp-material-check' } else { 'gojobracken' }
$bundle = Join-Path $copies[0].DirectoryName $bundleName
if ((Get-FileHash -LiteralPath $plugin).Hash -ne $expected.dllSha256) { throw 'Installed test DLL does not match this candidate.' }
if (-not (Test-Path -LiteralPath $bundle) -or (Get-FileHash -LiteralPath $bundle).Hash -ne $expected.bundleSha256) { throw 'Installed bundle does not match the approved candidate.' }
$config = Join-Path $profile 'BepInEx\config\FreakyFriday.GojoBracken.cfg'
$values = @{}
$section = ''
foreach ($line in (Get-Content -LiteralPath $config)) {
    if ($line -match '^\s*\[([^\]]+)\]') { $section = $Matches[1] }
    elseif ($line -match '^\s*([^#;=]+?)\s*=\s*(.*?)\s*$') { $values["$section.$($Matches[1].Trim())"] = $Matches[2].Trim() }
}
if ($Mode -eq 'RenderPreview') {
    foreach ($key in @('DevelopmentEffects.Enabled', 'DevelopmentEffects.StandingModelPreview', 'DevelopmentEffects.SoloLiveTest')) {
        if ($values[$key] -ne 'false') { throw "Preview configuration requires $key=false." }
    }
    if ($values['RenderPreview.Enabled'] -ne 'true') { throw 'RenderPreview.Enabled must be true for the simulated preview.' }
} else {
    foreach ($key in @('DevelopmentEffects.Enabled', 'DevelopmentEffects.StandingModelPreview', 'DevelopmentEffects.SoloLiveTest')) {
        if ($values[$key] -ne 'true') { throw "Test configuration requires $key=true." }
    }
    if ($values['RenderPreview.Enabled'] -ne 'false') { throw 'RenderPreview.Enabled must be false for the live enemy test.' }
}
$report = [ordered]@{
    preflightPassed = $true
    gameplayValidated = $false
    mode = $Mode
    simulatedChecksOnly = ($Mode -eq 'RenderPreview')
    bundleFile = $bundleName
    collectedUtc = [DateTime]::UtcNow.ToString('o')
    unityPlayerVersion = $playerVersion
    gameMvid = $mvid
    gameAssemblySha256 = (Get-FileHash -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll')).Hash
    dllSha256 = (Get-FileHash -LiteralPath $plugin).Hash
    bundleSha256 = (Get-FileHash -LiteralPath $bundle).Hash
    gameDirectory = $game
    profileDirectory = $profile
    configuration = $values
}
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), ($report | ConvertTo-Json -Depth 4))
Write-Output "Preflight passed; gameplay still needs visual/audio checks. Report: $OutputPath"
