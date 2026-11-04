# Solo in-game HDRP preview

Use this check with the inspected v81 game and Unity 2022.3.62f2. GojoBracken 0.1.12 provides the opt-in RenderPreview setting. The test bundle contains the calibrated Gojo and aura prefabs for local evaluation.

With the game closed, back up any existing GojoBracken DLL and config outside the profile's plugin folder. Place the test GojoBracken.dll and gojobracken-hdrp-material-check together in the existing GojoBracken plugin directory. Keep one GojoBracken.dll in the profile. Put the supplied FreakyFriday.GojoBracken.cfg in the profile's BepInEx/config directory. It enables RenderPreview and disables DevelopmentEffects and the rig report.

Start a solo session and stay inside the ship. Stand where you have a few metres of clear space and look horizontally down the aisle. Send the calm request from Windows PowerShell 5.1 and return to the game.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Request-HdrpPreview.ps1 -PluginDirectory $pluginDirectory -Command calm
~~~

The first request places a world-space model roughly 3.5 metres ahead at standing height. Wait two seconds and take a screenshot. Send full and take a second screenshot from the same spot. Send clear to remove the preview or reposition before placing it again. The console log shows placement, anger, aura level, and graphics settings under [RenderPreview]. If placement fails, share the log lines and stop there. No landing or encounter is needed.

The preview uses the v81 local gameplay camera and requires zero other connected players. It is a local visual object. The solo gameplay camera must render before the preview runner starts. Unity can recreate the runner on a later camera render if it is destroyed. The separate preview runner persists across scene loads and clears the model on camera or scene changes, another player joining, or game exit. A request sent before the solo camera is ready stays queued until the runner can poll. Send requests from the ship so placement uses your intended view. The model height is relative to your camera, so placement depends on the spot you choose.

The developer build is produced from the Release plugin and the Windows candidate bundle from Build-HdrpMaterials.ps1. The Unity fixture uses simulated camera/session state and the actual candidate bundle. Lethal Company's lighting, exposure, and runtime shader compatibility need the screenshots from this check. Restore your backed-up DLL and config after testing.

For lifecycle troubleshooting, the log includes the absolute watched request path, the first poll and a heartbeat every 15 seconds, scene loads, and runner disable/destruction. Collect those lines together with the directory listing if a request remains pending.
