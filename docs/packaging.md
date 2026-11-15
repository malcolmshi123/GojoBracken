# GojoBracken

Gojo holds a fixed standing pose and follows the Bracken. His aura scales with anger and a spatial voice clip plays on entry into aggro. This development build targets the inspected Lethal Company v81 and uses BepInEx 5.

GojoBracken was created by Malcolm Shi. The Japanese aggro line is performed by Ashton Mak.

## Install

Install BepInExPack 5.4.2305 and GojoBracken in a Lethal Company v81 profile. Keep GojoBracken.dll and the extensionless gojobracken bundle together under BepInEx/plugins. For a local ZIP, use [Import local mod](https://wiki.thunderstore.io/mods/mod-not-visible).

Launch once to generate BepInEx/config/FreakyFriday.GojoBracken.cfg, then close the game. Set the following values and relaunch.

~~~ini
[DevelopmentEffects]
Enabled = true
StandingModelPreview = true
SoloLiveTest = false

[RenderPreview]
Enabled = false
~~~

The standard package uses the regular attachment path. Multiplayer compatibility is unverified.

## Smoke-test a fresh install

Confirm the mod manager displays the portrait and installs one DLL beside the bundle. With the settings above, check BepInEx/LogOutput.log for Gojo Bracken loading and [Effects] Development cosmetics enabled. The latter confirms the plugin opened and validated the bundle. This check can end in the ship.

For a private candidate built with IncludeSoloConfig, confirm the imported config sets SoloLiveTest to true and check for the solo camera log as well. The 0.1.34 private candidate passed the fresh-profile import and startup check on v81. See the [development test guide](https://github.com/malcolmshi123/GojoBracken/blob/main/docs/in-game-testing.md#private-solo-bracken-attachment-test) for the controlled spawn workflow.


## Build the archive

Build the Release plugin and export the Windows x64 bundle and its ASSET-CREDITS.txt with the Unity exporter. Supply a 256 by 256 PNG icon and the directory containing both exported files.

~~~powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-ThunderstorePackage.ps1 -BundleDirectory .\artifacts\assets -IconPath $iconPath
~~~

Add -IncludeSoloConfig when building the private candidate for a fresh test profile. This option puts the tested solo configuration inside the archive. The packager writes a versioned ZIP to dist and prints its SHA-256 hash. The manifest version comes from the DLL. Package layout follows the [Thunderstore package requirements](https://wiki.thunderstore.io/mods/creating-a-package) and [BepInEx install routes](https://wiki.thunderstore.io/mods/packaging-your-mods). Use assets with documented distribution permissions before publishing.
