using System;
using System.Collections;
using GojoBracken;
using UnityEngine;

public sealed class CosmeticControllerProbe : MonoBehaviour
{
    private IEnumerator Start()
    {
        try { CheckRulesAndEffects(); }
        catch (Exception exception) { Fail(exception); yield break; }
        yield return null; // Let Destroy process the death/disable cleanup.
        try
        {
            Require(first.transform.childCount == 0 && second.transform.childCount == 0, "owned objects removed on death/disable");
            Require(first.GetComponent<MeshRenderer>().enabled, "host renderer unchanged");
            Require(first.GetComponent<BoxCollider>().enabled, "host collider unchanged");
            Require(first.transform.position == new Vector3(2f, 0f, 3f), "host transform unchanged");
            Require(BrackenCosmeticController.TryCreate(third.transform, BrackenV81Presentation.SupportedMvid,
                template, clip, 12f, 1f, out thirdController, out var error), error);
            thirdController.Apply(Input(2, 12f), true, 10f);
            Destroy(third);
        }
        catch (Exception exception) { Fail(exception); yield break; }
        yield return null;
        try
        {
            Require(third == null && thirdController == null, "despawn destroys controller and children");
            var scene = UnityEngine.SceneManagement.SceneManager.CreateScene("Synthetic round");
            var round = new GameObject("Round enemy anchor");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(round, scene);
            Require(BrackenCosmeticController.TryCreate(round.transform, BrackenV81Presentation.SupportedMvid,
                template, clip, 12f, 1f, out var controller, out var error), error);
            controller.Apply(Input(2, 12f), true, 10f);
            roundEffects = round.transform.GetChild(0).gameObject;
            sceneUnload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
        }
        catch (Exception exception) { Fail(exception); yield break; }
        yield return sceneUnload;
        try
        {
            Require(roundEffects == null, "round scene unload cleans up effects");
            Debug.Log("COSMETIC CONTROLLER PASS: per-instance rules, particle curves/bursts, spatial audio configuration and voice requests, remote approximation, zero strength, guards, death/disable/despawn/scene cleanup, unchanged host.");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception exception) { Fail(exception); }
    }

    private GameObject first, second, third, template;
    private AudioClip clip;
    private BrackenCosmeticController thirdController;
    private GameObject roundEffects;
    private AsyncOperation sceneUnload;

