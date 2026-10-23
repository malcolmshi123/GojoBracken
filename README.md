# GojoBracken

A cosmetic Lethal Company mod that will replace the Bracken with Gojo Satoru from Jujutsu Kaisen, inspired by [GokuBracken](https://thunderstore.io/c/lethal-company/p/Vulf/GokuBracken/).

- Animated Gojo in a black uniform and blindfold, with white hair.
- Blue-white aura that scales with anger in hunting, retreat, aggro, and corpse dragging.
- The Japanese "ryoiki tenkai muryokusho" voice line once on each entry into aggro.
- Vanilla AI, damage, movement, targeting, and corpse handling.

## Current status

Version 0.1.0 is a plugin foundation: it logs when BepInEx loads it.
It does not replace the Bracken or provide an aura or voice line yet.
No model, audio, Unity project, AssetBundle, or installable release is included.

Development proceeds in small review checkpoints. Staging, commits, and pushes are performed manually by the repository owner, including the initial commit.

## Build the foundation

On the PC used for game testing, install the .NET 10 SDK, Lethal Company, and BepInEx 5 for the game. The SDK runs the development tools; the plugin still targets netstandard2.1 for Unity compatibility. The plugin references the installed BepInEx and Unity assemblies rather than distributing copies.

1. Copy local.props.example to local.props in the repository root.
2. Set LethalCompanyDir to the installed game's directory.
3. Set BepInExCoreDir to the directory containing BepInEx.dll and 0Harmony.dll. With a mod manager, use your test profile's BepInEx/core directory.
4. From the repository root, run:

```powershell
dotnet build GojoBracken.sln -c Release
```

The plugin output is src/GojoBracken/bin/Release/netstandard2.1/GojoBracken.dll.
Copy only that DLL into BepInEx/plugins/GojoBracken in a separate test profile.
Start the game and check BepInEx/LogOutput.log for:

```text
Gojo Bracken 0.1.0 loaded. Plugin foundation only; cosmetics are not implemented yet.
```

For command-line builds, LethalCompanyDir, BepInExCoreDir, and GameManagedDir can also be passed using -p:Name="path". GameManagedDir defaults to LethalCompanyDir/Lethal Company_Data/Managed; BepInExCoreDir defaults to LethalCompanyDir/BepInEx/core when not supplied.

local.props, dependency DLLs, and generated output are ignored by Git.
Unity is not needed to compile this foundation; it will be needed to author and export the assets.

VS Code design-time project loading skips the build-only local reference validation. A real build still requires the reference paths above. Archived DLLs can support editing and a foundation compile check, but do not establish compatibility with the current game. After installing the SDK, fully restart VS Code to refresh SDK discovery.

## Check the testing PC

Before choosing a Unity Editor version, run the read-only compatibility script on the PC with Lethal Company installed. It reports file versions and SHA256 hashes without loading game assemblies or changing the game. It does not include your installation paths in the JSON.

From the repository root, use the actual game installation and, if applicable, your separate mod-manager test profile:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
.\scripts\Get-GameCompatibility.ps1 `
    -GameDirectory 'C:\Games\Steam\steamapps\common\Lethal Company' `
    -BepInExCoreDirectory 'C:\ModProfiles\GojoTest\BepInEx\core' |
    Set-Content -Encoding UTF8 .\artifacts\compatibility.json
```

For a manual BepInEx installation inside the game directory, omit BepInExCoreDirectory. Run the script from PowerShell 7 (pwsh). The generated artifacts directory is ignored by Git.

Bring back compatibility.json and the version number displayed in the game's main menu. If unityVersionFromPlayer is null, also provide the Unity version line from the game's Player.log. UnityPlayer file-version metadata is preserved in the report for inspection.

This is an installation inventory, not a compatibility certification. The active render settings, Bracken state mapping, anger range, corpse-carrying behavior, and anger replication remain pending code/runtime inspection before the Unity asset project is pinned.

## Research and asset requirements

The old malcolmshi123/GojoBracken source at commit aab524b was inspected and backed up before the fresh start. It contained a BepInEx entry point, a basic model/audio patch, and terminal patches, but no referenced Gojo prefab or media assets.

| Component | Old repository | Required next |
| --- | --- | --- |
| Plugin/build | .NET Framework 4.8; fixed machine paths | SDK-style netstandard2.1 project with local reference settings (provided here) |
| Model replacement | Spawned Gojo; removed one renderer | Validate assets before hiding vanilla visuals; preserve AI and animation behavior |
| Model and materials | Referenced assets/Gojo.prefab; asset absent | Rigged model, textures, compatible materials, prefab, and animations |
| Aura | Absent | Blue-white particle prefab and anger-driven controller |
| Aggro voice | Referenced assets/AngerVoice.mp3; asset absent | Trimmed Japanese voice clip and one-shot aggro transition handling |
| Unity asset build | Absent | Version-matched Unity project, .meta files, package settings, and Windows x64 bundle export |
| Packaging | Absent | DLL, AssetBundle, Thunderstore manifest, icon, README, and asset credits |
| Repository hygiene | Tracked bin/obj and dependency DLLs | Generated files and local DLLs ignored |

The new plugin will load AssetBundles through Unity directly, using BepInEx 5 and Harmony for integration. LC_API, terminal edits, and neck-snap sound replacements are not part of this rebuild. Harmony is referenced now for the later integration; the foundation applies no patches.

Required assets, none included yet:

- A rigged Gojo model with textures, materials, and animations adapted to the Bracken. First candidate: [Gojo Satoru by wing2](https://sketchfab.com/3d-models/gojo-satoru-1e19a0308d474704a04b662b65bf7f40). Appearance, rig, availability, and attribution must be checked before adoption.
- A blue-white aura prefab using shaders compatible with the game's render pipeline.
- The original Japanese voice clip, with source/provenance recorded separately from the code license.
- A reproducible Unity project that exports the model, effects, and audio in a Windows x64 AssetBundle.

Before creating that project, inspect the game on the testing PC to confirm its Unity version, render pipeline, Bracken state mapping, anger range, corpse-carrying flag, and multiplayer replication. No current game version has been validated.

## Planned behavior and checkpoints

1. Plugin foundation (complete; game testing pending).
2. Inspect the testing PC's game version; prepare the Unity asset pipeline and credited assets.
3. Implement the animated replacement with vanilla visuals retained if assets cannot load.
4. Add anger-scaled aura and one-shot spatial audio on aggro entry.
5. Package and validate in the game before publishing separately.

The aura has a faint baseline at zero anger and a bounded maximum, with configurable strength and maximum anger. This applies during retreat too, superseding the initial retreat-off idea. Corpse dragging gets the same aura scaling and no additional audio. Death and despawn stop effects and audio.

The mod must work alongside vanilla players without new network messages. If the game does not replicate usable anger to a modded client, that client uses a documented phase approximation (faint outside aggro, strong during aggro), rather than claiming authoritative anger scaling.

Validation will cover phase transitions, anger bounds, repeated aggro entries, multiple Brackens, missing assets, death, cleanup, and mixed modded/vanilla host-client combinations. Cosmetic events must not alter gameplay.

## References and license

- [GokuBracken published implementation](https://thunderstore.io/c/lethal-company/p/Vulf/GokuBracken/source/) demonstrates a controller attached to FlowermanAI, Unity asset loading, effects, and audio replacement.
- [Lethal Company modding setup](https://lethal.wiki/dev/starting-a-mod) describes plugin projects and game references.
- [BrackenAngerMeter published implementation](https://old.thunderstore.io/c/lethal-company/p/azumi/BrackenAngerMeter/source/) reads FlowermanAI.angerMeter; the exact current-game range and replication still require verification.
- [Unity AssetBundle documentation](https://docs.unity3d.com/2023.1/Documentation/Manual/AssetBundlesIntro.html) explains platform and Unity-version compatibility.
- [Thunderstore package format](https://wiki.thunderstore.io/mods/packaging-your-mods) describes eventual distribution requirements.

Code is MIT licensed; the original 2024 Malcolm Shi attribution is retained.
Any future third-party model, animation, effect, or audio has its own terms and credits; the code license does not license those assets.
