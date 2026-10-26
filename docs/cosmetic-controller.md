# Per-instance effects checkpoint

BrackenCosmeticController creates and owns an aura clone and a spatial AudioSource beneath a supplied visual anchor. It consumes BrackenObservation snapshots through Apply and uses the same v81 presentation rules as the Unity study. The plugin now has a default-off [game adapter](development-effects.md) for this component. No model replacement or adapted animations are included.

TryCreate requires the observed game assembly MVID, an aura prefab, a nonempty voice clip, an anger saturation value, and strength from 0 to 3. The future adapter must pass assets from the validated bundle and an upright visual anchor, supply actual observations, and select detailed anger only for trusted local ownership. It must not pass the Bracken animationContainer rotation directly during corpse carrying. Duplicate controllers on the same anchor are rejected.

Before cloning, the controller rejects missing components and anything except transforms, particle systems/renderers, mesh renderers, and mesh filters. This excludes prefab scripts, physics, animation events, and automatic prefab audio. The clone's particles do not play before the first observation. This narrow contract supports a native mesh flame envelope plus particle wisps; the current authoring-only script-generated envelope still needs to be baked into the release prefab.

The aura prefab should be authored at full intensity. Rate-over-time and rate-over-distance multipliers scale from its original curves, without modifying the source prefab. Burst probabilities scale down toward the faint baseline and stay bounded at full probability; strength above 1 boosts continuous emission without multiplying burst counts. Originally inactive children and disabled renderers remain inactive/disabled. Materials supporting _Strength and _Intensity receive the normalized flame shape and strength controls. At strength zero, particles are cleared and aura renderers hidden; the voice remains independent.

Each instance has its own voice source: spatial blend 1, no loop, no play-on-awake, no Doppler, logarithmic attenuation from 2 to 25 units. An aggro-entry event stops/restarts that source once; remaining in aggro does not retrigger it. VoiceRequests counts requests, not audible playback. Corpse and kill overlays use the shared suppression rules. Remote approximation and anger saturation never write game fields.

Death, component disable, destruction, or explicit Shutdown stops audio, clears particles, deactivates owned effects immediately, and schedules their destruction. Shutdown is terminal and idempotent. Re-enabling the same component does not restart it; a future pooling adapter must remove the stopped component before creating a fresh session. Destroy cosmetic instances before disposing their bundle. The controller does not hide vanilla renderers, move the host, modify colliders, or call AI/network methods.

From the repository root in Windows PowerShell 5.1:

```powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-CosmeticController.ps1
```

The isolated Unity 2022.3.62f2 project compiles the production controller and shared rules source and enters Play mode. Synthetic particles and silence exercise aura scaling, voice requests and spatial configuration, independent instances, guards, death/disable/despawn/scene cleanup, and unchanged host rendering, physics, and transform. This does not verify audible Japanese audio, final flame rendering, actual Bracken attachment, or multiplayer. No game launch or encounter is needed.
