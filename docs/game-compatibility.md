# Game compatibility

## Supported runtime

GojoBracken targets the inspected Lethal Company v81 assembly and Unity 2022.3.62f2 with HDRP. The read-only Mono.Cecil inspection recorded:

- Assembly-CSharp MVID: `aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b`
- Assembly-CSharp SHA256: `5F7DB5538B78DC408845A3002907619785AC9F9C6B6059D13DC9A602D9B65731`

Runtime guards reject an unrecognized assembly. A game version label alone does not establish compatibility. Game assemblies, extracted game assets and local inspection reports stay out of Git. The IL evidence is retained locally in `artifacts/v81-flowerman-il.txt`.

## State contract

`BrackenV81Presentation` consumes read-only observations and produces cosmetic intent. It does not change AI, networking, colliders, the original Animator or corpse handling.

| Observation | Presentation | Inspected FlowermanAI evidence |
| --- | --- | --- |
| Raw state 0 | Hunting / sneaking | Update dispatches to IL_054a; DoAIInterval chooses a nearby node |
| Raw state 1 | Retreat / evade | Update dispatches to IL_0234; DoAIInterval avoids a player or carries a body toward favoriteSpot |
| Raw state 2 | Aggro | Update dispatches to IL_0640; DoAIInterval moves toward the target player |
| carryingPlayerBody | Corpse-carry overlay | FinishKillAnimation attaches the body and sets the flag, then selects state 1 |
| inKillAnimation | Kill overlay | Update returns before normal phase processing |
| isEnemyDead | Death | Update returns before normal phase processing |

The local `isInAngerMode` flag can lag a transition. Voice events therefore use entry into raw state 2. Kill/corpse overlays suppress that entry, and ending an overlay cannot create another voice event. Death produces zero aura and a stop-effects signal; reset clears per-instance state.

`EnterAngerModeServerRpc` forwards an anger value to the client RPC, which assigns `angerMeter` and state 2. Clients count down locally; this is not a continuously replicated NetworkVariable. The presentation uses detailed anger when trusted and a conservative phase approximation otherwise. Ownership, latency, stun and hit effects require runtime evidence for networking claims.

The inspected game writes do not impose a fixed anger maximum. Stun paths assign values including 12, 2 and 6; HitEnemy assigns 11 before adding anger. The cosmetic default saturates at 12 while leaving the game's anger unchanged. Smoothed aura remains bounded between 0.15 and 1 in living phases before the configured strength multiplier.

## Transform and animation constraints

The game uses anger/sneak booleans, speedMultiplier, VelocityX/VelocityZ and killing/carryingBody animation parameters. CalculateAnimationDirection derives VelocityZ from negative local Y in animationContainer space. During carrying, the container's local Z rotation approaches 179 degrees. Copying these transforms or parameters directly into a conventional Gojo rig would require coordinate conversion.

The current replacement instead follows the enemy root with a fixed standing pose and a facing correction. It preserves the original gameplay animation and body grip. It does not implement movement/attack retargeting or a calibrated corpse grip. The approved in-ship simulation cannot establish live AI, cleanup, player-kill/body alignment or multiplayer behavior; see [testing scope](in-game-testing.md).

## Checks and diagnostics

From the checkout root in Git Bash:

```bash
dotnet build GojoBracken.sln -c Release
dotnet run --project tests/StateReaderChecks/StateReaderChecks.csproj -c Release
```

Offline checks cover phase mapping, stale flags, repeated aggro entries, overlays, smoothing, invalid anger inputs, remote approximation, death/reset, independent instances, schema drift and configuration guards. Unity launchers under `scripts/tests/` cover bundle loading, particles, cosmetic controllers, attachment/visibility, observer lifecycle and rig reports in isolated ignored projects. These checks do not launch the game or prove multiplayer parity.

For an installed testing profile, use the [profile preflight](in-game-testing.md) before launching. To inspect a different installation without executing its assemblies, set its actual paths and run:

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

The rendering collector reads existing files and logs; the metadata collector uses Mono.Cecil. Retain reports alongside the game's displayed version and Player.log. For a rig report, enable `Diagnostics.ExportBrackenRig` and inspect `BepInEx/config/GojoBracken` or the timeout in LogOutput.log. An already-loaded prefab is sufficient; this diagnostic never spawns an enemy.
