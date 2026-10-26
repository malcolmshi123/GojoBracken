using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace GojoBracken
{
    // Development-only observer. Never writes to an enemy or sends network messages.
    internal sealed class RuntimeDiagnostics
    {
        private readonly ManualLogSource logger;
        private readonly Type flowermanType;
        private readonly Dictionary<int, string> previousSnapshots = new Dictionary<int, string>();
        private string previousPipeline;
        private float nextSampleTime;
        private bool failed;

        internal RuntimeDiagnostics(ManualLogSource logger)
        {
            this.logger = logger;
            flowermanType = AccessTools.TypeByName("FlowermanAI");
            logger.LogInfo($"[Diagnostics] Unity={Application.unityVersion}; applicationVersion={Application.version}");
            if (flowermanType == null)
            {
                logger.LogWarning("[Diagnostics] FlowermanAI type was not found; Bracken sampling is unavailable.");
            }
            else
            {
                logger.LogInfo($"[Diagnostics] Game assembly={flowermanType.Assembly.GetName().Name}; MVID={flowermanType.Module.ModuleVersionId}");
            }
        }

        internal void Update(float now)
        {
            if (failed || now < nextSampleTime)
            {
                return;
            }

            nextSampleTime = now + 0.5f;
            try
            {
                LogPipeline();
                if (flowermanType != null)
                {
                    LogBrackens();
                }
            }
            catch (Exception exception)
            {
                // A diagnostic failure must not interrupt the game's update loop.
                failed = true;
                logger.LogWarning($"[Diagnostics] Sampling stopped after {exception.GetType().Name}: {exception.Message}");
            }
        }

        private void LogPipeline()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            string description = pipeline == null
                ? "<null: built-in pipeline or render pipeline not initialized>"
                : $"{pipeline.GetType().FullName}; assemblyVersion={pipeline.GetType().Assembly.GetName().Version}";

            if (description != previousPipeline)
            {
                logger.LogInfo($"[Diagnostics] Render pipeline={description}");
                previousPipeline = description;
            }
        }

        private void LogBrackens()
        {
            var activeIds = new HashSet<int>();
            // Unity excludes prefab assets and inactive objects from this search.
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType(flowermanType))
            {
                int id = enemy.GetInstanceID();
                activeIds.Add(id);
                string snapshot = CreateSnapshot(enemy);
                if (!previousSnapshots.TryGetValue(id, out var previous) || previous != snapshot)
                {
                    logger.LogInfo($"[Diagnostics] Bracken localId={id}; {snapshot}");
                    previousSnapshots[id] = snapshot;
                }
            }

            var removedIds = new List<int>();
            foreach (int id in previousSnapshots.Keys)
            {
                if (!activeIds.Contains(id))
                {
                    logger.LogInfo($"[Diagnostics] Bracken localId={id} no longer active.");
                    removedIds.Add(id);
                }
            }

            foreach (int id in removedIds)
            {
                previousSnapshots.Remove(id);
            }
        }

        private static string CreateSnapshot(object enemy)
        {
            return $"networkId={Format(ReadMember(enemy, "NetworkObjectId"))}; " +
                $"owner={Format(ReadMember(enemy, "IsOwner"))}; " +
                $"host={Format(ReadMember(enemy, "IsHost"))}; " +
                $"server={Format(ReadMember(enemy, "IsServer"))}; " +
                $"state={Format(ReadMember(enemy, "currentBehaviourStateIndex"))}; " +
                $"stateNames={DescribeStates(enemy)}; " +
                $"anger={Format(ReadMember(enemy, "angerMeter"))}; " +
                $"carryingBody={Format(ReadMember(enemy, "carryingPlayerBody"))}; " +
                $"killAnimation={Format(ReadMember(enemy, "inKillAnimation"))}; " +
                $"dead={Format(ReadMember(enemy, "isEnemyDead"))}";
        }

        private static string DescribeStates(object enemy)
        {
            var states = ReadMember(enemy, "enemyBehaviourStates") as IEnumerable;
            if (states == null)
            {
                return "<unavailable>";
            }

            var descriptions = new List<string>();
            int index = 0;
            foreach (var state in states)
            {
                descriptions.Add($"{index}:{Format(ReadMember(state, "name"))}");
                index++;
            }
            return "[" + string.Join(", ", descriptions) + "]";
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            try
            {
                FieldInfo field = AccessTools.Field(target.GetType(), name);
                if (field != null)
                {
                    return field.GetValue(target);
                }

                PropertyInfo property = AccessTools.Property(target.GetType(), name);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(target, null);
                }
            }
            catch (Exception exception)
            {
                return $"<unavailable:{exception.GetType().Name}>";
            }

            return null;
        }

        private static string Format(object value)
        {
            if (value == null)
            {
                return "<unavailable>";
            }

            if (value is float number)
            {
                return number.ToString("0.00", CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}

