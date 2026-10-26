using System;
using System.Reflection;

namespace GojoBracken
{
    // Raw observations only. State indices and flags are not translated into phases here.
    internal readonly struct BrackenObservation
    {
        internal readonly int StateIndex;
        internal readonly float Anger;
        internal readonly bool AngerMode;
        internal readonly bool CarryingBody;
        internal readonly bool InKillAnimation;
        internal readonly bool Dead;

        internal BrackenObservation(int stateIndex, float anger, bool angerMode,
            bool carryingBody, bool inKillAnimation, bool dead)
        {
            StateIndex = stateIndex;
            Anger = anger;
            AngerMode = angerMode;
            CarryingBody = carryingBody;
            InKillAnimation = inKillAnimation;
            Dead = dead;
        }
    }

    // Bind once per runtime type; read multiple instances without retaining enemy references.
    internal sealed class BrackenStateReader
    {
        private readonly Type type;
        private readonly FieldInfo state;
        private readonly FieldInfo anger;
        private readonly FieldInfo angerMode;
        private readonly FieldInfo carrying;
        private readonly FieldInfo killing;
        private readonly FieldInfo dead;
        private readonly string bindingError;

        internal BrackenStateReader(Type type)
        {
            this.type = type;
            state = Bind(type, "currentBehaviourStateIndex", typeof(int));
            anger = Bind(type, "angerMeter", typeof(float));
            angerMode = Bind(type, "isInAngerMode", typeof(bool));
            carrying = Bind(type, "carryingPlayerBody", typeof(bool));
            killing = Bind(type, "inKillAnimation", typeof(bool));
            dead = Bind(type, "isEnemyDead", typeof(bool));
            if (state == null || anger == null || angerMode == null || carrying == null || killing == null || dead == null)
                bindingError = "Bracken observation fields are missing or have unexpected types.";
        }

        internal bool TryRead(object enemy, out BrackenObservation observation, out string error)
        {
            observation = default;
            error = bindingError;
            if (error != null) return false;
            if (enemy == null || !type.IsInstanceOfType(enemy))
            {
                error = "Enemy is null or does not match the bound Bracken type.";
                return false;
            }
            try
            {
                // Preserve the raw value. Its authoritative range has not been established.
                float localAnger = (float)anger.GetValue(enemy);
                if (float.IsNaN(localAnger) || float.IsInfinity(localAnger))
                {
                    error = "Bracken anger is not finite.";
                    return false;
                }
                observation = new BrackenObservation((int)state.GetValue(enemy), localAnger,
                    (bool)angerMode.GetValue(enemy), (bool)carrying.GetValue(enemy),
                    (bool)killing.GetValue(enemy), (bool)dead.GetValue(enemy));
                return true;
            }
            catch (Exception exception)
            {
                error = "Bracken observation unavailable: " + exception.GetType().Name;
                return false;
            }
        }

        private static FieldInfo Bind(Type type, string name, Type expected)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.FieldType == expected ? field : null;
            }
            return null;
        }
    }
}
