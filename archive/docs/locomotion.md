# Movement-driven Gojo gait

Version 0.1.6 adds a first procedural locomotion pass to the experimental model preview. GojoLocomotion reads horizontal enemy-root displacement in LateUpdate and changes only the cloned Gojo rig. It does not move the enemy root, write AI/agent fields, use root motion, add footsteps, or alter the original gameplay rig.

The controller uses the candidate's named hip, thigh, calf, foot/toe, and upper-arm joints. It measures facing from the toes and rotates an owned wrapper to match the enemy root's forward direction. The original approved rig remains unchanged in its prefab. Missing, ambiguous, or incompatible joints retain the standing visual and report a standing-fallback status in the attachment log.

Movement blends into alternating foot swing/support targets, two-bone leg solving, a reach-based pelvis drop, and opposing arm swing. The pelvis lowers only enough for the current foot targets, avoiding a constant crouch. Swing lift is reduced to 4% of leg length. Foot orientation stays at its rest orientation; lateral reach is bounded by the rest stance width to avoid crossing ankles during side steps. Torso and neck rotations are not edited. Stride phase follows distance traveled; animation weight smooths speed changes. Stops restore the exact cached idle transforms. Kill animation, death, invalid/zero/large time steps, and large displacement jumps reset gait sampling. Corpse carrying uses the ordinary movement gait; hand gripping and body-handling poses are not adapted yet.

The legacy configuration key StandingModelPreview is retained. Both it and DevelopmentEffects.Enabled still default to false. A supported Gojo rig now walks when the root moves. Unsupported rigs remain standing. Game-facing placement, network interpolation, high-speed motion, stairs/slopes, and actual Bracken motion still need controlled validation. This is a first visual gait, not finished combat animation or terrain-aware foot placement.

Checks on the development laptop:

```powershell
dotnet build GojoBracken.sln -c Release
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Test-CosmeticController.ps1
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Build-LocalPreviewPlayer.ps1
```

Synthetic Unity fixtures cover idle/stop restoration, stepping/lift, level foot orientation, preserved torso/neck and input transform, reverse motion, independent instances, kill suppression, teleports/nonfinite timing, and unsupported-rig fallback. Existing attachment/rollback, state, visibility, and cleanup cases remain in the same command.

The standalone Windows application also exercises the actual downloaded Gojo rig. Its automated sequence moves a synthetic anchor, verifies rig motion and return to idle, writes walk-render.png, then performs the existing aura/state checks and saves the usual smoke report. The walk capture uses low aura so the legs remain visible. The final full-aura capture remains standing; locomotionBlend=0 there is expected after the stop check. Visual review is still required to judge the gait's quality.

Manual preview controls now include Walk, Back, Strafe, and Stop, plus a floor grid and an orbiting/following camera. The preview loops its movement path with a position reset; that reset deliberately tests teleport handling. Save report + render can capture another walking angle. No Unity installation or game encounter is needed on the testing PC. Private model data and ZIPs remain outside Git/public releases.

Next increment: adapt attack/kill presentation while preserving the original kill timing and gameplay rig; corpse-hand alignment and game material validation remain follow-up work. Review walking quality before layering those poses onto it.
