# Rendering preflight

A main-menu run with GojoBracken 0.1.8 reported HDRenderPipelineAsset on the inspected v81 build using Unity 2022.3.62f2. The reported assembly version was 0.0.0.0, so the game HDRP package patch remains unknown.

## Actual native material audit

Choose GojoBracken > Audit native prefab materials (read only) in Unity 2022.3.62f2 outside Play mode. The ignored local native model must be present. The tool writes a uniquely named material-audit JSON beside the Unity project, without saving/modifying prefab or material assets. With the main project this is the repository root; keep the generated report ignored.

The audit ran successfully against the same isolated asset copies used by the approved standalone preview. In Gamma/built-in authoring rendering:

| Asset | Current shader | Preparation needed if HDRP is confirmed |
| --- | --- | --- |
| Four Gojo skin materials | glTF/PbrMetallicRoughness | Copy materials into a separate game asset set; map each base-color texture and factors to a compatible lit shader. Check texture color space, metallic/smoothness and appearance. |
| Aura wisps | Legacy Shaders/Particles/Additive | Recreate additive transparent particle rendering with texture and particle vertex color/alpha. |
| Continuous flame | GojoBracken/FlameEnvelope | Port deformation and shading to a compatible transparent additive shader while preserving _Strength and _Intensity property controls. |

All six material slots had present shaders, no shader compilation errors and supported shaders in the authoring renderer. All reported untagged RenderPipeline values. That is an inventory, not a compatibility pass: Unity's [Shader.globalRenderPipeline documentation](https://docs.unity.cn/Documentation/ScriptReference/Shader-globalRenderPipeline.html) describes untagged subshaders as matching every pipeline; matching is not proof that the shader uses the required passes or renders correctly there.

Keep approved preview materials, textures, native idle prefab and rig untouched. Game materials should be separate copies. Revalidate four skins, texture mappings, full flame/wisps at 0/100% anger, transparency/depth, lighting/exposure and bundle reload under the confirmed target renderer. The normal built-in preview cannot certify that conversion.

## Testing PC: offline evidence only

No Unity install, game launch, encounter, game assembly execution or mod installation is required. Pull this increment after it is committed, or use the privately copied Get-RenderingCompatibility.ps1. In Windows PowerShell 5.1, set the actual game installation path and run:

```powershell
$game = 'C:\Program Files (x86)\Steam\steamapps\common\Lethal Company'
$output = Join-Path $env:USERPROFILE ('Documents\rendering-compatibility-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Get-RenderingCompatibility.ps1 -GameDirectory $game -OutputPath $output
```

For a mod-manager profile, pass its existing LogOutput.log using -BepInExLogPath; otherwise the script checks the game folder's BepInEx log. Optionally pass -PlayerLogPath. Missing logs are reported rather than treated as built-in. Existing logs are historical evidence with timestamps; compare the supplied game hash with the inspected v81 hash. Pipeline assembly file/product versions can be absent or insufficient to establish an exact package patch.

Return the generated JSON, not the game binaries or complete logs. The collector exports selected rendering lines and file versions/hashes only. It refuses to overwrite existing reports. PowerShell 5.1 fixture checks passed for selected-line filtering, absent assemblies/logs, a historical HDRP observation and an unknown result.

## Build the HDRP material candidates

Run from the repository root in Windows PowerShell 5.1 with the local native Gojo prefab present.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-HdrpMaterials.ps1
~~~

The script uses Unity 2022.3.62f2 and HDRP 14.0.12 as a local validation target. It creates a fresh ignored Unity project under artifacts/hdrp, with separate Gojo and aura prefab copies under Assets/HdrpCandidates. The converted body uses HDRP Lit with the original albedo textures. A low specular color and a 0.25 smoothness cap correct the mirror finish seen in the first HDRP captures. The wisps use additive blending with the particle color and alpha. The flame ports the approved vertex motion and retains _Strength and _Intensity.

A successful run compiles the materials and reopens a Windows verification bundle containing the two prefabs. Check the Unity log for HDRP MATERIALS PASS. This fixture covers material, texture and rig serialization. Visual calibration under HDRP and in-game rendering remain separate checks.

## Capture the HDRP preview

Pass the generated project directory from the material build.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Capture-HdrpPreview.ps1 -CandidateProject .\artifacts\hdrp\<run>\u
~~~

The script runs the existing aura controller against the HDRP prefab copies. It renders 0 and 12 anger with the same camera, fixed exposure, lighting and particle seed. The images are saved in the run's renders directory as hdrp-calm.png and hdrp-full.png.

The calibrated body retains the dark uniform and white hair. The HDRP flame uses lower peak opacity so the face and torso remain legible at full strength. The scene uses fixed exposure and bloom disabled for comparison. Game lighting, exposure and camera behavior still need a controlled in-game check.

Use the [solo in-game HDRP preview](ingame-hdrp-preview.md) to check the same materials with the game camera and lighting.
