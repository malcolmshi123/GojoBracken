# Inspected v81 behavior and cosmetic rules

We inspected the supplied Assembly-CSharp.dll with Mono.Cecil. The inspection read metadata and IL without loading or executing the game assembly. Both values match the earlier offline report:

- MVID: aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b
- SHA256: 5F7DB5538B78DC408845A3002907619785AC9F9C6B6059D13DC9A602D9B65731

These rules apply to that assembly. The IL evidence is in artifacts/v81-flowerman-il.txt, which Git ignores. Keep the game DLL out of Git.

## State mapping

| Raw index / flag | Interpretation | Evidence in FlowermanAI |
| --- | --- | --- |
| 0 | Hunting / sneaking | Update switch at IL_021e targets IL_054a; DoAIInterval routes non-1/non-2 behavior to ChooseClosestNodeToPlayer |
| 1 | Retreat / evade | Update targets IL_0234; DoAIInterval calls AvoidClosestPlayer or routes a carried body toward favoriteSpot |
| 2 | Aggro | Update targets IL_0640, enables anger and disables sneak; DoAIInterval IL_002d-0041 moves toward the target player |
| carryingPlayerBody | Corpse-carrying overlay, usually state 1 | FinishKillAnimation IL_006f-00bc attaches the body and sets the flag; IL_01c0-01d1 selects state 1 |
| inKillAnimation | Kill-animation overlay | Update IL_000f-0017 returns before normal phase processing |
| isEnemyDead | Death | Update IL_0006-000e returns before normal phase processing |

AddToAngerMeter IL_0106-0114 enters state 2 and sends the current anger through EnterAngerModeServerRpc. Retreat's timeout returns to state 0 (Update IL_0380-0397). Aggro's owner returns to state 1 after anger expires (IL_082a-0841).

The game updates isInAngerMode locally, so it can lag the raw index during a transition. The voice logic uses the verified index.

## Anger and networking

EnterAngerModeServerRpc forwards its Single argument to EnterAngerModeClientRpc (IL_0127-0129). In the receiving client body, IL_00e1-00ea assigns that argument to angerMeter and selects state 2.

The aggro branch subtracts deltaTime from angerMeter outside the owner-only exit check (Update IL_0818-0841). Non-owner clients count down from the value set by the event. The game does not send a continuous authoritative anger value through a NetworkVariable. Ownership, latency, stun and hit changes, and runtime behavior still need controlled integration tests.

The inspected FlowermanAI writes show no fixed upper limit on angerMeter:

- AddToAngerMeter adds its argument and can add timesThreatened / 1.75.
- The 0.99 clamp bounds the probability calculation, not angerMeter.
- Stun-related paths assign 12 or 2 before aggro, and 6 while aggro.
- HitEnemy's owner path assigns 11, then calls AddToAngerMeter(0.1).
- The aggro branch subtracts deltaTime.

The presentation uses 12 as the default saturation point because the game assigns that value in one strong-anger path. You can tune the threshold. Above it, the visual effect stays at full strength and the game keeps its original anger value.

## Animation and transform observations

Verified setter arguments include anger/sneak booleans, speedMultiplier, VelocityX/VelocityZ floats, and killing/carryingBody booleans.

CalculateAnimationDirection derives velocity in animationContainer space. Its VelocityZ calculation uses negative local Y (IL_008e-0099), so copying parameters into a conventional Y-up Gojo rig without a coordinate conversion would be unsafe.

During corpse carrying, Update blends animationContainer.localEulerAngles.z toward 179 degrees (IL_08b2-0919), returning toward zero otherwise. The visual replacement must account for that authored frame rather than blindly inheriting the container's rotation. Keep the original animator, body grip, colliders, and gameplay transform behavior intact.

The DLL does not contain the loaded prefab's bone hierarchy or its actual rendering configuration. Those and Gojo animation adaptation remain pending; this inspection does not complete a playable replacement.

## Prepared cosmetic state machine

