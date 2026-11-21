# In-game testing

## Current validation status

The Blender model and in-ship simulated phase cycle have been approved for release preparation. Further live solo and multiplayer checks were waived and remain unverified. The cycle does not test live AI, cleanup, player-kill/body alignment or multiplayer compatibility.

Gojo holds the same standing pose in every state. The release has no procedural reach/drag poses or calibrated corpse grip. Earlier step-by-step handoffs are under archive/docs/blender-handoffs.

## Configuration modes

Close the game before changing configuration. Use a separate profiles/test profile and a disposable save. Back up its GojoBracken DLL, bundle and FreakyFriday.GojoBracken.cfg outside the plugin directory. Keep exactly one GojoBracken.dll in the profile.

| Preflight mode | Config under packaging/ | Effects | Standing model | SoloLiveTest | RenderPreview |
| --- | --- | --- | --- | --- | --- |
| Multiplayer (normal release path) | release/ | true | true | false | false |
| LiveEnemy (private solo runner) | solo/ | true | true | true | false |
| RenderPreview (stationary simulation) | preview/ | false | false | false | true |

The normal release config works in solo play too. Multiplayer is the preflight name for general attachment; networking remains untested. Test-GameProfile.ps1 checks the installed DLL/bundle hashes, pinned Unity player/MVID, plugin copy count and selected config. It does not launch the game or check gameplay.

In Git Bash, set GAME and the actual test profile folder from Thunderstore Mod Manager, then choose a mode. Use your Thunderstore folder instead of an assumed r2modman location:

```bash
GAME='/d/SteamLibrary/steamapps/common/Lethal Company'
PROFILE='/c/replace/with/Thunderstore/profiles/test'
MODE=Multiplayer
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Test-GameProfile.ps1 -Mode "$MODE" \
  -GameDirectory "$(cygpath -w "$GAME")" \
  -ProfileDirectory "$(cygpath -w "$PROFILE")" \
  -ExpectedManifest "$(cygpath -w "$PWD/dist/GojoBracken-0.3.3.json")"
```

Use the expected manifest from the release package verifier. Add -OutputPath to choose where the report is saved. Stop if preflight fails and keep the version guard enabled. Reports always have gameplayValidated=false; simulatedChecksOnly=true marks RenderPreview.

## In-ship simulated cycle

Copy the release DLL and complete bundle into the test plugin directory. Name the preview bundle gojobracken-hdrp-material-check. Back up the regular gojobracken file and any existing preview copy. Apply packaging/preview/FreakyFriday.GojoBracken.cfg, then preflight with Mode=RenderPreview. Host a private solo game and stay in the ship. Face a clear, level aisle with about four metres free.

```bash
PLUGIN='/path/to/profiles/test/BepInEx/plugins/GojoBracken'
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File scripts/Request-HdrpPreview.ps1 \
  -PluginDirectory "$(cygpath -w "$PLUGIN")" -Command cycle
```

After four seconds, the standing figure cycles through hunting, retreat, aggro, corpse dragging on raw aggro, retreat, aggro again and hunting.

Expect seven Phase sample lines and two requests for the imported voice clip. The aura follows synthetic anger, and each aggro entry starts voice/music. Corpse dragging requests no new voice. Music may continue through that overlay while raw aggro remains active, then stops when raw aggro ends. The short cycle may end before a full music loop plays. There is no live enemy or player corpse in this check.

Send -Command clear before exiting to remove the local preview. Clearing it does not test live-enemy cleanup. Close the game and restore the exact backed-up profile files. Remove newly introduced preview/config files only if they were absent before setup. Save any pending request with the results before returning to normal play.

## Silent full-aura screenshot

Start a fresh preview with `-Command full` for stationary Gojo at maximum aura. It uses a silent fixture and loads no voice/music. If a cycle is running, send `clear` and return to the game until it consumes the request before placing Gojo again. Fixed aura width/depth is 1.2 and height is 1.5. The main flame height is 1.2 at zero anger and 1.5 at full anger, relative to its authored size. Take the screenshot yourself, then clear and restore the profile as above. Game ambience stays unchanged.

## Optional live and multiplayer checks

The following checks remain unverified for the current model:

- Live encounter: use LiveEnemy or the normal release path with AI enabled. Watch model/floor alignment, movement, natural phase transitions, aura/audio and cleanup after enemy removal or scene changes. Record debugger overrides.
- Kill/carry: keep an observer alive and let another participant be the victim in a disposable private game. Watch actual corpse handling and visual alignment without forcing carrying flags. The fixed hand pose can miss the body.
- Multiplayer: compare modded host/vanilla client and vanilla host/modded client. Vanilla players should see ordinary Brackens because the cosmetics are local. Non-owner anger uses a phase estimate, so aura intensity can differ between clients.

Save the full BepInEx LogOutput.log, preflight and candidate JSON reports, screenshots/video, and pass/fail/not-observed notes for each case. Leave the original AI, colliders, Animator and corpse handling unchanged. A simulated check or successful join does not establish gameplay parity.
