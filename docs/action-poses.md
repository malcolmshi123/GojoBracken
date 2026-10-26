# Aggro and kill presentation

Version 0.1.7 adds GojoActionPose to the experimental model attachment. It operates on six owned arm joints after the cloned idle Animator and locomotion. The approved legs, torso, neck, source prefab, and original gameplay rig are unchanged.

Aggro (state 2) blends into a restrained arms-ready pose while locomotion continues. The inKillAnimation flag takes priority and blends into a two-handed forward reach; locomotion resets during this flag. Exiting the flag restores walking or idle. Corpse carrying suppresses the ready pose and uses ordinary locomotion. Death resets the pose weights and attachment cleanup removes the clone. Missing arm joints leave locomotion/standing available with a useful status message.

This is a first state-driven presentation pass. The observations provide a kill flag, not a replicated animation timeline or victim neck target. The reach holds while that flag is active; it does not implement a precisely timed neck snap or hand-to-victim alignment. No victim transform, AI, damage, movement, root motion, collider, network state, or audio trigger is changed. Existing aggro voice rules remain independent of arm posing.

The standalone preview uses the production action class. Select Aggro to inspect the ready pose, toggle Kill animation overlay for the reach, and leave the overlay to check recovery. Review several camera angles with low anger so the arms are visible. Automated checks save aggro-render.png and kill-render.png alongside walk-render.png and smoke-report.json. Real-rig captures establish local rendering only; game interaction and multiplayer remain unverified.

Unity fixtures check aggro blending, kill priority and reentry, corpse recovery to exact idle, death, independent instances, missing-rig fallback, and unchanged host position. The standalone smoke test additionally exercises the actual native model and checks return to idle. Visual quality still requires review.

Next increment after pose approval: corpse-carry presentation, using the original game's body handling while adapting only Gojo's owned visual joints.
