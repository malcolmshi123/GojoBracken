# Asset loading checkpoint

GojoAssetBundle is the prepared runtime loader. The plugin does not call it yet: this increment does not attach cosmetics, hide vanilla renderers, change AI, or produce a release bundle. The approved Unity pose and aura preview are unchanged.

The future attachment code will call TryLoad with the directory beside GojoBracken.dll. It expects one Windows bundle named gojobracken, using the shared GojoAssetContract addresses:

| Address | Type | Structural requirements |
| --- | --- | --- |
| gojo | GameObject | Nonempty skinned mesh, matching bind poses and non-null bones, Animator with controller |
| aura | GameObject | At least one ParticleSystem |
| domain_expansion | AudioClip | Nonempty imported audio |
| asset_credits | TextAsset | Nonblank provenance text |

Both prefabs must have materials/shaders and no custom or missing scripts. Runtime checks reject unsupported shaders. These checks do not certify animation quality, active render-pipeline compatibility, appearance, or asset permissions.

The exporter now embeds CREDITS.txt as asset_credits and still emits ASSET-CREDITS.txt beside the bundle. Earlier bundles without embedded credits must be rebuilt. All asset addresses are shared between the exporter and loader, rather than maintained separately.

A missing, unreadable, corrupt, or structurally invalid bundle returns no asset set and a diagnostic saying to keep the vanilla Bracken visible. An opened bundle is unloaded on validation failure. No prefab is instantiated while loading. The future attachment code must finish its own rig/material checks before hiding vanilla visuals; this loader alone does not make a replacement safe to display.

A successful asset set owns its loaded assets. Destroy all cosmetic instances before disposing it; Dispose unloads the bundle and its assets and can be called repeatedly. No bundle is loaded at plugin startup in this checkpoint. Unity documents the asset lifetime behavior in [AssetBundle.Unload](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AssetBundle.Unload.html) and file loading in [AssetBundle.LoadFromFile](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AssetBundle.LoadFromFile.html).

From the repository root, in Windows PowerShell 5.1:

```powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-AssetBundle.ps1
```

The Unity check creates an isolated project under ignored artifacts/asset-bundle-tests, compiles the production loader and exporter source, builds actual Windows bundles, and exercises rejection, cleanup, disposal, and reloading. It also checks a round-trip from the production exporter to the loader, including matching embedded/sidecar credits. Tests use a synthetic triangle rig, a basic particle system, and silence. These are test fixtures, not completed Gojo assets or the requested voice line. No game installation or encounter is required.

Remaining integration work includes adapted model animation, final aura materials/prefab, the Japanese voice clip with provenance, game attachment with rollback, and controlled game/multiplayer validation.
