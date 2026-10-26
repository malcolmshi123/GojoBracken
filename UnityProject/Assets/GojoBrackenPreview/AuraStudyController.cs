using UnityEngine;

namespace GojoBracken.Authoring
{
    // Authoring only: simulated inputs drive the same state machine compiled into the plugin.
    public sealed class AuraStudyController : MonoBehaviour
    {
        public enum PreviewPhase { Hunting, Retreat, Aggro, Dragging }
        public ParticleSystem Aura;
        public AudioSource VoiceSource;
        [Range(0f, 1f)] public float Anger;
        public PreviewPhase Phase;
        public bool UsePhaseApproximation;
        public bool InKillAnimation;
        public bool Dead;
        [Range(0f, 3f)] public float AuraStrength = 1f;

        public int VoiceTriggerCount { get; private set; }
        public float VisibleAuraLevel { get; private set; }
        public bool EffectsStopped { get; private set; }
        public string ObservedPhase { get; private set; }

        private bool active = true;
        private FlameEnvelope envelope;
        private BrackenV81Presentation presentation;
        private Vector2 scroll;

        private void Awake()
        {
            if (Aura != null) ConfigureFlames(Aura);
            Step(0f);
        }

        private void Update() { Step(Time.unscaledDeltaTime); }

        public void Step(float deltaTime)
        {
            if (!active) return;
            if (presentation == null)
                presentation = new BrackenV81Presentation(BrackenV81Presentation.SupportedMvid);
            Anger = Safe01(Anger);
            int state = Phase == PreviewPhase.Hunting ? 0 : Phase == PreviewPhase.Aggro ? 2 : 1;
            var input = new BrackenObservation(state, Anger * BrackenV81Presentation.DefaultSaturationAnger,
                state == 2, Phase == PreviewPhase.Dragging, InKillAnimation, Dead);
            var frame = presentation.Evaluate(input, !UsePhaseApproximation, deltaTime);
            ObservedPhase = frame.Phase.ToString();
            VisibleAuraLevel = frame.AuraLevel;
            if (frame.StopEffects)
            {
                if (!EffectsStopped) StopEffects();
                EffectsStopped = true;
                return;
            }
            EffectsStopped = false;
            if (frame.PlayAggroVoice)
            {
                VoiceTriggerCount++;
                if (VoiceSource != null && VoiceSource.clip != null)
                {
                    VoiceSource.Stop();
                    VoiceSource.Play();
                }
            }
            float visualAnger = Safe01((frame.AuraLevel - BrackenV81Presentation.BaselineAura)
                / (1f - BrackenV81Presentation.BaselineAura));
            ApplyAuraLevel(visualAnger);
            if (Aura != null && !Aura.isPlaying) Aura.Play(true);
        }

        public static void ConfigureFlames(ParticleSystem aura)
        {
            aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = aura.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.85f);
            main.maxParticles = 384;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.useUnscaledTime = true;
            aura.useAutoRandomSeed = false;
            aura.randomSeed = 42;

            // Emit around the whole body, rather than only from the feet.
            var shape = aura.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            shape.radiusThickness = 0f;
            shape.position = new Vector3(0f, 1.05f, 0f);
            shape.rotation = Vector3.zero;
            shape.scale = new Vector3(0.52f, 1.02f, 0.52f);

            var velocity = aura.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = 1.2f;

