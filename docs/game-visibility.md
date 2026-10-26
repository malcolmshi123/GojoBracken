# Vanilla visibility and aura

The development aura now follows the Bracken body's live renderer visibility and layer. This prevents the cosmetic effect from bypassing the game's camera-layer or renderer-enable choices. It does not replace the Bracken model or change its phase/anger rules. The native Gojo model remains a local standing foundation; model replacement is a later increment.

Offline inspection of the supplied v81 DLL confirms that EnemyAI.Start fills skinnedMeshRenderers and meshRenderers. EnemyAI.EnableEnemyMesh changes their GameObject layers (19/23 in the inspected code) and can change renderer.enabled. Runtime code copies the observed layer rather than hard-coding those layer numbers. The inspected animator visibility function uses distance/linecast checks; this increment does not touch the original Animator.

BrackenVisibilityReader reads those cached renderer arrays by exact field type. It does not enumerate descendants, which may include a carried body or newly attached cosmetics. An aura is drawable when at least one cached body renderer is enabled, active in the hierarchy, and not force-hidden. Unity describes [forceRenderingOff](https://docs.unity.cn/2023.2/Documentation/ScriptReference/Renderer-forceRenderingOff.html) as suppressing drawing independently of enabled; the reader only observes this flag. Visible body renderers must agree on their layer, otherwise the binding stops conservatively instead of choosing a potentially wrong camera layer.

Before first attachment, the session waits beyond the discovery frame and for initialized arrays, allowing EnemyAI.Start to cache the original renderers before our aura exists. These arrays are public/serialized, so non-null alone is not proof that Start has run. Missing fields, empty arrays, missing/out-of-hierarchy renderers, mixed visible layers, or later read failures leave or restore vanilla-only behavior by stopping the owned effect. Original renderer flags, layers, animator references, and carried-body renderers are never written.

The binding sets the aura visibility mask before applying each observation in LateUpdate. The controller applies the mask together with its authored renderer flags and strength, and puts its own rendered objects on the observed body layer. Hidden effects continue their bounded particle/state simulation. Voice events remain independent of visual culling. The standalone Unity study, native assets, and approved pose are unchanged.

From the repository root in Windows PowerShell 5.1:

```powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-CosmeticController.ps1
```

Synthetic Play-mode cases cover deferred initialization, disabled/inactive/force-hidden bodies, live layer changes, mixed-layer rejection, invalid references, carried-body exclusion, and unchanged source visibility flags. Existing state, ownership, native-aura, and cleanup cases also run. This establishes the adapter's behavior against fixtures; actual game cameras, multiplayer, and final material compatibility remain unvalidated. No testing-PC action or encounter is requested.
