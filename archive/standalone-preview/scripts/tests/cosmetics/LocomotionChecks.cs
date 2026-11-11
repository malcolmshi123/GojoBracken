using System;
using System.Linq;
using GojoBracken;
using UnityEngine;

public static class LocomotionChecks
{
    public static void Run()
    {
        var host = new GameObject("Synthetic locomotion input host");
        var visual = new GameObject("Owned visual wrapper"); visual.transform.SetParent(host.transform, false);
        var hip = Bone(visual.transform, "hip", new Vector3(0f, 1f, 0f));
        var chest = Bone(hip, "chest", new Vector3(0f, .4f, 0f));
        Bone(chest, "neck", new Vector3(0f, .2f, 0f));
        foreach (string side in new[] { "left", "right" })
        {
            float x = side == "left" ? -.12f : .12f;
            var thigh = Bone(hip, side + "Thigh", new Vector3(x, 0f, 0f));
            var calf = Bone(thigh, side + "Calf", new Vector3(0f, -.45f, 0f));
            var foot = Bone(calf, side + "Foot", new Vector3(0f, -.45f, 0f));
            Bone(foot, side + "FootToe", new Vector3(0f, 0f, .15f));
            var arm = Bone(chest, side + "UpperArm", new Vector3(x * 2f, 0f, 0f));
            var forearm = Bone(arm, side + "Forearm", new Vector3(0f, -.3f, 0f));
            Bone(forearm, side + "Hand", new Vector3(0f, -.3f, 0f));
        }
        var nodes = visual.GetComponentsInChildren<Transform>();
        var positions = nodes.Select(node => node.localPosition).ToArray();
        var rotations = nodes.Select(node => node.localRotation).ToArray();
        var gait = GojoLocomotion.TryCreate(host.transform, visual.transform, out var status);
        Require(gait != null, status);
        gait.Step(1f / 60f, false);
        Require(gait.Blend == 0f && SamePose(nodes, positions, rotations), "stationary rig keeps exact idle pose");
        var leftFoot = nodes.First(node => node.name == "leftFoot");
        var rightFoot = nodes.First(node => node.name == "rightFoot");
        bool lifted = false;
        for (int i = 0; i < 60; i++)
        {
            host.transform.position += new Vector3(0f, 0f, .02f);
            gait.Step(1f / 60f, false);
            Require(leftFoot.position.y > .095f && rightFoot.position.y > .095f, "ankles do not sink below their idle ground baseline");
            foreach (var foot in new[] { leftFoot, rightFoot })
                if (foot.position.y < .101f)
                    Require(180f - Vector3.Angle(foot.parent.parent.position - foot.parent.position,
                        foot.position - foot.parent.position) < 12f, "supporting knee stays nearly straight");
            lifted |= leftFoot.position.y > .12f || rightFoot.position.y > .12f;
        }
        Require(lifted && gait.Blend > .9f, "movement drives stepping and lift");
        Require(Vector3.Angle(leftFoot.up, Vector3.up) < .05f && Vector3.Angle(rightFoot.up, Vector3.up) < .05f,
            "feet retain level orientation");
        Require(chest.localRotation == Quaternion.identity && nodes.First(node => node.name == "neck").localRotation == Quaternion.identity,
            "torso and neck rotations unchanged");
        Require(Vector3.Distance(host.transform.position, new Vector3(0f, 0f, 1.2f)) < .0001f,
            "animation never moves input host");
        for (int i = 0; i < 90; i++) gait.Step(1f / 60f, false);
        Require(gait.Blend == 0f && SamePose(nodes, positions, rotations), "stopping restores exact idle pose");
        host.transform.position += Vector3.right * .02f;
        gait.Step(1f / 60f, false);
        gait.Step(1f / 60f, true);
        Require(gait.Blend == 0f && SamePose(nodes, positions, rotations), "kill suppression restores idle");
        host.transform.position += Vector3.forward * 5f;
        gait.Step(1f / 60f, false);
        Require(gait.Blend == 0f && SamePose(nodes, positions, rotations), "teleport resets sampling and pose");
        gait.Step(float.NaN, false);
        Require(SamePose(nodes, positions, rotations), "nonfinite delta is safe");
        var otherHost = new GameObject("Independent input host");
        var otherVisual = UnityEngine.Object.Instantiate(visual, otherHost.transform, false);
        var other = GojoLocomotion.TryCreate(otherHost.transform, otherVisual.transform, out status);
        Require(other != null, status);
        for (int i = 0; i < 30; i++) { host.transform.position += Vector3.back * .02f; gait.Step(1f / 60f, false); other.Step(1f / 60f, false); }
        Require(gait.Blend > .9f && other.Blend == 0f && Vector3.Dot(gait.TravelDirection, Vector3.back) > .99f,
            "independent instances and reverse motion");
        for (int i = 0; i < 60; i++)
        {
            host.transform.position += Vector3.right * .02f; gait.Step(1f / 60f, false);
            Require(leftFoot.position.x < rightFoot.position.x, "side steps do not cross the ankles");
        }
        var incomplete = new GameObject("Unsupported rig");
        Require(GojoLocomotion.TryCreate(host.transform, incomplete.transform, out status) == null
            && status.Contains("standing fallback") && incomplete.transform.rotation == Quaternion.identity, "unsupported rig falls back without rotation changes");
        gait.Reset();
        var actions = GojoActionPose.TryCreate(host.transform, visual.transform, out status);
        var otherActions = GojoActionPose.TryCreate(otherHost.transform, otherVisual.transform, out status);
        Require(actions != null && otherActions != null, status);
        Vector3 inputPosition = host.transform.position;
        for (int i = 0; i < 60; i++) { actions.Restore(); actions.Apply(new BrackenObservation(2, 0f, true, false, false, false), 1f / 60f); }
        Require(actions.ReadyBlend > .99f && !SamePose(nodes, positions, rotations) && otherActions.ReadyBlend == 0f,
            "aggro pose changes owned arms independently");
        for (int i = 0; i < 60; i++) { actions.Restore(); actions.Apply(new BrackenObservation(2, 0f, true, false, true, false), 1f / 60f); }
        Require(actions.KillBlend > .99f && actions.ReadyBlend < .001f, "kill overrides ready pose");
        for (int i = 0; i < 90; i++) { actions.Restore(); actions.Apply(new BrackenObservation(2, 0f, true, true, false, false), 1f / 60f); }
        Require(actions.KillBlend == 0f && actions.ReadyBlend == 0f && actions.CarryBlend > .99f,
            "corpse overlay overrides aggro and kill poses");
        Require(nodes.First(node => node.name == "leftUpperArm").localRotation == Quaternion.identity
            && Quaternion.Angle(nodes.First(node => node.name == "rightUpperArm").localRotation, Quaternion.identity) > 10f,
            "drag pose changes only the right arm");
        for (int i = 0; i < 60; i++)
        {
            host.transform.position += Vector3.forward * .02f;
            actions.Restore(); gait.Step(1f / 60f, false);
            actions.Apply(new BrackenObservation(1, 0f, false, true, false, false), 1f / 60f);
        }
        Require(gait.Blend > .9f && actions.CarryBlend > .99f, "walking continues during carrying");
        gait.Reset();
        for (int i = 0; i < 90; i++) { actions.Restore(); actions.Apply(new BrackenObservation(0, 0f, false, false, false, false), 1f / 60f); }
        Require(actions.CarryBlend == 0f && SamePose(nodes, positions, rotations), "body drop restores exact idle");
        inputPosition = host.transform.position;
        actions.Restore(); actions.Apply(new BrackenObservation(2, 0f, true, false, true, false), .1f);
        Require(actions.KillBlend > .5f, "kill can reenter");
        actions.Restore(); actions.Apply(new BrackenObservation(2, 0f, true, false, true, true), .1f);
        Require(actions.KillBlend == 0f && actions.CarryBlend == 0f && SamePose(nodes, positions, rotations) && host.transform.position == inputPosition,
            "death clears actions without moving host");
        Require(GojoActionPose.TryCreate(host.transform, incomplete.transform, out status) == null, "missing action rig fallback");
        Debug.Log("ACTION POSE PASS: aggro, kill priority/reentry, corpse recovery, death, exact idle, independent instances, unchanged host, missing-rig fallback.");
        UnityEngine.Object.Destroy(host); UnityEngine.Object.Destroy(otherHost); UnityEngine.Object.Destroy(incomplete);
        Debug.Log("LOCOMOTION PASS: exact idle/stop, movement-driven step/lift, level feet, unchanged torso/neck/host, kill suppression, teleport/nonfinite guards, independent/reverse motion, unsupported-rig fallback.");
    }
    private static Transform Bone(Transform parent, string name, Vector3 position)
    {
        var bone = new GameObject(name).transform; bone.SetParent(parent, false); bone.localPosition = position; return bone;
    }
    private static bool SamePose(Transform[] nodes, Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < nodes.Length; i++)
            if (Vector3.Distance(nodes[i].localPosition, positions[i]) > .00001f || Quaternion.Angle(nodes[i].localRotation, rotations[i]) > .02f) return false;
        return true;
    }
    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Locomotion check failed: " + reason);
    }
}
