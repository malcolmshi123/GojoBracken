# Runtime behavior

## Bundle and attachment

The plugin loads the extensionless gojobracken bundle beside its DLL. Required asset addresses are gojo, aura, domain_expansion, and asset_credits. If the bundle is missing or invalid, the effects runner logs the reason and the vanilla Bracken stays visible. Restore the extensionless file beside GojoBracken.dll and restart if the log reports it missing.

The solo camera starts the private live runner. When a Bracken's renderer arrays are ready, the plugin validates and clones the standing model before masking the original body. The v81 filter preserves the scan node, map marker, and carried player renderer. Cleanup restores the original body masks.

## Model, aura, and voice

The model follows the enemy root and holds its baked standing pose. Both live model paths use the 3.000 m trial height with wisps and flame width/depth at 1.25× and flame height at 1.75×. The Bracken root scale can affect world height. Aura-only mode uses authored scale.

The aura follows anger with a faint baseline and a configurable saturation point. Where owner anger is unavailable, it approximates intensity from phase. Each attached Bracken has a spatial AudioSource. Entering aggro requests domain_expansion once when neither the kill nor corpse-carry overlay is active. If the bundle contains aggro_music, a second spatial source starts with the line and loops while the raw state is aggro. Its volume follows the same smoothed anger value as the aura, up to 20% source volume. Leaving aggro stops the music. Cleanup stops both sources on death or despawn.

See [game compatibility](game-compatibility.md) for the v81 state contract and regression checks.
