using System;
using System.Collections.Generic;
using UnityEngine;

namespace GojoBracken
{
    // Owns only a cosmetic clone and a reversible rendering mask. Never disables the original rig.
    internal sealed class BrackenModelAttachment : IDisposable
    {
        private readonly Dictionary<Renderer, bool> originalMasks = new Dictionary<Renderer, bool>();
        private GameObject root;
        private Renderer[] renderers;
        private bool[] authoredVisible;
        private bool disposed;
        private bool committed;
        private GojoLocomotion locomotion;
        private GojoActionPose actions;
        internal string MotionStatus { get; private set; }

        internal static BrackenModelAttachment Create(Transform host, Renderer[] originals, GameObject prefab, BrackenVisibility visibility)
        {
            if (host == null || originals == null || originals.Length == 0)
                throw new ArgumentException("A live anchor and explicit vanilla body renderers are required.");
            Validate(prefab);
            var attachment = new BrackenModelAttachment();
            try
            {
                foreach (var renderer in originals)
                {
                    if (renderer == null || !renderer.transform.IsChildOf(host))
                        throw new ArgumentException("A vanilla body renderer is missing or outside the enemy hierarchy.");
                    attachment.originalMasks.Add(renderer, renderer.forceRenderingOff);
                }
                attachment.root = new GameObject("GojoBracken model");
                attachment.root.SetActive(false);
                attachment.root.transform.SetParent(host, false);
                var clone = UnityEngine.Object.Instantiate(prefab, attachment.root.transform, false);
                clone.SetActive(true);
                foreach (var animator in clone.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
                attachment.renderers = clone.GetComponentsInChildren<Renderer>(true);
                attachment.authoredVisible = Array.ConvertAll(attachment.renderers, renderer => renderer.enabled);
                attachment.locomotion = GojoLocomotion.TryCreate(host, attachment.root.transform, out var status);
                attachment.actions = GojoActionPose.TryCreate(host, attachment.root.transform, out var actionStatus);
                attachment.MotionStatus = status + "; " + actionStatus;
                // All validation and cloning finishes before the first vanilla rendering change.
                attachment.Sync(visibility);
                attachment.root.SetActive(true);
                attachment.committed = true;
                return attachment;
            }
            catch { attachment.Dispose(); throw; }
        }

        private static void Validate(GameObject prefab)
        {
            if (prefab == null || !prefab.activeSelf) throw new ArgumentException("The Gojo model prefab is missing or inactive.");
            foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
                foreach (var component in node.GetComponents<Component>())
                    if (!(component is Transform || component is Animator || component is SkinnedMeshRenderer
                        || component is MeshRenderer || component is MeshFilter))
                        throw new ArgumentException("Gojo model contains a missing or unsupported component: " + node.name);
            var skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length == 0) throw new ArgumentException("Gojo model requires a skinned mesh.");
            foreach (var skin in skins)
            {
                if (skin.sharedMesh == null || skin.sharedMesh.vertexCount == 0 || skin.bones.Length == 0
                    || skin.bones.Length != skin.sharedMesh.bindposes.Length)
                    throw new ArgumentException("Gojo skin mesh/bind poses are incomplete.");
                foreach (var bone in skin.bones)
                    if (bone == null || !bone.IsChildOf(prefab.transform))
                        throw new ArgumentException("Gojo skin has a missing or external bone.");
            }
            var animators = prefab.GetComponentsInChildren<Animator>(true);
            if (animators.Length == 0) throw new ArgumentException("Gojo model requires an Animator.");
            foreach (var animator in animators)
            {
                if (animator.runtimeAnimatorController == null) throw new ArgumentException("Gojo Animator has no controller.");
                if (animator.runtimeAnimatorController.animationClips.Length == 0)
                    throw new ArgumentException("Gojo Animator has no clips.");
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (clip == null || clip.events.Length != 0)
                        throw new ArgumentException("Cosmetic model clips must exist and contain no animation events.");
            }
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0)
                    throw new ArgumentException("Gojo model has an empty static mesh.");
            bool drawable = false;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials.Length == 0) throw new ArgumentException("Gojo renderer has no materials.");
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported
                        || material.shader.name == "Hidden/InternalErrorShader")
                        throw new ArgumentException("Gojo model has a missing or unsupported material/shader.");
                bool active = true;
                for (var node = renderer.transform; node != null && node != prefab.transform.parent; node = node.parent)
                    active &= node.gameObject.activeSelf;
                drawable |= active && renderer.enabled && !renderer.forceRenderingOff;
            }
            if (!drawable) throw new ArgumentException("Gojo model has no drawable renderer.");
        }

        // The reader must ignore our own forceRenderingOff mask while respecting its original value.
        internal bool IsHiddenByGame(Renderer renderer)
        {
            if (disposed || !originalMasks.TryGetValue(renderer, out bool hidden))
                throw new InvalidOperationException("The vanilla body renderer set changed during model attachment.");
            return hidden;
        }

        internal void Sync(BrackenVisibility visibility)
        {
            if (disposed || root == null || (committed && !root.activeSelf))
                throw new InvalidOperationException("The Gojo visual was removed or disabled.");
            foreach (var renderer in renderers)
                if (renderer == null) throw new InvalidOperationException("A Gojo renderer was removed.");
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = authoredVisible[i] && visibility.Visible;
                renderers[i].gameObject.layer = visibility.Layer;
            }
            foreach (var renderer in originalMasks.Keys)
            {
                if (renderer == null) throw new InvalidOperationException("A vanilla body renderer was removed.");
                renderer.forceRenderingOff = true;
            }
        }

        internal void Animate(BrackenObservation observation, float deltaTime)
        {
            actions?.Restore();
            locomotion?.Step(deltaTime, observation.InKillAnimation || observation.Dead);
            actions?.Apply(observation, deltaTime);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            foreach (var pair in originalMasks)
                if (pair.Key != null) pair.Key.forceRenderingOff = pair.Value;
            originalMasks.Clear();
        }
    }
}
