# Unity asset project

This is the source scaffold for GojoBracken's real Unity AssetBundle. It contains a separate native aura study, but no Gojo model, voice clip, game-ready aura prefab, or finished bundle.

## Editor and project setup

Use **Unity 2022.3.62f2**, matching the player version reported from the v81 testing installation. [Official release and downloads](https://unity.com/releases/editor/whats-new/2022.3.62f2).

1. Install this Editor on the development laptop. Unity Hub has its own version number; no matching Hub release is required. Complete Unity's sign-in/license steps yourself if requested. Do not substitute a newer Editor automatically.
2. If the standalone Editor is missing from Hub, use Installs > Locate and select C:\Program Files\Unity 2022.3.62f2\Editor\Unity.exe. Add this repository's UnityProject folder and open it with 2022.3.62f2.
3. Choose **GojoBracken > Create asset folders**.
4. Import the actual assets and retain their .meta files. Review the additional project settings and package lock file Unity generates before committing them.
5. Configure the material/render pipeline for the game's confirmed runtime settings before treating any materials as game-ready.

Only built-in Unity modules are declared so far. No HDRP package patch or pipeline asset has been guessed. The report confirms HDRP assembly presence, not the active pipeline; startup-only diagnostics can establish the active pipeline without an enemy encounter. Pin matching rendering dependencies in a later increment.

## Native aura study

The development laptop imported this project with Unity 2022.3.62f2 and generated a separate authoring study. Open **Assets/GojoBrackenPreview/AuraStudy.unity**, select the **Game** view, and press **Play** in the Editor. Lethal Company is not needed.

- Use Hunting, Retreat, Aggro, and Corpse dragging to simulate phases.
- Move the normalized anger slider to change particle density, size, outward speed, and brightness. A continuous blue-white flame surrounds the body and emits outward-rising, tapered wisps. Both the envelope and wisps are strongest at 100%; wisp density, size, speed, and brightness increase with anger. At zero, a faint aura remains in every phase.
- Entering aggro increments the voice-trigger counter once. Leave aggro and return to trigger it again.
- No voice is supplied. Optionally assign your own local AudioClip to the AudioSource on **Preview controls - editor study only** before entering Play mode.
- End preview stops particles and audio; Reset preview clears the simulated state.

The capsule is a neutral stand-in, not the Gojo model. Materials use Unity's built-in authoring shaders and do not establish compatibility with the game's rendering pipeline. This scene and its AuraStudy.prefab stay outside the release asset paths. The release exporter still requires the actual assets below.

Flame settings are applied automatically when entering Play mode, including in the existing scene. Stop Play, allow scripts to compile, and press Play again after updating. To also save the new settings into the study scene and prefab, choose GojoBracken > Refresh study flame aura while outside Play mode. Review/save your current scene first.

The scene is already generated. The **GojoBracken > Create native aura study** menu creates it in a fresh checkout only when the study outputs are absent; it refuses to overwrite existing work.

## Required assets

| Project-relative path | Requirements |
| --- | --- |
| Assets/GojoBracken/Prefabs/Gojo.prefab | Actual Gojo skinned model with an Animator; animation adaptation and visual quality still need inspection |
| Assets/GojoBracken/Prefabs/Aura.prefab | Blue-white aura containing a ParticleSystem |
| Assets/GojoBracken/Audio/DomainExpansion.wav | Non-empty imported clip of the requested Japanese voice line |
| Assets/GojoBracken/CREDITS.txt | Sources, authors, licenses/permissions, and modifications for the imported assets |

Prefabs must use native Unity components only, with no custom or missing scripts and no missing materials/shaders. Runtime mod code will be attached by the plugin later. Referenced model textures, materials, animation controllers, and clips are included as bundle dependencies by Unity.

The initial model candidate remains [wing2's Gojo Satoru](https://sketchfab.com/3d-models/gojo-satoru-1e19a0308d474704a04b662b65bf7f40). It has not been downloaded, imported, or adopted. Its appearance, usable rig, actual download availability, and asset terms need verification. Keep third-party asset terms separate from the repository's MIT code license. The credits check validates presence, not legal permission.

Do not put the browser or native study stand-in into the release prefab slots. Do not copy game DLLs or extracted game assets into this project.

## Validate and build

In the Editor, choose **GojoBracken > Validate assets**, then **GojoBracken > Build Windows asset bundle**. Missing assets cause a build failure; no empty substitute bundle is generated.

Or, from the repository root in Windows PowerShell 5.1:

```powershell
# File-presence preflight only; does not require or launch Unity.
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned `
    -File .\scripts\Build-Assets.ps1 -PreflightOnly

# Real export; close any Editor instance using this project first.
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned `
    -File .\scripts\Build-Assets.ps1 `
    -UnityEditorPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe'
```

The wrapper detects the standard Hub or standalone installation path automatically when -UnityEditorPath is omitted. It accepts the Editor's revision suffix in file-version metadata while still requiring the exact pinned version. Unity Hub installation guidance: https://docs.unity.com/en-us/hub/add-editor

RemoteSigned affects that child process only; no persistent execution-policy change is made. Unity must already be installed and licensed. The command does not install software or fetch a model/audio clip.

Outputs go to the Git-ignored artifacts/assets folder at the repository root:

- gojobracken: Windows x64 AssetBundle.
- ASSET-CREDITS.txt: accompanying credits.
- Unity bundle manifests and a per-run unity-build-*.log.

The exporter reopens the bundle to verify these names and types:

| Runtime asset name | Type |
| --- | --- |
| gojo | GameObject |
| aura | GameObject |
| domain_expansion | AudioClip |

The wrapper requires a successful Unity exit, a bundle, and the completion marker in that run's own log. A leftover bundle from an earlier run is not accepted as proof of a successful build.

## Validation status

The PowerShell preflight/error paths are tested with fixtures using Windows PowerShell 5.1. Unity 2022.3.62f2 imported the project, compiled both editor scripts and the study controller, generated the native scene, and rendered a verification image successfully on the development laptop. Interactive Play-mode controls and real audio playback still need manual review. Actual release asset validation/export, game material compatibility, animation mapping, and game integration remain pending real assets and runtime evidence. No natural Bracken encounter is needed to author or inspect the assets.

References: [Unity bundle-building API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/BuildPipeline.BuildAssetBundles.html), [runtime asset aliases](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AssetBundleBuild-addressableNames.html), and [bundle compatibility](https://docs.unity3d.com/2022.3/Documentation/Manual/AssetBundlesIntro.html).
