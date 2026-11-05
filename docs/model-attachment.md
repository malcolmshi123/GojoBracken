# Standing-model attachment checkpoint

Version 0.1.5 introduced reversible model attachment behind DevelopmentEffects.StandingModelPreview. Version 0.1.33 uses a fixed standing pose for every live phase. It defaults to false and also requires DevelopmentEffects.Enabled. Aura-only mode retains the vanilla model. No game launch or encounter is requested by these local checks.

The optional mode validates and clones the bundle's Gojo prefab before changing vanilla rendering. The clone can contain native transforms, renderers, mesh filters, and Animators only. Skins need valid internal bones/bind poses, materials/shaders must be supported, and animation controllers need clips without animation events. At least one renderer must be drawable; a disabled replacement root triggers rollback. Original collider, Animator, bone, renderer-array, and gameplay references are retained.

Only renderer references from the game's cached skinnedMeshRenderers/meshRenderers arrays receive forceRenderingOff masks. No descendant search selects vanilla objects, so a carried corpse is excluded. Original renderer.enabled and GameObject.layer remain controlled by the game. The visibility reader ignores our own mask by consulting the captured pre-attachment values, while still reading enabled, active state, and layers live. The Gojo renderers and aura follow that visibility/layer result.

The native visual sits beneath a separate child of the enemy root, with its cloned Animators disabled. Gojo holds the baked pose while following the enemy root. Facing is aligned from the toe direction when available. Aura and voice still follow the live phase and anger rules. Both regular cosmetics and the solo live test use the 3.000 m trial size with Gojo replacement enabled. Wisps and flame width/depth use 1.25x scale, and flame height uses 1.75x. The model follows the Bracken root, whose scale can affect the final world height. Aura-only mode uses the authored size.

Invalid assets are rejected before vanilla renderers are masked. Later read/renderer failures, death, disable, destruction, session shutdown, and plugin shutdown deactivate and remove the clone and restore each original forceRenderingOff value. Cleanup is idempotent. It preserves current game-controlled enabled/layer choices rather than forcing every original renderer visible. The original rig continues running for gameplay and body handling.

Future configuration, after actual assets are ready; leave both flags disabled for now:

```ini
[DevelopmentEffects]
Enabled = false
StandingModelPreview = false
AngerSaturation = 12
AuraStrength = 1
```

Both opt-ins, the inspected v81 assembly, and a validated full bundle are required. The ignored local model and standalone preview are not automatically installed into the game or adopted as release assets. Voice, complete asset credits, game-compatible materials, and real-game/multiplayer validation remain pending.

Validation from the repository root in Windows PowerShell 5.1:

```powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-CosmeticController.ps1
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-ObserverLifecycle.ps1
```

Play-mode fixtures use a synthetic native skin and fake enemy fields. They check successful attachment, exact prior-mask restoration, live hiding/layers while masked, a rolling source container, unchanged arrays/Animator/collider/corpse, invalid-model rejection, read-failure rollback, death, disable, and repeated cleanup. The compiled-plugin check verifies default-off startup and version rejection. These checks do not establish successful attachment to an actual v81 Bracken.

Locomotion is now implemented as a first visual pass. Aggro-ready and kill-reach presentation is available for standalone review; exact victim alignment remains pending. Corpse poses and game material validation remain follow-up work.
