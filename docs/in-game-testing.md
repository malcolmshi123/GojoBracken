# Solo in-game HDRP preview

Use this check with the inspected v81 game and Unity 2022.3.62f2. GojoBracken 0.1.34 provides the opt-in RenderPreview setting. The test bundle contains the calibrated Gojo and aura prefabs for local evaluation.

With the game closed, back up any existing GojoBracken DLL and config outside the profile's plugin folder. Place the test GojoBracken.dll and gojobracken-hdrp-material-check together in the existing GojoBracken plugin directory. Keep one GojoBracken.dll in the profile. Put the supplied FreakyFriday.GojoBracken.cfg in the profile's BepInEx/config directory. It enables RenderPreview and disables DevelopmentEffects and the rig report.

Start a solo session and stay inside the ship. Stand where you have a few metres of clear space and look horizontally down the aisle. Send the calm request from Windows PowerShell 5.1 and return to the game.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command calm
~~~

The first request places a world-space model roughly 3.5 metres ahead and aligns the lowest posed mesh vertex to the deck. Wait two seconds and take a screenshot. Send full and take a second screenshot from the same spot. Send clear to remove the preview or reposition before placing it again. The console log shows placement, anger, aura level, and graphics settings under [RenderPreview]. If placement fails, share the log lines and stop there. No landing or encounter is needed.

The preview uses the v81 local gameplay camera and requires zero other connected players. It is a local visual object. The solo gameplay camera must render before the preview runner starts. Unity can recreate the runner on a later camera render if it is destroyed. The separate preview runner persists across scene loads and clears the model on camera or scene changes, another player joining, or game exit. A request sent before the solo camera is ready stays queued until the runner can poll. Send requests from the ship so placement uses your intended view. The preview searches from 0.65 metres above to 1.05 metres below the camera-based foot estimate for a walkable collider, ignoring triggers. It measures the posed mesh separately and places it 5 mm above the chosen surface. Face a clear aisle so nearby equipment does not intercept the search. If no surface is found, the request reports the problem without placing Gojo. The Floor alignment log records the collider, deck height, estimated anchor height, mesh offset, and applied correction.

The developer build is produced from the Release plugin and the Windows candidate bundle from Build-HdrpMaterials.ps1. The Unity fixture uses simulated camera/session state and the actual candidate bundle. Lethal Company's lighting, exposure, and runtime shader compatibility need the screenshots from this check. Restore your backed-up DLL and config after testing.

For lifecycle troubleshooting, the log includes the absolute watched request path, the first poll and a heartbeat every 15 seconds, scene loads, and runner disable/destruction. Collect those lines together with the directory listing if a request remains pending.

If placement fails, save the Floor probe and Floor diagnostic lines. The wider diagnostic ray reports nearby colliders on all layers, including triggers, without using them for placement.

The Standing height line records the measured height, the requested preview height, the final height, and the scale factor. Any scale correction applies to the model before deck alignment. The default is 3.000 m, chosen from the solo ship comparison. Size relative to a live Bracken remains uncalibrated.

## Controlled slide

In a solo ship, face down a clear, level aisle. Send one walk request and switch back to the game promptly.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command walk
~~~

The request places a fresh 3.000 m preview with full aura. After a four-second countdown, Gojo slides 1.2 m toward the placement camera over two seconds while holding his upright standing pose. Stay in place and watch that the pose holds, the flame shell moves with Gojo, and the wisps look attached while moving. Share the Walk complete line and what you saw. The line reports model travel and maximum aura anchor offset. The game’s AI and navigation are outside this test. If the preview rejects the path, share the warning and end the checkpoint. No landing or Bracken spawn is needed.

## Controlled phase cycle

Face a clear aisle in the solo ship, send cycle, then return to the game promptly and stay by the preview.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command cycle
~~~

After four seconds, a stationary Gojo cycles through hunting, retreating, aggro, corpse dragging, retreating, aggro again, and hunting. Each phase lasts about two and a half seconds. The aura should be faint in zero-anger hunting, visible during retreat and corpse dragging at intermediate anger, and strongest during both aggro entries. Each aggro entry should play the selected spatial audio once. The standard preview uses a generated tone. A private preview bundle with a supplied voice clip plays that clip and holds aggro long enough to hear it. The corpse-dragging phase is an aura and audio check using simulated inputs.

Share the Phase sample and Phase cycle complete lines, how the aura changed, and how many tones you heard. Expect two audio requests total and zero in the corpse-dragging sample. Clear removes the preview. No landing or spawn is needed.

## Compare two standing heights

Send calm to place the default 3.000 m preview. Keep the camera and model in place. Then run:

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command height -Height 2.400
~~~

This command accepts 1.000 to 3.000 m, keeps the same horizontal placement and aura state, and realigns the scaled mesh to the original deck point. Use the same command with 3.000 to go back. Both log lines report the result. The 2.400 m setting is a visual trial.

The flame and wisps scale with the measured standing height relative to the 2.100 m authoring reference. At 3.000 m the wisps and the flame shell width/depth scale by 1.250, while the flame shell height scales by 1.750. Height changes clear wisps emitted at the old scale, so allow a moment for new particles to appear.

## Private solo Bracken attachment test

The plugin starts the opt-in live test after the solo gameplay camera renders. It uses the approved 3.000 m preview size as a trial value, with 1.250× wisps and a flame shell scaled 1.250× across and 1.750× vertically. The model follows the actual enemy root, so fit, facing, and foot placement still need visual review on a live Bracken.

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

1. Start the solo ship session. Check LogOutput.log for the Gojo Bracken load line, LiveTest solo camera, and Effects development cosmetics enabled. If any are missing, send the log and stop in the ship.
2. Enable God Mode in Imperium's player settings. Land on a moon, enter the facility, and use Imperium's F2 spawn interface to spawn one Bracken in a clear interior spot. Use the entity selector and leave the AI enabled for the movement check. [Imperium controls](https://github.com/giosuel/imperium#interface-navigation)
3. Watch briefly for one Gojo replacing the Bracken, feet near the floor, plausible facing and movement, and an aura that follows. Take one screenshot if convenient. Return the Startup effective-config line, LiveTest scene and lifecycle lines, Effects live scans from before and after landing, and any candidate, attachment, or failure lines with your observations. Exit the test after this first check.

Imperium supplies the controlled spawn. GojoBracken only reads and adapts the live enemy's presentation. This run tests attachment and visible behavior with the debugger active. Gameplay parity, multiplayer, killing, carrying, and voice remain separate checks.

If attachment rejects mixed body layers, include every Visible body renderer line for that Bracken ID. Each line records the renderer type, layer number and name, and hierarchy path with sibling indices. The current visibility check preserves the vanilla model on rejection.

The observed v81 layout selects cached renderers on Enemies under FlowermanModel, including LOD1 and the eyes. ScanNode and MapDot (2) keep their own layers and rendering. The selected body tracks vanilla visibility and layer transitions. Unexpected layouts or ambiguous body layers restore the vanilla body.

For an unexpected renderer such as ParticleCollision, return the complete attachment warning. It includes the renderer type, full scene hierarchy path, layer, and enabled/active state. The disabled layer-15 MeshRenderer at FlowermanModel/AnimContainer/ParticleCollision is left untouched. A different layout or enabled renderer retains the vanilla fallback. Its function remains unverified.

For the movement checkpoint, use an ordinary facility and watch one Bracken move. Return the Motion sample line if it appears. It records sampled horizontal travel and model and aura anchor offsets. Check that Gojo holds his standing pose while the aura follows.
