# Packaging and release

The 0.3.1 package contains the approved Blender model and existing aura, Japanese voice and music. Install BepInExPack 5.4.2305 and the release ZIP in a Lethal Company v81 profile. For a manual install, copy the ZIP's BepInEx folder into the profile. Keep exactly one GojoBracken.dll beside the extensionless gojobracken bundle.

The packaged config enables development cosmetics and the standing model, with SoloLiveTest and RenderPreview disabled. Review existing custom settings when updating. The model and in-ship simulated cycle have been approved for release preparation; further live testing was waived. Multiplayer behavior and player-kill/body alignment remain unverified. See the [testing scope](in-game-testing.md).

## Build

From the checkout root in Git Bash, using the approved complete bundle and its matching credits:

```bash
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Build-ThunderstorePackage.ps1 \
  -BundleDirectory artifacts/unity/mod-bundle \
  -IconPath artifacts/unity/source/package/icon.png
```

The builder reads the assembly version and writes dist/GojoBracken-0.3.1.zip. It includes README.md, CHANGELOG.md, license, credits, a 256x256 PNG icon, DLL, bundle and release config. It refuses to overwrite an existing ZIP. Use `-IncludeSoloConfig` only for a private solo-test package.

## Verify

Use Mono.Cecil.dll from the BepInEx core directory to inspect DLL metadata without executing game code:

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Test-ReleasePackage.ps1 \
  -PackagePath dist/GojoBracken-0.3.1.zip \
  -BundleDirectory artifacts/unity/mod-bundle \
  -CecilPath '/path/to/BepInEx/core/Mono.Cecil.dll'
```

Pass a Windows path for CecilPath, or convert a Git Bash path with cygpath -w.

The verifier checks the exact ZIP layout and file bytes against the source README, changelog, license, bundle, credits and config. It also checks the icon and dependency identity. The filename, manifest, DLL assembly, compiled PluginVersion and project version must agree.

The adjacent JSON report contains hashes and the pinned game MVID; use it as Test-GameProfile.ps1's ExpectedManifest. A failed check replaces any earlier success report. These file checks do not establish gameplay compatibility.

Run scripts/tests/Test-ReleasePackage.ps1 with the same CecilPath to check packaging regressions. It accepts a valid package and rejects altered versions, bundles, configs, extra DLLs, and missing or duplicate entries. See [in-game testing](in-game-testing.md) for preview and live modes.

## Handoff

Before installing, back up the profile's plugin directory and config outside BepInEx/plugins. Keep the published 0.2.7 package for provenance and rollback. To roll back, close the game and restore the backed-up DLL, bundle and config together. Remove any newly introduced preview files and check for duplicate DLLs.

Upload only the new release ZIP, keeping private test kits local. Retain the AI Generated category and the source, voice and music attribution when uploading.

After installation, check LogOutput.log for bundle or config errors. To use vanilla visuals while investigating, disable DevelopmentEffects.Enabled and restart. A verified ZIP still needs a fresh-profile game launch and multiplayer testing before either can be claimed as tested.
