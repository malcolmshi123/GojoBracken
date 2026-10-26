using System;
using UnityEngine;
public sealed class RigReportFixtureAI : MonoBehaviour
{
    public float angerMeter = 0.25f;
    public RigReportFixtureState[] enemyBehaviourStates =
    {
        new RigReportFixtureState { name = "FixtureHunt" },
        new RigReportFixtureState { name = "FixtureAggro" }
    };
}
[Serializable]
public sealed class RigReportFixtureState { public string name; }

public sealed class RigReportEmptyAI : MonoBehaviour { }
