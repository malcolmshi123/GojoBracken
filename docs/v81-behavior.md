# Inspected v81 behavior and cosmetic rules

The supplied Assembly-CSharp.dll was read as metadata/IL with Mono.Cecil, never loaded or executed as a game assembly. Both identity checks match the earlier offline report:

- MVID: aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b
- SHA256: 5F7DB5538B78DC408845A3002907619785AC9F9C6B6059D13DC9A602D9B65731

The rules below are specific to this inspected assembly. IL evidence stays in ignored artifacts/v81-flowerman-il.txt; the game DLL is not added to Git.

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

isInAngerMode is a locally updated presentation flag. It can lag the raw index around state transitions; the prepared voice-edge logic uses the verified index rather than this flag.

## Anger and networking

EnterAngerModeServerRpc forwards its Single argument to EnterAngerModeClientRpc (IL_0127-0129). In the receiving client body, IL_00e1-00ea assigns that argument to angerMeter and selects state 2.

The aggro branch decrements angerMeter by deltaTime outside the owner-only exit check (Update IL_0818-0841). Thus non-owner clients have an event-initialized local countdown. This is not the same as an authoritative continuously replicated NetworkVariable. Ownership, latency, stun/hit changes, and runtime behavior still need controlled integration validation.

No fixed global upper clamp on angerMeter was found in the inspected FlowermanAI writes:

- AddToAngerMeter adds its argument and can add timesThreatened / 1.75.
- The 0.99 clamp bounds the probability calculation, not angerMeter.
- Stun-related paths assign 12 or 2 before aggro, and 6 while aggro.
- HitEnemy's owner path assigns 11, then calls AddToAngerMeter(0.1).
- The aggro branch subtracts deltaTime.

The presentation default of 12 is therefore a tunable saturation threshold chosen from an observed strong-anger assignment, not a proven game maximum. Values above it saturate the visual only; no game value is altered.

## Animation and transform observations

Verified setter arguments include anger/sneak booleans, speedMultiplier, VelocityX/VelocityZ floats, and killing/carryingBody booleans.

CalculateAnimationDirection derives velocity in animationContainer space. Its VelocityZ calculation uses negative local Y (IL_008e-0099), so copying parameters into a conventional Y-up Gojo rig without a coordinate conversion would be unsafe.

During corpse carrying, Update blends animationContainer.localEulerAngles.z toward 179 degrees (IL_08b2-0919), returning toward zero otherwise. The visual replacement must account for that authored frame rather than blindly inheriting the container's rotation. Keep the original animator, body grip, colliders, and gameplay transform behavior intact.

The DLL does not contain the loaded prefab's bone hierarchy or its actual rendering configuration. Those and Gojo animation adaptation remain pending; this inspection does not complete a playable replacement.

## Prepared cosmetic state machine

BrackenV81Presentation consumes the read-only BrackenObservation. It is not attached to game enemies yet and does not load the model, aura, or voice clip.

- Construction rejects an uninspected game MVID.
- It resolves the verified indices plus death, kill, and corpse overlays.
- Local/detailed anger is normalized to a configurable constructor saturation value. Aura smoothly approaches a bounded 0.15-1 level in every living phase.
- When detailed anger is not trusted, the caller selects an explicit phase approximation: faint outside raw state 2, strongest in raw state 2. Initial integration should select this conservative path for non-owner/unknown ownership.
- Voice emits one event on the raw non-2 to 2 transition. Kill/corpse overlays suppress that event and cannot create another event merely by ending.
- Death immediately returns zero aura with a stop-effects signal. Reset clears per-instance state for cleanup.
- A prepared [Unity effects controller](cosmetic-controller.md) now accepts strength/saturation parameters and applies these rules to particles and spatial audio. Bundle validation is also prepared. BepInEx configuration, actual game attachment, and final assets remain later work.

Checks:

~~~powershell
dotnet run --project tests/StateReaderChecks/StateReaderChecks.csproj -c Release
dotnet build GojoBracken.sln -c Release --no-restore
~~~

Passed: phase mapping, stale anger-mode flags, repeated aggro entries, kill/corpse overlays, smoothing, high/negative/nonfinite inputs, bounded aura, remote approximation, death/reset, independent instances, schema drift, and version/config guards. Plugin build: zero warnings/errors. These checks are offline presentation/adapter checks, not in-game or multiplayer validation. No further testing-PC launch or encounter is requested.
