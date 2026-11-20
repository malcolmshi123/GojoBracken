# Game compatibility

## Supported runtime

GojoBracken targets the inspected Lethal Company v81 assembly and Unity 2022.3.62f2 with HDRP. Mono.Cecil read the assembly without executing it and recorded:

- Assembly-CSharp MVID: `aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b`
- Assembly-CSharp SHA256: `5F7DB5538B78DC408845A3002907619785AC9F9C6B6059D13DC9A602D9B65731`

Runtime guards reject unrecognized assemblies. Check the assembly identity even when the game version label matches. Keep game assemblies, extracted game assets and local inspection reports out of Git. The local IL evidence is in `artifacts/v81-flowerman-il.txt`.

## State contract

`BrackenV81Presentation` reads observations and returns cosmetic events and effect levels. It leaves AI, networking, colliders, the original Animator and corpse handling unchanged.

| Observation | Presentation | Inspected FlowermanAI evidence |
| --- | --- | --- |
| Raw state 0 | Hunting / sneaking | Update dispatches to IL_054a; DoAIInterval chooses a nearby node |
| Raw state 1 | Retreat / evade | Update dispatches to IL_0234; DoAIInterval avoids a player or carries a body toward favoriteSpot |
| Raw state 2 | Aggro | Update dispatches to IL_0640; DoAIInterval moves toward the target player |
| carryingPlayerBody | Corpse-carry overlay | FinishKillAnimation attaches the body and sets the flag, then selects state 1 |
| inKillAnimation | Kill overlay | Update returns before normal phase processing |
| isEnemyDead | Death | Update returns before normal phase processing |

The local `isInAngerMode` flag can lag behind a state transition, so voice events use entry into raw state 2. Kill/corpse overlays suppress that entry; ending an overlay cannot trigger another voice event. Death sets aura to zero and signals the effects to stop. Reset clears each instance's state.

`EnterAngerModeServerRpc` passes an anger value to the client RPC, which sets `angerMeter` and state 2. Clients then count down locally; the value is not continuously replicated through a NetworkVariable. The presentation uses detailed anger when trusted and estimates it from the phase otherwise. Ownership, latency, stun and hit effects still need runtime checks before networking behavior can be confirmed.

The inspected game writes set no fixed anger maximum. Stun paths assign values including 12, 2 and 6. HitEnemy assigns 11 before adding anger. The aura reaches full strength at the default threshold of 12 without changing game anger. In living phases, smoothed aura stays between 0.15 and 1 before the configured strength multiplier.

## Transform and animation constraints

The game uses anger/sneak booleans, speedMultiplier, VelocityX/VelocityZ and killing/carryingBody animation parameters. CalculateAnimationDirection derives VelocityZ from negative local Y in animationContainer space. The container's local Z rotation approaches 179 degrees while carrying. Copying these transforms or parameters into a conventional Gojo rig requires coordinate conversion.

Gojo follows the enemy root in a fixed standing pose with a facing correction. The replacement keeps the original gameplay animation and body grip. It has no movement/attack retargeting or calibrated corpse grip. The approved ship simulation does not validate live AI, cleanup, player-kill/body alignment or multiplayer behavior. See the [testing scope](in-game-testing.md).

## Checks and diagnostics

From the checkout root in Git Bash:

```bash
dotnet build GojoBracken.sln -c Release
dotnet run --project tests/StateReaderChecks/StateReaderChecks.csproj -c Release
```

Offline checks cover phase mapping, stale flags, repeated aggro entries, overlays, smoothing, invalid anger inputs, remote approximation, death/reset, independent instances, schema drift and config guards. Unity launchers under `scripts/tests/` check bundle loading, particles, cosmetic controllers, attachment/visibility, observer lifecycle and rig reports in isolated ignored projects. They do not launch the game or establish multiplayer parity.

Run the [profile preflight](in-game-testing.md) before launching an installed test profile. To inspect a different installation without executing its assemblies, set the actual paths and run:

```bash
GAME='/d/SteamLibrary/steamapps/common/Lethal Company'
PROFILE='/c/replace/with/Thunderstore/profiles/test'
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/diagnostics/Get-GameCompatibility.ps1 \
  -GameDirectory "$(cygpath -w "$GAME")" \
  -BepInExCoreDirectory "$(cygpath -w "$PROFILE/BepInEx/core")"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/diagnostics/Get-RenderingCompatibility.ps1 \
  -GameDirectory "$(cygpath -w "$GAME")" \
  -OutputPath "$(cygpath -w "$PWD/artifacts/rendering-compatibility.json")"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/diagnostics/Get-OfflineGameMetadata.ps1 \
  -ManagedDirectory "$(cygpath -w "$GAME/Lethal Company_Data/Managed")" \
  -CecilPath "$(cygpath -w "$PROFILE/BepInEx/core/Mono.Cecil.dll")"
```

The rendering collector reads existing files and logs. The metadata collector uses Mono.Cecil. Keep the reports with the game's displayed version and Player.log. For a rig report, enable `Diagnostics.ExportBrackenRig` and check `BepInEx/config/GojoBracken` or the timeout in LogOutput.log. The diagnostic can use an already-loaded prefab and never spawns an enemy.
