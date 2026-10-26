# Standalone testing-PC preview

The testing PC can now check the actual native Gojo and aura prefabs without Unity, Lethal Company, BepInEx, or a .NET/PowerShell upgrade. This is a separate Windows x64 application with simulated enemy inputs and the production cosmetic controller/state rules. It does not install or modify anything in the game.

On the development laptop, the repeatable build is:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Build-LocalPreviewPlayer.ps1
```

Unity 2022.3.62f2 with Windows build support and the ignored local native model are required to build. The script creates an isolated project under artifacts/local-preview-player and prints a ZIP path. The ZIP contains downloaded model data for evaluation on your own PCs; it stays outside Git and public releases. Transfer the whole ZIP privately to the testing PC's Downloads folder. It cannot be obtained by pulling the Git repository.

On the testing PC, in Windows PowerShell 5.1:

```powershell
$zip = Join-Path $env:USERPROFILE 'Downloads\GojoBrackenActionPreview.zip'
$target = Join-Path $env:USERPROFILE ('Documents\GojoBrackenPreview-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
Expand-Archive -LiteralPath $zip -DestinationPath $target
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File (Join-Path $target 'Run-PreviewChecks.ps1')
Start-Process -FilePath (Join-Path $target 'GojoBrackenPreview.exe') -WorkingDirectory $target
```

The automated wrapper runs the EXE with --smoke-test and waits at most two minutes. It requires a fresh passing report, preserves smoke-report.json and smoke-render.png, and reports failures explicitly. Manual review then opens the normal window. If execution is blocked, return the error rather than changing machine-wide policy or security settings.

Review the posture and feet from several angles, texture/shader appearance, full-strength flame and wisps, anger changes, phase buttons, repeated aggro entries, remote approximation, simulated hiding, death, and reset. New locomotion builds also include Walk/Back/Strafe/Stop controls, a floor grid, and walk-render.png from automated testing. Strength is applied when Reset recreates the controller. Optional audio is a clearly labelled synthetic tone: no Japanese voice clip is included. Select Save report + render after reviewing a useful view.

Reports are written under:

```text
%USERPROFILE%\AppData\LocalLow\GojoBrackenLocalTests\GojoBracken Local Preview
```

Return smoke-report.json, a manual preview-render.png, and a short description of visual/audio issues to Testing PC: GojoBracken compatibility report. A manual preview-report.json uses automated=false and smokePassed=false because no automated sequence ran in that session; that alone is not an error. The preserved smoke report carries the automated result. Its checks include voice requests before reset, so the final voice counter can correctly be zero.

The report records Unity version, graphics device/API, color space, skin weights, source digest, current simulated state, and completed checks. Locomotion builds add gait status/blend and actual-rig walking/stop checks. An automatic pass is not a visual-quality verdict. It does not verify real game state discovery, renderer-array handling in v81, Bracken attacks/corpse animations, active game rendering pipeline, or vanilla-compatible multiplayer. Existing Unity fixtures cover adapter logic separately. Action-pose builds also save aggro-render.png and kill-render.png. Review Aggro and Kill animation overlay at low anger from multiple angles. The next increment after action-pose approval is corpse-carry presentation.
