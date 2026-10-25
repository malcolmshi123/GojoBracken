# Gojo candidate evaluation

Checked 2026-10-25. This is a local model study, not an adopted release asset or an in-game replacement.

## Source and archive

- Listing: [Gojo Satoru Dance | Jujutsu Kaisen](https://sketchfab.com/3d-models/gojo-satoru-dance-jujutsu-kaisen-556b63af173241d79598d62cf220671d)
- Listed uploader: Ayanokoji031; description also names VALHAISTO GAMES.
- Listing license: [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/), as observed on the live page.
- Archive: gojo-satoru-dance-jujutsu-kaisen.zip, 4,258,935 bytes.
- SHA256: B0969E72C9BD3C3D0D48BFF1DB6A9033297D8CF128552F51C66C8B355985F8A9.
- Contents: source/gojo_menu.gltf and four PNG texture files; no bundled license or credits document.
- glTF generator: Prisma3D Pro; glTF 2.0; buffers and images are embedded in the JSON.

The hierarchy retains the name FreeFire New Male 3d Model By PaceGaming and several .rip mesh names. This is evidence of an earlier source chain, not proof of original authorship or redistribution permission. Resolve and record the underlying model credits/terms before adopting it for a public release. The listing license is recorded as a listing claim. No model binaries or textures are added to Git in this checkpoint.

## Technical inspection

- Four skinned meshes; each has 31 bone references and 31 inverse bind matrices.
- Body joints include hips, spine/chest, neck/head, arms/hands, thighs/calves/feet, toes, and twist bones. No finger articulation was found in the listed joint set.
- 37,716 exported vertex records and 12,572 triangles across the four meshes. Exported vertex records can exceed the listing's vertex count because of duplication.
- Four imported materials and embedded textures; visible white hair, dark blindfold, and dark uniform.
- One non-legacy Unity clip, Animation, lasting 3.5 seconds; 40 glTF channels targeting rotation/translation.
- Unity glTFast 6.14.0 imports an Animator. The study assigns an explicit AnimatorController, disables root motion, and uses the supplied clip only to inspect deformation.
- Importer reports embedded-buffer performance notices (EmbedSlow). Import success does not establish optimization or game readiness.

Verified in Unity 2022.3.62f2: four nonempty skins with no missing bone references/material shaders; clip sampling changes rightUpperArm rotation between 0 and 0.8 seconds; the local study renders with the approved aura. Full animation playback, skin deformation throughout the clip, bind-pose suitability, humanoid mapping, movement/attack/corpse animation adaptation, game shaders, and multiplayer integration remain pending.

## Recreate the local study

From the repository root, using Windows PowerShell 5.1:

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Prepare-LocalModel.ps1
~~~

The preparation script verifies this exact archive hash and file layout, then extracts into the Git-ignored UnityProject/Assets/LocalModelStudy directory. It refuses to overwrite an existing study.

Open UnityProject with Unity 2022.3.62f2 and allow package resolution/import to finish. Choose **GojoBracken > Create local Gojo model study** outside Play mode. The menu saves a separate scene and animation controller under LocalModelStudy; it does not overwrite the capsule study. Open **Assets/LocalModelStudy/GojoModelStudy.unity**, choose Game, and press Play.

This scene is already prepared on the development laptop. Use the normal phase/anger controls to review Gojo with the aura. The supplied dance is a rig inspection aid; it is not a Bracken behavior animation. Keep the local study outside Assets/GojoBracken/Prefabs until the asset and gameplay requirements are satisfied.
