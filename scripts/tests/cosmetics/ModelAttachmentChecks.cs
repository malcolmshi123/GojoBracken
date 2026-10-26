using System;
using System.Collections.Generic;
using GojoBracken;
using UnityEngine;

public static class ModelAttachmentChecks
{
    public static void Run(GameObject model, GameObject aura, AudioClip voice)
    {
        var warnings = new List<string>();
        var host = MakeEnemy(out var enemy, out var body, out var hidden, out var rig);
        var originalSkinArray = enemy.skinnedMeshRenderers;
        var originalMeshArray = enemy.meshRenderers;
        var animator = host.AddComponent<Animator>(); animator.enabled = false;
        var collider = host.AddComponent<BoxCollider>();
        var corpse = new GameObject("Carried corpse outside cached body arrays");
        corpse.transform.SetParent(rig, false);
        var corpseRenderer = corpse.AddComponent<MeshRenderer>();
        corpse.layer = 7;
        var binding = BrackenGameBinding.Attach(enemy, BrackenV81Presentation.SupportedMvid,
            aura, voice, 12f, 1f, warnings.Add, model);
        binding.Sample(.1f);
        var replacement = host.transform.Find("GojoBracken model");
        Require(replacement != null && replacement.gameObject.activeSelf, "native model attached");
        var skins = replacement.GetComponentsInChildren<SkinnedMeshRenderer>();
        Require(skins.Length == 1 && skins[0].enabled && skins[0].gameObject.layer == 19, "replacement visible on original layer");
        Require(body.forceRenderingOff && body.enabled && hidden.forceRenderingOff && !hidden.enabled,
            "only rendering masks change; original enabled flags retained");
        Require(ReferenceEquals(enemy.skinnedMeshRenderers, originalSkinArray) && ReferenceEquals(enemy.meshRenderers, originalMeshArray),
            "original renderer arrays preserved");
        Require(!animator.enabled && animator.runtimeAnimatorController == null && collider.enabled,
            "original animator and collider untouched");
        Require(corpseRenderer.enabled && !corpseRenderer.forceRenderingOff && corpse.layer == 7,
            "carried body renderer untouched");
        rig.localRotation = Quaternion.Euler(0f, 0f, 179f);
        host.transform.position = new Vector3(3f, 0f, 2f);
        host.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
        binding.Sample(.1f);
        Require(Vector3.Distance(replacement.position, host.transform.position) < .0001f
            && Vector3.Angle(replacement.up, host.transform.up) < .01f, "model follows enemy root, not rolling animation container");
        body.enabled = false;
        binding.Sample(.1f);
        Require(!skins[0].enabled && !body.enabled, "game hiding remains effective while originals are masked");
        body.enabled = true; body.gameObject.layer = 23;
        binding.Sample(.1f);
        Require(skins[0].enabled && skins[0].gameObject.layer == 23, "live layer changes preserved");
        enemy.meshRenderers = new MeshRenderer[] { null };
        binding.Sample(.1f);
        Require(!body.forceRenderingOff && hidden.forceRenderingOff && body.enabled && body.gameObject.layer == 23
            && !replacement.gameObject.activeSelf && warnings.Count == 1, "read failure rolls back model and exact prior masks without resetting game visibility");
        binding.Stop();
        Require(!body.forceRenderingOff && hidden.forceRenderingOff, "cleanup idempotent");
        UnityEngine.Object.Destroy(host);

        var badHost = MakeEnemy(out var badEnemy, out var badBody, out _, out _);
        var invalid = UnityEngine.Object.Instantiate(model);
        invalid.AddComponent<BoxCollider>();
        bool rejected = false;
        try { BrackenGameBinding.Attach(badEnemy, BrackenV81Presentation.SupportedMvid, aura, voice, 12f, 1f, warnings.Add, invalid); }
        catch (ArgumentException) { rejected = true; }
        Require(rejected && !badBody.forceRenderingOff && badHost.transform.Find("GojoBracken model") == null,
            "invalid prefab rejected before original visuals are hidden");
        UnityEngine.Object.Destroy(invalid); UnityEngine.Object.Destroy(badHost);

        var blankHost = MakeEnemy(out _, out var blankBody, out _, out _);
        var blank = UnityEngine.Object.Instantiate(model);
        foreach (var renderer in blank.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        rejected = false;
        try { BrackenModelAttachment.Create(blankHost.transform, new Renderer[] { blankBody }, blank, new BrackenVisibility(true, 19)); }
        catch (ArgumentException) { rejected = true; }
        Require(rejected && !blankBody.forceRenderingOff, "undrawable model cannot hide vanilla");
        UnityEngine.Object.Destroy(blank); UnityEngine.Object.Destroy(blankHost);

        var deadHost = MakeEnemy(out var deadEnemy, out var deadBody, out _, out _);
        var deathBinding = BrackenGameBinding.Attach(deadEnemy, BrackenV81Presentation.SupportedMvid,
            aura, voice, 12f, 1f, warnings.Add, model);
        deadEnemy.isEnemyDead = true;
        deathBinding.Sample(0f);
        Require(!deadBody.forceRenderingOff && !deadHost.transform.Find("GojoBracken model").gameObject.activeSelf,
            "death restores vanilla rendering and deactivates model");
        UnityEngine.Object.Destroy(deadHost);

        var disabledHost = MakeEnemy(out var disabledEnemy, out var disabledBody, out _, out _);
        var disabledBinding = BrackenGameBinding.Attach(disabledEnemy, BrackenV81Presentation.SupportedMvid,
            aura, voice, 12f, 1f, warnings.Add, model);
        disabledBinding.enabled = false;
        Require(!disabledBody.forceRenderingOff, "binding disable restores vanilla rendering");
        UnityEngine.Object.Destroy(disabledHost);

        var removedHost = MakeEnemy(out var removedEnemy, out var removedBody, out _, out _);
        var removedBinding = BrackenGameBinding.Attach(removedEnemy, BrackenV81Presentation.SupportedMvid,
            aura, voice, 12f, 1f, warnings.Add, model);
        removedHost.transform.Find("GojoBracken model").gameObject.SetActive(false);
        removedBinding.Sample(.1f);
        Require(!removedBody.forceRenderingOff, "externally disabled model restores vanilla");
        UnityEngine.Object.Destroy(removedHost);
        Debug.Log("MODEL ATTACHMENT PASS: validated native clone, exact vanilla mask rollback, unchanged enabled/layers/animator/collider/arrays/corpse, root following, visibility, invalid asset, failure, death and disable.");
    }

    private static GameObject MakeEnemy(out BindingOwnerEnemy enemy, out MeshRenderer body, out MeshRenderer hidden, out Transform rig)
    {
        var host = new GameObject("Synthetic Bracken model-attachment host");
        enemy = host.AddComponent<BindingOwnerEnemy>(); enemy.Owner = true;
        rig = new GameObject("Original animation container").transform; rig.SetParent(host.transform, false);
        var bodyObject = new GameObject("Original body"); bodyObject.transform.SetParent(rig, false); bodyObject.layer = 19;
        body = bodyObject.AddComponent<MeshRenderer>();
        var hiddenObject = new GameObject("Originally masked body part"); hiddenObject.transform.SetParent(rig, false); hiddenObject.layer = 19;
        hidden = hiddenObject.AddComponent<MeshRenderer>(); hidden.enabled = false; hidden.forceRenderingOff = true;
        enemy.skinnedMeshRenderers = new SkinnedMeshRenderer[0]; enemy.meshRenderers = new[] { body, hidden };
        return host;
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Model attachment check failed: " + reason);
    }
}
