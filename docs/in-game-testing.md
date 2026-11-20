# In-game testing

## Current validation status

Release acceptance covers the approved Blender model and the in-ship simulated phase cycle. Further live solo and multiplayer checks were waived for package preparation; they remain unverified. Do not describe the cycle as proof of live AI, cleanup, player-kill/body alignment or multiplayer compatibility.

The current release uses the fixed standing model in every state. It does not provide the older procedural reach/drag poses or a calibrated corpse grip. Historical step-by-step handoffs are archived under archive/docs/blender-handoffs; this is the maintained guide.

## Configuration modes

Close the game before changing configuration. Use a separate profiles/test profile and a disposable save. Back up its GojoBracken DLL, bundle and FreakyFriday.GojoBracken.cfg outside the plugin directory. Keep exactly one GojoBracken.dll in the profile.

| Preflight mode | Config under packaging/ | Effects | Standing model | SoloLiveTest | RenderPreview |
| --- | --- | --- | --- | --- | --- |
| Multiplayer (normal release path) | release/ | true | true | false | false |
| LiveEnemy (private solo runner) | solo/ | true | true | true | false |
| RenderPreview (stationary simulation) | preview/ | false | false | false | true |

The normal release config also works in solo play. Multiplayer names the general attachment preflight configuration, not a claim that networking has been tested. Test-GameProfile.ps1 verifies the installed DLL/bundle hashes, pinned Unity player/MVID, single plugin copy and selected config. It does not launch or validate the game.

From Git Bash, set GAME and the actual test profile folder from Thunderstore Mod Manager, then select one mode. Do not assume an r2modman profile location:

```bash
GAME='/d/SteamLibrary/steamapps/common/Lethal Company'
PROFILE='/c/replace/with/Thunderstore/profiles/test'
MODE=Multiplayer
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Test-GameProfile.ps1 -Mode "$MODE" \
  -GameDirectory "$(cygpath -w "$GAME")" \
  -ProfileDirectory "$(cygpath -w "$PROFILE")" \
  -ExpectedManifest "$(cygpath -w "$PWD/dist/GojoBracken-0.3.0.json")"
```

The expected manifest is produced by the release package verifier. Add -OutputPath to select a report location. Stop on failure; do not bypass the version guard. Reports always retain gameplayValidated=false; simulatedChecksOnly=true identifies RenderPreview.

## In-ship simulated cycle

Copy the release DLL and the same complete bundle into the test plugin directory, naming the preview copy gojobracken-hdrp-material-check. Preserve the regular gojobracken file and any earlier preview copy in your backup. Apply packaging/preview/FreakyFriday.GojoBracken.cfg, then preflight with Mode=RenderPreview. Host a private solo game, stay in the ship, and face a clear level aisle with roughly four metres free.

```bash
PLUGIN='/path/to/profiles/test/BepInEx/plugins/GojoBracken'
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Request-HdrpPreview.ps1 \
  -PluginDirectory "$(cygpath -w "$PLUGIN")" -Command cycle
```

After four seconds, the local standing figure simulates hunting, retreat, aggro, corpse dragging on raw aggro, retreat, aggro again and hunting. Expect seven Phase sample lines and two voice requests using the imported clip. Aura responds to the synthetic anger; each aggro entry starts voice/music. Corpse dragging triggers no new voice, and existing music may continue while raw aggro remains active. Music stops when raw aggro ends. The short cycle need not include a full music loop. No live enemy or player corpse is involved.

Use the same request command with -Command clear before exiting. This removes the local preview; it is not a live-enemy cleanup test. Restore the exact backed-up profile files with the game closed, removing only newly introduced preview/config files that were originally absent. Preserve any pending request with the results before restoring normal play.

## Silent full-aura screenshot

With a fresh preview session, use the same request command with `-Command full`. It places stationary Gojo at maximum aura with a silent fixture and no voice/music loaded. Use `clear` and wait for it to be consumed before placing a fresh preview if a cycle is already running. The fixed aura width/depth scale is 1.2 and height scale is 1.5; the main flame height is 1.2 at zero anger and 1.5 at full anger relative to its authored size. Capture manually, then clear and restore the profile as above. Existing game ambience is unaffected.

## Optional live and multiplayer checks

These checks remain unverified for the current model unless new evidence is collected:

- Live encounter: use LiveEnemy or the normal release path, keep AI enabled, and observe model/floor alignment, movement, natural phase transitions, aura/audio and cleanup after enemy removal/scene changes. Record debugger overrides.
- Kill/carry: keep an observer alive and let another participant be the victim in a disposable private game. Observe actual corpse handling and visual alignment rather than forced carrying flags. A fixed hand pose can still miss the body.
- Multiplayer: compare modded host/vanilla client and vanilla host/modded client. Vanilla should retain ordinary Bracken visuals; cosmetics are local. Non-owner anger uses phase approximation, so matching aura intensity frame for frame is not required.

Collect the full BepInEx LogOutput.log, preflight and candidate JSON reports, screenshots/video and pass/fail/not-observed notes for each case. Preserve the original AI, colliders, Animator and corpse handling; do not turn simulated observations or a successful join into a gameplay-parity claim.
