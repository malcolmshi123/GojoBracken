# Private solo Bracken attachment test

GojoBracken 0.1.29 starts the opt-in live test after the solo gameplay camera renders. It uses the approved 3.000 m preview size as a trial value, with 1.250× wisps and a flame shell scaled 1.250× across and 1.750× vertically. The model follows the actual enemy root, so fit, facing, and foot placement still need visual review on a live Bracken.

## Prepare the build

Use the supplied private test ZIP, or build the Release DLL and run the following with the calibrated HDRP candidate project.

~~~powershell
dotnet build .\src\GojoBracken\GojoBracken.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-SoloLiveTestBundle.ps1 -CandidateProject .\artifacts\hdrp\<run>\u
~~~

The default bundle includes the Gojo and aura prefabs, provenance notes, and a generated silent test clip. An optional private build accepts a local Japanese voice clip. Release asset clearance remains pending.

## Build with a local voice clip

Use a trimmed mono WAV, up to ten seconds, containing the Japanese line. Keep the WAV and its source note outside the checkout. In the note, record the episode or recording, language, time range, edits, and permission or license details. The official source reference is [Jujutsu Kaisen episode 7, “Assault”](https://www.crunchyroll.com/watch/GY1QKKMZR/assault).

With the HDRP candidate project already built, pass its u directory and the two local files.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-SoloLiveTestBundle.ps1 -CandidateProject $candidateProject -VoiceWav $voiceWav -VoiceCredits $voiceCredits
~~~

The builder exports live-test-voice-bundle/gojobracken next to the candidate project. For a private solo ship check, copy that file beside the plugin as gojobracken-hdrp-material-check, use the preview config, and send cycle once. The log identifies imported voice clip as the audio source. Listen for the full line once on each of the two aggro entries. The live test uses the same bundle under its original gojobracken filename.

With the game closed, use a separate solo test profile and disposable save. Back up the profile's existing GojoBracken DLL and configuration. Put GojoBracken.dll and gojobracken together in the GojoBracken plugin folder, with one DLL copy. Put the supplied FreakyFriday.GojoBracken.cfg in BepInEx/config. It enables DevelopmentEffects, StandingModelPreview, and SoloLiveTest. RenderPreview stays disabled. No request file is needed.

Install Imperium 1.3.0 with its dependencies in that test profile. That version explicitly supports v81 and includes an indoor Bracken spawning fix. [Imperium changelog](https://thunderstore.io/c/lethal-company/p/giosuel/Imperium/changelog/)

## First live checkpoint

1. Start the solo ship session. Check LogOutput.log for Gojo Bracken 0.1.29, LiveTest solo camera, and Effects development cosmetics enabled. If any are missing, send the log and stop in the ship.
2. Enable God Mode in Imperium's player settings. Land on a moon, enter the facility, and use Imperium's F2 spawn interface to spawn one Bracken in a clear interior spot. Use the entity selector and leave the AI enabled for the movement check. [Imperium controls](https://github.com/giosuel/imperium#interface-navigation)
3. Watch briefly for one Gojo replacing the Bracken, feet near the floor, plausible facing and movement, and an aura that follows. Take one screenshot if convenient. Return the Startup effective-config line, LiveTest scene and lifecycle lines, Effects live scans from before and after landing, and any candidate, attachment, or failure lines with your observations. Exit the test after this first check.

Imperium supplies the controlled spawn. GojoBracken only reads and adapts the live enemy's presentation. This run tests attachment and visible behavior with the debugger active. Gameplay parity, multiplayer, killing, carrying, and voice remain separate checks.

If attachment rejects mixed body layers, include every Visible body renderer line for that Bracken ID. Each line records the renderer type, layer number and name, and hierarchy path with sibling indices. The current visibility check preserves the vanilla model on rejection.

The observed v81 layout selects cached renderers on Enemies under FlowermanModel, including LOD1 and the eyes. ScanNode and MapDot (2) keep their own layers and rendering. The selected body tracks vanilla visibility and layer transitions. Unexpected layouts or ambiguous body layers restore the vanilla body.

For an unexpected renderer such as ParticleCollision, return the complete attachment warning. It includes the renderer type, full scene hierarchy path, layer, and enabled/active state. The disabled layer-15 MeshRenderer at FlowermanModel/AnimContainer/ParticleCollision is left untouched. A different layout or enabled renderer retains the vanilla fallback. Its function remains unverified.

For the movement checkpoint, use an ordinary facility and watch one Bracken move. Return the Motion sample line if it appears. It records sampled horizontal travel and model and aura anchor offsets. Check that Gojo holds his standing pose while the aura follows.
