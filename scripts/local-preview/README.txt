GojoBracken LOCAL PREFAB PREVIEW - private testing on your own PCs

This is a standalone Unity Windows application, not a game mod or public release.
No Lethal Company, BepInEx, Unity Editor, or PowerShell upgrade is required to run it.
Extract the WHOLE ZIP first. Keep the EXE, UnityPlayer.dll and data folder together.

Automated smoke test (Windows PowerShell 5.1, from this extracted folder):
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\Run-PreviewChecks.ps1

Manual visual review: open GojoBrackenPreview.exe.
Check the standing posture, feet, textures, flame/wisps, phase buttons, anger slider,
remote approximation, simulated hiding, death/reset and view angle.
Use Walk/Back/Strafe/Stop to review locomotion and idle recovery. The camera follows
the model over a floor grid. The path resets periodically to test teleport handling.
Automation also creates walk-render.png with the low aura so the legs are visible.
Strength changes take effect when you click Reset.
The optional synthetic test tone is NOT Gojo's voice; no Japanese clip is included.

Save report + render writes files under:
%USERPROFILE%\AppData\LocalLow\GojoBrackenLocalTests\GojoBracken Local Preview

The automated wrapper preserves smoke-report.json and smoke-render.png before manual
review can overwrite the app's preview files. Return smoke-report.json plus a manual
preview-render.png and any visual issues to the testing chat. A manual report has
automated=false and smokePassed=false because automated tests were not run in that session.
For locomotion builds, include walk-render.png and describe gait/foot issues. The
final smoke image is standing after the stop check, so locomotionBlend=0 is expected.
Automated success does not replace a visual review. It does not validate real game
state discovery, Bracken animations, HDRP compatibility, or multiplayer.

Downloaded Gojo data is included only for local evaluation. Its provenance review
is unchanged. Keep this preview outside GitHub releases and Thunderstore.

Aggro selects the arms-ready pose. Kill animation overlay selects the two-handed
reach; toggle it off to inspect recovery. These are state-driven poses, without
victim alignment or a synchronized neck snap. Automated tests also write
aggro-render.png and kill-render.png; include those with the smoke report.
