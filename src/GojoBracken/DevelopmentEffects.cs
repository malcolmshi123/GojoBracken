using System;
using System.Collections.Generic;
using UnityEngine;

namespace GojoBracken
{
    // Default-off cosmetics; optional standing-model preview has reversible vanilla rendering masks.
    internal sealed class DevelopmentEffects : IDisposable
    {
        private readonly Type enemyType;
        private readonly GojoAssetBundle assets;
        private readonly float saturation;
        private readonly float strength;
        private readonly Action<string> info;
        private readonly Action<string> warning;
        private readonly BrackenVisibilityReader visibilityReader;
        private readonly Dictionary<Component, BrackenGameBinding> attempted = new Dictionary<Component, BrackenGameBinding>();
        private readonly Dictionary<Component, int> discovered = new Dictionary<Component, int>();
        private bool stopped;
        private readonly bool standingModelPreview;

        private DevelopmentEffects(Type enemyType, GojoAssetBundle assets, float saturation, float strength,
            Action<string> info, Action<string> warning, bool standingModelPreview)
        {
            this.enemyType = enemyType;
            this.assets = assets;
            this.saturation = saturation;
            this.strength = strength;
            this.info = info;
            this.warning = warning;
            this.standingModelPreview = standingModelPreview;
            visibilityReader = new BrackenVisibilityReader(enemyType);
        }

        internal static DevelopmentEffects Start(Type enemyType, string directory, float saturation, float strength,
            Action<string> info, Action<string> warning, bool standingModelPreview = false)
        {
            if (enemyType == null || !typeof(Component).IsAssignableFrom(enemyType)
                || enemyType.Module.ModuleVersionId != BrackenV81Presentation.SupportedMvid)
            {
                warning("[Effects] Disabled: FlowermanAI does not match the inspected v81 assembly. Vanilla visuals retained.");
                return null;
            }
            // Check parameters before opening a bundle.
            _ = new BrackenV81Presentation(enemyType.Module.ModuleVersionId, saturation);
            if (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f || strength > 3f)
                throw new ArgumentOutOfRangeException(nameof(strength));
            if (!GojoAssetBundle.TryLoad(directory, out var assets, out var error))
            {
                warning("[Effects] Disabled: " + error);
                return null;
            }
            info("[Effects] Development cosmetics enabled; standingModelPreview=" + standingModelPreview
                + ". Non-owner/unknown anger uses phase approximation. Animation adaptation is incomplete.");
            return new DevelopmentEffects(enemyType, assets, saturation, strength, info, warning, standingModelPreview);
        }

        // Called by the plugin's existing 0.5-second real-time coroutine; bindings sample each LateUpdate.
        internal void Scan()
        {
            if (stopped) return;
            var removed = new List<Component>();
            foreach (var entry in attempted)
                if (entry.Key == null) removed.Add(entry.Key);
            foreach (var entry in discovered)
                if (entry.Key == null) removed.Add(entry.Key);
            foreach (var key in removed) { attempted.Remove(key); discovered.Remove(key); }
            foreach (var found in UnityEngine.Object.FindObjectsOfType(enemyType))
            {
                var enemy = found as Component;
                if (enemy == null || attempted.ContainsKey(enemy)) continue;
                if (enemy is Behaviour behaviour && !behaviour.isActiveAndEnabled) { discovered.Remove(enemy); continue; }
                // The arrays are serialized and can already be non-null on a prefab. Never attach in
                // its discovery frame: EnemyAI.Start must first cache the original body, not our aura.
                if (!discovered.TryGetValue(enemy, out int seenFrame))
                { discovered.Add(enemy, Time.frameCount); continue; }
                if (Time.frameCount <= seenFrame) continue;
                if (!visibilityReader.IsReady(enemy)) continue;
                attempted.Add(enemy, null); // Fail once per instance, without a repeated log/attachment loop.
                discovered.Remove(enemy);
                try
                {
                    var binding = BrackenGameBinding.Attach(enemy, enemyType.Module.ModuleVersionId,
                        assets.Aura, assets.Voice, saturation, strength, warning, standingModelPreview ? assets.Model : null);
                    attempted[enemy] = binding;
                    if (binding != null)
                        info("[Effects] Attached cosmetics to Bracken " + enemy.GetInstanceID()
                            + "; standingModelPreview=" + standingModelPreview);
                }
                catch (Exception exception)
                {
                    warning("[Effects] Attachment skipped; vanilla visuals retained. " + exception.Message);
                }
            }
        }

        public void Dispose()
        {
            if (stopped) return;
            stopped = true;
            foreach (var binding in attempted.Values)
                if (binding != null)
                {
                    binding.Stop();
                    UnityEngine.Object.Destroy(binding);
                }
            attempted.Clear();
            discovered.Clear();
            // Stop/deactivate and schedule destruction of all clones before releasing bundle assets.
            assets.Dispose();
        }
    }
}
