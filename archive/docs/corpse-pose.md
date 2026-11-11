# Corpse-drag presentation

Version 0.1.8 adds a right-arm dragging pose to the owned Gojo rig. While carryingPlayerBody is true, the right hand reaches down and behind the shoulder; the left arm keeps its ordinary gait swing. Walking and anger-based aura continue. No additional voice or gameplay effect is triggered. The approved standing, aggro-ready and kill-reach poses are retained.

Kill animation takes priority over carrying, which takes priority over aggro-ready arms. Transitions blend smoothly; dropping the body fades the right arm back into walking or idle. Death/invalid time steps reset the weights, and normal attachment cleanup removes the clone. Pose weights are per instance.

This is a state-driven visual approximation, not a calibrated grip. The inspected v81 game has rightHandGrip and rotates its original animationContainer during carrying. Those original transforms, corpse attachments, renderer references, physics and body handling stay under game control. This increment does not read or move the live grip/corpse, or attach the corpse to Gojo. The pose could miss the actual body; live grip placement and model scale remain integration checks.

Standalone review: select Corpse dragging, then Walk/Back/Strafe/Stop and orbit with View angle at low anger. Switch to Hunting to simulate dropping the body, and toggle Kill animation overlay to review its priority. No actual corpse is loaded in this standalone application. Automated checks save carry-render.png and record carryBlend; final report weights are zero after reset.

Unity fixtures check right-arm-only posing at rest, carrying while walking, priority, drop recovery to exact idle, independent instances, and death cleanup. Existing adapter tests preserve the original collider, Animator, cached renderer arrays and carried-body renderer. Standalone native-model checks cover walking with the drag pose and return to idle. These checks do not establish in-game corpse alignment or multiplayer behavior.

Next increment after pose review: prepare game-compatible model/aura materials and inspect the asset-bundle rendering requirements. Real game render-pipeline verification remains necessary before claiming compatible rendering.
