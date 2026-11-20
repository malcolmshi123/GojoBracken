# Packaging and release

The 0.3.0 package uses the approved Blender model and the existing aura, Japanese voice and music. Install BepInExPack 5.4.2305 and the release ZIP in a Lethal Company v81 profile, or place its BepInEx folder in the profile manually. Keep exactly one GojoBracken.dll beside the extensionless gojobracken bundle.

The package includes the normal attachment configuration: development cosmetics and the standing model enabled; SoloLiveTest and RenderPreview disabled. Existing customized configurations should be reviewed when updating. Multiplayer behavior and player-kill/body alignment are unverified for this model. Release acceptance is based on the approved model and in-ship simulated cycle. Further live testing was waived; see the [testing scope](in-game-testing.md).

## Build

From the checkout root in Git Bash, using the approved complete bundle and its matching credits:

```bash
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Build-ThunderstorePackage.ps1 \
  -BundleDirectory artifacts/unity/mod-bundle \
  -IconPath artifacts/unity/source/package/icon.png
```

The builder reads the assembly version, includes README.md, CHANGELOG.md, license, credits, a 256x256 PNG icon, DLL, bundle and release config, then writes dist/GojoBracken-0.3.0.zip. It refuses to overwrite an existing archive. `-IncludeSoloConfig` selects the private solo-test configuration instead and is not used for a public release.

## Verify

Use Mono.Cecil.dll from the BepInEx core directory to inspect DLL metadata without executing game code:

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Test-ReleasePackage.ps1 \
  -PackagePath dist/GojoBracken-0.3.0.zip \
  -BundleDirectory artifacts/unity/mod-bundle \
  -CecilPath '/path/to/BepInEx/core/Mono.Cecil.dll'
```

Pass a Windows path or convert a Git Bash path with cygpath -w when setting CecilPath. The verifier checks exact file layout and bytes, source README/changelog/license, matching bundle/credits, config, icon, dependency identity and agreement between filename, manifest, DLL assembly, compiled PluginVersion and project version. Its adjacent JSON report includes hashes and the pinned game MVID and can serve as Test-GameProfile.ps1's ExpectedManifest. Failed verification replaces any earlier success report. File verification does not establish gameplay compatibility.

For packaging regression checks, run scripts/tests/Test-ReleasePackage.ps1 with the same CecilPath. The suite tests valid packaging and rejects altered version, bundle, config, extra DLLs, and missing or duplicate entries. See [in-game testing](in-game-testing.md) for the optional preview and live modes.

## Handoff

Before installing, back up the profile's plugin directory and configuration outside BepInEx/plugins. Keep the approved 0.2.7 published package as provenance and rollback material. To roll back, close the game, restore the backed-up DLL, bundle and configuration together, and remove any newly introduced preview files. Do not leave duplicate plugin DLLs. Only the newly versioned ZIP is a release candidate; do not upload old private test kits. Publication, Git staging/commits/pushes and tags remain separate user actions. When uploading, retain the AI Generated category and the source/voice/music attribution. After installation, inspect LogOutput.log for bundle/configuration failures. Disable DevelopmentEffects.Enabled and restart to use vanilla visuals while diagnosing a problem. File verification does not replace a fresh-profile game launch or multiplayer test.
