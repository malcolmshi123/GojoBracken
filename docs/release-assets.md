# Release asset credits and open items

The private mod-manager ZIP contains the assets below. The public release needs a verified source and redistribution basis for each third-party asset.

| Asset | Recorded source and changes | Release status |
| --- | --- | --- |
| Gojo model, rig, and textures | [Gojo Satoru Dance](https://sketchfab.com/models/556b63af173241d79598d62cf220671d/embed), uploaded by Ayanokoji031. The [archive inspection](../archive/docs/model-evaluation.md#source-and-archive) records VALHAISTO GAMES in the listing description and FreeFire New Male 3d Model By PaceGaming in the model hierarchy. GojoBracken posed the rig, baked the standing prefab, and converted the materials for HDRP. | The [Sketchfab API listing](https://api.sketchfab.com/v3/models/556b63af173241d79598d62cf220671d) confirms the upload is marked [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The source and redistribution terms for the underlying mesh and textures remain unverified. |
| Japanese aggro audio | Original performance by `the_greymouse`, supplied as ryoiki_tenkai_muryokusho.mp3. Converted to mono and boosted with peak limiting. | Permission to redistribute the edited recording in the public mod remains to be confirmed with the performer. |
| Aura | GojoBracken contributors. Native wisps and flame shell with HDRP materials. | Project asset. Credit GojoBracken contributors. |
| Package icon | User-supplied gojo.jpg, cropped to a square and resized to a 256 by 256 PNG for Thunderstore. Source SHA-256: 27192FF5D96C14C25161594B700A62A88E37F04FDCB2FF17F1F2FFC1F89EB60D. | Selected for the private package. The original source and public-use permission are unknown. |

Source archive SHA-256 for the model is B0969E72C9BD3C3D0D48BFF1DB6A9033297D8CF128552F51C66C8B355985F8A9. Current voice source SHA-256 is 80E2F04FFB0058B7EB46A0EB4E727392660FCD7370F6EDD807F09C4296610E36.

When the model, audio, and icon are cleared or replaced, copy the final attribution, source links, license or permission details, and modifications into the Unity exporter’s CREDITS.txt. The exporter embeds the same text in the bundle and writes ASSET-CREDITS.txt for the [package build](packaging.md). The repository’s MIT license covers the code.
