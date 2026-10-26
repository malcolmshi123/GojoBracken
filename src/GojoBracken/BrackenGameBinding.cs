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

        // The session verifies the actual type/module identity before calling this binding.
        internal static BrackenGameBinding Attach(Component enemy, Guid gameMvid, GameObject aura, AudioClip voice,
            float saturation, float strength, Action<string> warning, GameObject modelPrefab = null)
        {
            var reader = new BrackenStateReader(enemy.GetType());
            if (!reader.TryRead(enemy, out var initial, out var error)) throw new InvalidOperationException(error);
            if (initial.Dead) return null;
            var visibilityReader = new BrackenVisibilityReader(enemy.GetType());
            if (!visibilityReader.TryRead(enemy, out var initialVisibility, out error)) throw new InvalidOperationException(error);
            if (!BrackenCosmeticController.TryCreate(enemy.transform, gameMvid, aura, voice, saturation, strength,
                out var controller, out error)) throw new InvalidOperationException(error);
            BrackenGameBinding binding = null;
            BrackenModelAttachment replacement = null;
            try
            {
                if (modelPrefab != null)
                    replacement = BrackenModelAttachment.Create(enemy.transform,
                        visibilityReader.SnapshotRenderers(enemy), modelPrefab, initialVisibility);
                binding = enemy.gameObject.AddComponent<BrackenGameBinding>();
                binding.enemy = enemy;
                binding.reader = reader;
                binding.visibilityReader = visibilityReader;
                binding.controller = controller;
                binding.warning = warning;
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
                if (!visibilityReader.TryRead(enemy, out var visibility, out error, forceHidden))
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
            }
            catch (Exception exception)
            {
                Stop();
                warning?.Invoke("[Effects] Binding stopped; vanilla visuals restored. " + exception.Message);
            }
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
