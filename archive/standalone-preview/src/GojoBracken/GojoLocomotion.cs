using System;
using System.Collections.Generic;
using UnityEngine;

namespace GojoBracken
{
    // Procedural visual-only gait. The host transform is an input, never an animation target.
    internal sealed class GojoLocomotion
    {
        private sealed class Rest
        {
            internal Transform Bone;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Rest(Transform bone) { Bone = bone; Position = bone.localPosition; Rotation = bone.localRotation; }
            internal void Apply() { Bone.localPosition = Position; Bone.localRotation = Rotation; }
        }

        private readonly Transform host, hip, leftThigh, leftCalf, leftFoot, rightThigh, rightCalf, rightFoot, leftArm, rightArm;
        private readonly Rest[] rest;
        private readonly float legLength;
        private Vector3 previous, direction;
        private float weight, cycle;
        internal float Blend => weight;
        internal Vector3 TravelDirection => direction;

        private GojoLocomotion(Transform host, Transform visual, Dictionary<string, Transform> bones)
        {
            this.host = host;
            hip = bones["hip"];
            leftThigh = bones["leftThigh"]; leftCalf = bones["leftCalf"]; leftFoot = bones["leftFoot"];
            rightThigh = bones["rightThigh"]; rightCalf = bones["rightCalf"]; rightFoot = bones["rightFoot"];
            leftArm = bones["leftUpperArm"]; rightArm = bones["rightUpperArm"];
            if (!leftCalf.IsChildOf(leftThigh) || !leftFoot.IsChildOf(leftCalf)
                || !rightCalf.IsChildOf(rightThigh) || !rightFoot.IsChildOf(rightCalf)
                || !bones["leftFootToe"].IsChildOf(leftFoot) || !bones["rightFootToe"].IsChildOf(rightFoot)
                || !leftThigh.IsChildOf(hip) || !rightThigh.IsChildOf(hip)
                || !leftArm.IsChildOf(hip) || !rightArm.IsChildOf(hip))
                throw new ArgumentException("Leg chains do not match the Gojo rig.");
            legLength = .5f * (Vector3.Distance(leftThigh.position, leftCalf.position) + Vector3.Distance(leftCalf.position, leftFoot.position)
                + Vector3.Distance(rightThigh.position, rightCalf.position) + Vector3.Distance(rightCalf.position, rightFoot.position));
            if (!Finite(legLength) || legLength < .1f) throw new ArgumentException("Gojo leg lengths are invalid.");
            Vector3 facing = Vector3.ProjectOnPlane(bones["leftFootToe"].position - leftFoot.position
                + bones["rightFootToe"].position - rightFoot.position, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(host.forward, Vector3.up);
            if (facing.sqrMagnitude < .0001f || forward.sqrMagnitude < .0001f)
                throw new ArgumentException("Cannot establish horizontal model facing.");
            // This wrapper is outside the model Animator, so its yaw cannot be reset by the idle clip.
            visual.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(facing, forward, Vector3.up), Vector3.up) * visual.rotation;
            rest = new[] { new Rest(hip), new Rest(leftThigh), new Rest(leftCalf), new Rest(leftFoot),
                new Rest(rightThigh), new Rest(rightCalf), new Rest(rightFoot), new Rest(leftArm), new Rest(rightArm) };
            previous = host.position;
            direction = forward.normalized;
        }

        internal static GojoLocomotion TryCreate(Transform host, Transform visual, out string status)
        {
            var required = new HashSet<string> { "hip", "leftThigh", "leftCalf", "leftFoot", "leftFootToe",
                "rightThigh", "rightCalf", "rightFoot", "rightFootToe", "leftUpperArm", "rightUpperArm" };
            var bones = new Dictionary<string, Transform>();
            Quaternion original = visual.localRotation;
            try
            {
                foreach (var bone in visual.GetComponentsInChildren<Transform>(true))
                    if (required.Contains(bone.name)) bones.Add(bone.name, bone);
                if (bones.Count != required.Count) throw new ArgumentException("Required Gojo gait joints are missing.");
                var gait = new GojoLocomotion(host, visual, bones);
                status = "movement-driven gait ready";
                return gait;
            }
            catch (Exception exception)
            {
                visual.localRotation = original;
                status = "standing fallback: " + exception.Message;
                return null;
            }
        }

