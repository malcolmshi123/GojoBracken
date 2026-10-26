# Opt-in game-state binding

Version 0.1.4 connects the prepared bundle loader and per-instance effects controller behind DevelopmentEffects.Enabled, which defaults to false. This mode adds local aura/audio only. It deliberately retains the vanilla Bracken model: Gojo rig adaptation and model replacement are still pending. The current authoring study is not an installable release bundle.

No testing-PC launch or Bracken encounter is requested for this checkpoint. The following settings document the future integration path, after the actual assets and materials are ready. Leave the feature disabled for now:

```ini
[DevelopmentEffects]
Enabled = false
AngerSaturation = 12
AuraStrength = 1
```

These settings require a restart. Saturation must be positive/finite and is a cosmetic threshold, not a game limit. Strength ranges from 0 to 3; zero hides aura while leaving voice events independent. Enabling requires the inspected v81 FlowermanAI module MVID and the validated gojobracken bundle beside the plugin DLL, including model, aura, voice, and embedded credits. The model is validated but not instantiated in this mode.

The plugin's existing real-time coroutine scans for active scene Brackens every 0.5 seconds, without instantiating enemies or searching prefab assets. Each bound instance then reads the six verified fields in LateUpdate, after the enemy's Update. The anchor is the enemy root, not its rolling animationContainer; final visual height and placement still need calibration.

IsOwner=true selects detailed local anger. IsOwner=false selects faint-outside-aggro/strong-in-aggro approximation. An unavailable or throwing getter also selects approximation and logs once per instance. Ownership is checked each sample, so transfers change which input is used. This is a conservative presentation policy, not proof of network authority or synchronized anger.

Already-dead or disabled enemies are skipped. Read failures stop that binding and warn once. Death, disable, despawn, scene cleanup, plugin disable, and startup/scan failure stop owned effects. The session attempts each existing instance once to avoid repeated errors or attachment loops; reactivating a pooled instance does not start a new cosmetic session in this increment. Destroyed instance records are removed on later scans. A future pooling adapter must explicitly create a fresh session if required by the game.

When enabled, startup logs either a disabled reason or the development-mode notice. Successful attachments log their local instance ID. No patches, AI calls, enemy field writes, renderer hiding, or network messages are added. A missing bundle or rejected version leaves vanilla visuals visible. With all development/diagnostic options disabled, no observation coroutine or enemy scan starts.

Validation commands from the repository root in Windows PowerShell 5.1:

```powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-CosmeticController.ps1
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-ObserverLifecycle.ps1
```

The first Unity check compiles production binding/controller source and uses synthetic field-bearing components to test owner/non-owner/unknown observations, voice edges, overlays, read failure, death, disable, and unchanged enemy fields. Only those isolated binding fixtures supply the known MVID directly; production session startup reads the actual module identity. The compiled plugin lifecycle check verifies default-off startup and rejection of an uninspected module without starting scans. Earlier bundle tests separately validate the exporter and loader.

These checks do not establish successful scene discovery/attachment against the actual v81 game, final material rendering, audible Japanese audio, multiplayer compatibility, or pooling behavior. Those remain controlled integration work once the assets and rig are ready.
