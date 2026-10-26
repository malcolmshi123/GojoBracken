using System;
using GojoBracken;

internal static class PresentationChecks
{
    private static BrackenObservation O(int state, float anger = 0f, bool carry = false, bool kill = false, bool dead = false, bool flag = false)
    { return new BrackenObservation(state, anger, flag, carry, kill, dead); }

    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); }

    internal static void Run()
    {
        var id = BrackenV81Presentation.SupportedMvid;
        var p = new BrackenV81Presentation(id);
        Check(p.Evaluate(O(0), true, 0f).Phase == BrackenPhase.Hunting, "hunting");
        Check(p.Evaluate(O(1), true, 0f).Phase == BrackenPhase.Retreating, "retreating");
        var entry = p.Evaluate(O(2, flag: false), true, 0f);
        Check(entry.Phase == BrackenPhase.Aggro && entry.PlayAggroVoice, "state edge works before local anger flag catches up");
        Check(!p.Evaluate(O(2, 8), true, .1f).PlayAggroVoice, "no per-frame retrigger");
        p.Evaluate(O(1, flag: true), true, .1f);
        Check(p.Evaluate(O(2), true, .1f).PlayAggroVoice, "reentry");
        var drag = p.Evaluate(O(1, 6, carry: true), true, 1f);
        Check(drag.Phase == BrackenPhase.CorpseDragging && !drag.PlayAggroVoice && drag.AuraLevel > .15f, "dragging aura without extra audio");
        p.Evaluate(O(2, 6, kill: true), true, .1f);
        Check(!p.Evaluate(O(2, 6), true, .1f).PlayAggroVoice, "kill overlay exit is not a new aggro edge");
        Check(p.Evaluate(O(2, dead: true), true, .1f).StopEffects, "death stops immediately");
        p.Reset();
        Check(p.Evaluate(O(0), true, 0f).AuraLevel == .15f, "faint baseline after reset");
        var smooth = new BrackenV81Presentation(id);
        float partial = smooth.Evaluate(O(0, 12), true, .01f).AuraLevel;
        Check(partial > .15f && partial < 1f, "smooth rise");
        Check(smooth.Evaluate(O(1, 1000), true, 100f).AuraLevel == 1f, "bounded maximum in retreat");
        Check(smooth.Evaluate(O(0, -4), true, 100f).AuraLevel == .15f, "negative countdown cannot underflow baseline");
        var remote = new BrackenV81Presentation(id);
        Check(remote.Evaluate(O(0, 999), false, 100).AuraLevel == .15f, "remote nonaggro approximation");
        var strong = remote.Evaluate(O(2, -999), false, 100);
        Check(strong.UsesPhaseApproximation && strong.AuraLevel == 1f, "remote aggro approximation is explicit");
        var other = new BrackenV81Presentation(id);
        Check(other.Evaluate(O(2), true, .1f).PlayAggroVoice, "independent Brackens");
        Check(!other.Evaluate(O(99, 10), true, .1f).PlayAggroVoice, "unknown states silent");
        float level = other.Evaluate(O(0, 12), true, float.NaN).AuraLevel;
        Check(!float.IsNaN(level) && level >= .15f && level <= 1, "invalid delta safe");
        bool rejected = false;
        try { new BrackenV81Presentation(Guid.Empty); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "uninspected assembly rejected");
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            rejected = false;
            try { new BrackenV81Presentation(id, invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "invalid saturation rejected");
        }
        Console.WriteLine("v81 presentation checks passed: phases, overlays, voice edges, bounds, smoothing, remote approximation, cleanup, isolation, version guard.");
    }
}
