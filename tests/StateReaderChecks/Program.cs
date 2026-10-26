using System;
using GojoBracken;

internal static class StateReaderChecks
{
    private class Enemy
    {
        public int currentBehaviourStateIndex;
        public bool isEnemyDead;
    }
    private sealed class Bracken : Enemy
    {
        public float angerMeter;
        public bool isInAngerMode;
        public bool carryingPlayerBody;
        public bool inKillAnimation;
    }
    private sealed class WrongSchema
    {
        public double angerMeter;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        var a = new Bracken { currentBehaviourStateIndex = 17, angerMeter = 24.5f,
            isInAngerMode = true, carryingPlayerBody = true, inKillAnimation = false };
        var b = new Bracken { currentBehaviourStateIndex = -2, angerMeter = -0.5f,
            isEnemyDead = true, inKillAnimation = true };
        var reader = new BrackenStateReader(typeof(Bracken));
        Require(reader.TryRead(a, out var first, out var error) && error == null, "read inherited fields");
        Require(first.StateIndex == 17 && first.Anger == 24.5f && first.AngerMode && first.CarryingBody
            && !first.InKillAnimation && !first.Dead, "preserve raw values and overlapping flags");
        Require(reader.TryRead(b, out var second, out _) && second.Dead && second.InKillAnimation
            && second.StateIndex == -2 && second.Anger == -0.5f && !second.AngerMode, "independent instances, no assumed phase/range");
        Require(a.angerMeter == 24.5f && a.currentBehaviourStateIndex == 17 && !a.isEnemyDead, "does not write enemy fields");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            b.angerMeter = invalid;
            Require(!reader.TryRead(b, out _, out error) && error.Contains("finite"), "reject nonfinite inputs");
        }
        Require(!reader.TryRead(null, out _, out _), "null input");
        Require(!reader.TryRead(new object(), out _, out _), "wrong input type");
        Require(!new BrackenStateReader(typeof(WrongSchema)).TryRead(new WrongSchema(), out _, out _), "schema mismatch");
        Require(!new BrackenStateReader(null).TryRead(a, out _, out _), "missing runtime type");
        PresentationChecks.Run();
        Console.WriteLine("Bracken state reader checks passed: inherited fields, raw values, independent instances, no mutation, invalid inputs, schema drift.");
    }
}
