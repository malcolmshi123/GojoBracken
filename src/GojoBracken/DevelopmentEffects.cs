using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private readonly Dictionary<Component, string> candidateStatus = new Dictionary<Component, string>();
        private float nextScanLog;
        private int lastScanScene = int.MinValue;
        private int lastFoundCount = -1;
        private bool stopped;
        private readonly bool standingModelPreview;
        private readonly bool soloLiveTest;

        private DevelopmentEffects(Type enemyType, GojoAssetBundle assets, float saturation, float strength,
            Action<string> info, Action<string> warning, bool standingModelPreview, bool soloLiveTest)
        {
            this.enemyType = enemyType;
            this.assets = assets;
            this.saturation = saturation;
            this.strength = strength;
            this.info = info;
            this.warning = warning;
            this.standingModelPreview = standingModelPreview;
            this.soloLiveTest = soloLiveTest;
            visibilityReader = new BrackenVisibilityReader(enemyType);
        }

        internal static DevelopmentEffects Start(Type enemyType, string directory, float saturation, float strength,
            Action<string> info, Action<string> warning, bool standingModelPreview = false, bool soloLiveTest = false)
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
            return new DevelopmentEffects(enemyType, assets, saturation, strength, info, warning, standingModelPreview, soloLiveTest);
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
            foreach (var key in removed)
            {
                attempted.Remove(key);
                discovered.Remove(key);
                candidateStatus.Remove(key);
            }
            var foundEnemies = UnityEngine.Object.FindObjectsOfType(enemyType, soloLiveTest);
            if (soloLiveTest)
            {
                var scene = SceneManager.GetActiveScene();
                float now = Time.realtimeSinceStartup;
                if (now >= nextScanLog || scene.handle != lastScanScene || foundEnemies.Length != lastFoundCount)
                {
                    info($"[Effects] Live scan: activeScene={scene.name}, loadedScenes={SceneManager.sceneCount}, " +
                        $"FlowermanAI found={foundEnemies.Length}, tracked={attempted.Count}.");
                    nextScanLog = now + 5f;
                    lastScanScene = scene.handle;
                    lastFoundCount = foundEnemies.Length;
                }
            }
            foreach (var found in foundEnemies)
            {
                var enemy = found as Component;
                if (enemy == null || attempted.ContainsKey(enemy)) continue;
                if (!enemy.gameObject.activeInHierarchy || (enemy is Behaviour behaviour && !behaviour.isActiveAndEnabled))
                {
                    TraceCandidate(enemy, "skipped for now: GameObject inactive or component disabled");
                    // Keep inactive candidates tracked so their diagnostic records can be removed on despawn.
                    if (soloLiveTest) discovered[enemy] = Time.frameCount;
                    else discovered.Remove(enemy);
                    continue;
                }
                // Wait for EnemyAI.Start to cache the original renderers.
                if (!discovered.TryGetValue(enemy, out int seenFrame))
                {
                    TraceCandidate(enemy, "discovered; waiting one frame for EnemyAI.Start");
                    discovered.Add(enemy, Time.frameCount);
                    continue;
                }
                if (Time.frameCount <= seenFrame) continue;
                if (!visibilityReader.IsReady(enemy))
                {
                    TraceCandidate(enemy, "skipped for now: vanilla renderer arrays are not initialized");
                    continue;
                }
                TraceCandidate(enemy, "renderer arrays ready; attempting attachment");
                attempted.Add(enemy, null); // Fail once per instance, without a repeated log/attachment loop.
                discovered.Remove(enemy);
                try
                {
                    var binding = BrackenGameBinding.Attach(enemy, enemyType.Module.ModuleVersionId,
                        assets.Aura, assets.Voice, saturation, strength, warning, standingModelPreview ? assets.Model : null,
                        standingModelPreview ? 3f / 2.1f : 1f, standingModelPreview ? 1.25f : 1f, standingModelPreview ? 1.75f : 1f,
                        soloLiveTest ? info : null, useV81BodyLayout: true, music: assets.Music, boostVoice: assets.BoostVoice);
                    attempted[enemy] = binding;
                    if (binding != null)
                        info("[Effects] Attached cosmetics to Bracken " + enemy.GetInstanceID()
                            + "; standingModelPreview=" + standingModelPreview + "; " + binding.ModelPoseStatus
                            + (standingModelPreview ? "; trial height=3.000 m; wisps=1.250; flame width/depth=1.250; flame height=1.750" : ""));
                    else TraceCandidate(enemy, "skipped: Bracken was already dead");
                }
                catch (Exception exception)
                {
                    warning("[Effects] Attachment skipped; vanilla visuals retained. " + exception.Message
                        + (soloLiveTest ? $" Candidate id={enemy.GetInstanceID()}." : ""));
                }
            }
        }

        private void TraceCandidate(Component enemy, string status)
        {
            if (!soloLiveTest || (candidateStatus.TryGetValue(enemy, out var previous) && previous == status)) return;
            candidateStatus[enemy] = status;
            info($"[Effects] FlowermanAI id={enemy.GetInstanceID()}, name={enemy.name}, scene={enemy.gameObject.scene.name}: {status}.");
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
            candidateStatus.Clear();
            // Stop/deactivate and schedule destruction of all clones before releasing bundle assets.
            assets.Dispose();
        }
    }
}
