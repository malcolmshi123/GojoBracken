using System;
using UnityEngine;

namespace GojoBracken
{
    // A future game adapter supplies observations. This component never calls AI or network methods.
    internal sealed class BrackenCosmeticController : MonoBehaviour
    {
        private BrackenV81Presentation presentation;
        private GameObject effects;
        private ParticleSystem[] particles;
        private float[] timeRates;
        private float[] distanceRates;
        private ParticleSystem.Burst[][] bursts;
        private ParticleSystem.Burst[][] scaledBursts;
        private Renderer[] renderers;
        private Vector3[] rendererScales;
        private bool[] visible;
        private MaterialPropertyBlock[] properties;
        // Measured from the comparison bundle over the first 5.094 seconds.
        private const float BalancedVoiceVolume = .95493f;
        private const float BalancedMusicVolume = .32137f;
        private float musicMaxVolume = 1f;
        private AudioSource voice;
        private AudioSource music;
        private float strength;
        private bool stopped;
        private bool visibleByGame = true;
        private int gameLayer;
        internal int VoiceRequests { get; private set; }
        internal BrackenCosmeticFrame LastFrame { get; private set; }

        // The caller must pass the observed game assembly MVID, not a guessed version number.
        internal static bool TryCreate(Transform host, Guid gameMvid, GameObject auraPrefab, AudioClip clip,
            float saturationAnger, float auraStrength, out BrackenCosmeticController controller, out string error, AudioClip musicClip = null, bool boostVoice = false)
        {
            controller = null;
            error = null;
            BrackenCosmeticController pending = null;
            try
            {
                if (host == null || !host.gameObject.activeInHierarchy)
                    throw new ArgumentException("A live, active visual anchor is required.");
                if (host.GetComponent<BrackenCosmeticController>() != null)
                    throw new InvalidOperationException("This anchor already has a cosmetic controller.");
                var rules = new BrackenV81Presentation(gameMvid, saturationAnger);
                if (float.IsNaN(auraStrength) || float.IsInfinity(auraStrength) || auraStrength < 0f || auraStrength > 3f)
                    throw new ArgumentOutOfRangeException(nameof(auraStrength), "Aura strength must be between 0 and 3.");
                if (auraPrefab == null || auraPrefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                    throw new ArgumentException("An aura prefab with particles is required.");
                if (clip == null || clip.samples <= 0)
                    throw new ArgumentException("A nonempty voice clip is required.");
                // Validate before instantiation: no scripts, colliders, animation events, or prefab audio.
                foreach (var node in auraPrefab.GetComponentsInChildren<Transform>(true))
                    foreach (var component in node.GetComponents<Component>())
                        if (!(component is Transform || component is ParticleSystem || component is ParticleSystemRenderer
                            || component is MeshRenderer || component is MeshFilter))
                            throw new ArgumentException("Aura contains an unsupported or missing component on " + node.name);
                pending = host.gameObject.AddComponent<BrackenCosmeticController>();
                pending.presentation = rules;
                pending.strength = auraStrength;
                pending.effects = new GameObject("GojoBracken cosmetics");
                pending.effects.transform.SetParent(host, false);
                // Prevent play-on-awake while preparing the clone.
                pending.effects.SetActive(false);
                var aura = Instantiate(auraPrefab, pending.effects.transform, false);
                aura.SetActive(true);
                pending.particles = aura.GetComponentsInChildren<ParticleSystem>(true);
                pending.timeRates = new float[pending.particles.Length];
                pending.distanceRates = new float[pending.particles.Length];
                pending.bursts = new ParticleSystem.Burst[pending.particles.Length][];
                pending.scaledBursts = new ParticleSystem.Burst[pending.particles.Length][];
                for (int i = 0; i < pending.particles.Length; i++)
                {
                    var system = pending.particles[i];
                    var main = system.main;
                    main.playOnAwake = false;
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var emission = system.emission;
                    pending.timeRates[i] = emission.rateOverTimeMultiplier;
                    pending.distanceRates[i] = emission.rateOverDistanceMultiplier;
                    var saved = new ParticleSystem.Burst[emission.burstCount];
                    emission.GetBursts(saved);
                    pending.bursts[i] = saved;
                    pending.scaledBursts[i] = (ParticleSystem.Burst[])saved.Clone();
                }
                pending.renderers = aura.GetComponentsInChildren<Renderer>(true);
                pending.rendererScales = Array.ConvertAll(pending.renderers, renderer => renderer.transform.localScale);
                pending.visible = Array.ConvertAll(pending.renderers, renderer => renderer.enabled);
                pending.properties = Array.ConvertAll(pending.renderers, renderer => new MaterialPropertyBlock());
                foreach (var renderer in pending.renderers) renderer.enabled = false;
                pending.voice = pending.CreateAudioSource(clip, loop: false);
                if (boostVoice)
                {
                    pending.voice.gameObject.AddComponent<GojoVoiceGain>();
                    pending.voice.volume = BalancedVoiceVolume;
                    pending.musicMaxVolume = BalancedMusicVolume;
                }
                if (musicClip != null && musicClip.samples > 0)
                    pending.music = pending.CreateAudioSource(musicClip, loop: true);
                pending.effects.SetActive(true);
                controller = pending;
                return true;
            }
            catch (Exception exception)
            {
                if (pending != null)
                {
                    pending.Shutdown();
                    Destroy(pending);
                }
                error = "Cosmetic attachment failed; keep vanilla Bracken visible. " + exception.Message;
                return false;
            }
        }

        private AudioSource CreateAudioSource(AudioClip clip, bool loop)
        {
            var audioObject = new GameObject(loop ? "Gojo aggro music" : "Gojo aggro voice");
            audioObject.transform.SetParent(effects.transform, false);
            var source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.clip = clip;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = 25f;
            return source;
        }

        internal void Apply(BrackenObservation observation, bool useDetailedAnger, float deltaTime)
        {
            if (stopped || presentation == null) return;
            LastFrame = presentation.Evaluate(observation, useDetailedAnger, deltaTime);
            if (LastFrame.StopEffects || effects == null) { Shutdown(); return; }
            float amount = LastFrame.AuraLevel * strength;
            float visualAnger = Mathf.Clamp01((LastFrame.AuraLevel - BrackenV81Presentation.BaselineAura)
                / (1f - BrackenV81Presentation.BaselineAura));
            for (int i = 0; i < particles.Length; i++)
            {
                var system = particles[i];
                if (system == null) { Shutdown(); return; }
                var emission = system.emission;
                emission.rateOverTimeMultiplier = timeRates[i] * amount;
                emission.rateOverDistanceMultiplier = distanceRates[i] * amount;
                var scaled = scaledBursts[i];
                for (int b = 0; b < scaled.Length; b++)
                    scaled[b].probability = bursts[i][b].probability * Mathf.Clamp01(amount);
                emission.SetBursts(scaled);
                if (amount == 0f) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                else if (system.gameObject.activeInHierarchy && !system.isPlaying) system.Play(false);
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) { Shutdown(); return; }
                renderer.enabled = visible[i] && amount > 0f && visibleByGame;
                renderer.gameObject.layer = gameLayer;
                // Preserve existing property overrides; only compatible aura materials use these controls.
                renderer.GetPropertyBlock(properties[i]);
                properties[i].SetFloat("_Strength", visualAnger);
                properties[i].SetFloat("_Intensity", strength);
                renderer.SetPropertyBlock(properties[i]);
            }
            if (music != null)
            {
                music.volume = visualAnger * musicMaxVolume;
                if (observation.StateIndex != 2 && music.isPlaying) music.Stop();
            }
            if (LastFrame.PlayAggroVoice)
            {
                VoiceRequests++;
                voice.Stop();
                voice.Play();
                if (music != null) music.Play();
            }
        }

