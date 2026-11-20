# Unity authoring studies

This project contains the aura study, original glTF import tools and raw-asset exporter. The release pipeline uses the approved Blender model and the isolated native project at `artifacts/unity/project`. Follow [the asset pipeline](../docs/asset-pipeline.md) to import, verify or rebuild the release bundle.

## Editor

Open this folder with Unity 2022.3.62f2. The glTF importer and shared presentation package are pinned in `Packages/`. Keep generated Library, logs and local source assets out of Git; preserve source `.meta` files. This project's study materials use authoring shaders and do not establish HDRP compatibility for a release.

## Studies

- `Assets/GojoBrackenPreview/AuraStudy.unity` provides synthetic hunting, retreat, aggro, carrying, anger, death and reset controls. It uses the shared presentation rules and a neutral stand-in. An optional local voice clip can be assigned for authoring.
- The GojoBracken menu creates or refreshes the aura study, bakes its native prefab and audits materials. Creation refuses to overwrite an existing study; save work before refreshing.
- `Assets/LocalModelStudy/GojoIdleStudy.unity` is a local source-model inspection scene, available when its ignored assets have been imported. It is separate from the approved Blender model.

Run isolated controller checks from the checkout root in Git Bash:

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/tests/Test-ConnectedCosmetics.ps1
```

These checks exercise synthetic presentation and native effects without the game. Use [in-game testing](../docs/in-game-testing.md) for the shipped assets and validation limits.

## Alternate raw-asset export

`Build-Assets.ps1` and the GojoBracken Validate/Build menus support the original raw-asset workflow. They expect `Assets/GojoBracken/Prefabs/Gojo.prefab`, `Aura.prefab`, `Audio/DomainExpansion.wav` and `CREDITS.txt`, with native components and valid dependencies. Missing inputs fail the build. Do not put study stand-ins in release asset slots or replace the current complete bundle with a study export.

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Build-Assets.ps1 -PreflightOnly
```

The alternate export writes to ignored `artifacts/assets/`. Current release authoring, HDRP verification, audio preservation and packaging are maintained in [docs/asset-pipeline.md](../docs/asset-pipeline.md) and [docs/packaging.md](../docs/packaging.md). Historical research and increment records belong in `archive/docs/`.
