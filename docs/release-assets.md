# Release asset credits and open items

The private mod-manager ZIP contains the assets below. The public release needs a verified source and redistribution basis for each third-party asset.

| Asset | Recorded source and changes | Release status |
| --- | --- | --- |
| Gojo model, rig, and textures | [Gojo Satoru Dance](https://sketchfab.com/models/556b63af173241d79598d62cf220671d/embed), uploaded by Ayanokoji031. The [archive inspection](model-evaluation.md#source-and-archive) records VALHAISTO GAMES in the listing description and FreeFire New Male 3d Model By PaceGaming in the model hierarchy. GojoBracken posed the rig, baked the standing prefab, and converted the materials for HDRP. | The earlier listing inspection recorded [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Confirm the source and redistribution terms for the underlying mesh and textures before using them in the public bundle. |
| Japanese aggro audio | User-supplied gojo-uses-his-domain-expansion.mp3. The private build uses the full 6.374-second edit, decoded to 44.1 kHz mono and boosted with peak limiting. | Confirm the recording source and the permission or license covering redistribution of the edited audio. |
| Aura | GojoBracken contributors. Native wisps and flame shell with HDRP materials. | Project asset. Credit GojoBracken contributors. |
| Package icon | User-supplied Gojo portrait. Resized from the square attachment to a 256 by 256 PNG for Thunderstore. Source SHA-256: 4EA96B6B71E802D4F57AED17F5DDF62D974099F1DCCE823A98EAC376DBBA8BF6. | Selected for the package. Confirm the original source and public-use permissions before uploading. |

Source archive SHA-256 for the model is B0969E72C9BD3C3D0D48BFF1DB6A9033297D8CF128552F51C66C8B355985F8A9. Source MP3 SHA-256 is DCFEB340323881F83F9014D5EFB636CE17730EBD9661611DDFB5C56377A7E9E4.

When the model and audio are cleared or replaced, copy the final attribution, source links, license or permission details, and modifications into the Unity exporter’s CREDITS.txt. The exporter embeds the same text in the bundle and writes ASSET-CREDITS.txt for the [package build](packaging.md). The repository’s MIT license covers the code.

