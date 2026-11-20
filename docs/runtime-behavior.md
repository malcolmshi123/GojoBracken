# Runtime behavior

## Installation and attachment

Keep the extensionless `gojobracken` bundle beside `GojoBracken.dll`. The plugin requires `gojo`, `aura`, `domain_expansion` and `asset_credits`; `aggro_music` supplies the looping music. If the bundle is missing or invalid, attachment stops and the vanilla Bracken stays visible. Restore the matching release files and restart after a loading error.

The release config enables `DevelopmentEffects.Enabled` and `StandingModelPreview`, with `SoloLiveTest` and `RenderPreview.Enabled` false. Keep the packaged config on a fresh install because the plugin defaults are disabled. Review any custom settings when upgrading.

The normal attachment path scans loaded Brackens. Once the original renderer arrays are ready, it validates and clones the replacement, then hides the original body. The scan node, map marker and carried player renderer stay unchanged. Cleanup restores the original body masks in code; in-game cleanup and multiplayer behavior remain unverified.

## Model and effects

Gojo follows the enemy root in a fixed standing pose, with the approved hands and smooth neck join. Attachment disables the model Animator and corrects its facing. The authored model is 2.1 m tall and scales to a nominal 3 m; the enemy root scale can change its world height.

Aura width/depth stays at 1.2 and height at 1.5, regardless of preview model height. The flame shader scales height from 0.8 at zero anger to 1.0 at full anger, giving final multipliers of 1.2 and 1.5 relative to the authored flame. Wisps use the same fixed scales, while their emission follows anger. Hand poses stay fixed through every phase, and the model has no calibrated player-body grip.

The aura starts faint in living phases and grows smoothly with anger. `AngerSaturation` defaults to 12. It controls when the aura reaches full strength without limiting game anger. `AuraStrength` accepts 0 through 3; zero hides the aura but keeps voice events. If owner anger is unavailable, the plugin estimates intensity from the phase.

Each attachment has spatial voice and music sources. Entering raw aggro requests the voice once, unless a kill or corpse-carry overlay suppresses that entry. Ending an overlay cannot trigger another voice event. Optional music starts with the voice and loops until raw aggro ends. Its volume follows the smoothed aura intensity. Death or despawn stops the effects. The bundle keeps the published voice/music streams and their playback gain and balance.

## Private test modes

`SoloLiveTest` waits for the solo gameplay camera to render before attaching effects. `RenderPreview.Enabled` accepts local request files and requires development effects disabled. It feeds synthetic observations into the same assets and presentation rules, without spawning or controlling a live Bracken.

See [in-game testing](in-game-testing.md) for mode configurations, preflight and the simulated ship cycle. The [compatibility contract](game-compatibility.md) describes the supported game assembly and state mapping.
