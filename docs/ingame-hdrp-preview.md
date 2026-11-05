# Solo in-game HDRP preview

Use this check with the inspected v81 game and Unity 2022.3.62f2. GojoBracken 0.1.29 provides the opt-in RenderPreview setting. The test bundle contains the calibrated Gojo and aura prefabs for local evaluation.

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

## Compare two standing heights

Send calm to place the default 3.000 m preview. Keep the camera and model in place. Then run:

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command height -Height 2.400
~~~

This command accepts 1.000 to 3.000 m, keeps the same horizontal placement and aura state, and realigns the scaled mesh to the original deck point. Use the same command with 3.000 to go back. Both log lines report the result. The 2.400 m setting is a visual trial.

The flame and wisps scale with the measured standing height relative to the 2.100 m authoring reference. At 3.000 m the wisps and the flame shell width/depth scale by 1.250, while the flame shell height scales by 1.750. Height changes clear wisps emitted at the old scale, so allow a moment for new particles to appear.
