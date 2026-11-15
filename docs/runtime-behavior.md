# Runtime behavior

## Bundle and attachment

Keep the extensionless gojobracken bundle beside GojoBracken.dll. The plugin requires four asset addresses: gojo, aura, domain_expansion, and asset_credits. If the bundle is missing or invalid, the plugin logs the reason and keeps the vanilla Bracken visible. If the log says the bundle is missing, restore the file and restart.

The private live test starts when the solo camera renders. Once a Bracken's renderer arrays are ready, the plugin validates and clones the standing model, then hides the original body. The v81 filter leaves the scan node, map marker, and carried player renderer alone. On cleanup, the plugin restores the original body masks.

## Model, aura, and voice

The model follows the enemy root and holds its baked standing pose. Both live model paths use the 3.000 m trial height with wisps and flame width/depth at 1.25× and flame height at 1.75×. The Bracken root scale can affect world height. Aura-only mode uses authored scale.

The aura starts faint and grows with anger up to a configurable threshold. If owner anger is unavailable, the plugin estimates intensity from the phase.

Each attached Bracken has a spatial AudioSource. When the Bracken enters aggro, it requests domain_expansion once, provided neither the kill nor the corpse-carry overlay is active. If the bundle includes aggro_music, a second spatial source starts with the line and loops until the raw state leaves aggro. Its volume follows the same smoothed anger value as the aura. Death or despawn stops both sources.

At full anger, the private comparison bundle targets −10 dBFS RMS for the voice and −15 dBFS RMS for the music over the 5.09-second overlap, before spatial attenuation. UniformVoiceBoost doubles each voice sample during playback and applies only to the voice source.

See [game compatibility](game-compatibility.md) for the v81 state contract and regression checks.