        internal void Shutdown()
        {
            if (stopped) return;
            stopped = true;
            presentation?.Reset();
            if (voice != null) voice.Stop();
            if (music != null) music.Stop();
            if (particles != null)
                foreach (var system in particles)
                    if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (effects != null)
            {
                effects.SetActive(false);
                Destroy(effects);
            }
        }

        internal Vector3 WorldPosition => effects.transform.position;

        internal void SetEffectScale(float wispScale, float flameHeightScale)
        {
            if (stopped || effects == null) return;
            effects.transform.localScale = Vector3.one * wispScale;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!(renderers[i] is MeshRenderer)) continue;
                var scale = rendererScales[i];
                scale.y *= flameHeightScale / wispScale;
                renderers[i].transform.localScale = scale;
            }
            foreach (var system in particles)
            {
                var main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                // Existing world-space wisps belong to the previous size.
                system.Clear(false);
            }
        }

        internal void SetVisibility(BrackenVisibility visibility)
        {
            if (visibility.Layer < 0 || visibility.Layer > 31)
                throw new ArgumentOutOfRangeException(nameof(visibility));
            visibleByGame = visibility.Visible;
            gameLayer = visibility.Layer;
        }

        private void OnDisable() { Shutdown(); }
        private void OnDestroy() { Shutdown(); }
    }

    internal sealed class GojoVoiceGain : MonoBehaviour
    {
        internal void OnAudioFilterRead(float[] samples, int channels)
        {
            for (int i = 0; i < samples.Length; i++) samples[i] *= 2f;
        }
    }
}
