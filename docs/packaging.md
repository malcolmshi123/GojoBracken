# GojoBracken

Gojo follows the Bracken in a fixed standing pose. His aura grows stronger with anger, and a spatial voice clip plays when the Bracken enters aggro. This development build targets Lethal Company v81 and uses BepInEx 5.

Malcolm Shi created GojoBracken. Ashton Mak performed the Japanese aggro line.

## Install

Install BepInExPack 5.4.2305 and GojoBracken in a Lethal Company v81 profile. Keep GojoBracken.dll and the extensionless gojobracken bundle together under BepInEx/plugins. For a local ZIP, use [Import local mod](https://wiki.thunderstore.io/mods/mod-not-visible).

Launch the game once to generate BepInEx/config/FreakyFriday.GojoBracken.cfg. Close the game, set these values, then relaunch.

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

Check that the mod manager shows the portrait and installs one DLL beside the bundle. In BepInEx/LogOutput.log, look for the Gojo Bracken load message and [Effects] Development cosmetics enabled. The effects message confirms the plugin opened and validated the bundle. You can stop in the ship.

For a private candidate built with IncludeSoloConfig, confirm the imported config sets SoloLiveTest to true and check for the solo camera log as well. The 0.1.34 private candidate passed the fresh-profile import and startup check on v81. See the [development test guide](https://github.com/malcolmshi123/GojoBracken/blob/main/docs/in-game-testing.md#private-solo-bracken-attachment-test) for the controlled spawn workflow.


## Build the archive

Build the Release plugin and export the Windows x64 bundle and its ASSET-CREDITS.txt with the Unity exporter. Supply a 256 by 256 PNG icon and the directory containing both exported files.

~~~powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-ThunderstorePackage.ps1 -BundleDirectory .\artifacts\assets -IconPath $iconPath
~~~

Use -IncludeSoloConfig to put the tested solo settings in a private ZIP for a fresh test profile. The script reads the version from the DLL, writes the ZIP to dist, and prints its SHA-256 hash. The layout follows the [Thunderstore package requirements](https://wiki.thunderstore.io/mods/creating-a-package) and [BepInEx install routes](https://wiki.thunderstore.io/mods/packaging-your-mods). Before publishing, document permission to distribute the assets.
