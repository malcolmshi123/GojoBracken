using System;

namespace GojoBracken
{
    internal enum BrackenPhase { Unknown, Hunting, Retreating, Aggro, CorpseDragging, Killing, Dead }

    internal readonly struct BrackenCosmeticFrame
    {
        internal readonly BrackenPhase Phase;
        internal readonly float AuraLevel;
        internal readonly bool PlayAggroVoice;
        internal readonly bool StopEffects;
        internal readonly bool UsesPhaseApproximation;

        internal BrackenCosmeticFrame(BrackenPhase phase, float auraLevel, bool playVoice, bool stop, bool approximate)
        {
            Phase = phase;
            AuraLevel = auraLevel;
            PlayAggroVoice = playVoice;
            StopEffects = stop;
            UsesPhaseApproximation = approximate;
        }
    }

    // Presentation only; no enemy references, field writes, AI calls, or network messages.
    internal sealed class BrackenV81Presentation
    {
        internal static readonly Guid SupportedMvid = new Guid("aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b");
        internal const float BaselineAura = 0.15f;
        // A tunable cosmetic saturation point, NOT a hard upper bound in the game.
        internal const float DefaultSaturationAnger = 12f;
        private readonly float saturationAnger;
        private readonly float response;
        private bool wasAggro;
        private float aura = BaselineAura;

        internal BrackenV81Presentation(Guid assemblyMvid,
            float saturationAnger = DefaultSaturationAnger, float response = 8f)
        {
            if (assemblyMvid != SupportedMvid)
                throw new ArgumentException("State mapping is verified only for the inspected v81 assembly.", nameof(assemblyMvid));
            if (!Finite(saturationAnger) || saturationAnger <= 0f)
                throw new ArgumentOutOfRangeException(nameof(saturationAnger));
            if (!Finite(response) || response <= 0f)
                throw new ArgumentOutOfRangeException(nameof(response));
            this.saturationAnger = saturationAnger;
            this.response = response;
        }

        internal BrackenCosmeticFrame Evaluate(BrackenObservation observation, bool useDetailedAnger, float deltaTime)
        {
            if (observation.Dead)
            {
                Reset();
                return new BrackenCosmeticFrame(BrackenPhase.Dead, 0f, false, true, !useDetailedAnger);
            }
            bool aggro = observation.StateIndex == 2;
            bool playVoice = aggro && !wasAggro && !observation.InKillAnimation && !observation.CarryingBody;
            // Track the real state edge; ending an overlay must not retrigger the aggro line.
            wasAggro = aggro;
            BrackenPhase phase = observation.InKillAnimation ? BrackenPhase.Killing
                : observation.CarryingBody ? BrackenPhase.CorpseDragging
                : observation.StateIndex == 0 ? BrackenPhase.Hunting
                : observation.StateIndex == 1 ? BrackenPhase.Retreating
                : aggro ? BrackenPhase.Aggro : BrackenPhase.Unknown;
            float anger = useDetailedAnger
                ? (Finite(observation.Anger) ? Clamp01(observation.Anger / saturationAnger) : 0f)
                : (aggro ? 1f : 0f);
            float target = BaselineAura + (1f - BaselineAura) * anger;
            float dt = Finite(deltaTime) ? Math.Max(0f, deltaTime) : 0f;
            float blend = (float)(1d - Math.Exp(-(double)response * dt));
            aura = Math.Max(BaselineAura, aura);
            aura = Math.Max(BaselineAura, Clamp01(aura + (target - aura) * blend));
            return new BrackenCosmeticFrame(phase, aura, playVoice, false, !useDetailedAnger);
        }

        internal void Reset()
        {
            wasAggro = false;
            aura = 0f;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static float Clamp01(float value) { return Math.Max(0f, Math.Min(1f, value)); }
    }
}
