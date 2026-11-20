# Runtime behavior

## Installation and attachment

Keep the extensionless `gojobracken` bundle beside `GojoBracken.dll`. Required bundle addresses are `gojo`, `aura`, `domain_expansion` and `asset_credits`; `aggro_music` supplies the looping music. A missing or invalid bundle disables attachment and leaves the vanilla Bracken visible. Restore the matching release files and restart after a loading error.

The release configuration enables `DevelopmentEffects.Enabled` and `StandingModelPreview`. `SoloLiveTest` and `RenderPreview.Enabled` stay false. The plugin's built-in defaults are disabled, so retain the packaged configuration on a fresh install. Review an existing custom configuration when upgrading.

The normal attachment path scans loaded Brackens. After validating the replacement and waiting for the original renderer arrays, it clones the model and hides the original body. It preserves the scan node, map marker and carried player renderer. Cleanup is implemented to restore the original body masks. These implementation paths do not establish that cleanup or multiplayer behavior has been validated in game.

## Model and effects

Gojo follows the enemy root in a fixed standing pose with the approved hands and continuous neck. The attachment disables the model Animator and applies a facing correction. It scales the authored 2.1 m model to a nominal 3 m height; the enemy root scale can affect world height. All aura geometry uses fixed width/depth multipliers of 1.2 and a height multiplier of 1.5, independent of preview model height. The flame shader interpolates height from 0.8 at zero anger to 1.0 at full anger, producing final height multipliers of 1.2 and 1.5 relative to the authored flame. Wisps inherit the same width/depth and height scales; their emission still follows anger. There are no phase-specific hand poses or calibrated player-body grip.

The aura remains faint in living phases and grows smoothly with anger. `AngerSaturation` defaults to 12, a cosmetic saturation threshold rather than a game anger limit. `AuraStrength` accepts 0 through 3; zero hides the aura while retaining voice events. When owner anger is unavailable, intensity uses a phase approximation.

Each attachment has spatial voice and music sources. Entry into raw aggro requests the voice once unless a kill or corpse-carry overlay suppresses that entry. Ending an overlay does not generate a new aggro entry. Optional music starts with the voice and loops while raw aggro continues; its volume follows the smoothed aura intensity. Death or despawn stops the effects. The approved bundle retains the published voice/music streams and the existing playback gain and balance.

## Private test modes

`SoloLiveTest` delays attachment until the solo gameplay camera renders. `RenderPreview.Enabled` instead accepts local request files and requires development effects disabled. The preview uses the same assets and presentation rules with synthetic observations; it does not spawn or control a live Bracken.

See [in-game testing](in-game-testing.md) for mode configurations, preflight and the simulated ship cycle. The [compatibility contract](game-compatibility.md) describes the supported game assembly and state mapping.