        internal void Step(float deltaTime, bool suppress)
        {
            Restore();
            Vector3 current = host.position;
            Vector3 delta = Vector3.ProjectOnPlane(current - previous, Vector3.up);
            previous = current;
            if (suppress || !Finite(deltaTime) || deltaTime <= 0f || deltaTime > .25f || !Finite(delta.x) || !Finite(delta.z)
                || delta.magnitude > Mathf.Max(1.5f, 2f * legLength))
            { weight = cycle = 0f; return; }
            float speed = delta.magnitude / deltaTime;
            float target = Mathf.InverseLerp(.05f, 1.2f, speed);
            float blend = 1f - Mathf.Exp(-10f * deltaTime);
            weight = Mathf.Lerp(weight, target, blend);
            if (delta.sqrMagnitude > .000001f)
            {
                var next = Vector3.Dot(direction, delta.normalized) < -.8f
                    ? delta.normalized : Vector3.Lerp(direction, delta.normalized, blend);
                direction = next.sqrMagnitude > .000001f ? next.normalized : delta.normalized;
                cycle = Mathf.Repeat(cycle + delta.magnitude / legLength, 1f);
            }
            if (weight < .001f) { weight = cycle = 0f; return; }
            Vector3 leftRest = leftFoot.position, rightRest = rightFoot.position;
            Quaternion leftRotation = leftFoot.rotation, rightRotation = rightFoot.rotation;
            Vector3 forward = Vector3.ProjectOnPlane(host.forward, Vector3.up).normalized;
            Vector3 axis = Vector3.Cross(Vector3.up, forward).normalized;
            float lateralLimit = Mathf.Abs(Vector3.Dot(rightRest - leftRest, axis)) * .25f;
            Vector3 leftTarget = FootTarget(leftRest, cycle, axis, lateralLimit);
            Vector3 rightTarget = FootTarget(rightRest, Mathf.Repeat(cycle + .5f, 1f), axis, lateralLimit);
            float drop = Mathf.Max(RequiredDrop(leftThigh, leftCalf, leftFoot, leftTarget),
                RequiredDrop(rightThigh, rightCalf, rightFoot, rightTarget));
            hip.position -= Vector3.up * drop;
            Solve(leftThigh, leftCalf, leftFoot, leftTarget, forward, leftRotation);
            Solve(rightThigh, rightCalf, rightFoot, rightTarget, forward, rightRotation);
            float swing = 20f * weight * Mathf.Cos(cycle * Mathf.PI * 2f) * Vector3.Dot(direction, forward);
            leftArm.rotation = Quaternion.AngleAxis(-swing, axis) * leftArm.rotation;
            rightArm.rotation = Quaternion.AngleAxis(swing, axis) * rightArm.rotation;
        }

        private Vector3 FootTarget(Vector3 baseline, float phase, Vector3 right, float lateralLimit)
        {
            float stride = .25f * legLength * weight;
            float along, lift;
            if (phase < .5f)
            {
                float t = phase * 2f;
                along = Mathf.Lerp(-stride, stride, Mathf.SmoothStep(0f, 1f, t));
                lift = Mathf.Sin(t * Mathf.PI) * .04f * legLength * weight;
            }
            else { along = Mathf.Lerp(stride, -stride, (phase - .5f) * 2f); lift = 0f; }
            Vector3 offset = direction * along;
            float sideways = Vector3.Dot(offset, right);
            offset += right * (Mathf.Clamp(sideways, -lateralLimit, lateralLimit) - sideways);
            return baseline + offset + Vector3.up * lift;
        }

        private float RequiredDrop(Transform thigh, Transform calf, Transform foot, Vector3 target)
        {
            float reach = (Vector3.Distance(thigh.position, calf.position) + Vector3.Distance(calf.position, foot.position))
                * (1f - .001f * weight);
            float sideways = Vector3.ProjectOnPlane(target - thigh.position, Vector3.up).sqrMagnitude;
            float vertical = Mathf.Sqrt(Mathf.Max(0f, reach * reach - sideways));
            return Mathf.Max(0f, thigh.position.y - target.y - vertical);
        }

        private static void Solve(Transform thigh, Transform calf, Transform foot, Vector3 target, Vector3 pole, Quaternion footRotation)
        {
            float a = Vector3.Distance(thigh.position, calf.position), b = Vector3.Distance(calf.position, foot.position);
            Vector3 line = target - thigh.position;
            float length = Mathf.Clamp(line.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
            Vector3 axis = line.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(pole, axis).normalized;
            float reach = (a * a - b * b + length * length) / (2f * length);
            Vector3 knee = thigh.position + axis * reach + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - reach * reach));
            thigh.rotation = Quaternion.FromToRotation(calf.position - thigh.position, knee - thigh.position) * thigh.rotation;
            Vector3 ankle = thigh.position + axis * length;
            calf.rotation = Quaternion.FromToRotation(foot.position - calf.position, ankle - calf.position) * calf.rotation;
            foot.rotation = footRotation;
        }

        internal void Reset()
        {
            Restore(); weight = cycle = 0f; previous = host.position;
        }

        private void Restore()
        {
            foreach (var pose in rest)
            {
                if (pose.Bone == null) throw new InvalidOperationException("A Gojo locomotion joint was removed.");
                pose.Apply();
            }
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
