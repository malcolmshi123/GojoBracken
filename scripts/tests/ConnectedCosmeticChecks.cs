using System;
using GojoBracken.Authoring;
using UnityEngine;

public static class ConnectedCosmeticChecks
{
    public static void Run()
    {
        var first = Create("Connected fixture one");
        first.Anger = 1f;
        first.Step(0.01f);
        Require(first.VisibleAuraLevel > .15f && first.VisibleAuraLevel < 1f, "smoothed visual");
        first.Step(10f);
        Require(first.VisibleAuraLevel == 1f && Mathf.Abs(first.Aura.emission.rateOverTime.constant - 220f) < .01f, "maximum approved wisps");
        first.Phase = AuraStudyController.PreviewPhase.Aggro;
        first.Step(.1f);
        first.Step(.1f);
        Require(first.VoiceTriggerCount == 1, "one event on aggro");
        first.Phase = AuraStudyController.PreviewPhase.Retreat;
        first.Anger = .5f;
        first.Step(10f);
        Require(first.VisibleAuraLevel > .15f && first.ObservedPhase == "Retreating", "retreat stays active");
        first.Phase = AuraStudyController.PreviewPhase.Aggro;
        first.Step(.1f);
        Require(first.VoiceTriggerCount == 2, "reentry");
        first.Phase = AuraStudyController.PreviewPhase.Dragging;
        first.Step(.1f);
        Require(first.VoiceTriggerCount == 2 && first.ObservedPhase == "CorpseDragging", "dragging no extra event");
        first.UsePhaseApproximation = true;
        first.Phase = AuraStudyController.PreviewPhase.Hunting;
        first.Anger = 1f;
        first.Step(10f);
        Require(first.VisibleAuraLevel == .15f, "remote faint outside aggro");
        first.Phase = AuraStudyController.PreviewPhase.Aggro;
        first.Anger = 0f;
        first.Step(10f);
        Require(first.VisibleAuraLevel == 1f, "remote strong during aggro");
        int voices = first.VoiceTriggerCount;
        first.Phase = AuraStudyController.PreviewPhase.Hunting;
        first.Step(.1f);
        first.InKillAnimation = true;
        first.Phase = AuraStudyController.PreviewPhase.Aggro;
        first.Step(.1f);
        first.InKillAnimation = false;
        first.Step(.1f);
        Require(first.VoiceTriggerCount == voices, "kill overlay does not fabricate voice entry");
        first.AuraStrength = .5f;
        first.Step(10f);
        var envelope = first.Aura.GetComponentInChildren<FlameEnvelope>();
        var properties = new MaterialPropertyBlock();
        envelope.GetComponent<Renderer>().GetPropertyBlock(properties);
        Require(properties.GetFloat("_Intensity") == .5f
            && Mathf.Abs(first.Aura.emission.rateOverTime.constant - 110f) < .01f, "strength reaches shell and particles");
        first.SimulateDeath();
        Require(first.EffectsStopped && first.VisibleAuraLevel == 0f && !first.Aura.isPlaying
            && first.Aura.particleCount == 0 && !envelope.gameObject.activeSelf, "death stops actual effects");
        first.ResetPreview();
        Require(!first.EffectsStopped && first.VoiceTriggerCount == 0 && first.VisibleAuraLevel == .15f, "reset");
        var second = Create("Connected fixture two");
        second.Step(0f);
        first.Phase = AuraStudyController.PreviewPhase.Aggro;
        first.Step(.1f);
        Require(first.VoiceTriggerCount == 1 && second.VoiceTriggerCount == 0, "independent controllers");
        first.EndPreview();
        first.Step(10f);
        Require(first.EffectsStopped && first.VisibleAuraLevel == 0f && !first.Aura.isPlaying, "end remains stopped");
        UnityEngine.Object.DestroyImmediate(first.gameObject);
        UnityEngine.Object.DestroyImmediate(second.gameObject);
        Debug.Log("CONNECTED COSMETICS PASS: shared rules, smoothing, phases, overlays, voice gating, remote approximation, strength, death/reset, independent instances.");

    }

    private static AuraStudyController Create(string name)
    {
        var root = new GameObject(name);
        var aura = root.AddComponent<ParticleSystem>();
        AuraStudyController.ConfigureFlames(aura);
        var controller = root.AddComponent<AuraStudyController>();
        controller.Aura = aura;
        controller.VoiceSource = root.AddComponent<AudioSource>();
        return controller;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Connected cosmetic check failed: " + message);
    }
}
