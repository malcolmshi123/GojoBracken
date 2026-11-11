using System;
using System.Reflection;
using UnityEngine;

namespace GojoBracken
{
    // Reads after the enemy's Update. No AI calls, field writes, Harmony patches, or network messages.
    internal sealed class BrackenGameBinding : MonoBehaviour
    {
        private Component enemy;
        private BrackenStateReader reader;
        private BrackenVisibilityReader visibilityReader;
        private PropertyInfo owner;
        private BrackenCosmeticController controller;
        private Action<string> warning;
        private bool ownerWarning;
        private bool stopped;
        private BrackenModelAttachment replacement;
        private Func<Renderer, bool> forceHidden;
        private Action<string> traceVisibleLayers;
        private bool motionLogged;
        private Vector3? previousVisiblePosition;
        private float sampledTravel;
        internal string ModelPoseStatus => replacement != null ? "fixed standing pose" : "model disabled";

        // The session verifies the actual type/module identity before calling this binding.
        internal static BrackenGameBinding Attach(Component enemy, Guid gameMvid, GameObject aura, AudioClip voice,
            float saturation, float strength, Action<string> warning, GameObject modelPrefab = null, float modelScale = 1f, float wispScale = 1f, float flameHeightScale = 1f,
            Action<string> traceVisibleLayers = null, bool useV81BodyLayout = false, AudioClip music = null)
        {
            var reader = new BrackenStateReader(enemy.GetType());
            if (!reader.TryRead(enemy, out var initial, out var error)) throw new InvalidOperationException(error);
            if (initial.Dead) return null;
            var visibilityReader = new BrackenVisibilityReader(enemy.GetType());
            if (useV81BodyLayout) visibilityReader.SelectV81Body(enemy);
            if (!visibilityReader.TryRead(enemy, out var initialVisibility, out error, traceVisibleLayers: traceVisibleLayers)) throw new InvalidOperationException(error);
            if (!BrackenCosmeticController.TryCreate(enemy.transform, gameMvid, aura, voice, saturation, strength,
                out var controller, out error, music)) throw new InvalidOperationException(error);
            BrackenGameBinding binding = null;
            BrackenModelAttachment replacement = null;
            try
            {
                if (modelPrefab != null)
                    replacement = BrackenModelAttachment.Create(enemy.transform,
                        visibilityReader.SnapshotRenderers(enemy), modelPrefab, initialVisibility, modelScale);
                if (wispScale != 1f || flameHeightScale != 1f) controller.SetEffectScale(wispScale, flameHeightScale);
                binding = enemy.gameObject.AddComponent<BrackenGameBinding>();
                binding.enemy = enemy;
                binding.reader = reader;
                binding.visibilityReader = visibilityReader;
                binding.controller = controller;
                binding.warning = warning;
                binding.traceVisibleLayers = traceVisibleLayers;
                binding.replacement = replacement;
                if (replacement != null) binding.forceHidden = replacement.IsHiddenByGame;
                var property = enemy.GetType().GetProperty("IsOwner", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                binding.owner = property != null && property.PropertyType == typeof(bool) && property.CanRead
                    && property.GetIndexParameters().Length == 0 ? property : null;
                binding.Sample(0f);
                if (binding.stopped) throw new InvalidOperationException("Initial cosmetic sample failed; vanilla visuals restored.");
                return binding;
            }
            catch
            {
                replacement?.Dispose();
                controller.Shutdown();
                Destroy(controller);
                if (binding != null) Destroy(binding);
                throw;
            }
        }

        private void LateUpdate() { Sample(Time.deltaTime); }

        internal void Sample(float deltaTime)
        {
            if (stopped) return;
            try
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || controller == null
                    || (enemy is Behaviour behaviour && !behaviour.isActiveAndEnabled))
                { Stop(); return; }
                if (!reader.TryRead(enemy, out var observation, out var error))
                    throw new InvalidOperationException(error);
                if (observation.Dead) { controller.Apply(observation, false, deltaTime); Stop(); return; }
                if (!visibilityReader.TryRead(enemy, out var visibility, out error, forceHidden, traceVisibleLayers))
                    throw new InvalidOperationException(error);
                replacement?.Sync(visibility);
                controller.SetVisibility(visibility);
                bool detailed = false;
                try
                {
                    if (owner != null) detailed = (bool)owner.GetValue(enemy, null);
                    else WarnOwnership();
                }
                catch { WarnOwnership(); }
                controller.Apply(observation, detailed, deltaTime);
                TraceMotion(visibility.Visible, deltaTime);
            }
            catch (Exception exception)
            {
                Stop();
                warning?.Invoke("[Effects] Binding stopped; vanilla visuals restored. " + exception.Message);
            }
        }

        private void TraceMotion(bool visible, float deltaTime)
        {
            if (traceVisibleLayers == null || motionLogged || replacement == null) return;
            if (!visible || deltaTime <= 0f)
            {
                previousVisiblePosition = null;
                sampledTravel = 0f;
                return;
            }

            Vector3 current = enemy.transform.position;
            Vector3? previous = previousVisiblePosition;
            previousVisiblePosition = current;
            if (!previous.HasValue) return;
            float step = Vector3.ProjectOnPlane(current - previous.Value, Vector3.up).magnitude;
            if (step > .75f)
            {
                sampledTravel = 0f;
                return;
            }

            sampledTravel += step;
            if (sampledTravel < .25f) return;
            motionLogged = true;
            traceVisibleLayers(FormattableString.Invariant(
                $"[Effects] Motion sample: Bracken id={enemy.GetInstanceID()}, horizontal travel={sampledTravel:F3} m, model anchor offset={Vector3.Distance(current, replacement.WorldPosition):F3} m, aura anchor offset={Vector3.Distance(current, controller.WorldPosition):F3} m."));
        }

        private void WarnOwnership()
        {
            if (ownerWarning) return;
            ownerWarning = true;
            warning?.Invoke("[Effects] Ownership unavailable; using phase approximation for this Bracken.");
        }

        internal void Stop()
        {
            if (stopped) return;
            stopped = true;
            replacement?.Dispose();
            if (controller != null)
            {
                controller.Shutdown();
                Destroy(controller);
            }
        }

        private void OnDisable() { Stop(); }
        private void OnDestroy() { Stop(); }
    }
}
