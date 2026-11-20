# Approved model: solo behavior checkpoint

Use the current private `GojoBracken-GameTestKit.zip`. The model has Malcolm's visual approval. This checkpoint checks behavior on the testing PC; it does not change the model, runtime, or game compatibility guard.

The model retains its baked standing pose during movement, attacks and carrying. Do not expect the older procedural kill/reach or corpse-drag arm poses described in historical increment notes. Record actual corpse alignment separately from the standing pose's appearance.

## Setup and file check

Close the game. Use the existing isolated solo profile and a disposable save. Extract the updated kit into a new folder. Import its inner `GojoBracken-LocalTest.zip` only if the profile does not already contain the exact candidate. Keep one `GojoBracken.dll` with its extensionless `gojobracken` bundle beside it, and the supplied solo configuration. Read `expected.json` for the candidate hashes.

Run these commands from the extracted kit directory in Git Bash. Replace both paths with the actual directories. The command creates a separate result directory for this run; retain it after testing.

```bash
GAME='/d/SteamLibrary/steamapps/common/Lethal Company'
PROFILE="$HOME/AppData/Roaming/r2modmanPlus-local/LethalCompany/profiles/Gojo Model Test"
RESULTS="$PWD/results/$(date +%Y%m%d-%H%M%S)"
mkdir -p "$RESULTS"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned \
  -File "$(cygpath -w "$PWD/Test-GameProfile.ps1")" \
  -GameDirectory "$(cygpath -w "$GAME")" \
  -ProfileDirectory "$(cygpath -w "$PROFILE")" \
  -OutputPath "$(cygpath -w "$RESULTS/preflight-report.json")"
```

Stop if preflight fails. Launch the profile modded through the mod manager only after it passes. A passing report verifies files/configuration, not gameplay. Keep AI enabled. An already-working, game-compatible spawn/debug tool can make encounters repeatable; record any invulnerability, AI overrides or forced states used. Do not install a guessed debugger version for this checkpoint.

## Observe in order

| Check | Observe and record |
| --- | --- |
| Startup and movement | Exactly one Gojo per Bracken; approved hands/neck; sensible facing and feet; model and aura follow the enemy without detaching. Record attachment warnings or failures. |
| Aura | Compare hunting, retreat and aggro. Intensity should respond to anger; returning to a non-aggro phase need not immediately make it faint if anger remains elevated. Record whether phase transitions were natural or forced. |
| First aggro | Hear the Japanese line once on entry, with music starting and looping during sustained raw aggro. A short encounter cannot verify a full music loop: mark it not observed if necessary. |
| Leave and re-enter aggro | Music stops on leaving raw aggro. Re-enter once and count another single voice request. A kill/carry overlay suppresses a new voice request; ending that overlay alone must not manufacture another aggro-entry line. |
| Enemy death or controlled removal | After a fresh encounter, kill or remove the enemy using the working test tool if needed. Check for leftover Gojo clones, aura or music. Brief particle fade is distinct from a persistent orphan. Record natural death versus forced removal separately. |
| Scene change | Leave the facility/moon or end the session with an active encounter. Check that the old cosmetics/audio disappear. Start another encounter and check for one fresh model with working effects. |
| Player kill and corpse carry | Run last with a disposable save. Disable invulnerability only when ready for the kill check. Record visible model/body alignment and duplicate/missing bodies. In true solo play, death may end the round or remove the gameplay camera before carrying can be observed. Mark that portion not observed and defer it to a later controlled multiplayer check; do not bypass the solo guard or treat forced carrying flags as a natural gameplay pass. |

The mod's normal path reads enemy state and leaves AI, the original Animator, colliders and corpse handling under game control. Debug tools can alter those systems, so this checkpoint cannot establish gameplay parity. Stop on repeated exceptions, duplicate enemies/cosmetics, broken body handling or audio that persists after cleanup; collect evidence before another attempt.

## Return evidence

After exiting the game, use the same Git Bash session so `PROFILE` and `RESULTS` still refer to this run:

```bash
cp -- "$PROFILE/BepInEx/LogOutput.log" "$RESULTS/LogOutput.log"
cp -- expected.json "$RESULTS/expected.json"
cat > "$RESULTS/observations.md" <<'EOF'
Game version:
Profile mods/debug tool and enabled overrides:
Candidate bundle hash (from expected.json):

| Check | Pass / fail / not observed | What I saw or heard; screenshot/video time |
| --- | --- | --- |
| Startup, pose and movement | | |
| Aura through phases | | |
| Voice on first aggro | | |
| Music loop and stop | | |
| Voice on second aggro | | |
| Enemy death/removal cleanup | | |
| Scene cleanup and fresh attachment | | |
| Player kill/body alignment | | |
| Corpse carry | | |
EOF
printf 'Results directory: %s\n' "$RESULTS"
```

Fill in the observations file. Keep the complete log, preflight report, candidate manifest and useful screenshots/video together. Logs alone do not prove that hands, carried bodies, aura or audio looked/sounded correct. Return this evidence before choosing a fix or the next multiplayer checkpoint. No staging, commits, pushes or publication are part of this test.
