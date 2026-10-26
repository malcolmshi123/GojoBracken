using System;
using System.Collections.Generic;
using UnityEngine;

namespace GojoBracken
{
    // Owned arm joints only. State-driven presentation, not victim alignment or gameplay animation.
    internal sealed class GojoActionPose
    {
        private readonly Transform host;
        private readonly Transform[] bones;
        private readonly Quaternion[] idle;
        private float ready, reach;
        internal float ReadyBlend => ready;
        internal float KillBlend => reach;

        private GojoActionPose(Transform host, Transform[] bones)
        {
            this.host = host; this.bones = bones;
            idle = Array.ConvertAll(bones, bone => bone.localRotation);
        }

        internal static GojoActionPose TryCreate(Transform host, Transform visual, out string status)
        {
            var found = new Dictionary<string, Transform>();
            string[] names = { "leftUpperArm", "leftForearm", "leftHand", "rightUpperArm", "rightForearm", "rightHand" };
            try
            {
                foreach (var bone in visual.GetComponentsInChildren<Transform>(true))
                    if (Array.IndexOf(names, bone.name) >= 0) found.Add(bone.name, bone);
                var chain = Array.ConvertAll(names, name => found[name]);
                for (int i = 0; i < 6; i += 3)
                    if (!chain[i + 1].IsChildOf(chain[i]) || !chain[i + 2].IsChildOf(chain[i + 1])
                        || Vector3.Distance(chain[i].position, chain[i + 1].position) < .01f
                        || Vector3.Distance(chain[i + 1].position, chain[i + 2].position) < .01f)
                        throw new ArgumentException("Invalid arm chain.");
                status = "aggro-ready and kill-reach poses ready";
                return new GojoActionPose(host, chain);
            }
            catch (Exception exception) { status = "action pose fallback: " + exception.Message; return null; }
        }

        // Call before locomotion so a fading action pose cannot accumulate or erase gait arm swing.
        internal void Restore()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) throw new InvalidOperationException("A Gojo action joint was removed.");
                bones[i].localRotation = idle[i];
            }
        }

        internal void Apply(BrackenObservation observation, float deltaTime)
        {
            if (observation.Dead || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f || deltaTime > .25f)
            { ready = reach = 0f; return; }
            float blend = 1f - Mathf.Exp(-12f * deltaTime);
            reach = Mathf.Lerp(reach, observation.InKillAnimation ? 1f : 0f, blend);
            ready = Mathf.Lerp(ready, !observation.InKillAnimation && !observation.CarryingBody
                && observation.StateIndex == 2 ? 1f : 0f, blend);
            if (reach < .001f) reach = 0f;
            if (ready < .001f) ready = 0f;
            float weight = Mathf.Clamp01(ready + reach);
            if (weight == 0f) return;
            for (int i = 0; i < 6; i += 3)
            {
                var upper = bones[i]; var lower = bones[i + 1]; var hand = bones[i + 2];
                var upperRotation = upper.rotation; var lowerRotation = lower.localRotation;
                float length = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
                float kill = reach / (ready + reach);
                float side = i == 0 ? -1f : 1f;
                Vector3 target = upper.position + length * (host.forward * Mathf.Lerp(.42f, .86f, kill)
                    - Vector3.up * Mathf.Lerp(.55f, .12f, kill) + host.right * side * .08f);
                Vector3 line = target - upper.position;
                float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, hand.position);
                float d = Mathf.Clamp(line.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
                Vector3 axis = line.normalized;
                Vector3 bend = Vector3.ProjectOnPlane(-Vector3.up + host.right * side * .3f, axis).normalized;
                float along = (a * a - b * b + d * d) / (2f * d);
                Vector3 elbow = upper.position + axis * along + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
                upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
                lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
                var solvedUpper = upper.rotation; var solvedLower = lower.localRotation;
                upper.rotation = Quaternion.Slerp(upperRotation, solvedUpper, weight);
                lower.localRotation = Quaternion.Slerp(lowerRotation, solvedLower, weight);
            }
        }
    }
}
