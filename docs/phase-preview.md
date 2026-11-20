# Private 0.2.7 phase-cycle preview

This kit uses the current approved Gojo model, hands and neck, aura, Japanese voice and music. The DLL and bundle are copied byte for byte from the approved private 0.2.7 package. The bundle is named `gojobracken-hdrp-material-check` for the existing preview loader; the regular `gojobracken` file is left alone.

**All phases are simulated.** This displays a local standing figure and feeds synthetic observations into the presentation controller. It does not spawn or exercise a live Bracken, show a player corpse, or validate live AI, cleanup, player-kill/body alignment or multiplayer. Corpse dragging is an aura/audio state, not an animated hand grip or body attachment. The approved hands/neck stay in the baked standing pose.

## Extract and temporarily configure profiles/test

Transfer `GojoBracken-PhasePreviewKit.zip` to Downloads and extract it into a fresh folder. This is a private manual-install kit, not a Thunderstore mod ZIP. Use Git Bash. Adjust the game path and profile root below; the profile name stays `test`.

```bash
set -e
cd "$HOME/Downloads"
unzip GojoBracken-PhasePreviewKit.zip -d GojoBracken-PhasePreview
cd GojoBracken-PhasePreview
KIT="$PWD"
GAME='/d/SteamLibrary/steamapps/common/Lethal Company'
PROFILE="$HOME/AppData/Roaming/r2modmanPlus-local/LethalCompany/profiles/test"
test "$(basename "$PROFILE")" = test
test -d "$PROFILE/BepInEx/core"
powershell.exe -NoProfile -Command 'if (Get-Process -Name "Lethal Company" -ErrorAction SilentlyContinue) { exit 1 }'
mapfile -d '' DLLS < <(find "$PROFILE/BepInEx/plugins" -type f -iname GojoBracken.dll -print0)
test "${#DLLS[@]}" -eq 1
PLUGIN="$(dirname "${DLLS[0]}")"
CONFIG="$PROFILE/BepInEx/config/FreakyFriday.GojoBracken.cfg"
test ! -e "$PLUGIN/gojobracken-preview.request"
mkdir -p "$KIT/backups"
BACKUP="$KIT/backups/$(date +%Y%m%d-%H%M%S)"
mkdir "$BACKUP"
cp -p -- "$PLUGIN/GojoBracken.dll" "$BACKUP/GojoBracken.dll"
if test -e "$CONFIG"; then
  cp -p -- "$CONFIG" "$BACKUP/FreakyFriday.GojoBracken.cfg"
else
  touch "$BACKUP/config-was-absent"
fi
if test -e "$PLUGIN/gojobracken-hdrp-material-check"; then
  cp -p -- "$PLUGIN/gojobracken-hdrp-material-check" "$BACKUP/gojobracken-hdrp-material-check"
else
  touch "$BACKUP/preview-bundle-was-absent"
fi
cp -- "$KIT/payload/GojoBracken.dll" "$PLUGIN/GojoBracken.dll"
cp -- "$KIT/payload/gojobracken-hdrp-material-check" "$PLUGIN/gojobracken-hdrp-material-check"
mkdir -p "$PROFILE/BepInEx/config"
cp -- "$KIT/FreakyFriday.GojoBracken.cfg" "$CONFIG"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File "$(cygpath -w "$KIT/Test-GameProfile.ps1")" \
  -Mode RenderPreview \
  -GameDirectory "$(cygpath -w "$GAME")" \
  -ProfileDirectory "$(cygpath -w "$PROFILE")" \
  -ExpectedManifest "$(cygpath -w "$KIT/expected.json")" \
  -OutputPath "$(cygpath -w "$KIT/preflight-report.json")"
printf 'Keep this shell open. Restore backup: %s\nPlugin directory: %s\n' "$BACKUP" "$PLUGIN"
```

Stop on any failure. A zero-DLL or duplicate-DLL profile needs correction before setup; do not guess an installation folder. If installation has already started when a failure occurs, use the restore block below. The preflight requires the inspected game MVID, Unity 2022.3.62f2, exact DLL/preview-bundle hashes, preview enabled and all development/live-model flags disabled. Its report says `simulatedChecksOnly=true` and `gameplayValidated=false`.

