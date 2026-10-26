# Baked native aura

Assets/GojoBracken/Prefabs/Aura.prefab now contains the approved continuous blue-white flame and tapered wisps using native Unity components only. Its mesh, shader, materials, and texture live in Assets/GojoBracken/Effects with stable .meta files. They have no dependencies on the preview scripts or the locally downloaded Gojo model.

The full-strength geometry and particle settings come from the approved study. The flame mesh has 2,665 vertices and a 3-unit tip. The bake reuses the study's mesh generator and particle settings rather than maintaining another visual recipe. The shell shader has its own GojoBracken/FlameEnvelope name; the preview shader and approved pose remain unchanged. The generated mesh/texture/shader derive from this repository's authoring code, under the repository license; the particle material also uses Unity's built-in shader.

This is an actual aura asset, but its materials currently target the built-in authoring renderer. The active game pipeline and material compatibility remain unverified. It is not a complete installable mod or a claim of game-ready rendering. The full game bundle still needs the adapted model, Japanese voice clip, and complete asset credits.

Open Aura.prefab in Unity to inspect the native mesh, material references, and particle settings. Particle play-on-awake is off: runtime observations start the effect. The shell is visible when inspecting the prefab directly. The runtime controller initially hides all cloned renderers until its first observation, then drives emission and shell shader controls. Native particle simulation uses game time. Full intensity matches the approved study; intermediate runtime wisp size/speed and visual calibration remain separate from the study's richer authoring slider.

The GojoBracken > Bake native aura prefab menu creates these assets in a checkout where they are absent. It refuses to overwrite existing files or reset GUIDs. The baked outputs are tracked sources; later visual revisions should update them deliberately and preserve their metadata.

Repeatable checks from the repository root, in Windows PowerShell 5.1:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-BakedAura.ps1
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-CosmeticController.ps1
```

The first uses an isolated Unity project to verify native components, persistent references, approved maximum settings, independence from study assets, the overwrite guard, and a Windows bundle export/reopen. Its native-aura-check bundle is a test artifact, not the full gojobracken bundle. The second drives the actual baked prefab in Play mode, including initial hidden renderers, maximum shell/wisps, lower emission, and cleanup. No game or downloaded model is required. A separate local render around the approved Gojo pose checks the full-strength appearance without changing that pose or scene.
