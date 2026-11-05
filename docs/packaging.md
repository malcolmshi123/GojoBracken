# GojoBracken

Gojo holds a fixed standing pose and follows the Bracken. His aura scales with anger and a spatial voice clip plays on entry into aggro. This development build targets the inspected Lethal Company v81 and uses BepInEx 5.

## Install for local validation

Install [BepInExPack 5.4.2305](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) in a separate test profile. Import the ZIP as a local mod, or extract its BepInEx folder into the profile. Keep GojoBracken.dll and the extensionless gojobracken bundle together under BepInEx/plugins. Launch once to generate the config, then close the game.

In BepInEx/config/FreakyFriday.GojoBracken.cfg, set the following values before relaunching.

~~~ini
[DevelopmentEffects]
Enabled = true
StandingModelPreview = true
SoloLiveTest = true

[RenderPreview]
Enabled = false
~~~

These settings enable the solo-only attachment path with the verified v81 body filter. For a controlled spawn, follow the development repository's [solo live-test instructions](https://github.com/malcolmshi123/GojoBracken/blob/main/docs/solo-live-test.md). Live movement and multiplayer compatibility still need validation.

## Check a local mod-manager install

Use a fresh Lethal Company profile with BepInExPack installed. In the mod manager settings, choose Import local mod and select the ZIP. Browse the profile folder and confirm there is one GojoBracken.dll and that gojobracken sits beside it. [Thunderstore local import instructions](https://wiki.thunderstore.io/mods/mod-not-visible)

Set the config above before starting a solo session. If the config is absent in the fresh profile, create it as FreakyFriday.GojoBracken.cfg in BepInEx/config. In the ship, look for the Gojo Bracken load line, the LiveTest solo camera line, and Effects Development cosmetics enabled in BepInEx/LogOutput.log. The last line means the runtime loader opened and validated the packaged bundle. This checkpoint ends in the ship.

## Build the archive

Build the Release plugin and export the Windows x64 bundle and its ASSET-CREDITS.txt with the Unity exporter. Supply a 256 by 256 PNG icon and the directory containing both exported files.

~~~powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-ThunderstorePackage.ps1 -BundleDirectory .\artifacts\assets -IconPath $iconPath
~~~

The packager writes a versioned ZIP to dist and prints its SHA-256 hash. The manifest version comes from the DLL. Package layout follows the [Thunderstore package requirements](https://wiki.thunderstore.io/mods/creating-a-package) and [BepInEx install routes](https://wiki.thunderstore.io/mods/packaging-your-mods). Use assets with documented distribution permissions before publishing.