    private void CheckRulesAndEffects()
    {
        template = new GameObject("Synthetic particle template - no Gojo assets");
        template.SetActive(false);
        var system = template.AddComponent<ParticleSystem>();
        var main = system.main;
        main.playOnAwake = false;
        var emission = system.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(2f, AnimationCurve.Linear(0f, 20f, 1f, 60f));
        emission.rateOverDistance = 10f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 4) });
        var inactive = new GameObject("Inactive authored system");
        inactive.transform.SetParent(template.transform, false);
        inactive.AddComponent<ParticleSystem>();
        inactive.SetActive(false);
        clip = AudioClip.Create("Synthetic silence - not the Japanese line", 96000, 1, 48000, false);
        GameBindingChecks.Run(template, clip);
        first = new GameObject("First enemy anchor");
        first.transform.position = new Vector3(2f, 0f, 3f);
        first.AddComponent<MeshRenderer>();
        first.AddComponent<BoxCollider>();
        second = new GameObject("Second enemy anchor");
        third = new GameObject("Despawn anchor");
        Require(!BrackenCosmeticController.TryCreate(first.transform, Guid.Empty, template, clip,
            12f, 1f, out _, out _), "unknown game version rejected");
        Require(!BrackenCosmeticController.TryCreate(first.transform, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, float.NaN, out _, out _), "invalid strength rejected");
        Require(first.transform.childCount == 0 && first.GetComponent<BrackenCosmeticController>() == null,
            "failed validation leaves no component or effects");
        var invalidAura = new GameObject("Invalid aura with prefab audio");
        invalidAura.SetActive(false);
        invalidAura.AddComponent<ParticleSystem>();
        invalidAura.AddComponent<AudioSource>();
        Require(!BrackenCosmeticController.TryCreate(first.transform, BrackenV81Presentation.SupportedMvid,
            invalidAura, clip, 12f, 1f, out _, out _), "prefab audio rejected before instantiation");
        Destroy(invalidAura);
        Require(BrackenCosmeticController.TryCreate(first.transform, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, 1f, out var one, out var error), error);
        Require(!BrackenCosmeticController.TryCreate(first.transform, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, 1f, out _, out _), "duplicate controller rejected");
        Require(BrackenCosmeticController.TryCreate(second.transform, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, 0f, out var two, out error), error);
        Require(!first.GetComponentInChildren<ParticleSystem>(true).isPlaying
            && !first.GetComponentInChildren<AudioSource>(true).isPlaying, "attachment waits for first observation");
        one.Apply(Input(0, 0f), true, 10f);
        var aura = first.GetComponentsInChildren<ParticleSystem>(true)[0];
        var savedCurve = template.GetComponent<ParticleSystem>().emission.rateOverTime.curve;
        Require(aura.emission.rateOverTime.mode == ParticleSystemCurveMode.Curve
            && aura.emission.rateOverTime.curve.keys.Length == savedCurve.keys.Length
            && Mathf.Abs(aura.emission.rateOverTimeMultiplier - .3f) < .0001f, "authored curve and baseline multiplier preserved");
        Require(Mathf.Abs(aura.emission.rateOverDistanceMultiplier - 1.5f) < .0001f, "distance emission scales");
        Require(Mathf.Abs(aura.emission.GetBurst(0).probability - .15f) < .0001f, "bursts bounded by intensity");
        Require(!first.GetComponentsInChildren<ParticleSystem>(true)[1].isPlaying, "inactive authored child remains stopped");
        one.Apply(Input(2, 12f), true, 10f);
        one.Apply(Input(2, 12f), true, .1f);
        Require(one.VoiceRequests == 1 && aura.isPlaying, "aggro effects and single voice request");
        var voice = first.GetComponentInChildren<AudioSource>();
        Require(voice.clip == clip && voice.spatialBlend == 1f && !voice.loop && !voice.playOnAwake
            && voice.dopplerLevel == 0f && voice.minDistance == 2f && voice.maxDistance == 25f, "owned spatial voice source configured");
        one.Apply(Input(1, 6f), true, 10f);
        Require(one.LastFrame.AuraLevel > .15f && aura.isPlaying, "retreat retains anger aura");
        one.Apply(Input(2, 12f), true, 10f);
        Require(one.VoiceRequests == 2, "aggro reentry requests again");
        one.Apply(Input(1, 6f, carrying: true), true, 10f);
        Require(one.VoiceRequests == 2 && aura.isPlaying, "corpse carry continues aura without voice");
        one.Apply(Input(2, 12f, killing: true), true, 10f);
        one.Apply(Input(2, 12f), true, .1f);
        Require(one.VoiceRequests == 2, "overlay exit does not fabricate voice");
        one.Apply(Input(0, 12f), false, 10f);
        Require(one.LastFrame.UsesPhaseApproximation && one.LastFrame.AuraLevel == .15f, "remote approximation ignores local anger");
        two.Apply(Input(2, 12f), true, 10f);
        Require(two.VoiceRequests == 1 && one.VoiceRequests == 2, "independent instances");
        Require(!second.GetComponentInChildren<ParticleSystem>(true).isPlaying
            && !second.GetComponentInChildren<ParticleSystemRenderer>(true).enabled, "zero strength clears particles and hides renderers");
        one.Apply(Input(2, 12f, dead: true), true, 0f);
        Require(!voice.isPlaying && !aura.isPlaying && aura.particleCount == 0 && !aura.gameObject.activeInHierarchy,
            "death stops actual audio and particles immediately");
        one.Apply(Input(2, 12f), true, 10f);
        Require(one.VoiceRequests == 2, "terminal death cannot restart effects");
        two.enabled = false;
        Require(!second.GetComponentInChildren<AudioSource>(true).isPlaying, "disable stops voice");
        Require(template.GetComponent<ParticleSystem>().emission.rateOverTimeMultiplier == 2f,
            "source prefab emission unchanged");
    }

    private static BrackenObservation Input(int state, float anger, bool carrying = false, bool killing = false, bool dead = false)
    {
        return new BrackenObservation(state, anger, state == 2, carrying, killing, dead);
    }
    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Cosmetic controller check failed: " + reason);
    }
    private static void Fail(Exception exception)
    {
        Debug.LogException(exception);
        UnityEditor.EditorApplication.Exit(1);
    }
}
