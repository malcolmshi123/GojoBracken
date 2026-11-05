using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GojoBracken
{
    internal readonly struct BrackenVisibility
    {
        internal readonly bool Visible;
        internal readonly int Layer;
        internal BrackenVisibility(bool visible, int layer) { Visible = visible; Layer = layer; }
    }

    // Read the game's cached body arrays, never enumerate descendants (which can include carried corpses).
    internal sealed class BrackenVisibilityReader
    {
        private readonly FieldInfo skins;
        private readonly FieldInfo meshes;
        private Renderer[] originalLayout;
        private Renderer[] selectedBody;
        private Transform modelRoot;
        private Renderer particleCollision;

        internal BrackenVisibilityReader(Type type)
        {
            skins = Bind(type, "skinnedMeshRenderers", typeof(SkinnedMeshRenderer[]));
            meshes = Bind(type, "meshRenderers", typeof(MeshRenderer[]));
        }

        // EnemyAI.Start initializes these arrays. A newly spawned instance can be found before Start.
        internal bool IsReady(Component enemy)
        {
            if (skins == null || meshes == null) return true; // Let TryRead report permanent schema failure.
            return skins.GetValue(enemy) != null && meshes.GetValue(enemy) != null;
        }

        internal void SelectV81Body(Component enemy)
        {
            if (!IsReady(enemy) || skins == null || meshes == null)
                throw new InvalidOperationException("Bracken renderer arrays are unavailable.");
            var layout = SnapshotRenderers(enemy);
            var body = new List<Renderer>();
            var seen = new HashSet<Renderer>();
            Transform root = null;
            bool hasLod = false;
            Renderer auxiliary = null;
            foreach (var renderer in layout)
            {
                if (renderer == null || !renderer.transform.IsChildOf(enemy.transform) || !seen.Add(renderer))
                    throw new InvalidOperationException("Unexpected cached renderer reference in the v81 Bracken layout.");
                int layer = renderer.gameObject.layer;
                if ((layer == 22 && renderer.name == "ScanNode") || (layer == 14 && renderer.name == "MapDot (2)"))
                    continue;

                if (IsParticleCollision(renderer))
                {
                    if (auxiliary != null)
                        throw new InvalidOperationException("Multiple ParticleCollision renderers in the v81 Bracken layout.");
                    auxiliary = renderer;
                    continue;
                }

                Transform ancestor = renderer.transform.parent;
                while (ancestor != null && ancestor != enemy.transform && ancestor.name != "FlowermanModel")
                    ancestor = ancestor.parent;
                if (layer != 19 || ancestor == null || ancestor == enemy.transform || (root != null && root != ancestor))
                    throw new InvalidOperationException("Unexpected v81 Bracken renderer: " + DescribeRenderer(renderer));
                root = ancestor;
                hasLod |= renderer.name == "LOD1";
                body.Add(renderer);
            }

            if (!hasLod || body.Count == 0)
                throw new InvalidOperationException("No v81 Bracken body with LOD1 under FlowermanModel on layer 19.");
            if (auxiliary != null && auxiliary.transform.parent.parent != root)
                throw new InvalidOperationException("ParticleCollision belongs to a different model: " + DescribeRenderer(auxiliary));
            particleCollision = auxiliary;
            originalLayout = layout;
            selectedBody = body.ToArray();
            modelRoot = root;
        }

        private static bool IsParticleCollision(Renderer renderer)
        {
            if (!(renderer is MeshRenderer) || renderer.enabled || renderer.gameObject.layer != 15
                || renderer.name != "ParticleCollision") return false;
            var parent = renderer.transform.parent;
            return parent != null && parent.name == "AnimContainer"
                && parent.parent != null && parent.parent.name == "FlowermanModel";
        }

        internal Renderer[] SnapshotRenderers(Component enemy)
        {
            if (selectedBody != null) return selectedBody;
            var skinArray = (SkinnedMeshRenderer[])skins.GetValue(enemy);
            var meshArray = (MeshRenderer[])meshes.GetValue(enemy);
            var result = new Renderer[skinArray.Length + meshArray.Length];
            Array.Copy(skinArray, 0, result, 0, skinArray.Length);
            Array.Copy(meshArray, 0, result, skinArray.Length, meshArray.Length);
            return result;
        }

        internal bool TryRead(Component enemy, out BrackenVisibility visibility, out string error, Func<Renderer, bool> forceHidden = null,
            Action<string> traceVisibleLayers = null)
        {
            visibility = default;
            error = null;
            try
            {
                if (enemy == null || skins == null || meshes == null)
                    throw new InvalidOperationException("Bracken renderer fields are missing or have unexpected types.");
                var skinArray = skins.GetValue(enemy) as SkinnedMeshRenderer[];
                var meshArray = meshes.GetValue(enemy) as MeshRenderer[];
                if (skinArray == null || meshArray == null || skinArray.Length + meshArray.Length == 0)
                    throw new InvalidOperationException("Bracken body renderer arrays are not initialized or empty.");
                bool visible = false;
                bool mixed = false;
                int layer = -1;
                int fallbackLayer = -1;
                if (selectedBody != null)
                {
                    CheckLayout(skinArray, meshArray, enemy.transform);
                    Accumulate(selectedBody, enemy.transform, forceHidden, ref visible, ref layer, ref fallbackLayer, ref mixed);
                }
                else
                {
                    Accumulate(skinArray, enemy.transform, forceHidden, ref visible, ref layer, ref fallbackLayer, ref mixed);
                    Accumulate(meshArray, enemy.transform, forceHidden, ref visible, ref layer, ref fallbackLayer, ref mixed);
                }
                if (mixed)
                {
                    if (traceVisibleLayers != null)
                    {
                        if (selectedBody != null) TraceVisibleLayers(selectedBody, enemy, forceHidden, traceVisibleLayers);
                        else
                        {
                            TraceVisibleLayers(skinArray, enemy, forceHidden, traceVisibleLayers);
                            TraceVisibleLayers(meshArray, enemy, forceHidden, traceVisibleLayers);
                        }
                    }
                    throw new InvalidOperationException("Visible Bracken body renderers use mixed layers; no safe single aura layer.");
                }
                visibility = new BrackenVisibility(visible, visible ? layer : fallbackLayer);
                return true;
            }
            catch (Exception exception)
            {
                error = "Bracken visibility unavailable: " + exception.Message;
                return false;
            }
        }

        private void CheckLayout(SkinnedMeshRenderer[] skinArray, MeshRenderer[] meshArray, Transform enemy)
        {
            if (skinArray.Length + meshArray.Length != originalLayout.Length || modelRoot == null
                || !modelRoot.IsChildOf(enemy) || modelRoot.name != "FlowermanModel")
                throw new InvalidOperationException("The v81 Bracken renderer layout changed after attachment.");
            for (int i = 0; i < skinArray.Length; i++)
                if (!ReferenceEquals(skinArray[i], originalLayout[i]))
                    throw new InvalidOperationException("The v81 Bracken skinned renderer list changed after attachment.");
            for (int i = 0; i < meshArray.Length; i++)
                if (!ReferenceEquals(meshArray[i], originalLayout[skinArray.Length + i]))
                    throw new InvalidOperationException("The v81 Bracken mesh renderer list changed after attachment.");
            if (particleCollision != null && (!IsParticleCollision(particleCollision)
                || particleCollision.transform.parent.parent != modelRoot))
                throw new InvalidOperationException("The exempt ParticleCollision renderer changed: " + DescribeRenderer(particleCollision));
            foreach (var renderer in selectedBody)
                if (renderer == null || !renderer.transform.IsChildOf(modelRoot)
                    || (renderer.gameObject.layer != 19 && renderer.gameObject.layer != 23))
                    throw new InvalidOperationException("A selected Bracken renderer changed hierarchy or moved to an unexpected layer.");
        }

        private static void Accumulate(Renderer[] group, Transform enemy, Func<Renderer, bool> forceHidden, ref bool visible, ref int layer, ref int fallbackLayer, ref bool mixed)
        {
            foreach (var renderer in group)
            {
                if (renderer == null || !renderer.transform.IsChildOf(enemy))
                    throw new InvalidOperationException("Bracken body renderer is missing or outside its hierarchy.");
                if (fallbackLayer < 0) fallbackLayer = renderer.gameObject.layer;
                if (!IsVisible(renderer, forceHidden)) continue;
                int current = renderer.gameObject.layer;
                mixed |= layer >= 0 && layer != current;
                layer = current;
                visible = true;
            }
        }

        private static bool IsVisible(Renderer renderer, Func<Renderer, bool> forceHidden)
        {
            bool hidden = forceHidden != null ? forceHidden(renderer) : renderer.forceRenderingOff;
            return renderer.enabled && renderer.gameObject.activeInHierarchy && !hidden;
        }

        private static void TraceVisibleLayers(Renderer[] group, Component enemy, Func<Renderer, bool> forceHidden,
            Action<string> trace)
        {
            foreach (var renderer in group)
            {
                if (!IsVisible(renderer, forceHidden)) continue;
                trace($"[Effects] Visible body renderer: Bracken id={enemy.GetInstanceID()}, {DescribeRenderer(renderer)}");
            }
        }

        private static string DescribeRenderer(Renderer renderer)
        {
            var path = new Stack<string>();
            for (var node = renderer.transform; node != null; node = node.parent)
                path.Push($"{node.name}[{node.GetSiblingIndex()}]");
            int layer = renderer.gameObject.layer;
            string layerName = LayerMask.LayerToName(layer);
            return $"type={renderer.GetType().Name}, layer={layer} ({(string.IsNullOrEmpty(layerName) ? "unnamed" : layerName)}), " +
                $"path={string.Join("/", path)}, enabled={renderer.enabled}, active={renderer.gameObject.activeInHierarchy}.";
        }

        private static FieldInfo Bind(Type type, string name, Type expected)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.FieldType == expected ? field : null;
            }
            return null;
        }
    }
}
