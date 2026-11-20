# Asset pipeline

The current release uses the edited Blender model and a native Unity prefab/bundle. Blender, Unity and the source assets are development requirements; players install the release ZIP.

## Compile

Install the .NET 10 SDK and configure `local.props` from `local.props.example` with the BepInEx core and game Managed directories. The plugin targets netstandard2.1.

```bash
dotnet build GojoBracken.sln -c Release
dotnet run --project tests/StateReaderChecks/StateReaderChecks.csproj -c Release
```

## Model and bundle

Retain these ignored local authoring files when cleaning build output:

- `artifacts/blender/Gojo.blend`: the approved editable model, including hands and neck, with four packed textures.
- `artifacts/blender/source/`: published 0.2.7 package, source textures, license and credits used as provenance.
- `artifacts/blender/scripts/`: validation and consistent preview rendering; `tools/` and `user/` hold the isolated Blender installation/settings.
- `artifacts/unity/project`: native model and HDRP assets in Unity 2022.3.62f2 / HDRP 14.0.12.
- `artifacts/unity/mod-bundle/`: the approved complete `gojobracken`, credits and build manifest.

The model has seven skinned meshes and 31 bones. Exported bind poses and a standing controller preserve the approved pose when runtime attachment disables the Animator. The jacket placket has two closed rectangular layers with constant widths and defined edges. Connected hand surfaces and the smooth neck join are retained. Custom normals are rebuilt for the edited surfaces. Blender fingerprints include posed normals; Unity checks both positions and normals and rejects a deliberately inverted normal. Independent finger animation and a calibrated corpse grip are not provided.

Run from the checkout root in Git Bash:

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Import-BlenderModel.ps1
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Build-GojoBundle.ps1
```

Import verifies existing outputs instead of overwriting them. After deliberately editing Gojo.blend, use `-Reimport` on Import-BlenderModel.ps1 to replace generated native model assets. Use `-VerifyOnly` on either script to check existing outputs. Complete-bundle verification requires its existing build manifest to match the current flame shader and bundle bytes; it preserves that provenance instead of relabeling an older bundle. A normal bundle build refreshes the canonical HDRP shader copies before export. Keep the Editor project closed during batch commands. Build-GojoBundle uses pinned UnityPy 1.25.4 through uvx to preserve the released encoded audio streams.

The complete bundle retains `gojo`, `aura`, `domain_expansion`, `aggro_music` and the existing metadata aliases. Model/source hashes and verification records are under artifacts. Do not replace the complete bundle with the model-only `gojo-model` output.

## Release and historical tools

Use the [packaging guide](packaging.md) to build and verify the release ZIP and [asset credits](release-assets.md) for attribution. The current bundle's credits already include the Blender modification notice; preserve their bytes with the bundle.

UnityProject contains the earlier authoring studies and alternate raw-asset exporter. The older HDRP/solo bundle builders remain available for those studies; they are not the current release route. Superseded Blender handoff guides and the one-off 0.2.7 preview-kit builder are under ignored archive/, alongside increment records. They are historical snapshots, not current build instructions.
