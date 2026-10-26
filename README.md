# GojoBracken

A cosmetic Lethal Company mod that will replace the Bracken with Gojo Satoru from Jujutsu Kaisen, inspired by [GokuBracken](https://thunderstore.io/c/lethal-company/p/Vulf/GokuBracken/).

- Animated Gojo in a black uniform and blindfold, with white hair.
- Blue-white aura that scales with anger in hunting, retreat, aggro, and corpse dragging.
- The Japanese "ryoiki tenkai muryokusho" voice line once on each entry into aggro.
- Vanilla AI, damage, movement, targeting, and corpse handling.

## Current status

Version 0.1.7 includes optional runtime diagnostics, rig reporting, development aura/audio binding, and an opt-in model attachment with movement-driven locomotion and aggro/kill arm poses.
Development cosmetics require the inspected game assembly and a validated bundle; both development and model-preview flags default to disabled. Animation adaptation and actual game validation remain pending.
A Unity asset project, native aura prefab, and exporter are included. The adapted model, audio clip, game-compatible materials, full AssetBundle, and installable release remain pending.

The native Unity study now uses the same C# state rules as the plugin, with simulated anger, phase, kill/death, cleanup, and remote approximation controls. See [Unity study instructions](UnityProject/README.md#native-aura-study). Game validation, rig adaptation, and release assets remain pending.

A runtime bundle loader and shared exporter addresses are prepared, with checks for missing/invalid assets and bundle cleanup. See [asset-loading checks and remaining integration](docs/asset-loading.md).

A per-instance aura/audio controller now has an opt-in game adapter, with Unity Play-mode checks. It awaits final assets and game validation. See [development-mode behavior and validation](docs/development-effects.md).

The aura adapter also [respects vanilla body visibility and camera layers](docs/game-visibility.md), without including carried-body renderers or changing original visual state.

The optional [standing-model attachment](docs/model-attachment.md) validates Gojo before masking selected vanilla renderers and restores their prior rendering masks on failure or cleanup. Original enabled/layer, animator, collider, and body-handling state are preserved.

The [first locomotion pass](docs/locomotion.md) reads enemy movement, steps the cloned legs, swings its arms, and restores the approved pose at rest. The original idle prefab and torso/neck rotations remain unchanged.

The [action pose pass](docs/action-poses.md) adds an aggro-ready pose and kill-flag reach on the owned arms. Victim alignment and game timing remain pending.

The approved flame/wisps are now [baked as a native aura prefab](docs/native-aura.md), with no preview-script dependency. Rendering compatibility with the game remains pending.

The approved standing pose is also [baked into a local-only native Gojo prefab](docs/local-native-gojo.md). It has a constant idle controller and no dance/root motion; downloaded model data remains ignored pending provenance review.

Development proceeds in small review checkpoints. Staging, commits, and pushes are performed manually by the repository owner, including the initial commit.

## Preview without playing the game

A [standalone Windows prefab preview](docs/testing-pc-preview.md) is now available for testing the actual Gojo/aura assets on the other PC without Unity or a game encounter. Build it on the development laptop and privately transfer the generated ZIP; model data is not in Git. The browser study below remains a schematic prototype.

Open preview/index.html in a browser. In Windows PowerShell 5.1, from the repository root:

```powershell
Start-Process .\preview\index.html
```

Keep preview-state.js beside index.html. No server, PowerShell upgrade, Node.js, Unity installation, or game session is required to use the preview. It uses no external libraries or network services.

- Choose hunting, retreat, aggro, or corpse dragging and adjust the simulated anger slider.
- Aura strength ranges from a 15% calm baseline to 100%, in every phase. The slider is normalized and does not claim to match the game's unverified anger range.
- Load a local audio file if you want sound. Entering aggro triggers it once; repeated aggro selection or anger adjustments do not retrigger it. Leave aggro and return to trigger it again.
- Replay loaded clip auditions the audio manually without incrementing the aggro-entry counter.
- End preview stops the aura and audio; Reset preview clears state and counters. Pause animation freezes the aura motion.
- The character is a schematic stand-in and the aura is drawn in a browser canvas. This is a presentation prototype, not the Gojo 3D model, an animation rig, or a Unity AssetBundle.

The preview records voice-trigger events even when no clip is loaded, but never labels those as successful playback. Audio stays local to the browser; no voice clip is shipped.

For development, the preview's state tests use Node.js's built-in test runner:

```powershell
node --test .\preview\preview-state.test.cjs
```

These tests cover the browser prototype's phase/anger rules and event gating. They do not validate the C# plugin, Unity rendering/audio, or real Bracken state mapping. Controlled in-game and multiplayer checks remain separate and deferred; finding random Brackens is not a prerequisite for reviewing the cosmetics.

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
Gojo Bracken 0.1.7 loaded. Development cosmetics are opt-in; animation adaptation is incomplete.
```

For command-line builds, LethalCompanyDir, BepInExCoreDir, and GameManagedDir can also be passed using -p:Name="path". GameManagedDir defaults to LethalCompanyDir/Lethal Company_Data/Managed; BepInExCoreDir defaults to LethalCompanyDir/BepInEx/core when not supplied.

local.props, dependency DLLs, and generated output are ignored by Git.
Unity is not needed to compile this foundation; it will be needed to author and export the assets.

VS Code design-time project loading skips the build-only local reference validation. A real build still requires the reference paths above. Archived DLLs can support editing and a foundation compile check, but do not establish compatibility with the current game. After installing the SDK, fully restart VS Code to refresh SDK discovery.

## Check the testing PC

Before choosing a Unity Editor version, run the read-only compatibility script on the PC with Lethal Company installed. It reports file versions and SHA256 hashes without loading game assemblies or changing the game. It does not include your installation paths in the JSON.

Use the built-in Windows PowerShell 5.1; PowerShell 7 is not required. From the repository root, use the actual game installation and, if applicable, your separate mod-manager test profile:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned `
    -File .\scripts\Get-GameCompatibility.ps1 `
    -GameDirectory 'C:\Games\Steam\steamapps\common\Lethal Company' `
    -BepInExCoreDirectory 'C:\ModProfiles\GojoTest\BepInEx\core' |
    Set-Content -Encoding UTF8 .\artifacts\compatibility.json
```

For a manual BepInEx installation inside the game directory, omit BepInExCoreDirectory. The generated artifacts directory is ignored by Git.

RemoteSigned applies only to the child PowerShell process above; it does not change machine-wide or user-wide settings. It permits local unsigned scripts while requiring signatures for internet-marked scripts. Review the script before running it. Managed execution policies still take precedence; if one blocks the script, share the error rather than overriding it. See [Microsoft execution-policy documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies?view=powershell-5.1).

Bring back compatibility.json and the version number displayed in the game's main menu. If unityVersionFromPlayer is null, also provide the Unity version line from the game's Player.log. UnityPlayer file-version metadata is preserved in the report for inspection.

This is an installation inventory, not a compatibility certification. The active render settings, Bracken state mapping, anger range, corpse-carrying behavior, and anger replication remain pending code/runtime inspection before the Unity asset project is pinned.

## Runtime diagnostics on the testing PC

The reported baseline is game **v81**, Steam build **22825947**, Unity player **2022.3.62f2**, BepInEx file version **5.4.23.5**, and Harmony file version **2.9.0.0** (2026-10-23). The HDRP assembly is present. This identifies the test installation; runtime compatibility and the final Unity project settings remain unverified.

Version 0.1.1 adds an optional observation mode. It does not patch enemy methods, change enemy fields, or add network messages.

1. Build Release and copy GojoBracken.dll into the separate test profile's BepInEx/plugins/GojoBracken folder, replacing the earlier foundation DLL. Ensure the profile contains only one copy of this plugin.
2. Launch once and exit to generate BepInEx/config/FreakyFriday.GojoBracken.cfg.
3. In that profile's config file, set:

```ini
[Diagnostics]
Enabled = true
```

4. For a startup-only check, restart and stay in the main menu to collect runtime and render-pipeline lines. Bracken state checks are deferred until a controlled private sandbox is available; do not search for random encounters.
5. Return startup lines containing [Diagnostics] from that profile's BepInEx/LogOutput.log when convenient. Host/client state comparisons belong to a later controlled integration test, not the current preview checkpoint.
6. Set Enabled = false and restart when finished.

Diagnostics report Unity/application versions, the currently selected render pipeline type and assembly version, and per-Bracken instance/network IDs, ownership flags, numbered state names, anger, corpse-carrying, kill-animation, and death fields. Missing fields appear as <unavailable>; no state numbers or anger limits are guessed. A null pipeline is logged as built-in-or-not-initialized rather than assumed HDRP.

Sampling runs every 0.5 seconds and logs changed snapshots, so very short transitions may be missed. Values are local observations; these logs alone do not establish anger bounds or prove replication. A sampling failure stops the observer and logs a warning. No observation scan runs when diagnostics are disabled.

Implementation references: [Unity's current render pipeline API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Rendering.GraphicsSettings-currentRenderPipeline.html) and [BepInEx configuration](https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/4_configuration.html).

## Inspect the Bracken rig without an encounter

Version 0.1.2 adds opt-in ExportBrackenRig diagnostics. It reads an already-loaded prefab and exports bone paths, renderer/shader names, and controller/clip metadata into BepInEx/config/GojoBracken. It never spawns an enemy or plays its controller. Unavailable prefab animator parameters are marked explicitly; state labels are not assumed to prove phase mapping.

See [testing-PC instructions and verification limits](docs/testing-pc-rig-report.md). If the prefab is not loaded at the main menu, return the timeout message instead of seeking an encounter. The 0.1.2 testing-PC attempt produced startup lines but no report or pipeline line. That handoff is paused; no further launch is requested. Version 0.1.3 adds immediate coroutine sampling and explicit progress/cancellation logs. See [the investigation and offline metadata reader](docs/observer-investigation.md). The testing-PC cause remains unconfirmed.

The supplied offline metadata report matches the logged v81 game assembly MVID. The supplied DLL also matches its MVID and hash. Read-only inputs and the v81-specific state machine now feed an opt-in development effects adapter; actual game validation remains pending. See [verified phase mapping, anger/RPC findings, and checks](docs/v81-behavior.md). See [confirmed members, checks, and remaining evidence](docs/v81-metadata-contract.md).

## Author the real Unity assets

Open UnityProject with Unity 2022.3.62f2 on the development laptop. See [the asset-project instructions](UnityProject/README.md) for import paths, validation, and the Windows x64 bundle command. The exporter requires actual assets and verified shader dependencies; it does not export the browser stand-in. Unity 2022.3.62f2 now imports and compiles the project. Open Assets/GojoBrackenPreview/AuraStudy.unity in the Editor and press Play for a safe particle study with phase/anger controls. It uses a neutral capsule and authoring materials; the actual Gojo model, voice clip, game materials, and release bundle remain pending.

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

Required release assets:

- A rigged Gojo model with textures, materials, and animations adapted to the Bracken. The selected candidate has a local native standing bake; underlying credits and movement/attack/corpse adaptation remain pending. See [model evaluation](docs/model-evaluation.md).
- The blue-white aura prefab is included; its shaders still need validation/adaptation for the game's render pipeline.
- The original Japanese voice clip, with source/provenance recorded separately from the code license.
- A reproducible Unity project that exports the model, effects, and audio in a Windows x64 AssetBundle.

The testing-PC report and main menu identify game v81 and Unity 2022.3.62f2. The new project uses that exact Editor version as its initial authoring baseline. Active render settings, Bracken state mapping, anger range, corpse-carrying behavior, and replication still require inspection before finalized material and runtime integration choices.

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