BrackenV81Presentation consumes the read-only BrackenObservation. The default-off [development adapter](runtime-behavior.md) now connects it to scene enemies and validated aura/audio assets. Actual game validation is pending; the presentation class itself does not load assets or replace models.

- Construction rejects an uninspected game MVID.
- It resolves the verified indices plus death, kill, and corpse overlays.
- Local/detailed anger is normalized to a configurable constructor saturation value. Aura smoothly approaches a bounded 0.15-1 level in every living phase.
- When detailed anger is not trusted, the caller selects an explicit phase approximation: faint outside raw state 2, strongest in raw state 2. Initial integration should select this conservative path for non-owner/unknown ownership.
- Voice emits one event on the raw non-2 to 2 transition. Kill/corpse overlays suppress that event and cannot create another event merely by ending.
- Death immediately returns zero aura with a stop-effects signal. Reset clears per-instance state for cleanup.
- The [Unity effects controller](runtime-behavior.md) accepts strength/saturation parameters and applies these rules to particles and spatial audio. Bundle validation, BepInEx configuration, and an opt-in attachment path are implemented. Final assets and game/multiplayer validation remain later work.

Checks:

~~~powershell
dotnet run --project tests/StateReaderChecks/StateReaderChecks.csproj -c Release
dotnet build GojoBracken.sln -c Release --no-restore
~~~

Passed: phase mapping, stale anger-mode flags, repeated aggro entries, kill/corpse overlays, smoothing, high/negative/nonfinite inputs, bounded aura, remote approximation, death/reset, independent instances, schema drift, and version/config guards. Plugin build: zero warnings/errors. These checks are offline presentation/adapter checks, not in-game or multiplayer validation. No further testing-PC launch or encounter is requested.

## Collect compatibility evidence

Before choosing a Unity Editor version, run the read-only compatibility script on the PC with Lethal Company installed. It reports file versions and SHA256 hashes without loading game assemblies or changing the game. It does not include your installation paths in the JSON.

Run the command from the repository root in Windows PowerShell 5.1. Use your game installation path and, if you have one, the BepInEx core path for your separate mod-manager test profile:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned `
    -File .\scripts\diagnostics\Get-GameCompatibility.ps1 `
    -GameDirectory 'C:\Games\Steam\steamapps\common\Lethal Company' `
    -BepInExCoreDirectory 'C:\ModProfiles\GojoTest\BepInEx\core' |
    Set-Content -Encoding UTF8 .\artifacts\compatibility.json
```

For a manual BepInEx installation inside the game directory, omit BepInExCoreDirectory. The generated artifacts directory is ignored by Git.

RemoteSigned applies only to the child PowerShell process above; it does not change machine-wide or user-wide settings. It permits local unsigned scripts while requiring signatures for internet-marked scripts. Review the script before running it. Managed execution policies still take precedence; if one blocks the script, share the error rather than overriding it. See [Microsoft execution-policy documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies?view=powershell-5.1).

Bring back compatibility.json and the version number displayed in the game's main menu. If unityVersionFromPlayer is null, also provide the Unity version line from the game's Player.log. UnityPlayer file-version metadata is preserved in the report for inspection.

The rendering collector reads installed files and existing logs. The metadata collector reads the assembly through Mono.Cecil. Set the variables to the actual game directory, output JSON, Managed directory, and BepInEx Mono.Cecil DLL.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnostics\Get-RenderingCompatibility.ps1 -GameDirectory $game -OutputPath $output
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnostics\Get-OfflineGameMetadata.ps1 -ManagedDirectory $managed -CecilPath $cecil
~~~

For a rig report, enable Diagnostics.ExportBrackenRig and inspect BepInEx/config/GojoBracken or the timeout in LogOutput.log. An already-loaded prefab is sufficient.

## Regression checks

Run the six launchers under scripts/tests in Windows PowerShell 5.1. They check bundle loading, the native aura, connected cosmetics, attachment and visibility, observer lifecycle, and rig reports. Pass BepInExCoreDirectory to Test-ObserverLifecycle.ps1 if needed. The launchers create isolated Unity projects under artifacts, which Git ignores.
