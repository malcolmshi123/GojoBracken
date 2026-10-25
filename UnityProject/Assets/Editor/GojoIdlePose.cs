using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GojoBracken.Editor
{
    internal static class GojoIdlePose
    {


        public static void Apply(GameObject model)
        {
            foreach (var animator in model.GetComponentsInChildren<Animator>(true))
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.enabled = false;
            }
            var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            var bindposes = skin.sharedMesh.bindposes;
            if (bindposes.Length != skin.bones.Length)
                throw new InvalidOperationException("Bone and bind-pose counts differ.");
            // Restore the source bind matrices before authoring a neutral pose.
            var rest = new Dictionary<Transform, Matrix4x4>();
            for (int i = 0; i < skin.bones.Length; i++)
                rest.Add(skin.bones[i], skin.transform.localToWorldMatrix * bindposes[i].inverse);
            foreach (var bone in rest.Keys.OrderBy(Depth))
            {
                Matrix4x4 local = bone.parent.worldToLocalMatrix * rest[bone];
                bone.localPosition = local.GetColumn(3);
                bone.localRotation = local.rotation;
                bone.localScale = local.lossyScale;
            }



            var skeleton = model.GetComponentsInChildren<Transform>(true);
            var leftFoot = skeleton.First(bone => bone.name == "leftFoot");
            var leftToe = skeleton.First(bone => bone.name == "leftFootToe");
            var rightFoot = skeleton.First(bone => bone.name == "rightFoot");
            var rightToe = skeleton.First(bone => bone.name == "rightFootToe");
            Vector3 forward = Vector3.ProjectOnPlane(leftToe.position - leftFoot.position
                + rightToe.position - rightFoot.position, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f) throw new InvalidOperationException("Could not determine the model's facing direction.");
            UprightSpine(skeleton, forward);
            var pelvis = skeleton.First(item => item.name == "hip");
            var chest = skeleton.First(item => item.name == "chest");
            var neck = skeleton.First(item => item.name == "neck");
            // Offset the chest, not the waist: preserve the approved upper pose without pulling the lumbar joint forward.
            chest.position += forward * Vector3.Distance(pelvis.position, neck.position) * 0.075f;
            // Correct the cervical joint without tilting the approved torso or the face.
            var head = skeleton.First(item => item.name == "head");
            Quaternion headRotation = head.rotation;
            float headHeight = head.position.y;
            neck.rotation = Quaternion.AngleAxis(24f, Vector3.Cross(Vector3.up, forward).normalized) * neck.rotation;
            head.rotation = headRotation;
            head.position = new Vector3(head.position.x, headHeight, head.position.z);
            RelaxArm(model.transform, "leftUpperArm", "leftForearm", "leftHand");
            RelaxArm(model.transform, "rightUpperArm", "rightForearm", "rightHand");
            RelaxLeg(model.transform, "leftThigh", "leftCalf", "leftFoot", "leftFootToe", forward);
            RelaxLeg(model.transform, "rightThigh", "rightCalf", "rightFoot", "rightFootToe", forward);
            FlattenSole(model, leftFoot);
            FlattenSole(model, rightFoot);
            model.transform.rotation = Quaternion.FromToRotation(forward, Vector3.back) * model.transform.rotation;
        }

        private static void RelaxLeg(Transform root, string thighName, string calfName, string footName, string toeName, Vector3 forward)
        {
            var bones = root.GetComponentsInChildren<Transform>(true);
            var thigh = bones.First(bone => bone.name == thighName);
            var calf = bones.First(bone => bone.name == calfName);
            var foot = bones.First(bone => bone.name == footName);
            var toe = bones.First(bone => bone.name == toeName);
            thigh.rotation = Quaternion.FromToRotation(calf.position - thigh.position, Vector3.down) * thigh.rotation;
            calf.rotation = Quaternion.FromToRotation(foot.position - calf.position, Vector3.down) * calf.rotation;
            foot.rotation = Quaternion.FromToRotation(toe.position - foot.position, forward) * foot.rotation;
            if (Vector3.Dot((foot.position - thigh.position).normalized, Vector3.down) < 0.999f)
                throw new InvalidOperationException("Idle leg did not straighten: " + thighName);
        }

        private static void UprightSpine(Transform[] bones, Vector3 forward)
        {
            string[] chain = { "hip", "waist", "chest", "shoulder", "neck", "head", "top" };
            // Compensate the source skin's arched torso; vertical bone markers alone look stiff.
            float[] forwardBias = { 0.09f, -0.045f, -0.03f, 0f, 0f, 0f };
            for (int i = 0; i < chain.Length - 1; i++)
            {
                var bone = bones.First(item => item.name == chain[i]);
                var next = bones.First(item => item.name == chain[i + 1]);
                bone.rotation = Quaternion.FromToRotation(next.position - bone.position, (Vector3.up + forward * forwardBias[i]).normalized) * bone.rotation;
            }
        }

        private static void FlattenSole(GameObject model, Transform foot)
        {
            for (int pass = 0; pass < 10; pass++)
            {
                var sole = MeasureSole(model, foot);
                float angle = Vector3.Angle(-sole.Normal, Vector3.up);
                if (angle < 0.3f) break;
                foot.rotation = Quaternion.FromToRotation(-sole.Normal, Vector3.up) * foot.rotation;
            }
            var result = MeasureSole(model, foot);
            Debug.Log("IDLE SOLE: " + foot.name + "; tilt=" + Vector3.Angle(-result.Normal, Vector3.up)
                + "; minY=" + result.MinY);
            if (Vector3.Angle(-result.Normal, Vector3.up) > 1f)
                throw new InvalidOperationException("Sole did not flatten: " + foot.name);
        }

        private struct Sole
        {
            public Vector3 Normal;
            public float MinY;
        }

        private static Sole MeasureSole(GameObject model, Transform foot)
        {
            var baked = new Mesh();
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            var points = new List<Vector3>();
            var triangles = new List<int>();
            try
            {
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(baked, true);
                    var vertices = baked.vertices;
                    var weights = skin.sharedMesh.boneWeights;
                    var related = skin.bones.Select(bone => bone == foot || bone.IsChildOf(foot)).ToArray();
                    var selected = new bool[vertices.Length];
                    int offset = points.Count;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var weight = weights[i];
                        float amount = (related[weight.boneIndex0] ? weight.weight0 : 0f)
                            + (related[weight.boneIndex1] ? weight.weight1 : 0f)
                            + (related[weight.boneIndex2] ? weight.weight2 : 0f)
                            + (related[weight.boneIndex3] ? weight.weight3 : 0f);
                        var point = skin.transform.TransformPoint(vertices[i]);
                        points.Add(point);
                        selected[i] = amount > 0.5f;
                        if (selected[i]) { minY = Mathf.Min(minY, point.y); maxY = Mathf.Max(maxY, point.y); }
                    }
                    var indices = baked.triangles;
                    for (int i = 0; i < indices.Length; i += 3)
                        if (selected[indices[i]] && selected[indices[i + 1]] && selected[indices[i + 2]])
                        {
                            triangles.Add(offset + indices[i]);
                            triangles.Add(offset + indices[i + 1]);
                            triangles.Add(offset + indices[i + 2]);
                        }
                    baked.Clear();
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
            Vector3 normal = Vector3.zero;
            float cutoff = minY + (maxY - minY) * 0.35f;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a = points[triangles[i]], b = points[triangles[i + 1]], c = points[triangles[i + 2]];
                var areaNormal = Vector3.Cross(b - a, c - a);
                if ((a.y + b.y + c.y) / 3f <= cutoff
                    && Vector3.Dot(areaNormal.normalized, Vector3.down) > 0.5f)
                    normal += areaNormal;
            }
            if (normal.sqrMagnitude < 0.00000001f)
                throw new InvalidOperationException("Could not measure shoe sole: " + foot.name);
            return new Sole { Normal = normal.normalized, MinY = minY };
        }

        public static void GroundFeet(GameObject model)
        {
            var bones = model.GetComponentsInChildren<Transform>(true);
            foreach (string side in new[] { "left", "right" })
            {
                var foot = bones.First(bone => bone.name == side + "Foot");
                var thigh = bones.First(bone => bone.name == side + "Thigh");
                float offset = MeasureSole(model, foot).MinY;
                // Small authoring correction for the source skeleton's unequal leg heights.
                // Refuse larger changes instead of silently stretching the stance.
                if (Mathf.Abs(offset) > 0.02f)
                    throw new InvalidOperationException("Foot requires more than 2 cm of grounding: " + side);
                thigh.position -= Vector3.up * offset;
                var sole = MeasureSole(model, foot);
                if (Mathf.Abs(sole.MinY) > 0.001f)
                    throw new InvalidOperationException("Foot is not on the floor: " + side);
                Debug.Log("IDLE GROUND: " + side + "; correctionMeters=" + offset
                    + "; soleY=" + sole.MinY + "; tilt=" + Vector3.Angle(-sole.Normal, Vector3.up));
            }
            var hip = bones.First(bone => bone.name == "hip");
            var neckBase = bones.First(bone => bone.name == "neck");
            float lean = Vector3.Angle(neckBase.position - hip.position, Vector3.up);
            if (lean > 5f) throw new InvalidOperationException("Neutral-pose lean limit exceeded: " + lean);
            Debug.Log("IDLE POSTURE: hip-to-neck-base lean=" + lean);
        }

        private static int Depth(Transform bone)
        {
            int depth = 0;
            while (bone.parent != null) { depth++; bone = bone.parent; }
            return depth;
        }

        private static void RelaxArm(Transform root, string upperName, string lowerName, string handName)
        {
            var bones = root.GetComponentsInChildren<Transform>(true);
            var upper = bones.First(bone => bone.name == upperName);
            var lower = bones.First(bone => bone.name == lowerName);
            var hand = bones.First(bone => bone.name == handName);
            float side = Mathf.Sign(root.InverseTransformPoint(upper.position).x);
            Vector3 down = root.TransformDirection(new Vector3(side * 0.1f, -1f, 0.02f)).normalized;
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, down) * upper.rotation;
            Vector3 forearmDirection = root.TransformDirection(new Vector3(side * 0.04f, -1f, 0.08f)).normalized;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, forearmDirection) * lower.rotation;
        }

        public static Bounds VisibleBounds(GameObject model)
        {
            var baked = new Mesh();
            bool initialized = false;
            var bounds = new Bounds();
            try
            {
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(baked, true);
                    foreach (var vertex in baked.vertices)
                    {
                        Vector3 point = skin.transform.TransformPoint(vertex);
                        if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                        else bounds.Encapsulate(point);
                    }
                    baked.Clear();
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
            if (!initialized) throw new InvalidOperationException("No visible skinned vertices.");
            return bounds;
        }
    }
}
