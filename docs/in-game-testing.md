# Solo in-game HDRP preview

Use the ship preview with Lethal Company v81 and Unity 2022.3.62f2. GojoBracken 0.1.34 offers the opt-in RenderPreview setting. The test bundle contains the calibrated Gojo and aura prefabs.

With the game closed, back up any existing GojoBracken DLL and config outside the profile's plugin folder. Place the test GojoBracken.dll and gojobracken-hdrp-material-check together in the existing GojoBracken plugin directory. Keep one GojoBracken.dll in the profile. Put the supplied FreakyFriday.GojoBracken.cfg in the profile's BepInEx/config directory. It enables RenderPreview and disables DevelopmentEffects and the rig report.

Start a solo session and stay inside the ship. Stand where you have a few metres of clear space and look horizontally down the aisle. Send the calm request from Windows PowerShell 5.1 and return to the game.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command calm
~~~

The first request places Gojo about 3.5 metres ahead and lines up the lowest vertex of the posed mesh with the deck. Wait two seconds, then take a screenshot. Send full and take another screenshot from the same spot. Send clear to remove Gojo before placing him again. Look under [RenderPreview] for placement, anger, aura level, and graphics settings. If placement fails, share those lines and stop in the ship.

The preview runs on the local v81 gameplay camera when you play alone. The runner starts after that camera renders and survives scene loads. Unity can recreate it on a later camera render if it was destroyed. It clears the model when the camera or scene changes, another player joins, or the game exits. Requests sent before the camera is ready wait for the runner to poll.

Send requests from inside the ship and face a clear aisle so equipment stays out of the floor search. The preview looks for a walkable collider from 0.65 metres above to 1.05 metres below the camera's foot estimate and skips triggers. It measures the posed mesh separately, then sets it 5 mm above the chosen surface. If it finds no surface, it logs the failure and skips placement. The Floor alignment line records the collider, deck height, estimated anchor height, mesh offset, and correction.

Build the developer preview with the Release plugin and the Windows candidate bundle from Build-HdrpMaterials.ps1. The Unity fixture uses the actual bundle with a simulated camera and session. Take screenshots in Lethal Company to check the lighting, exposure, and shaders. Restore your backed-up DLL and config after testing.

For lifecycle troubleshooting, the log includes the absolute watched request path, the first poll and a heartbeat every 15 seconds, scene loads, and runner disable/destruction. Collect those lines together with the directory listing if a request remains pending.

If placement fails, save the Floor probe and Floor diagnostic lines. The wider diagnostic ray reports nearby colliders on all layers, including triggers, without using them for placement.

The Standing height line records the measured height, the requested preview height, the final height, and the scale factor. Any scale correction applies to the model before deck alignment. The default is 3.000 m, chosen from the solo ship comparison. Size relative to a live Bracken remains uncalibrated.

## Controlled slide

In a solo ship, face down a clear, level aisle. Send one walk request and switch back to the game promptly.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command walk
~~~

The walk request places a fresh 3.000 m preview at full aura. After four seconds, Gojo slides 1.2 m toward the camera that placed him. The slide takes two seconds. Stay in place and watch whether Gojo holds his standing pose and the flame and wisps follow him. Share what you saw and the Walk complete line, which reports travel and maximum aura anchor offset. This checks the visual effect from the ship. If the preview rejects the path, share the warning and stop.

## Controlled phase cycle

Face a clear aisle in the solo ship, send cycle, then return to the game promptly and stay by the preview.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command cycle
~~~

After four seconds, a stationary Gojo cycles through hunting, retreating, aggro, corpse dragging, retreating, aggro again, and hunting. Each phase lasts about two and a half seconds. The aura should be faint in zero-anger hunting, visible during retreat and corpse dragging at intermediate anger, and strongest during both aggro entries. Each aggro entry should play the selected spatial audio once. The standard preview uses a generated tone. A private preview bundle with a supplied voice clip plays that clip and holds aggro long enough to hear it. The corpse-dragging phase is an aura and audio check using simulated inputs.

Share the Phase sample and Phase cycle complete lines. Describe how the aura changed and count the tones or voice lines you heard. Expect two audio requests, both on entry into aggro, with zero in the corpse-dragging sample. Send clear to remove Gojo. Stay in the ship for this check.

## Compare two standing heights

Send calm to place the default 3.000 m preview. Keep the camera and model in place. Then run:

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command height -Height 2.400
~~~

The command accepts 1.000 to 3.000 m. It keeps the horizontal position and aura state, then realigns the mesh to the original deck point. Run it with 3.000 to restore the default. Check both log entries. Use 2.400 m as a visual comparison.

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

Use Imperium to spawn the enemy. GojoBracken reads the live enemy and changes its presentation. This check covers attachment and visible movement with the debugger active. Gameplay parity, multiplayer, killing, carrying, and voice need separate checks.

If attachment rejects mixed body layers, include every Visible body renderer line for that Bracken ID. Each line records the renderer type, layer number and name, and hierarchy path with sibling indices. The current visibility check preserves the vanilla model on rejection.

The observed v81 layout selects cached renderers on Enemies under FlowermanModel, including LOD1 and the eyes. ScanNode and MapDot (2) keep their own layers and rendering. The selected body tracks vanilla visibility and layer transitions. Unexpected layouts or ambiguous body layers restore the vanilla body.

For an unexpected renderer such as ParticleCollision, return the complete attachment warning. It includes the renderer type, full scene hierarchy path, layer, and enabled/active state. The disabled layer-15 MeshRenderer at FlowermanModel/AnimContainer/ParticleCollision is left untouched. A different layout or enabled renderer retains the vanilla fallback. Its function remains unverified.

For the movement checkpoint, use an ordinary facility and watch one Bracken move. Return the Motion sample line if it appears. It records sampled horizontal travel and model and aura anchor offsets. Check that Gojo holds his standing pose while the aura follows.
