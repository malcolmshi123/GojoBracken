# Observer investigation and offline inspection

## Testing-PC result: 0.1.2

The returned log establishes plugin startup, diagnostics construction, discovery of FlowermanAI, and construction of the rig reporter. It contains no render-pipeline line, scan result, completion, or report file.

The game assembly MVID logged there is aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b. Application.version is 0.1; the separately reported main-menu game version is 81.

The cause on that PC remains unconfirmed. Both missing outputs depended on Plugin.Update. The reporter tests called its Update directly and did not cover delivery through the BepInEx/Unity plugin lifecycle. That was a verification gap, not proof of a particular game-side failure. Do not attribute the result to other mods or the unrelated render-texture messages without evidence.

The testing-PC handoff is paused. Keep both diagnostics options false; no further launch or enemy encounter is requested.

## Development change: 0.1.3

- Opt-in observations start through a coroutine in Awake. Its first sample runs immediately rather than waiting for Plugin.Update.
- Scheduling uses Stopwatch and WaitForSecondsRealtime; neither scaled game time nor the old unscaled-time polling gate drives the observer.
- Startup logs the two option values and enabled/active state, then marks entry into the first sample.
- The rig reporter logs the first scan and periodic progress, plus completion, timeout, failure, or cancellation.
- Disable, destruction, and application quit stop the coroutine and identify why a pending search ended.
- Runtime diagnostics and the rig reporter have separate exception handling.
- With both options false, no observation coroutine starts.

This improves scheduling and visibility. It does not certify that the testing-PC cause has been fixed, that its prefab is loaded, or that rendering/gameplay integration is compatible.

## Local verification

Release compilation passes with zero warnings/errors against the laptop's archived references.

The original synthetic prefab checks still pass, with added first-scan progress and cancellation checks. A new Play-mode test loads the compiled Release DLL with BepInEx dependencies, using a synthetic FlowermanAI prefab and a BepInEx-style hidden manager. It verifies native Awake/coroutine delivery at Time.timeScale = 0, a pipeline line, successful prefab export, no spawned enemy, logged disable, and no observer when options are false. It does not load the game assembly.

Reproduce on the development laptop, using the installed/licensed Unity 2022.3.62f2 and Windows PowerShell 5.1:

~~~powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-RigReport.ps1
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-ObserverLifecycle.ps1
~~~

The lifecycle runner reads the literal BepInExCoreDir in local.props, or accepts -BepInExCoreDirectory explicitly. It copies the plugin and BepInEx support DLLs into a fresh ignored test project; no game DLLs or assets are used. Generated projects, logs, and fixture reports stay under artifacts.

## Prepared alternate route: file-only metadata

scripts/Get-OfflineGameMetadata.ps1 reads Assembly-CSharp.dll with Mono.Cecil from an existing BepInEx core directory. The game stays closed. The target assembly is not loaded or executed by the CLR.

It exports selected type/field/method signatures, method call references, written fields, string literals, SHA256, and MVID into an ignored JSON report. Installation paths and game binary contents are not included. It does not export prefab bones or media, discover the active render pipeline, or prove phase meanings/anger replication.

The script passed on Windows PowerShell 5.1 using the archived reference assembly (MVID af9b1eec-498a-45ae-bd42-601d6ab85015). That is a different assembly from the testing PC and cannot establish v81 behavior.

When a file-only inspection of the current installation is authorized, the prepared command is:

~~~powershell
$testProfile = Join-Path $env:APPDATA 'Thunderstore Mod Manager\DataFolder\LethalCompany\profiles\GeeGianni'
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Get-OfflineGameMetadata.ps1 -ManagedDirectory 'E:\SteamLibrary\steamapps\common\Lethal Company\Lethal Company_Data\Managed' -CecilPath (Join-Path $testProfile 'BepInEx\core\Mono.Cecil.dll')
~~~

No testing-PC action is needed for the current handoff. If inspected later, compare the report MVID with the logged current-game MVID before drawing conclusions. Do not commit game DLLs or generated inspection reports.
