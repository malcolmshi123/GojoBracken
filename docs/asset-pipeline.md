# Asset pipeline

Build the release from the edited Blender model and native Unity prefab/bundle. Developers need Blender, Unity and the source assets. Players install the release ZIP.

## Compile

Install the .NET 10 SDK and configure `local.props` from `local.props.example` with the BepInEx core and game Managed directories. The plugin targets netstandard2.1.

```bash
dotnet build GojoBracken.sln -c Release
dotnet run --project tests/StateReaderChecks/StateReaderChecks.csproj -c Release
```

## Model and bundle

Keep these local authoring files when cleaning build output. Git ignores them:

- `artifacts/blender/Gojo.blend`: the approved editable model, including hands and neck, with four packed textures.
- `artifacts/blender/source/`: published 0.2.7 package, source textures, license and credits used as provenance.
- `artifacts/blender/scripts/`: validation and consistent preview rendering; `tools/` and `user/` hold the isolated Blender installation/settings.
- `artifacts/unity/project`: native model and HDRP assets in Unity 2022.3.62f2 / HDRP 14.0.12.
- `artifacts/unity/mod-bundle/`: the approved complete `gojobracken`, credits and build manifest.

The model has seven skinned meshes and 31 bones. Its exported bind poses and standing controller keep the approved pose after runtime attachment disables the Animator.

The jacket's inner armholes sit lower, with the original shoulder caps unchanged. The placket has two closed rectangular layers, each with a constant width and defined edges. The hands have connected surfaces, and the neck joins smoothly. Custom normals smooth the edited surfaces. Blender fingerprints include posed normals, and Unity checks positions and normals, including a test that rejects an inverted normal. The model has no independent finger animation or calibrated corpse grip.

Run from the checkout root in Git Bash:

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Import-BlenderModel.ps1
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Build-GojoBundle.ps1
```

Import checks existing outputs without overwriting them. After editing Gojo.blend, pass `-Reimport` to Import-BlenderModel.ps1 to replace the generated native model assets. Use `-VerifyOnly` on either script to check existing outputs. Complete-bundle verification checks the existing build manifest against the current flame shader and bundle bytes, then leaves the manifest unchanged. A normal build refreshes the HDRP shader copies before export. Close the Editor project before running batch commands. Build-GojoBundle uses pinned UnityPy 1.25.4 through uvx to preserve the released encoded audio streams.

The complete bundle contains `gojo`, `aura`, `domain_expansion`, `aggro_music` and the existing metadata aliases. Look under artifacts for model/source hashes and verification records. Keep the model-only `gojo-model` output separate from the complete release bundle.

## Release and historical tools

Follow the [packaging guide](packaging.md) to build and verify the ZIP. The [asset credits](release-assets.md) cover attribution. The bundle credits include the Blender modification notice; keep those exact credits with the bundle.

UnityProject holds the earlier authoring studies and alternate raw-asset exporter. The older HDRP/solo builders still support those studies. Use the Blender pipeline above for the current release. Earlier Blender handoffs, the one-off 0.2.7 preview-kit builder and increment records are under ignored archive/.