## Run the simulated cycle

Launch `profiles/test` modded through your mod manager, host a private solo game, and stay in the ship. No debugger or spawned enemy is needed. Face a clear, level aisle with roughly four metres free and look horizontally. The log should show `[RenderPreview]` ready/solo-camera messages, with DevelopmentEffects and SoloLiveTest disabled.

In the same Git Bash shell, queue the cycle once and return to the game promptly:

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File "$(cygpath -w "$KIT/Request-HdrpPreview.ps1")" \
  -PluginDirectory "$(cygpath -w "$PLUGIN")" -Command cycle
```

After a four-second delay the stationary figure cycles through these synthetic observations:

| Phase | Anger | Expected presentation |
| --- | --- | --- |
| Hunting | 0/12 | Faint aura; no voice request |
| Retreat | 4/12 | Intermediate aura; no voice request |
| Aggro | 12/12 | Strong aura; one Japanese line and music start |
| Corpse dragging overlay on raw aggro | 6/12 | Reduced aura; no new voice; existing music may continue at reduced volume because raw state remains aggro |
| Retreat | 2/12 | Lower aura; music stops when raw aggro ends |
| Aggro again | 12/12 | Strong aura; one new line and music restart |
| Hunting | 0/12 | Aura fades down; music stops |

Non-aggro samples last about 2.5 seconds; aggro lasts at least the voice clip length plus 0.25 seconds. Expect seven `Phase sample` lines and `Phase cycle complete: 2 voice requests (imported voice clip)`. Two aggro entries request two lines total. This short cycle demonstrates music start/stop transitions, not necessarily a full music loop. If the log says synthetic tone, the kit is not loading the approved voice bundle: collect the error instead of counting it as a pass.

Record hands/neck, aura changes and audio counts. The figure remains in its standing pose throughout, including corpse dragging. A failed floor probe is a placement failure, not a phase result; retain its log. Do not queue repeated commands over a pending request or enable live effects alongside the preview.

## Clear, collect and restore

While still in the solo ship, send `clear`. This removes the local preview; it is not evidence of live-enemy cleanup.

```bash
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File "$(cygpath -w "$KIT/Request-HdrpPreview.ps1")" \
  -PluginDirectory "$(cygpath -w "$PLUGIN")" -Command clear
```

Wait for `[RenderPreview] Cleared.`, then exit the game. Retain screenshots/video and the complete log, along with the preflight report and `expected.json`. In the same shell, restore the exact files backed up before setup:

```bash
powershell.exe -NoProfile -Command 'if (Get-Process -Name "Lethal Company" -ErrorAction SilentlyContinue) { exit 1 }'
test -f "${BACKUP:?}/GojoBracken.dll"
test -d "${PLUGIN:?}"
test -d "${KIT:?}"
if test -f "$PROFILE/BepInEx/LogOutput.log"; then
  cp -- "$PROFILE/BepInEx/LogOutput.log" "$BACKUP/preview-LogOutput.log"
fi
cp -p -- "$BACKUP/GojoBracken.dll" "$PLUGIN/GojoBracken.dll"
if test -f "$BACKUP/config-was-absent"; then
  rm -f -- "$CONFIG"
else
  cp -p -- "$BACKUP/FreakyFriday.GojoBracken.cfg" "$CONFIG"
fi
if test -f "$BACKUP/preview-bundle-was-absent"; then
  rm -f -- "$PLUGIN/gojobracken-hdrp-material-check"
else
  cp -p -- "$BACKUP/gojobracken-hdrp-material-check" "$PLUGIN/gojobracken-hdrp-material-check"
fi
if test -f "$PLUGIN/gojobracken-preview.request"; then
  mv -- "$PLUGIN/gojobracken-preview.request" "$BACKUP/pending-preview.request"
fi
printf 'Restored profile files; retained backup and preview log in %s\n' "$BACKUP"
```

Keep the backup until the restored profile is checked. The regular `gojobracken` bundle and unrelated profile files are not replaced. Do not stage, commit, push or publish this private kit.
