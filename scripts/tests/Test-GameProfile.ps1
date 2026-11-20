#requires -Version 5.1
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $CecilPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$root = Join-Path $repo ('artifacts\game-profile-checks\' + [Guid]::NewGuid().ToString('N'))
$game = Join-Path $root 'SyntheticGame'
$profile = Join-Path $root 'SyntheticProfile'
$plugin = Join-Path $profile 'BepInEx\plugins\GojoBracken'
foreach ($folder in @($game, "$game\Lethal Company_Data\Managed", "$profile\BepInEx\core", "$profile\BepInEx\config", $plugin)) {
    New-Item -ItemType Directory -Force $folder | Out-Null
}
[IO.File]::WriteAllText((Join-Path $game 'Lethal Company.exe'), 'Synthetic preflight fixture; never executable.')
$versionSource = '[assembly: System.Reflection.AssemblyInformationalVersion("2022.3.62f2")] public sealed class GameProfileFixture {}'
Add-Type -TypeDefinition $versionSource -OutputAssembly (Join-Path $game 'UnityPlayer.dll')
Copy-Item -LiteralPath (Join-Path $game 'UnityPlayer.dll') -Destination "$game\Lethal Company_Data\Managed\Assembly-CSharp.dll"
Copy-Item -LiteralPath $CecilPath -Destination "$profile\BepInEx\core\Mono.Cecil.dll"
Copy-Item -LiteralPath (Join-Path $repo 'packaging\solo\FreakyFriday.GojoBracken.cfg') -Destination "$profile\BepInEx\config"
[IO.File]::WriteAllText((Join-Path $plugin 'GojoBracken.dll'), 'Synthetic plugin payload')
[IO.File]::WriteAllText((Join-Path $plugin 'gojobracken'), 'Synthetic bundle payload')
Add-Type -Path $CecilPath
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $game 'UnityPlayer.dll'))
try { $mvid = $assembly.MainModule.Mvid.ToString() } finally { $assembly.Dispose() }
$expected = [ordered]@{gameMvid=$mvid;dllSha256=(Get-FileHash "$plugin\GojoBracken.dll").Hash;bundleSha256=(Get-FileHash "$plugin\gojobracken").Hash}
$manifest = Join-Path $root 'expected.json'
[IO.File]::WriteAllText($manifest, ($expected | ConvertTo-Json))
$report = Join-Path $root 'report.json'
function Invoke-Check([bool] $ShouldPass, [string] $Name, [string] $Mode = '') {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'RemoteSigned', '-File', ('"' + (Join-Path $repo 'scripts\Test-GameProfile.ps1') + '"'),
        '-GameDirectory', ('"' + $game + '"'), '-ProfileDirectory', ('"' + $profile + '"'),
        '-ExpectedManifest', ('"' + $manifest + '"'), '-OutputPath', ('"' + $report + '"'))
    if ($Mode) { $arguments += @('-Mode', $Mode) }
    $process = Start-Process -FilePath powershell.exe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $root "$Name.log") -RedirectStandardError (Join-Path $root "$Name-error.log")
    $passed = $process.ExitCode -eq 0
    if ($passed -ne $ShouldPass) { throw "Unexpected preflight result: $Name" }
    $record = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if ($record.preflightPassed -ne $ShouldPass -or $record.gameplayValidated) { throw "Incorrect or stale report: $Name" }
    if ($ShouldPass -and $Mode -and ($record.mode -ne $Mode -or -not $record.simulatedChecksOnly)) { throw "Incorrect preview mode report: $Name" }
}
Invoke-Check $true 'valid-fixture'
[IO.File]::AppendAllText((Join-Path $plugin 'gojobracken'), 'tampered')
Invoke-Check $false 'tampered-bundle'
[IO.File]::WriteAllText((Join-Path $plugin 'gojobracken'), 'Synthetic bundle payload')
$duplicate = Join-Path $profile 'BepInEx\plugins\Duplicate'
New-Item -ItemType Directory -Force $duplicate | Out-Null
Copy-Item -LiteralPath (Join-Path $plugin 'GojoBracken.dll') -Destination $duplicate
Invoke-Check $false 'duplicate-plugin'
Remove-Item -LiteralPath (Join-Path $duplicate 'GojoBracken.dll')
$expected.gameMvid = [Guid]::NewGuid().ToString('D')
[IO.File]::WriteAllText($manifest, ($expected | ConvertTo-Json))
Invoke-Check $false 'unsupported-game'
$expected.gameMvid = $mvid
[IO.File]::WriteAllText($manifest, ($expected | ConvertTo-Json))
$previewBundle = Join-Path $plugin 'gojobracken-hdrp-material-check'
Move-Item -LiteralPath (Join-Path $plugin 'gojobracken') -Destination $previewBundle
$config = Join-Path $profile 'BepInEx\config\FreakyFriday.GojoBracken.cfg'
$previewConfig = Get-Content -LiteralPath (Join-Path $repo 'packaging/preview/FreakyFriday.GojoBracken.cfg') -Raw
[IO.File]::WriteAllText($config, $previewConfig)
Invoke-Check $true 'valid-preview' 'RenderPreview'
Invoke-Check $false 'preview-config-in-live-mode'
[IO.File]::AppendAllText($previewBundle, 'tampered')
Invoke-Check $false 'tampered-preview' 'RenderPreview'
[IO.File]::WriteAllText($previewBundle, 'Synthetic bundle payload')
Move-Item -LiteralPath $previewBundle -Destination (Join-Path $plugin 'gojobracken')
Invoke-Check $false 'preview-requires-preview-filename' 'RenderPreview'
Move-Item -LiteralPath (Join-Path $plugin 'gojobracken') -Destination $previewBundle
[IO.File]::WriteAllText($config, $previewConfig.Replace('Enabled = false', 'Enabled = true'))
Invoke-Check $false 'preview-rejects-live-effects' 'RenderPreview'
[IO.File]::WriteAllText($config, $previewConfig.Replace('SoloLiveTest = false', 'SoloLiveTest = true'))
Invoke-Check $false 'preview-rejects-live-test' 'RenderPreview'
[IO.File]::WriteAllText($config, $previewConfig.Replace('Enabled = true', 'Enabled = false'))
Invoke-Check $false 'preview-requires-enabled' 'RenderPreview'
[IO.File]::WriteAllText($config, $previewConfig)
Copy-Item -LiteralPath (Join-Path $plugin 'GojoBracken.dll') -Destination $duplicate
Invoke-Check $false 'preview-rejects-duplicate-dll' 'RenderPreview'
Write-Output "GAME PROFILE CHECKS PASS: live/preview fixtures, mode isolation, tampering, duplicate DLLs, unsupported MVID and stale-report rejection. Synthetic files only; no game run. Logs: $root"
