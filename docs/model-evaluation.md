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
- Unity glTFast 6.14.0 imports an Animator. The earlier rig check sampled the dance clip. The current study leaves the Animator disabled, clears its controller, and disables root motion; it does not play the supplied dance.
- Importer reports embedded-buffer performance notices (EmbedSlow). Import success does not establish optimization or game readiness.

Verified in Unity 2022.3.62f2: four nonempty skins with no missing bone references/material shaders; clip sampling changes rightUpperArm rotation between 0 and 0.8 seconds; the local study renders with the approved aura. Full animation playback, skin deformation throughout the clip, bind-pose suitability, humanoid mapping, movement/attack/corpse animation adaptation, game shaders, and multiplayer integration remain pending.

## Recreate the local study

From the repository root, using Windows PowerShell 5.1:

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Prepare-LocalModel.ps1
~~~

The preparation script verifies this exact archive hash and file layout, then extracts into the Git-ignored UnityProject/Assets/LocalModelStudy directory. It refuses to overwrite an existing study.

Open UnityProject with Unity 2022.3.62f2 and allow package resolution/import to finish. Choose **GojoBracken > Create local Gojo idle study** outside Play mode. The menu reconstructs the source bind pose, straightens the spine and both legs, relaxes the arms, fits the skinned geometry, then aligns the actual shoe-sole surfaces and grounds both feet, and saves a separate idle scene under LocalModelStudy; it does not overwrite the capsule study. Open **Assets/LocalModelStudy/GojoIdleStudy.unity**, choose Game, and press Play.

This idle scene is already prepared on the development laptop. Use the normal phase/anger controls to review Gojo standing still with the aura. Unity compilation and the standing render passed; the generator checks both leg chains point down. The imported source bind pose itself has a raised leg, so disabling animation alone is insufficient. The idle pose is an authoring adjustment; it does not modify mesh weights or claim a completed humanoid retarget.

Intended game behavior: Gojo stands still when the Bracken is stationary, follows the position/rotation controlled by the Bracken AI, and adapts locomotion, attack, kill, and corpse-handling poses to the Bracken. The source dance must not drive gameplay movement. Actual game transform following and skeleton/animation adaptation remain pending. Keep the local study outside Assets/GojoBracken/Prefabs until the asset and gameplay requirements are satisfied.


Posture correction: choose **GojoBracken > Refresh local Gojo idle pose** outside Play mode to update the existing idle scene. This uses baked shoe geometry to estimate the soles, rather than assuming foot-to-toe bone direction matches the shoe bottom. Small leg-height corrections are limited to 2 cm; larger errors fail validation. The isolated Unity check measured 0 degrees hip-to-head lean, less than 0.1 degrees sole tilt, and both soles within 1 mm of the floor. Front and side renders were inspected, including a side view with the aura hidden.


Scale correction: the earlier baked-mesh measurement counted hierarchy scale twice. Repeated refreshes could alternate model sizes. Both geometry measurement paths now bake with explicit scale handling before conversion into world coordinates. The model is fitted to 2.1 world units for the study, alongside the 3.0-unit maximum flame envelope. This preview height is not a calibrated in-game Bracken replacement size. A batch-only VerifyIdleStudy regression checks three repeated refreshes, stable scale and floor contact, and a maximum flame tip above the model.


Relaxed posture revision: forcing every spine segment vertically left an exaggerated arch in the visible source skin. The pose now uses small pelvis/spine compensations and brings the upper body forward relative to the ankles. This replaces the earlier zero-degree bone-marker target; a small head-to-hip bone offset is intentional. Feet, 2.1-unit preview height, disabled dance, and the existing aura remain part of the regression check. Apply through the same Refresh local Gojo idle pose menu.


Lower-back refinement: the forward offset now starts at the chest rather than the waist, reducing the lumbar curve while preserving the approved upper-back placement. An isolated before/after check found unchanged chest/shoulder/neck/head rotations and less than 1 mm change in their positions relative to the pelvis. The three-refresh scale, foot-contact, and maximum-aura checks passed again.


Neck-only refinement: the cervical joint receives a forward pitch compensation for the source skin. The head is counter-rotated to keep the face level and its height is retained. Torso validation now uses the hip-to-neck-base direction, so an intentional head/neck adjustment does not masquerade as a changed back pose. Pelvis, waist/chest compensation, leg setup, and sole alignment are unchanged.
