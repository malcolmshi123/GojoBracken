using UnityEngine;

namespace GojoBracken.Authoring
{
    // Editor study scene only. Never attached to the game's enemies or release prefabs.
    public sealed class AuraStudyController : MonoBehaviour
    {
        public enum PreviewPhase { Hunting, Retreat, Aggro, Dragging }
        public ParticleSystem Aura;
        public AudioSource VoiceSource;
        [Range(0f, 1f)] public float Anger;
        public PreviewPhase Phase;

        private PreviewPhase previousPhase;
        private bool active = true;
        private int voiceTriggers;
        private FlameEnvelope envelope;

        private void Awake()
        {
            previousPhase = Phase;
            if (Aura != null) ConfigureFlames(Aura);
            ApplyAura();
            if (Aura != null) Aura.Play(true);
        }

        private void Update()
        {
            if (!active) return;
            Anger = Mathf.Clamp01(Anger);
            if (Phase != previousPhase && Phase == PreviewPhase.Aggro)
            {
                voiceTriggers++;
                if (VoiceSource != null && VoiceSource.clip != null)
                {
                    VoiceSource.Stop();
                    VoiceSource.Play();
                }
            }
            previousPhase = Phase;
            ApplyAura();
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

        public void ApplyAura()
        {
            if (Aura == null) return;
            float anger = Mathf.Clamp01(Anger);
            if (envelope == null && active) envelope = FlameEnvelope.Create(Aura.transform);
            if (envelope != null) { envelope.gameObject.SetActive(active); envelope.SetStrength(anger); }
            var main = Aura.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.05f, 0.35f, anger), Mathf.Lerp(0.15f, 1.0f, anger));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.035f, 0.1f, anger), Mathf.Lerp(0.08f, 0.26f, anger));
            main.startColor = new Color(0.65f, 0.85f, 1f, Mathf.Lerp(0.25f, 0.7f, anger));
            var emission = Aura.emission;
            emission.rateOverTime = active ? Mathf.Lerp(18f, 220f, anger * anger) : 0f;
        }

        private void StopPreview()
        {
            active = false;
            if (envelope != null) envelope.gameObject.SetActive(false);
            if (Aura != null) Aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (VoiceSource != null) VoiceSource.Stop();
        }

        private void ResetPreview()
        {
            StopPreview();
            Anger = 0f;
            Phase = previousPhase = PreviewPhase.Hunting;
            voiceTriggers = 0;
            active = true;
            ApplyAura();
            if (Aura != null) Aura.Play(true);
        }

        private void OnDisable() { StopPreview(); }
        private void OnDestroy()
        {
            if (envelope == null) return;
            if (Application.isPlaying) Destroy(envelope.gameObject);
            else DestroyImmediate(envelope.gameObject);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 300f, 330f), GUI.skin.box);
            GUILayout.Label("GojoBracken - native aura study");
            GUILayout.Label("Stand-in only. No game or Bracken is running.");
            GUILayout.Label("Authoring shader; game materials are pending.");
            bool previousEnabled = GUI.enabled;
            GUI.enabled = active;
            if (GUILayout.Button("Hunting")) Phase = PreviewPhase.Hunting;
            if (GUILayout.Button("Retreat")) Phase = PreviewPhase.Retreat;
            if (GUILayout.Button("Aggro")) Phase = PreviewPhase.Aggro;
            if (GUILayout.Button("Corpse dragging")) Phase = PreviewPhase.Dragging;
            GUILayout.Label("Simulated anger: " + Mathf.RoundToInt(Anger * 100f) + "%");
            Anger = GUILayout.HorizontalSlider(Anger, 0f, 1f);
            GUILayout.Label("Aggro voice triggers: " + voiceTriggers);
            GUILayout.Label(VoiceSource != null && VoiceSource.clip != null
                ? "Voice clip assigned in Inspector."
                : "No voice clip assigned.");
            if (GUILayout.Button("End preview")) StopPreview();
            GUI.enabled = previousEnabled;
            if (GUILayout.Button("Reset preview")) ResetPreview();
            GUILayout.EndArea();
        }
    }
}
