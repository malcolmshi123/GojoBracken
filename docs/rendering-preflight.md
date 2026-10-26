# Rendering preflight

The game render pipeline is still unknown. Earlier logs did not contain a completed pipeline observation. HDRP assembly presence alone does not prove which pipeline/settings were active. Do not select a package patch from an assembly major version or convert the approved authoring assets on that basis.

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

This checkpoint prepares evidence and a conversion map; it does not pin HDRP, convert materials or produce a game-ready bundle. Next increment: review the returned pipeline evidence and implement the appropriate material conversion in a separate asset set. If evidence remains unknown, resolve that before claiming game-compatible rendering.