            var noise = aura.noise;
            noise.enabled = true;
            noise.strength = 0.28f;
            noise.frequency = 1.5f;
            noise.scrollSpeed = 0.7f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            var fade = new Gradient();
            fade.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.2f, 0.55f, 1f), 0f),
                    new GradientColorKey(new Color(0.65f, 0.9f, 1f), 0.3f),
                    new GradientColorKey(new Color(0.1f, 0.4f, 1f), 1f) },
                new[] {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.7f, 0.55f), new GradientAlphaKey(0f, 1f) });
            var color = aura.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(fade);
            var size = aura.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.2f, 1f),
                new Keyframe(0.65f, 0.6f), new Keyframe(1f, 0f)));
            var renderer = aura.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.cameraVelocityScale = 0f;
            renderer.lengthScale = 1.8f;
            renderer.velocityScale = 0.12f;
        }

        // Static asset/render inspection keeps the established direct anger control.
        public void ApplyAura() { ApplyAuraLevel(Safe01(Anger)); }

        private void ApplyAuraLevel(float anger)
        {
            if (Aura == null) return;
            float strength = float.IsNaN(AuraStrength) || float.IsInfinity(AuraStrength)
                ? 1f : Mathf.Clamp(AuraStrength, 0f, 3f);
            if (envelope == null && active) envelope = FlameEnvelope.Create(Aura.transform);
            if (envelope != null)
            {
                envelope.gameObject.SetActive(active);
                envelope.SetStrength(anger, strength);
            }
            var main = Aura.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.05f, 0.35f, anger), Mathf.Lerp(0.15f, 1.0f, anger));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.035f, 0.1f, anger), Mathf.Lerp(0.08f, 0.26f, anger));
            main.startColor = new Color(0.65f, 0.85f, 1f, Mathf.Clamp01(Mathf.Lerp(0.25f, 0.7f, anger) * strength));
            var emission = Aura.emission;
            emission.rateOverTime = active ? Mathf.Lerp(18f, 220f, anger * anger) * strength : 0f;
        }

        private void StopEffects()
        {
            if (envelope != null) envelope.gameObject.SetActive(false);
            if (Aura != null) Aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (VoiceSource != null) VoiceSource.Stop();
        }

        public void EndPreview()
        {
            active = false;
            presentation?.Reset();
            StopEffects();
            EffectsStopped = true;
            VisibleAuraLevel = 0f;
        }

        public void SimulateDeath() { Dead = true; Step(0f); }

        public void ResetPreview()
        {
            EndPreview();
            Anger = 0f;
            Phase = PreviewPhase.Hunting;
            InKillAnimation = Dead = UsePhaseApproximation = false;
            VoiceTriggerCount = 0;
            active = true;
            Step(0f);
        }

        private void OnDisable() { EndPreview(); }
        private void OnDestroy()
        {
            if (envelope == null) return;
            if (Application.isPlaying) Destroy(envelope.gameObject);
            else DestroyImmediate(envelope.gameObject);
        }

        private static float Safe01(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 330f, Mathf.Min(520f, Screen.height - 32f)), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("GojoBracken - connected cosmetic study");
            GUILayout.Label("Simulated inputs; no game or Bracken is running.");
            GUILayout.Label("Authoring materials; game integration is pending.");
            bool previousEnabled = GUI.enabled;
            GUI.enabled = active && !Dead;
            if (GUILayout.Button("Hunting")) Phase = PreviewPhase.Hunting;
            if (GUILayout.Button("Retreat")) Phase = PreviewPhase.Retreat;
            if (GUILayout.Button("Aggro")) Phase = PreviewPhase.Aggro;
            if (GUILayout.Button("Corpse dragging")) Phase = PreviewPhase.Dragging;
            GUILayout.Label("Observed phase: " + ObservedPhase);
            GUILayout.Label("Simulated anger: " + Mathf.RoundToInt(Anger * 100f) + "%");
            Anger = GUILayout.HorizontalSlider(Anger, 0f, 1f);
            UsePhaseApproximation = GUILayout.Toggle(UsePhaseApproximation, "Remote simulation: phase approximation");
            InKillAnimation = GUILayout.Toggle(InKillAnimation, "Kill animation overlay");
            GUILayout.Label("Aura strength: " + AuraStrength.ToString("0.0"));
            AuraStrength = GUILayout.HorizontalSlider(AuraStrength, 0f, 3f);
            GUILayout.Label("Aggro voice triggers: " + VoiceTriggerCount);
            GUILayout.Label(VoiceSource != null && VoiceSource.clip != null ? "Local voice clip assigned." : "No voice clip assigned.");
            if (GUILayout.Button("Simulate death")) SimulateDeath();
            if (GUILayout.Button("End preview")) EndPreview();
            GUI.enabled = previousEnabled;
            if (GUILayout.Button("Reset preview")) ResetPreview();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
