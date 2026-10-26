using System;
using System.Collections.Generic;
using GojoBracken;
using UnityEngine;

// Synthetic fields test the adapter; they are not actual FlowermanAI or multiplayer evidence.
public class BindingEnemyFields : MonoBehaviour
{
    public int currentBehaviourStateIndex;
    public float angerMeter;
    public bool isInAngerMode;
    public bool carryingPlayerBody;
    public bool inKillAnimation;
    public bool isEnemyDead;
    public int health = 100;
    public SkinnedMeshRenderer[] skinnedMeshRenderers;
    public MeshRenderer[] meshRenderers;
}
public sealed class BindingOwnerEnemy : BindingEnemyFields
{
    public bool Owner;
    public bool ThrowOwner;
    public bool IsOwner { get { if (ThrowOwner) throw new InvalidOperationException("Synthetic unavailable ownership"); return Owner; } }
}

public static class GameBindingChecks
{
    public static void Run(GameObject template, AudioClip clip)
    {
        var warnings = new List<string>();
        Require(DevelopmentEffects.Start(typeof(BindingOwnerEnemy), "does-not-exist", 12f, 1f,
            _ => { }, warnings.Add) == null && warnings.Count == 1 && warnings[0].Contains("v81"),
            "production session rejects actual synthetic MVID before bundle access");
        warnings.Clear();
        var host = new GameObject("Synthetic observed enemy");
        var enemy = host.AddComponent<BindingOwnerEnemy>();
        var visibilityReader = new BrackenVisibilityReader(enemy.GetType());
        Require(!visibilityReader.IsReady(enemy), "defer new enemy until Start initializes renderer arrays");
        var body = PrepareBody(enemy);
        Require(visibilityReader.IsReady(enemy), "initialized renderer arrays are ready");
        var corpse = new GameObject("Unrelated carried body renderer");
        corpse.transform.SetParent(host.transform, false);
        corpse.layer = 7;
        var corpseRenderer = corpse.AddComponent<MeshRenderer>();
        enemy.Owner = true;
        enemy.angerMeter = 12f;
        // Only this isolated binding test supplies the known MVID; session startup uses the real module identity.
        var binding = BrackenGameBinding.Attach(enemy, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, 1f, warnings.Add);
        var controller = host.GetComponent<BrackenCosmeticController>();
        binding.Sample(10f);
        Require(controller.LastFrame.AuraLevel == 1f && !controller.LastFrame.UsesPhaseApproximation, "owner reads detailed anger");
        Require(visibilityReader.TryRead(enemy, out var visible, out _) && visible.Layer == 19 && visible.Visible,
            "only cached body arrays determine visibility, not carried descendants");
        var auraRenderers = host.transform.Find("GojoBracken cosmetics").GetComponentsInChildren<Renderer>(true);
        Require(Array.TrueForAll(auraRenderers, renderer => renderer.enabled && renderer.gameObject.layer == 19), "aura inherits body layer");
        body.enabled = false;
        binding.Sample(.1f);
        Require(Array.TrueForAll(auraRenderers, renderer => !renderer.enabled) && !body.enabled, "disabled body hides aura without rewriting body flag");
        body.enabled = true;
        body.gameObject.layer = 23;
        binding.Sample(.1f);
        Require(Array.TrueForAll(auraRenderers, renderer => renderer.enabled && renderer.gameObject.layer == 23), "camera-layer changes propagate to aura");
        body.forceRenderingOff = true;
        binding.Sample(.1f);
        Require(Array.TrueForAll(auraRenderers, renderer => !renderer.enabled) && body.forceRenderingOff, "force-hidden body hides aura without rewriting source");
        body.forceRenderingOff = false;
        body.gameObject.SetActive(false);
        binding.Sample(.1f);
        Require(Array.TrueForAll(auraRenderers, renderer => !renderer.enabled), "inactive body hides aura");
        body.gameObject.SetActive(true);
        body.gameObject.layer = 19;
        enemy.meshRenderers = new[] { body, corpseRenderer };
        Require(!visibilityReader.TryRead(enemy, out _, out var mixedError) && mixedError.Contains("mixed layers"), "ambiguous body layers fail closed");
        enemy.meshRenderers = new MeshRenderer[] { null };
        Require(!visibilityReader.TryRead(enemy, out _, out _), "missing body renderer fails closed");
        var external = new GameObject("Renderer outside the enemy hierarchy");
        enemy.meshRenderers = new[] { external.AddComponent<MeshRenderer>() };
        Require(!visibilityReader.TryRead(enemy, out _, out _), "external renderer reference fails closed");
        UnityEngine.Object.Destroy(external);
        enemy.meshRenderers = new[] { body };
        binding.Sample(.1f);
        Require(corpseRenderer.enabled && corpse.layer == 7, "carried body remains untouched");
        enemy.Owner = false;
        binding.Sample(10f);
        Require(controller.LastFrame.AuraLevel == .15f && controller.LastFrame.UsesPhaseApproximation, "remote uses phase approximation");
        enemy.currentBehaviourStateIndex = 2;
        enemy.angerMeter = 0f;
        binding.Sample(10f);
        binding.Sample(.1f);
        Require(controller.LastFrame.AuraLevel == 1f && controller.VoiceRequests == 1, "remote raw phase drives voice once");
        enemy.ThrowOwner = true;
        binding.Sample(.1f);
        binding.Sample(.1f);
        Require(warnings.Count == 1 && controller.LastFrame.UsesPhaseApproximation, "failed ownership getter falls back and warns once");
        enemy.carryingPlayerBody = true;
        binding.Sample(.1f);
        Require(controller.LastFrame.Phase == BrackenPhase.CorpseDragging && controller.VoiceRequests == 1,
            "carrying overlay read without extra voice");
        enemy.angerMeter = float.NaN;
        binding.Sample(.1f);
        binding.Sample(.1f);
        Require(warnings.Count == 2 && warnings[1].Contains("Binding stopped")
            && !host.transform.Find("GojoBracken cosmetics").gameObject.activeSelf, "schema/read failure stops effects and logs once");
        Require(enemy.health == 100 && enemy.currentBehaviourStateIndex == 2
            && enemy.carryingPlayerBody && float.IsNaN(enemy.angerMeter), "binding never rewrites enemy fields");
        UnityEngine.Object.Destroy(host);

        var unknownHost = new GameObject("Synthetic unknown ownership");
        var unknown = unknownHost.AddComponent<BindingEnemyFields>();
        PrepareBody(unknown);
        unknown.angerMeter = 12f;
        var unknownBinding = BrackenGameBinding.Attach(unknown, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, 1f, warnings.Add);
        unknownBinding.Sample(10f);
        Require(unknownHost.GetComponent<BrackenCosmeticController>().LastFrame.AuraLevel == .15f,
            "missing ownership property conservatively approximates");
        unknown.isEnemyDead = true;
        unknownBinding.Sample(0f);
        Require(!unknownHost.transform.Find("GojoBracken cosmetics").gameObject.activeSelf, "observed death stops owned effects");
        UnityEngine.Object.Destroy(unknownHost);
        var deadHost = new GameObject("Already dead enemy");
        var dead = deadHost.AddComponent<BindingEnemyFields>();
        dead.isEnemyDead = true;
        Require(BrackenGameBinding.Attach(dead, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, 1f, warnings.Add) == null && deadHost.transform.childCount == 0,
            "already dead enemy gets no controller or effects");
        UnityEngine.Object.Destroy(deadHost);
        var disabledHost = new GameObject("Disabled enemy component");
        var disabled = disabledHost.AddComponent<BindingEnemyFields>();
        PrepareBody(disabled);
        var disabledBinding = BrackenGameBinding.Attach(disabled, BrackenV81Presentation.SupportedMvid,
            template, clip, 12f, 1f, warnings.Add);
        disabled.enabled = false;
        disabledBinding.Sample(.1f);
        Require(!disabledHost.transform.Find("GojoBracken cosmetics").gameObject.activeSelf, "disabled enemy component stops effects");
        UnityEngine.Object.Destroy(disabledHost);
        Debug.Log("GAME BINDING PASS: actual-module gate, owner/non-owner/unknown reads, voice edges, overlays, fail-closed sampling, death, no enemy field writes.");
        Debug.Log("GAME VISIBILITY PASS: initialized cached arrays, enabled/active/force-hidden state, live camera layers, carried-body exclusion, mixed-layer rejection, no source writes.");
    }
    private static MeshRenderer PrepareBody(BindingEnemyFields enemy)
    {
        var body = new GameObject("Synthetic vanilla body");
        body.transform.SetParent(enemy.transform, false);
        body.layer = 19;
        var renderer = body.AddComponent<MeshRenderer>();
        enemy.skinnedMeshRenderers = new SkinnedMeshRenderer[0];
        enemy.meshRenderers = new[] { renderer };
        return renderer;
    }
    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Game binding check failed: " + reason);
    }
}
