# Testing PC: Bracken rig report

**Handoff paused after the inconclusive 0.1.2 run. No further game launch or Bracken encounter is requested. Keep both diagnostics options false.** See [the observer investigation and prepared file-only route](observer-investigation.md). The launch instructions below describe the earlier attempt and are not a request to repeat it.


GojoBracken 0.1.2 adds an optional report of an already-loaded Bracken prefab. It excludes scene instances and never spawns an enemy, plays an animation, alters a controller, patches methods, or adds network messages. No moon landing or Bracken encounter is needed for this attempt.

Review, commit, and push the development-laptop changes before pulling on the testing PC. Staging, commits, and pushes remain manual.

## Build and install

Close Lethal Company. In Windows PowerShell 5.1:

~~~powershell
Set-Location 'C:\Users\Malcolm Shi\repos\GojoBracken'
git pull --ff-only
dotnet build GojoBracken.sln -c Release
~~~

Use the existing local.props configured for this PC's actual game and BepInEx profile. GameManagedDir must include UnityEngine.AnimationModule.dll alongside the existing Unity references. A failed build is not a usable new plugin.

After a successful build:

~~~powershell
$testProfile = Join-Path $env:APPDATA 'Thunderstore Mod Manager\DataFolder\LethalCompany\profiles\GeeGianni'
$pluginFolder = Join-Path $testProfile 'BepInEx\plugins\GojoBracken'
New-Item -ItemType Directory -Force $pluginFolder | Out-Null
Copy-Item -LiteralPath '.\src\GojoBracken\bin\Release\netstandard2.1\GojoBracken.dll' -Destination (Join-Path $pluginFolder 'GojoBracken.dll') -Force
~~~

Ensure this profile has only one copy of GojoBracken. No Unity Editor installation is required on this PC.

## Collect the report

1. Launch once through this test profile and exit, so the plugin adds its new config entry.
2. Open BepInEx/config/FreakyFriday.GojoBracken.cfg in that profile. Under the existing Diagnostics section, set:

~~~ini
[Diagnostics]
Enabled = true
ExportBrackenRig = true
~~~

3. Launch through the same profile and stay at the main menu for up to two minutes. The existing diagnostics also collect the active render-pipeline line.
4. Exit. Look inside BepInEx/config/GojoBracken for the newest bracken-rig-*.txt file. Each export has a unique filename and UTC timestamp.
5. Return that file plus log lines containing [RigReport] and the [Diagnostics] render-pipeline line from BepInEx/LogOutput.log.
6. Set both options back to false and restart when finished.

If the log says no loaded Bracken prefab was found, return that message and the pipeline line. No report was produced in that run. Do not search for a Bracken or enter a facility to make the test succeed; we can use another inspection route. The exporter checks every five seconds, at most 24 times, then stops.

## What the report establishes

It records prefab-local transforms, indexed bone paths, skinned mesh vertex counts, material/shader names, controller and clip metadata, authored behavior-state labels, Unity/application versions, game assembly MVID, and the declared anger field type.

These are prefab defaults, not observed phase transitions, anger bounds, animation poses, or proof of multiplayer replication. Uninitialized prefab Animators cannot reliably expose parameters; the report marks them unavailable instead of rebinding or playing the controller. Mesh geometry, textures, and audio data are not exported.

The report will guide rig mapping and rendering dependencies. Movement/attack/kill/corpse animation adaptation and actual in-game replacement remain unimplemented.

## Development-laptop verification

The plugin builds with zero warnings/errors against the laptop's archived references; this does not validate game v81.

The reporter passed native Unity 2022.3.62f2 fixture checks: scene-instance exclusion, bounded missing-prefab timeout, loaded-prefab export, state/bone/clip data, explicit unavailable parameters, one report per run, unchanged prefab data, missing component type, and I/O failure handling. Reproduce locally using Windows PowerShell 5.1:

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-RigReport.ps1
~~~

This uses the installed/licensed pinned Editor in a fresh project under ignored artifacts/rig-report-tests. It needs no game, model download, or enemy encounter. Fixture sources live in scripts/tests. The real game report is still pending the testing-PC run.
